using System;
using System.Windows.Forms;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

public partial class CustomDialog : WixCLRDialog
{
    private string obj;

    public CustomDialog()
    {
        InitializeComponent();
    }

    public CustomDialog(Session session, string obj)
        : base(session)
    {
        InitializeComponent();
        this.obj = obj;
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

    void button1_Click(object sender, EventArgs e)
    {
        MessageBox.Show(obj, "Wix#");
    }
}
