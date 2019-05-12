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

namespace Karen
{
    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLIC = 4,
        ACCENT_INVALID_STATE = 5
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
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }

    public partial class KarenPopup : UserControl, INotifyPropertyChanged
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        private uint _blurBackgroundColor = 0x99FFFFFF;

        public event PropertyChangedEventHandler PropertyChanged;

        private Brush foreground;
        public Brush ForegroundColor
        {
            get { return foreground; }
            set
            {
                foreground = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ForegroundColor"));
                }
            }
        }

        public KarenPopup()
        {
            InitializeComponent();
            BorderBrush = SystemParameters.WindowGlassBrush;
            DataContext = this;
        }

        // Wait for Control to Load
        void KarenPopup_Loaded(object sender, RoutedEventArgs e)
        {
            // Get PresentationSource
            PresentationSource presentationSource = PresentationSource.FromVisual((Visual)sender);

            // Subscribe to PresentationSource's ContentRendered event
            presentationSource.ContentRendered += KarenPopup_ContentRendered;
        }

        void KarenPopup_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                //Get Light/Dark theme from registry
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
                RegistryValueKind kind = registryKey.GetValueKind("AppsUseLightTheme");
                string lightThemeOn = registryKey.GetValue("AppsUseLightTheme").ToString();

                if (lightThemeOn != "0")
                {
                    _blurBackgroundColor = 0x99FFFFFF;
                    ForegroundColor = System.Windows.Media.Brushes.Black;
                }
                else
                {
                    _blurBackgroundColor = 0xAA000000;
                    ForegroundColor = System.Windows.Media.Brushes.White;
                }
            } catch (Exception)
            {
                //eh 
            }

            EnableBlur((HwndSource)sender);
        }

        //Enable Acrylic on the Popup's HWND.
        internal void EnableBlur(HwndSource source)
        {
            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLIC;
            accent.GradientColor = _blurBackgroundColor;

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

        private void Show_Config(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow == null)
                Application.Current.MainWindow = new MainWindow();

            Application.Current.MainWindow.Show();

            if (Application.Current.MainWindow.WindowState == WindowState.Minimized)
                Application.Current.MainWindow.WindowState = WindowState.Normal;

            ((Popup)this.Parent).IsOpen = false;
        }
    }
}
