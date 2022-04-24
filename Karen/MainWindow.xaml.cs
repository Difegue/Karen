using System.ComponentModel;
using System.Windows;
using System.Windows.Navigation;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System;
using Karen.Interop;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Windows.ApplicationModel;
using Windows.Storage.Pickers;
using Microsoft.Win32;

namespace Karen
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private IntPtr Handle;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
            Handle = helper.Handle;
        }

        private void PickFolder(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            ((IInitializeWithWindow)(object)picker).Initialize(Handle);
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.CommitButtonText = "Set as LRR Content Folder";

            var folder = picker.PickSingleFolderAsync().GetAwaiter().GetResult();

            if (folder != null)
                Settings.Default.ContentFolder = folder.Path;
        }

        private void PickThumbFolder(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            ((IInitializeWithWindow)(object)picker).Initialize(Handle);
            picker.FileTypeFilter.Add("*");
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.CommitButtonText = "Set as LRR Thumbnail Folder";

            var folder = picker.PickSingleFolderAsync().GetAwaiter().GetResult();

            if (folder != null)
                Settings.Default.ThumbnailFolder = folder.Path;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Set first launch to false
            Settings.Default.FirstLaunch = false;

            if (Settings.Default.StartWithWindows)
            {
                if (!AddApplicationToStartup())
                {
                    e.Cancel = true;
                }
            }
            else
                RemoveApplicationFromStartup();
        }

        public bool AddApplicationToStartup()
        {
            if (new DesktopBridge.Helpers().IsRunningAsUwp())
            {
                var startupTask = StartupTask.GetAsync("Karen").GetAwaiter().GetResult();
                switch (startupTask.State)
                {
                    case StartupTaskState.Disabled:
                        return startupTask.RequestEnableAsync().GetAwaiter().GetResult() == StartupTaskState.Enabled;
                    case StartupTaskState.DisabledByUser:
                        App.ShowMessageDialog("Auto startup disabled in Task Manager", "Close", Handle);
                        return false;
                    case StartupTaskState.Enabled:
                        return true;
                    case StartupTaskState.DisabledByPolicy:
                        App.ShowMessageDialog("Auto startup disabled by policy", "Close", Handle);
                        return false;
                    case StartupTaskState.EnabledByPolicy:
                        return true;
                }
            } 
            else using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                // Old-fashioned way -- WASDK would help here
                key.SetValue("Karen", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
                return true;
            }

            return false;
        }

        public void RemoveApplicationFromStartup()
        {
            if (new DesktopBridge.Helpers().IsRunningAsUwp())
            {
                var startupTask = StartupTask.GetAsync("Karen").GetAwaiter().GetResult();
                startupTask.Disable();
            }
            else using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.DeleteValue("Karen", false);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Window_ContentRendered(object sender, System.EventArgs e)
        {
            if (WCAUtils.IsWin11)
            {
                // Set a transparent background to let the mica brush come through
                Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255));

                WCAUtils.UpdateStyleAttributes((HwndSource)sender);
                ModernWpf.ThemeManager.Current.ActualApplicationThemeChanged += (s, ev) => WCAUtils.UpdateStyleAttributes((HwndSource)sender);
            }
            else
            {
                WindowChrome.SetWindowChrome(this, null);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Get PresentationSource
            PresentationSource presentationSource = PresentationSource.FromVisual((Visual)sender);

            // Subscribe to PresentationSource's ContentRendered event
            presentationSource.ContentRendered += Window_ContentRendered;
        }
    }
}
