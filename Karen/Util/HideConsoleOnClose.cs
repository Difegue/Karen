using System;
using System.Runtime.InteropServices;

namespace HideConsoleOnCloseManaged
{
    public static class HideConsoleOnClose
    {

        [DllImport(
            "HideConsoleOnClose32",
            EntryPoint = "EnableForWindow",
            SetLastError = true
        )]
        private static extern bool EnableForWindow32(IntPtr hWnd);

        [DllImport(
            "HideConsoleOnClose64",
            EntryPoint = "EnableForWindow",
            SetLastError = true
        )]
        private static extern bool EnableForWindow64(IntPtr hWnd);

        public static void EnableForWindow(IntPtr hWnd)
        {
            bool success;

            if (IntPtr.Size == 4)
            {
                success = EnableForWindow32(hWnd);
            }
            else
            {
                success = EnableForWindow64(hWnd);
            }

            if (!success)
            {
                Marshal.ThrowExceptionForHR(
                    Marshal.GetHRForLastWin32Error()
                );
            }
        }

    }
}