using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;
using CliWrap;
using System.Windows.Forms;
using File = WixSharp.File;
using System.Threading.Tasks;

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

            var project = new Project("LANraragi",
                             new Dir(@"%AppData%\LANraragi",
                                 new Files(@"..\Karen\bin\x64\Release\*.*"),
                                            new File(@"..\External\package.tar"),
                                             new Dir("LxRunOffline",
                                                 new Files(@"..\External\LxRunOffline\*.*")),
                                            uninstallerShortcut
                                    ),
                             new Dir(@"%ProgramMenu%\LANraragi for Windows",
                                 new ExeFileShortcut("LANraragi", "[INSTALLDIR]Karen.exe", ""),
                                 new ExeFileShortcut("Uninstall LANraragi", "[System64Folder]msiexec.exe", "/x [ProductCode]")),
                             new RegValue(RegistryHive.LocalMachineOrUsers, @"Software\Microsoft\Windows\CurrentVersion\Run", "Karen", "[INSTALLDIR]Karen.exe"),
                             new ManagedAction(RegisterWslDistro,
                                 Return.check,
                                 When.After,
                                 Step.InstallFinalize,
                                 Condition.NOT_BeingRemoved),
                             new ManagedAction(UnRegisterWslDistro,
                                 Return.check,
                                 When.Before,
                                 Step.RemoveFiles,
                                 Condition.BeingUninstalled)
                            );

            project.GUID = new Guid("6fe30b47-2577-43ad-1337-1861ba25889b");
            project.Platform = Platform.x64;
            project.MajorUpgrade = new MajorUpgrade
            {
                Schedule = UpgradeSchedule.afterInstallValidate, // Remove previous version entirely before reinstalling, so that the WSL distro isn't uninstall on upgrade.
                DowngradeErrorMessage = "A later version of [ProductName] is already installed. Setup will now exit."
            };

            // Version number is based on the LRR_VERSION_NUM env variable
            var version = "0.7.4";
            if (Environment.GetEnvironmentVariable("LRR_VERSION_NUM") != null)
                version = Environment.GetEnvironmentVariable("LRR_VERSION_NUM");

            try
            {
                project.Version = Version.Parse(version.Replace("-EX", ".38")); //dotnet versions don't accept text or dashes but I ain't about to fuck up my versioning schema dagnabit
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
            //MessageBox.Show(session.GetMainWindow(), "The WSL Distro will now be installed on your system. You should see one or two cmd windows.");

            var result = UnRegisterWslDistro(session);

            if (session.IsUninstalling())
                return result;

            return session.HandleErrors(() =>
            {
                new InstallDistroDialog(session).Show(session.GetMainWindow());
            });
        }

        [CustomAction]
        public static ActionResult UnRegisterWslDistro(Session session)
        {
            return session.HandleErrors(async () =>
            {
                session.Log("Removing previous WSL Distro");

                await Cli.Wrap("wslconfig.exe")
                    .WithArguments("/unregister lanraragi")
                    .ExecuteAsync();
            });
        }

        [CustomAction]
        public static ActionResult ShowDialogIfWslDisabled(Session session)
        {
            return WixCLRDialog.ShowAsMsiDialog(new WslCheckDialog(session));
        }

    }
}