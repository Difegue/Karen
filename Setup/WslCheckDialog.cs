using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Windows.Forms;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

public partial class WslCheckDialog : WixCLRDialog
{
    private string obj;

    public WslCheckDialog()
    {
        InitializeComponent();
    }

    public WslCheckDialog(Session session)
        : base(session)
    {
        InitializeComponent();

        // Check if the output of "wsl.exe --status" returns anything.
        // wsl.exe is now preinstalled on most recent Windows setups so we can't simply check if it exists or not..
        // If it doesn't, we'll just assume that WSL is not installed and display the error message.
        var wsl = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "wsl.exe",
                Arguments = "--status",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                StandardOutputEncoding = System.Text.Encoding.Unicode,
                CreateNoWindow = true
            }
        };
        try
        {
            wsl.Start();
            var output = wsl.StandardOutput.ReadToEnd();
            wsl.WaitForExit(1000);

            session.Log("WSL --status output: " + output);

            if (wsl.ExitCode != 0) {
                obj += "wsl.exe --status failed. \n(output was: " + output + ")";
            }
        } 
        catch (Exception e)
        {
            // wsl.exe probably doesn't exist
            obj += "Error running wsl.exe: " + e.ToString();
        }
       
    }

    void backBtn_Click(object sender, EventArgs e)
    {
        MSIBack();

    }

    void nextBtn_Click(object sender, EventArgs e)
    {
        MSICancel();
    }

    void cancelBtn_Click(object sender, EventArgs e)
    {
        MSICancel();
    }

    private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start("https://learn.microsoft.com/en-us/windows/wsl/install");
    }

    private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        MessageBox.Show(obj, "Error thrown while looking for WSL");
    }

    private void CustomDialog_Load(object sender, EventArgs e)
    {
        // Do nothing if WSL is enabled
        // This prevents going back to the very first page of the setup but whatever tbh
        if (obj == null)
            MSINext();
    }
}
