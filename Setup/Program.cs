using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;
using System.Linq;
using System.Windows.Forms;
using File = WixSharp.File;

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

            var registerAction = new ManagedAction(RegisterWslDistro,
                                 Return.check,
                                 When.After,
                                 Step.InstallFinalize,
                                 Condition.NOT_BeingRemoved);
            registerAction.ProgressText = "Installing the LANraragi WSL Distro... (This will show a cmd window)";

            var unregisterAction = new ManagedAction(UnRegisterWslDistro,
                                 Return.check,
                                 When.Before,
                                 Step.RemoveFiles,
                                 Condition.BeingUninstalled);
            unregisterAction.ProgressText = "Removing the previous LANraragi WSL Distro (This will show a cmd window)";

            var project = new Project("LANraragi",
                             new Dir(@"%AppData%\LANraragi",
                                 new Files(@"..\Karen\bin\x64\Release\*.*"),
                                            new File(@"..\External\package.tar"),
                                            uninstallerShortcut
                                    ),
                             new Dir(@"%ProgramMenu%\LANraragi for Windows",
                                 new ExeFileShortcut("LANraragi", "[INSTALLDIR]Karen.exe", "")),
                             new RegValue(RegistryHive.LocalMachineOrUsers, @"Software\Microsoft\Windows\CurrentVersion\Run", "Karen", "[INSTALLDIR]Karen.exe"),
                             registerAction,
                             unregisterAction
                            );

            project.GUID = new Guid("6fe30b47-2577-43ad-1337-1861ba25889b");
            project.Platform = Platform.x64;
            project.MajorUpgrade = new MajorUpgrade
            {
                Schedule = UpgradeSchedule.afterInstallValidate, // Remove previous version entirely before reinstalling, so that the WSL distro isn't uninstalled on upgrade.
                DowngradeErrorMessage = "A later version of [ProductName] is already installed. Setup will now exit."
            };

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

            //Schedule custom dialog between WelcomeDlg and InstallDirDlg standard MSI dialogs.
            project.InjectClrDialog(nameof(ShowDialogIfWslDisabled), NativeDialogs.WelcomeDlg, NativeDialogs.InstallDirDlg);

            //remove LicenceDlg
            project.RemoveDialogsBetween(NativeDialogs.InstallDirDlg, NativeDialogs.VerifyReadyDlg);

            // Customize
            project.BackgroundImage = @"Images\dlgbmp.bmp";
            project.BannerImage = @"Images\bannrbmp.bmp";

            project.ControlPanelInfo.UrlInfoAbout = "https://github.com/Difegue/LANraragi";
            project.ControlPanelInfo.UrlUpdateInfo = "https://sugoi.gitbook.io/lanraragi/";
            project.ControlPanelInfo.ProductIcon = @"Images\favicon.ico";
            project.ControlPanelInfo.Contact = "Difegue";
            project.ControlPanelInfo.Manufacturer = "Difegue";

            project.OutDir = "bin";
            project.BuildMsi();
        }

        [CustomAction]
        public static ActionResult RegisterWslDistro(Session session)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            var result = UnRegisterWslDistro(session);

            if (session.IsUninstalling())
                return result;

            var packageLocation = session.Property("INSTALLDIR") + @"package.tar";
            var distroLocation = @"%AppData%\LANraragi\Distro";

            Directory.CreateDirectory(distroLocation);

            return session.HandleErrors(() =>
            {
                // Use wsl.exe to either install or uninstall the WSL distro.
                session.Log("Installing WSL Distro from package.tar");
                session.Log("package.tar location: " + packageLocation);

                var wslProc = Process.Start("wsl.exe", $"--import lanraragi \"{distroLocation}\" \"{packageLocation}\" --version 1");
                wslProc.WaitForExit();

                session.Log("Exit code of wsl.exe is " + wslProc.ExitCode);

                // We delete /etc/resolv.conf here as it's a leftover from the package's origins as a Docker image.
                // Deleting it in Linux would be too late as WSL already started!
                var cmd = $"/S /K \"del \"{distroLocation}\\rootfs\\etc\\resolv.conf\"\" && exit";
                session.Log("Running cmd.exe with arguments" + cmd);
                Process.Start("cmd.exe", cmd).WaitForExit();
            });
        }

        [CustomAction]
        public static ActionResult UnRegisterWslDistro(Session session)
        {
            return session.HandleErrors(() =>
            {
                session.Log("Removing previous WSL Distro");
                var wslProc = Process.Start("wsl.exe", "--unregister lanraragi");
                wslProc.WaitForExit();
            });
        }

        [CustomAction]
        public static ActionResult ShowDialogIfWslDisabled(Session session)
        {
            return WixCLRDialog.ShowAsMsiDialog(new WslCheckDialog(session));
        }

    }
}
