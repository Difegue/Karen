using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;

namespace LANraragi.DistroInstaller
{
    public static class Program
    {

        private static readonly ApplicationDataContainer Settings = new DesktopBridge.Helpers().IsRunningAsUwp() ? ApplicationData.Current.LocalSettings : null;

        public static int Main(string[] args)
        {
            Console.Title = "LANraragi Installer";
            string distro = "lanraragi";

#if DEBUG
            Debugger.Launch();
#endif

            StringBuilder wsl = new StringBuilder("wsl.exe", 260);
            if (!Win32.PathFindOnPath(wsl, null))
            {
                Console.WriteLine("Windows Subsystem for Linux is not installed on this machine and is required by this program.");
                Console.WriteLine("Please install it by following the instructions in this link: https://docs.microsoft.com/en-us/windows/wsl/install");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                return 0;
            }

            var karen = Settings?.Values["Karen"] == null ? false : (bool)Settings.Values["Karen"];
            if (Settings != null)
                Settings.Values["Karen"] = false;

            if (Process.GetProcessesByName("Karen").Count() >= 1 && !karen)
            {
                Console.WriteLine("LANraragi for Windows is already running.\nClose it to access installer options.\n\n");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                return 0;
            }

            // If running in MSIX context, check version saved in settings against package version to see if we have to upgrade
            // Otherwise, just refer to the CLI argument
            bool needsUpgrade = new DesktopBridge.Helpers().IsRunningAsUwp() ? NeedsUpgradeMSIX() : NeedsUpgradeCLI(args);
            
            // Unused by the actual MSI setup, but might as well leave the functionality in
            bool needsUninstall = args.Length > 0 && args[0] == "-remove";

            if (!WslApi.WslIsDistributionRegistered(distro) || needsUpgrade)
            {
                if (needsUpgrade)
                {
                    Console.WriteLine("Upgrading distro...");
                    UnInstall(distro);
                }
                else
                {
                    Console.WriteLine("Installing distro...");
                }
                Install(distro, args);
            }
            else if (needsUninstall)
            {
                Console.WriteLine("Uninstalling distro...");
                UnInstall(distro);
            }
            else
            {
                Console.WriteLine(" == LANraragi WSL Distro Tool == ");
                Console.WriteLine(" r - Reinstall");
                Console.WriteLine(" u - Uninstall");
                Console.WriteLine(" c - Exit");
                var key = Console.ReadKey();
                switch (key.Key)
                {
                    case ConsoleKey.R:
                        UnInstall(distro);
                        Install(distro, args);
                        return 0;
                    case ConsoleKey.U:
                        UnInstall(distro);
                        return 0;
                    case ConsoleKey.C:
                        return 0;
                }
            }
            
            if (new DesktopBridge.Helpers().IsRunningAsUwp())
            {
                Package.Current.GetAppListEntriesAsync().GetAwaiter().GetResult()
                .First(app => app.AppUserModelId == Package.Current.Id.FamilyName + "!Karen").LaunchAsync().GetAwaiter().GetResult();
            }
            return 0;
        }

        private static bool NeedsUpgradeCLI(string[] args)
        {
            return args.Length > 0 && args[0] == "-upgrade";
        }

        private static bool NeedsUpgradeMSIX()
        {
            return Version.TryParse(Settings?.Values["Version"]?.ToString() ?? "", out var oldVersion) && oldVersion < GetVersion();
        }

        private static void Install(string distro, string[] args)
        {
            // Check for package.tar file first
            var packageFile = Path.Combine(Directory.GetCurrentDirectory(), "package.tar");
            if (args.Length > 0 && args[0] == "-upgrade")
            {
                packageFile = args[1];
            }
            
            if (!File.Exists(packageFile))
            {
                Console.WriteLine("package.tar not found. Please run this program from the LANraragi folder.");
                Console.WriteLine("(You are running this program from: " + Path.GetFullPath(Directory.GetCurrentDirectory()) + ")");
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                return;
            }

            // If Windows build number > 18362, just use wsl.exe --import which is more reliable
            var buildNumber = Environment.OSVersion.Version.Build;
            if (buildNumber >= 18362) // 1903
            {
                var arguments = $"--import {distro} \"Distro\" \"{packageFile}\"";
                Console.WriteLine("\nUsing WSL CLI: wsl.exe " + arguments);
                var wslProc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wsl.exe",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.Unicode,
                        CreateNoWindow = true,
                    }
                };
                // Write stdout of process to our own
                wslProc.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                wslProc.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

                wslProc.Start();
                wslProc.BeginOutputReadLine();
                wslProc.BeginErrorReadLine();
                wslProc.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
                
                if (wslProc.HasExited)
                    Console.WriteLine("Exit code of wsl.exe is " + wslProc.ExitCode);
                else
                    Console.WriteLine("wsl.exe did not exit within 5 minutes, moving on.");
            }
            else
            {
                // Use legacy WSL API
                Console.WriteLine("\nUsing WSL API");
                WslApi.WslRegisterDistribution(distro, "package.tar");
            }
            
            if (Settings != null)
                Settings.Values["Version"] = GetVersion().ToString();
        }
        
        private static void UnInstall(string distro)
        {
            var buildNumber = Environment.OSVersion.Version.Build;
            if (buildNumber >= 18362) // 1903
            {
                var arguments = $"--unregister {distro}";
                Console.WriteLine("\nUsing WSL CLI: wsl.exe " + arguments);
                var wslProc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wsl.exe",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.Unicode,
                        CreateNoWindow = true,
                    }
                };
                // Write stdout of process to our own
                wslProc.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                wslProc.ErrorDataReceived += (sender, e) => Console.WriteLine(e.Data);

                wslProc.Start();
                wslProc.BeginOutputReadLine();
                wslProc.BeginErrorReadLine();
                wslProc.WaitForExit((int)TimeSpan.FromMinutes(1).TotalMilliseconds);
            }
            else
            {
                Console.WriteLine("\nUsing WSL API");
                WslApi.WslUnregisterDistribution(distro);
            }
            Settings?.Values.Remove("Version");
        }

        private static Version GetVersion()
        {
            var version = new DesktopBridge.Helpers().IsRunningAsUwp() ? Package.Current.Id.Version : new PackageVersion();
            return new Version(version.Major, version.Minor, version.Build, version.Revision);
        }
    }
}
