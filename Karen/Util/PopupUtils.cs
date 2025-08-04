using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.Win32;
using WinRT.Interop;

namespace Karen.Util;

public static class PopupUtils
{

    public static void ShowMessageDialog(string title, string content, string button, IntPtr? hwnd = null)
    {
        var msg = new MessageDialog(content, title);
        msg.Commands.Add(new UICommand(button));
        InitializeWithWindow.Initialize(msg, hwnd ?? PInvoke.GetDesktopWindow());
        msg.ShowAsync().GetAwaiter().GetResult();
    }



}

