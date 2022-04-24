using System;
using System.Runtime.InteropServices;

namespace Karen.Interop
{
    public static class User32
    {
        [DllImport("user32.dll", SetLastError = false)]
        public static extern IntPtr GetDesktopWindow();
    }
}
