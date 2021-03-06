﻿using System;
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
            var version = "0.0.1";
            if (Environment.GetEnvironmentVariable("LRR_VERSION_NUM") != null)
                version = Environment.GetEnvironmentVariable("LRR_VERSION_NUM");

            try
            {
                project.Version = Version.Parse(version.Replace("-EX",".38")); //dotnet versions don't accept text or dashes but I ain't about to fuck up my versioning schema dagnabit
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
            MessageBox.Show(session.GetMainWindow(), "The WSL Distro will now be installed on your system. You should see one or two cmd windows.");

            var result = UnRegisterWslDistro(session);

            if (session.IsUninstalling())
                return result;

            var packageLocation = session.Property("INSTALLDIR") + @"package.tar";
            var lxRunLocation = session.Property("INSTALLDIR") + @"LxRunOffline";
            var distroLocation = @"%AppData%\LANraragi\Distro";

            Directory.CreateDirectory(distroLocation);

            return session.HandleErrors(() =>
            {
                // Use LxRunOffline to either install or uninstall the WSL distro.
                session.Log("Installing WSL Distro from package.tar");
                session.Log("LxRunOffline location: " + lxRunLocation);
                session.Log("package.tar location: " + packageLocation);

                // The extra quote after the /K flag is needed.
                // "If command starts with a quote, the first and last quote chars in command will be removed, whether /s is specified or not."
                var procArgs = "/S /K \"\"" + lxRunLocation + "\\LxRunOffline.exe\" i -n lanraragi -d " + distroLocation 
                                + " -f \"" + packageLocation + "\" && del \"" + distroLocation +"\\rootfs\\etc\\resolv.conf\" && pause && exit\"";
                // We delete /etc/resolv.conf here as it's a leftover from the package's origins as a Docker image.
                // Deleting it in Linux would be too late as WSL already started!
                session.Log("Launching cmd.exe with arguments " + procArgs);

                var lxProc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = procArgs
                    }
                };

                lxProc.Start();
                lxProc.WaitForExit();
                session.Log("Exit code of LxRunOffline is " + lxProc.ExitCode);
            });
        }

        [CustomAction]
        public static ActionResult UnRegisterWslDistro(Session session)
        {
            return session.HandleErrors(() =>
            {
                session.Log("Removing previous WSL Distro");
                var wslProc = Process.Start("wslconfig.exe", "/unregister lanraragi");
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