using CommunityToolkit.Mvvm.ComponentModel;
using Karen.Util;
using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Karen.Services
{
    public partial class Server : ObservableObject
    {
        private readonly Settings Settings;
        private readonly VirtualConsole VirtualConsole;

        private Process? redis, server;

        private string workDir, tempDir, logsDir;

        [ObservableProperty]
        public partial bool CanRun { get; private set; }
        [ObservableProperty]
        public partial bool IsRunning { get; private set; }
        [ObservableProperty]
        public partial string Version { get; private set; } = "";

        public Server(Settings settings, VirtualConsole virtualConsole)
        {
            Settings = settings;
            VirtualConsole = virtualConsole;
            workDir = Path.Combine(AppContext.BaseDirectory, "lanraragi");
            tempDir = Path.Combine(workDir, "temp");
            logsDir = Path.Combine(workDir, "log");
            PInvoke.AllocConsole();
            PInvoke.SetConsoleOutputCP(65001u);
            PInvoke.SetConsoleTitle("LANraragi Log Console");
            // This is needed to ensure processes launched by Karen (Redis/Perl) don't spawn additional windows
            PInvoke.ShowWindow(PInvoke.GetConsoleWindow(), SHOW_WINDOW_CMD.SW_HIDE);

            // Try to load version name (and check if the runtime is actually ok)
            try
            {
                var procInfo = CreateProcInfo(workDir);
                procInfo.Arguments = "script\\get_version";
                procInfo.FileName = Path.Combine(workDir, "runtime", "bin", "perl.exe");
                procInfo.RedirectStandardOutput = true;

                using var verProc = Process.Start(procInfo);

                if (verProc != null)
                {
                    var output = verProc.StandardOutput.ReadToEnd().Trim();
                    verProc.WaitForExit();

                    if (verProc.ExitCode == 0)
                    {
                        Version = $"Version {output}";
                        CanRun = true;
                    }
                    else
                    {
                        VirtualConsole.AddLine(output);
                        Version = "An error occurred when testing the server runtime.\nPlease check the console";
                    }
                }
                else
                {
                    Version = "An unknown error occurred when testing the server runtime.";
                }
            }
            catch (Exception ex)
            {
                VirtualConsole.AddLine(ex.Message);
                Version = "Something went wrong with the server runtime.\nPlease check the console.";
            }
        }

        public async Task StartAsync()
        {
            if (string.IsNullOrEmpty(Settings.ContentFolder))
            {
                await WinUIUtils.ShowMessageDialog("LANraragi", "Please setup your Content Folder in the Settings before starting the server!", "OK");
                return;
            }

            try
            {
                // Ensure this exist, otherwise we are in trouble
                Directory.CreateDirectory(tempDir);
                Directory.CreateDirectory(logsDir);

                if (redis == null)
                {
                    var redisPid = Path.Combine(tempDir, "redis.pid");

                    redis = GetProcess(redisPid, "redis-server.exe");

                    if (redis == null)
                    {
                        var procInfo = CreateProcInfo(workDir);
                        // redis on windows has broken absolute paths to config files so define it as relative instead
                        procInfo.ArgumentList.Add("./runtime/redis/redis.conf");
                        procInfo.ArgumentList.Add("--dir");
                        procInfo.ArgumentList.Add(Settings.ContentFolder);
                        procInfo.ArgumentList.Add("--logfile");
                        procInfo.ArgumentList.Add(Path.Combine(logsDir, "redis.log"));

                        procInfo.FileName = Path.Combine(workDir, "runtime", "redis", "redis-server.exe");

                        redis = Process.Start(procInfo);

                        File.WriteAllText(redisPid, redis!.Id.ToString());
                    }
                }

                if (server == null)
                {
                    var serverPid = Path.Combine(tempDir, "server.pid");

                    server = GetProcess(serverPid, "perl.exe");

                    if (server == null)
                    {
                        var procInfo = CreateProcInfo(workDir);
                        procInfo.Environment["LRR_DATA_DIRECTORY"] = Settings.ContentFolder;
                        procInfo.Environment["LRR_THUMB_DIRECTORY"] = string.IsNullOrWhiteSpace(Settings.ThumbnailFolder) ? Path.Combine(Settings.ContentFolder, "thumb") : Settings.ThumbnailFolder;
                        procInfo.Environment["LRR_NETWORK"] = $"http://*:{Settings.NetworkPort}";

                        if (Settings.ForceDebugMode)
                            procInfo.Environment["LRR_FORCE_DEBUG"] = "1";

                        procInfo.ArgumentList.Add("script\\launcher.pl");
                        procInfo.ArgumentList.Add("-d");
                        procInfo.ArgumentList.Add("script\\lanraragi");

                        procInfo.RedirectStandardError = true;
                        procInfo.RedirectStandardOutput = true;

                        procInfo.FileName = Path.Combine(workDir, "runtime", "bin", "perl.exe");

                        server = new Process
                        {
                            StartInfo = procInfo
                        };
                        server.OutputDataReceived += (sender, e) =>
                        {
                            VirtualConsole.AddLine(e.Data);
                        };
                        server.ErrorDataReceived += (sender, e) =>
                        {
                            VirtualConsole.AddLine(e.Data);
                        };
                        server.Start();
                        server.BeginErrorReadLine();
                        server.BeginOutputReadLine();

                        File.WriteAllText(serverPid, server!.Id.ToString());
                    }
                }
                IsRunning = true;
            }
            catch (Exception e)
            {
                VirtualConsole.AddLine(e.ToString());
                await WinUIUtils.ShowMessageDialog("Unable to start server", e.Message, "OK");
                IsRunning = false;
            }

        }

        public async Task StopAsync()
        {
            var exception = await Task.Run(() =>
            {
                try
                {
                    if (server != null && !server.HasExited)
                    {
                        var toWait = new List<Process>();

                        foreach (var process in Process.GetProcessesByName("perl"))
                        {
                            if (process.MainModule!.FileName.Equals(server.MainModule!.FileName))
                                toWait.Add(process);
                            else
                                process.Dispose();
                        }
                        PInvoke.SetConsoleCtrlHandler(null, true);
                        PInvoke.GenerateConsoleCtrlEvent(0, 0);

                        foreach (var process in toWait)
                        {
                            process.WaitForExit(5000);
                            if (!process.HasExited)
                            {
                                try
                                {
                                    process.Kill();
                                }
                                catch { }
                            }
                            process.Dispose();
                        }

                        PInvoke.SetConsoleCtrlHandler(null, false);
                    }

                    if (redis != null && !redis.HasExited)
                    {
                        var procInfo = CreateProcInfo(workDir);

                        procInfo.FileName = Path.Combine(workDir, "runtime", "redis", "redis-cli.exe");
                        procInfo.Arguments = "shutdown";

                        using var tmp = Process.Start(procInfo);

                        tmp!.WaitForExit();
                        redis.WaitForExit();
                    }

                    redis?.Dispose();
                    server?.Dispose();
                    redis = null;
                    server = null;

                    return null;
                }
                catch (Exception e)
                {
                    VirtualConsole.AddLine(e.ToString());
                    return e;
                }
            });
            if (exception != null)
            {
                await WinUIUtils.ShowMessageDialog("Error while stopping server", exception.Message, "OK");
            }
            IsRunning = false;
        }

        private ProcessStartInfo CreateProcInfo(string lrrDirectory)
        {
            var procInfo = new ProcessStartInfo();

            procInfo.Environment["Path"] = $"{Path.Combine(lrrDirectory, "runtime", "bin")};{Path.Combine(lrrDirectory, "runtime", "redis")};%SystemRoot%\\system32;%SystemRoot%;%SystemRoot%\\System32\\Wbem;%SYSTEMROOT%\\System32\\WindowsPowerShell\\v1.0\\";
            procInfo.WorkingDirectory = lrrDirectory;

            return procInfo;
        }

        private Process? GetProcess(string pathToPid, string exe)
        {
            if (File.Exists(pathToPid) && int.TryParse(File.ReadAllText(pathToPid), out int pid))
            {
                try
                {
                    var proc = Process.GetProcessById(pid);
                    if (Path.GetFileName(proc.MainModule!.FileName).Equals(exe))
                        return proc;
                }
                catch { }
            }
            return null;
        }
    }
}
