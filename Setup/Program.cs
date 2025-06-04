using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;

namespace Setup
{
    public class Program
    {
        static void Main()
        {
            // This project type has been superseded with the EmbeddedUI based "WixSharp Managed Setup - Custom Dialog"
            // project type. Which provides by far better final result and user experience.
            // However due to the Burn limitations (see this discussion: https://wixsharp.codeplex.com/discussions/645838)
            // currently "Custom CLR Dialog" is the only working option for having bootstrapper silent UI displaying
            // individual MSI packages UI implemented in managed code.

            var uninstallerShortcut = new ExeFileShortcut("Uninstall LANraragi", "[System64Folder]msiexec.exe", "/x [ProductCode]");

            var installWinAppSdkAction = new ManagedAction(InstallWinAppSdk, Return.check, When.After, Step.InstallFinalize, Condition.NOT_BeingRemoved)
            {
                ProgressText = "Installing Windows App SDK Runtime..."
            };

            var project = new Project("LANraragi",
                new Dir(@"%AppData%\LANraragi",
                    new Files(@"..\Karen\bin\win-x64\publish\*.*", file => !file.EndsWith("pdb")),
                    uninstallerShortcut
                ),
                new Dir(@"%ProgramMenu%\LANraragi for Windows",
                    new ExeFileShortcut("LANraragi", "[INSTALLDIR]Karen.exe", "")),
                installWinAppSdkAction
            );

            project.GUID = new Guid("6fe30b47-2577-43ad-1337-1861ba25889b");
            project.Platform = Platform.x64;
            project.MajorUpgrade = new MajorUpgrade
            {
                Schedule = UpgradeSchedule.afterInstallValidate, // Remove previous version entirely before reinstalling, so that the WSL distro isn't uninstalled on upgrade.
                DowngradeErrorMessage = "A later version of [ProductName] is already installed. Setup will now exit."
            };
            project.InstallScope = InstallScope.perUser;

            // Version number is based on the LRR_VERSION_NUM env variable
            var version = "0.0.1";
            if (Environment.GetEnvironmentVariable("LRR_VERSION_NUM") != null)
                version = Environment.GetEnvironmentVariable("LRR_VERSION_NUM");

            try
            {
                project.Version = Version.Parse(version);
            }
            catch
            {
                Console.WriteLine("Couldn't get version from the environment variable " + version);
                project.Version = Version.Parse("0.0.1");
            }

            // Check for x64 Windows 10
            project.LaunchConditions.Add(new LaunchCondition("VersionNT64", "LANraragi for Windows can only be installed on a 64-bit Windows."));
            project.LaunchConditions.Add(new LaunchCondition("VersionNT>=\"603\"", "LANraragi for Windows can only be installed on Windows 10 and up."));

            // https://stackoverflow.com/a/53525753
            project.UI = WUI.WixUI_InstallDir;

            //remove LicenceDlg
            project.RemoveDialogsBetween(NativeDialogs.WelcomeDlg, NativeDialogs.InstallDirDlg);

            // Customize
            project.BackgroundImage = @"Images\dlgbmp.bmp";
            project.BannerImage = @"Images\bannrbmp.bmp";

            project.ControlPanelInfo.UrlInfoAbout = "https://github.com/Difegue/LANraragi";
            project.ControlPanelInfo.UrlUpdateInfo = "https://sugoi.gitbook.io/lanraragi/";
            project.ControlPanelInfo.ProductIcon = @"Images\favicon.ico";
            project.ControlPanelInfo.Contact = "Difegue";
            project.ControlPanelInfo.Manufacturer = "Difegue";

            // Fix some ids being the same as internal ones
            project.CustomIdAlgorithm = entity =>
            {
                if (entity is Dir dir)
                {
                    var path = project.GetTargetPathOf(dir);
                    var filename = path.PathGetFileName();
                    if (filename.Equals("Time"))
                        return $"{filename}_{Math.Abs(path.GetHashCode32())}";
                }
                return null;
            };

            project.OutDir = "bin";
            project.BuildMsi();
        }

        [CustomAction]
        public static ActionResult InstallWinAppSdk(Session session)
        {
            return session.HandleErrors(() =>
            {
                ResetProgressBar(session, 2);
                IncrementProgressBar(session, 1);

                var temp = session.Property("TempFolder");

                session.Log("Installing WinAppSDK Runtime");

                var exe = Path.Combine(temp, $"{Path.GetRandomFileName()}.exe");

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                using (var client = new WebClient())
                {
                    client.DownloadFile("https://aka.ms/windowsappsdk/1.7/1.7.250513003/windowsappruntimeinstall-x64.exe", exe);
                }
                IncrementProgressBar(session, 1);

                var procInfo = new ProcessStartInfo(exe, "--quiet")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                var proc = Process.Start(procInfo);
                proc.WaitForExit();
                System.IO.File.Delete(exe);
                IncrementProgressBar(session, 1);

                session.Log("Exit code: " + proc.ExitCode);
            });
        }

        // https://www.advancedinstaller.com/forums/viewtopic.php?t=27535#p69452
        public static MessageResult ResetProgressBar(Session session, int totalStatements)
        {
            var record = new Record(3);
            record[1] = 0; // "Reset" message 
            record[2] = totalStatements;  // total ticks 
            record[3] = 0; // forward motion 
            return session.Message(InstallMessage.Progress, record);
        }

        public static MessageResult IncrementProgressBar(Session session, int progressPercentage)
        {
            var record = new Record(3);
            record[1] = 2; // "ProgressReport" message 
            record[2] = progressPercentage; // ticks to increment 
            record[3] = 0; // ignore 
            return session.Message(InstallMessage.Progress, record);
        }

    }
}
