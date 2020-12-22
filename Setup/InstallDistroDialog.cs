using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CliWrap;
using CliWrap.EventStream;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Setup
{
    public partial class InstallDistroDialog : Form
    {
        private Session _wixSess;
        public InstallDistroDialog(Session session)
        {
            InitializeComponent();

            _wixSess = session;

            var packageLocation = session.Property("INSTALLDIR") + @"package.tar";
            var lxRunLocation = session.Property("INSTALLDIR") + @"LxRunOffline";
            var distroLocation = @"%AppData%\LANraragi\Distro";

            Directory.CreateDirectory(distroLocation);

            // Use LxRunOffline to install the WSL distro.
            logText("Installing WSL Distro from package.tar");
            logText("LxRunOffline location: " + lxRunLocation);
            logText("package.tar location: " + packageLocation);

            // The extra quote after the /K flag is needed.
            // "If command starts with a quote, the first and last quote chars in command will be removed, whether /s is specified or not."
            var procArgs = "/S /K \"\"" + lxRunLocation + "\\LxRunOffline.exe\" i -n lanraragi -d " + distroLocation
                            + " -f \"" + packageLocation + "\" && del \"" + distroLocation + "\\rootfs\\etc\\resolv.conf\" && exit\"";
            // We delete /etc/resolv.conf here as it's a leftover from the package's origins as a Docker image.
            // Deleting it in Linux would be too late as WSL already started!
            logText("Launching cmd.exe with arguments " + procArgs);

            Task.Run(async () =>
            {
                var cmd = Cli.Wrap("cmd").WithArguments(procArgs);

                await foreach (var cmdEvent in cmd.ListenAsync())
                {
                    switch (cmdEvent)
                    {
                        case StartedCommandEvent started:
                            logText($"Process started; ID: {started.ProcessId}");
                            break;
                        case StandardOutputCommandEvent stdOut:
                            logText($"Out> {stdOut.Text}");
                            break;
                        case StandardErrorCommandEvent stdErr:
                            logText($"Err> {stdErr.Text}");
                            break;
                        case ExitedCommandEvent exited:
                            logText($"Process exited; Code: {exited.ExitCode}");
                            break;
                    }
                }

                // Close this form once the install is done
                this.Invoke(new System.Action(Close));
            });

        }

        public void logText(string txt)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(logText), new object[] { txt });
                return;
            }
            textBox1.Text += (txt + "\n");
            _wixSess.Log(txt);
        }


        private void cancelBtn_Click(object sender, EventArgs e)
        {
            // Just throw a whole wrench into the gears m8 it'll be caught by wix anyway
            throw new OperationCanceledException();
        }

    }
}
