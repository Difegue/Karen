using Karen.Interop;
using ModernWpf;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using static Karen.Interop.WCAUtils;

namespace Karen
{

    public partial class KarenPopup : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Version => ((App)Application.Current).Distro.Version;
        public AppStatus DistroStatus => ((App)Application.Current).Distro.Status;
        public bool IsStarted => DistroStatus == AppStatus.Started;
        public bool IsStopped => DistroStatus == AppStatus.Stopped;
        public bool IsNotInstalled => DistroStatus == AppStatus.NotInstalled;

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
            UpdateStyleAttributes((HwndSource)sender);

            // Round off corners for the popup
            if (IsWin11)
            {
                Brd.CornerRadius = new CornerRadius(6);

                int value = 0x02;
                DwmSetWindowAttribute(((HwndSource)sender).Handle, DwmWindowAttribute.DWMWA_WINDOW_CORNER_PREFERENCE, ref value, Marshal.SizeOf(typeof(int)));
            } 

            ThemeManager.Current.ActualApplicationThemeChanged += (s, ev) =>
            {
                UpdateStyleAttributes((HwndSource)sender);

                // Update theme manually since we're not in a window
                if (ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark)
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Dark);
                else
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Light);
            };
        }

        private void Show_Config(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).ShowConfigWindow();

            ((Popup)this.Parent).IsOpen = false;
        }

        private void UpdateProperties()
        {
            PropertyChanged(this, new PropertyChangedEventArgs("DistroStatus"));
            PropertyChanged(this, new PropertyChangedEventArgs("IsStarted"));
            PropertyChanged(this, new PropertyChangedEventArgs("IsStopped"));
            PropertyChanged(this, new PropertyChangedEventArgs("Version"));
        }

        private void Show_Console(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Distro.ShowConsole();
        }

        private void Shutdown_App(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Start_Distro(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Distro.StartApp();
            UpdateProperties();

            // Prevent the popup from closing
            ((Popup)this.Parent).IsOpen = true;
        }

        private void Stop_Distro(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Distro.StopApp();
            UpdateProperties();
        }

        private void Open_Webclient(object sender, RoutedEventArgs e)
        {
            Process.Start("http://localhost:"+ Settings.Default.NetworkPort);
        }

        private void Open_Distro(object sender, RoutedEventArgs e)
        {
            Process.Start(@"\\wsl$\lanraragi\home\koyomi\lanraragi");
        }

        private void Install_Distro(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Distro.Repair();
        }
    }
}
