using HideConsoleOnCloseManaged;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Karen.Interop
{
    /// <summary>
    /// Abstracts a WSL distro, allowing to start and stop a process within it.
    /// Also features a togglable console with the process' STDOUT.
    /// </summary>
    public class WslDistro
    {

        public AppStatus Status { get; private set; }
        public String Version { get; private set; }

        private IntPtr _lrrHandle;
        private Process _lrrProc;

        public WslDistro()
        {
            Status = CheckDistro() ? AppStatus.Stopped : AppStatus.NotInstalled;

            // Compute version only if the distro exists
            if (Status == AppStatus.Stopped)
                Version = GetVersion();
        }

        public void ShowConsole()
        {
            ShowWindow(GetConsoleWindow(), SW_SHOW);
            ShowWindow(GetConsoleWindow(), SW_RESTORE);
        }

        public void HideConsole()
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
        }

        private string GetWSLPath(string winPath)
        {
            string wslPath = "/mnt/" + Char.ToLowerInvariant(winPath[0]);
            return wslPath + winPath.Substring(1).Replace(":", "").Replace("\\", "/");
        }

        public bool? StartApp()
        {
            if (!Directory.Exists(Settings.Default.ContentFolder))
            {
                Version = "Content Folder doesn't exist!";
                return false;
            } else 
                Version = GetVersion(); //Show the version anew if the content folder is now set

            Status = AppStatus.Starting;

            // Spawn a new console 
            AllocConsole();

            // Hide it 
            HideConsole();
            HideConsoleOnClose.Enable();

            // Get its handles
            var stdIn = GetStdHandle(STD_INPUT_HANDLE);
            var stdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            var stdError = GetStdHandle(STD_ERROR_HANDLE);

            // Switch the distro to the specified WSL version
            // (Does nothing if we're already using the proper one)
#if WSL1_LEGACY
            SetWSLVersion(1);
#else
            SetWSLVersion(2);
#endif

            // Map the user's content folder to its WSL equivalent
            // This means lowercasing the drive letter, removing the : and replacing every \ by a /.
            string winPath = Settings.Default.ContentFolder;
            string thumbPath = Settings.Default.ThumbnailFolder;

            string contentFolder = GetWSLPath(winPath);
            string thumbnailFolder = string.IsNullOrWhiteSpace(thumbPath) ? contentFolder + "/thumb" : GetWSLPath(thumbPath);

            var wslCommands = new List<string>();

           
            var driveLetter = winPath.Split('\\')[0];
            if (!IsLocalDrive(driveLetter))
            {
                var mountpoint = "/mnt/" + Char.ToLowerInvariant(driveLetter.First());
                wslCommands.Add($"mkdir -p '{mountpoint}' && mount -t drvfs {driveLetter} '{mountpoint}'");
            }

            driveLetter = thumbPath.Split('\\')[0];
            if (!string.IsNullOrWhiteSpace(thumbPath) && !IsLocalDrive(driveLetter))
            {
                var mountpoint = "/mnt/" + Char.ToLowerInvariant(driveLetter.First());
                wslCommands.Add($"mkdir -p '{mountpoint}' && mount -t drvfs {driveLetter} '{mountpoint}'");
            }

            if (Settings.Default.ForceDebugMode)
            {
                wslCommands.Add($"export LRR_FORCE_DEBUG=1");
            }

            // The big bazooper. Export port, folders and start both redis and the server.
            wslCommands.Add($"export LRR_NETWORK=http://*:{Settings.Default.NetworkPort}");
            wslCommands.Add($"export LRR_DATA_DIRECTORY='{contentFolder}'");
            wslCommands.Add($"export LRR_THUMB_DIRECTORY='{thumbnailFolder}'");
            wslCommands.Add($"cd /home/koyomi/lanraragi && rm -f public/temp/server.pid");
            wslCommands.Add($"mkdir -p log && mkdir -p content && mkdir -p database && sysctl vm.overcommit_memory=1");
            wslCommands.Add($"redis-server /home/koyomi/lanraragi/tools/build/docker/redis.conf --dir '{contentFolder}/' --daemonize yes");
            wslCommands.Add($"perl ./script/launcher.pl -f ./script/lanraragi");

            // Concat all commands into one string we'll throw at WSL
            var command = string.Join(" && ", wslCommands);
            Console.WriteLine("Executing the following command on WSL: " + command);

            // Start process in WSL and hook up handles 
            // This will direct WSL output to the new console window, or to Visual Studio if running with the debugger attached.
            // See https://stackoverflow.com/questions/15604014/no-console-output-when-using-allocconsole-and-target-architecture-x86
            WslApi.WslLaunch(Properties.Resources.DISTRO_NAME, command, false, stdIn,stdOut,stdError, out _lrrHandle);

            // Get Process ID of the returned procHandle
            int lrrId = GetProcessId(_lrrHandle);
            _lrrProc = Process.GetProcessById(lrrId);

            // Check that the returned process is still alive
            if (_lrrProc != null && !_lrrProc.HasExited)
                Status = AppStatus.Started;

            return !_lrrProc?.HasExited;
        }

        public bool? StopApp()
        {  
            // Kill WSL Process
            if (_lrrProc != null && !_lrrProc.HasExited)
            {
                _lrrProc.Kill();

                // Ensure child unix processes are killed as well by killing the distro. This is only possible on 1809 and up.
                new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wslconfig.exe",
                        Arguments = "/terminate " + Properties.Resources.DISTRO_NAME,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                    }
                }.Start();
            }

            // No need to remove the attached console here in case the app is restarted later down
            Status = AppStatus.Stopped;
            return _lrrProc?.HasExited;
        }

        internal void Repair()
        {
            var pid = Process.GetCurrentProcess().Id;
            
            // Run a piece of script that closes us, runs distroinstaller twice and then restarts the application
            Process.Start("cmd.exe", $"/c \"echo Repairing... & taskkill /f /pid {pid} & .\\DistroInstaller.exe -remove & .\\DistroInstaller.exe & echo Relaunching... & .\\Karen.exe\"");
        }

        #region Various ProcessStartInfo calls to WSL
        public bool CheckDistro()
        {
            // return WslApi.WslIsDistributionRegistered(Properties.Resources.DISTRO_NAME);
            // ^ This WSL API call is currently broken from WPF applications.
            // See https://stackoverflow.com/questions/55681500/why-did-wslapi-suddenly-stop-working-in-wpf-applications.
            // Stuck doing a manual wsl.exe call for now...
            
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Environment.SystemDirectory + "\\wslconfig.exe",
                    Arguments = "/l",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            try
            {
                proc.Start();
                proc.WaitForExit(3000);

                if (proc.HasExited)
                {
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string line = proc.StandardOutput.ReadLine();
                        if (line.Replace("\0", "").Contains(Properties.Resources.DISTRO_NAME))
                        {
                            return true;
                        }
                    }
                    return false;
                } 
                else
                {
                    proc.Kill();
                    throw new Exception("Timed out getting WSL info. \n(Try a reboot before reinstalling)");
                }
            }
            catch (Exception e)
            {
                //WSL might not be enabled ?
                Version = e.Message;
                return false;
            }
        }

        private bool SetWSLVersion(int wslVer)
        {
            Console.WriteLine($"Making sure the WSL distro is using specified WSL mode: {wslVer}.");
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Environment.SystemDirectory + "\\wsl.exe",
                    Arguments = $"--set-version {Properties.Resources.DISTRO_NAME} {wslVer}",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.Unicode,
                    StandardErrorEncoding = Encoding.Unicode,
                    CreateNoWindow = true,
                }
            };
            try
            {
                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                {
                    Console.WriteLine(proc.StandardOutput.ReadLine());
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while setting WSL version: {e}");
                return false;
            }
        }

        private string GetVersion()
        {
            // Use the included get-version script in LRR to get the version of the distro
            // wsl.exe -d lanraragi --exec ash -c "cd /home/koyomi/lanraragi && perl script/get_version"
            string oneLiner = "ash -c \"cd /home/koyomi/lanraragi && perl script/get_version\" ";

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Environment.SystemDirectory + "\\wsl.exe",
                    Arguments = $"-d {Properties.Resources.DISTRO_NAME} --exec {oneLiner}",
                    UseShellExecute = false,
                    StandardOutputEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                }
            };
            try
            {
                proc.Start();

                // Read the output of the command
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                
                if (proc.ExitCode == 0 && output != "")
                {
                    return "Version " + output;
                }
                else
                {
                    // Distro exists but the one-liner returns nothing
                    Status = AppStatus.NotInstalled;
                    return output;
                }
            }
            catch (Exception e)
            {
                // Distro exists but the one-liner fails ?
                Status = AppStatus.NotInstalled;
                return e.Message;
            }
        }

        #endregion

        #region Your friendly neighborhood P/Invokes for console host wizardry

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        static extern int GetProcessId(IntPtr handle);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("User32")]
        static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("mpr.dll")]
        static extern uint WNetGetConnection(string lpLocalName, StringBuilder lpRemoteName, ref int lpnLength);

        internal static bool IsLocalDrive(String driveName)
        {
            bool isLocal = true;  // assume local until disproved

            // strip trailing backslashes from driveName
            driveName = driveName.Substring(0, 2);

            int length = 256; // to be on safe side 
            StringBuilder networkShare = new StringBuilder(length);
            uint status = WNetGetConnection(driveName, networkShare, ref length);

            // does a network share exist for this drive?
            if (networkShare.Length != 0)
            {
                // now networkShare contains a UNC path in format \\MachineName\ShareName
                // retrieve the MachineName portion
                String shareName = networkShare.ToString();
                string[] splitShares = shareName.Split('\\');
                // the 3rd array element now contains the machine name
                if (Environment.MachineName == splitShares[2])
                    isLocal = true;
                else
                    isLocal = false;
            }

            return isLocal;
        }

        private const int STD_INPUT_HANDLE = -10;
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        #endregion
    }
}
