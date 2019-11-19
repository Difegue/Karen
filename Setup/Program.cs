using System;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;
using System.Management.Automation;

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

            var project = new Project("MyProduct",
                             new Dir(@"%AppData%\LANraragi",
                                 new Files(@"..\Karen\bin\x64\Release\*.*"),
                                    new ExeFileShortcut("Uninstall My Product",
                                                    "[System64Folder]msiexec.exe",
                                                    "/x [ProductCode]")));

            project.GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b");
            project.Platform = Platform.x64;

            // Check for x64 Windows 10
            project.LaunchConditions.Add(new LaunchCondition("VersionNT64","LANraragi for Windows can only be installed on a 64-bit Windows."));
            project.LaunchConditions.Add(new LaunchCondition("VersionNT>=\"603\"", "LANraragi for Windows can only be installed on Windows 10 and up."));

            // Check for WSL
            //var wslProp = new Property("WSLInstalled", "false");
            //project.AddProperty(wslProp);
            //project.LaunchConditions.Add(new LaunchCondition("WSLInstalled=\"true\"", "You must install the Windows Subsystem for Linux in order to use LANraragi for Windows. Check here: https://docs.microsoft.com/en-us/windows/wsl/install-win10"));

            //Schedule custom dialog between WelcomeDlg and InstallDirDlg standard MSI dialogs.
            project.InjectClrDialog(nameof(ShowDialogIfWslDisabled), NativeDialogs.WelcomeDlg, NativeDialogs.InstallDirDlg);

            //remove LicenceDlg
            project.RemoveDialogsBetween(NativeDialogs.InstallDirDlg, NativeDialogs.VerifyReadyDlg);

            //reference assembly that is needed by the custom dialog
            //project.DefaultRefAssemblies.Add(<External Asm Location>);

            //project.SourceBaseDir = "<input dir path>";
            project.OutDir = "bin";

            project.BuildMsi();
        }

        [CustomAction]
        public static ActionResult ShowDialogIfWslDisabled(Session session)
        {
            return WixCLRDialog.ShowAsMsiDialog(new CustomDialog(session));
        }
    }
}