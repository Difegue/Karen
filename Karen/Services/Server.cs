using HideConsoleOnCloseManaged;
using Karen.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Karen.Services
{
    public class Server
    {
        private readonly Settings Settings;

        private Process? redis, server;

        private string workDir, tempDir, logsDir;

        public bool CanRun { get; private set; }
        public bool IsRunning { get; private set; }
        public string Version { get; private set; } = "";

        public Server(Settings settings)
        {
            Settings = settings;
            workDir = Path.Combine(AppContext.BaseDirectory, "lanraragi");
            tempDir = Path.Combine(workDir, "temp");
            logsDir = Path.Combine(workDir, "log");
            PInvoke.AllocConsole();
            PInvoke.SetConsoleOutputCP(65001u);
            PInvoke.SetConsoleTitle("LANraragi Log Console");
            HideConsole();
            HideConsoleOnClose.EnableForWindow(PInvoke.GetConsoleWindow());

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
                    var output = verProc.StandardOutput.ReadToEnd();
                    verProc.WaitForExit();

                    if (verProc.ExitCode == 0)
                    {
                        Version = $"Version {output}";
                        CanRun = true;
                    }
                    else
                    {
                        Console.WriteLine(output);
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
                Console.WriteLine(ex.Message);
                Version = "Something went wrong with the server runtime.\nPlease check the console.";
            }
        }

        public void Start()
        {
            if (string.IsNullOrEmpty(Settings.ContentFolder))
            {
                PopupUtils.ShowMessageDialog("LANraragi", "Please setup your Content Folder in the Settings before starting the server!", "OK");
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

                        procInfo.FileName = Path.Combine(workDir, "runtime", "bin", "perl.exe");

                        server = Process.Start(procInfo);

                        File.WriteAllText(serverPid, server!.Id.ToString());
                    }
                }
                IsRunning = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                PopupUtils.ShowMessageDialog("Unable to start server", e.Message, "OK");
                IsRunning = false;
            }

        }

        public Task Stop()
        {
            IsRunning = false;
            return Task.Run(() =>
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
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    PopupUtils.ShowMessageDialog("Error while stopping server", e.Message, "OK");
                }
            });
        }

        public static void ShowConsole()
        {
            PInvoke.ShowWindow(PInvoke.GetConsoleWindow(), SHOW_WINDOW_CMD.SW_SHOW);
            PInvoke.ShowWindow(PInvoke.GetConsoleWindow(), SHOW_WINDOW_CMD.SW_RESTORE);
        }

        public static void HideConsole()
        {
            PInvoke.ShowWindow(PInvoke.GetConsoleWindow(), SHOW_WINDOW_CMD.SW_HIDE);
        }

        private ProcessStartInfo CreateProcInfo(string lrrDirectory)
        {
            var procInfo = new ProcessStartInfo();

            procInfo.Environment["Path"] = $"{Path.Combine(lrrDirectory, "runtime", "bin")};{Path.Combine(lrrDirectory, "runtime", "redis")};{Environment.GetEnvironmentVariable("Path")}";
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
