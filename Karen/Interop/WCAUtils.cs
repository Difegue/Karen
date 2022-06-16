using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Karen.Interop
{
    public static class WCAUtils
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

        public static bool IsWin11 = Environment.OSVersion.Version >= new Version(10, 0, 22000, 0);

        [Flags]
        public enum DwmWindowAttribute : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWA_SYSTEMBACKDROP_TYPE = 38,
            DWMWA_MICA_EFFECT = 1029
        }

        // Enable Acrylic on the Popup's HWND.
        public static void EnableBlur(HwndSource source, uint color)
        {
            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = color;

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(source.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        // Enable Mica on the given HWND.
        public static void EnableMica(HwndSource source, bool darkThemeEnabled)
        {
            int trueValue = 0x01;
            int falseValue = 0x00;

            int micaValue = 0x02;

            // Set dark mode before applying the material, otherwise you'll get an ugly flash when displaying the window.
            if (darkThemeEnabled) 
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref trueValue, Marshal.SizeOf(typeof(int)));
            else
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref falseValue, Marshal.SizeOf(typeof(int)));

            if (Environment.OSVersion.Version.Build >= 22523)
            {
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_SYSTEMBACKDROP_TYPE, ref micaValue, Marshal.SizeOf(typeof(int)));
            }
            else
            {
                // Old undocumented API
                DwmSetWindowAttribute(source.Handle, DwmWindowAttribute.DWMWA_MICA_EFFECT, ref trueValue, Marshal.SizeOf(typeof(int)));
            }

        }

        public static void UpdateStyleAttributes(HwndSource hwnd)
        {
            var darkThemeEnabled = ModernWpf.ThemeManager.Current.ActualApplicationTheme == ModernWpf.ApplicationTheme.Dark;

            if (IsWin11)
                EnableMica(hwnd, darkThemeEnabled);
            else
            {
                if (darkThemeEnabled)
                    EnableBlur(hwnd, 0xAA000000);
                else
                    EnableBlur(hwnd, 0x99FFFFFF);
            }
            
        }
    }

    #region Some more Win32 to ask the DWM for acrylic
    internal enum AccentState
    {
        ACCENT_DISABLED = 0x0,
        ACCENT_ENABLE_GRADIENT = 0x1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 0x2,
        ACCENT_ENABLE_BLURBEHIND = 0x3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 0x4,
        ACCENT_ENABLE_HOSTBACKDROP = 0x5,
        ACCENT_INVALID_STATE = 0x6
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public uint AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        WCA_UNDEFINED = 0x0,
        WCA_NCRENDERING_ENABLED = 0x1,
        WCA_NCRENDERING_POLICY = 0x2,
        WCA_TRANSITIONS_FORCEDISABLED = 0x3,
        WCA_ALLOW_NCPAINT = 0x4,
        WCA_CAPTION_BUTTON_BOUNDS = 0x5,
        WCA_NONCLIENT_RTL_LAYOUT = 0x6,
        WCA_FORCE_ICONIC_REPRESENTATION = 0x7,
        WCA_EXTENDED_FRAME_BOUNDS = 0x8,
        WCA_HAS_ICONIC_BITMAP = 0x9,
        WCA_THEME_ATTRIBUTES = 0xA,
        WCA_NCRENDERING_EXILED = 0xB,
        WCA_NCADORNMENTINFO = 0xC,
        WCA_EXCLUDED_FROM_LIVEPREVIEW = 0xD,
        WCA_VIDEO_OVERLAY_ACTIVE = 0xE,
        WCA_FORCE_ACTIVEWINDOW_APPEARANCE = 0xF,
        WCA_DISALLOW_PEEK = 0x10,
        WCA_CLOAK = 0x11,
        WCA_CLOAKED = 0x12,
        WCA_ACCENT_POLICY = 0x13,
        WCA_FREEZE_REPRESENTATION = 0x14,
        WCA_EVER_UNCLOAKED = 0x15,
        WCA_VISUAL_OWNER = 0x16,
        WCA_HOLOGRAPHIC = 0x17,
        WCA_EXCLUDED_FROM_DDA = 0x18,
        WCA_PASSIVEUPDATEMODE = 0x19,
        WCA_USEDARKMODECOLORS = 0x1A,
        WCA_CORNER_STYLE = 0x1B,
        WCA_PART_COLOR = 0x1C,
        WCA_DISABLE_MOVESIZE_FEEDBACK = 0x1D,
        WCA_SYSTEMBACKDROP_TYPE = 0x1E,
        WCA_LAST = 0x1F
    }
    #endregion
}
