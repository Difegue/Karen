using System;
using System.Linq;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Karen.Interop;
using Windows.ApplicationModel;
using Windows.UI.Popups;

namespace Karen
{
    /// <summary>
    /// Simple application. Check the XAML for comments.
    /// </summary>
    public partial class App : Application
    {
        private TaskbarIcon notifyIcon;
        public WslDistro Distro { get; set; }

        public void ToastNotification(string text)
        {
            notifyIcon.ShowBalloonTip("LANraragi", text, notifyIcon.Icon, true);
        }

        public void ShowConfigWindow()
        {
            var mainWindow = Application.Current.MainWindow;

            if (mainWindow == null || mainWindow.GetType() != typeof(MainWindow))
                mainWindow = new MainWindow();

            mainWindow.Show();

            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Only one instance of the bootloader allowed at a time
            var exists = System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1;
            if (exists)
            {
                ShowMessageDialog("Another instance of the application is already running.", "Close");
                Current.Shutdown();
            }

            Settings.Default.MigrateUserConfigToMSIX();

            Distro = new WslDistro();

            // If the currently installed version is more recent than the one saved in settings, run the installer to update the distro
            // This is only required in MSIX mode/Package Identity, as the MSI installer updates the distro automatically.
            if (new DesktopBridge.Helpers().IsRunningAsUwp())
            {
                bool needsUpgrade = Version.TryParse(Settings.Default.Version, out var oldVersion) && oldVersion < GetVersion();
                if (!Distro.CheckDistro() || needsUpgrade)
                {
                    Settings.Default.Karen = true;
                    Package.Current.GetAppListEntriesAsync().GetAwaiter().GetResult()
                        .First(app => app.AppUserModelId == Package.Current.Id.FamilyName + "!Installer").LaunchAsync().GetAwaiter().GetResult();
                    Current.Shutdown();
                    return;
                }
            }

            // First time ?
            if (Settings.Default.FirstLaunch)
            {
                ShowMessageDialog("Looks like this is your first time running the app! Please setup your Content Folder in the Settings.", "Ok");
                ShowConfigWindow();
            }

            // Create the Taskbar Icon now so it appears in the tray
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");

            // Check if server starts with app 
            if (Settings.Default.StartServerAutomatically && Distro.Status == AppStatus.Stopped)
            {
                ToastNotification("LANraragi is starting automagically...");
                Distro.StartApp();
            }
            else
                ToastNotification("The Launcher is now running! Please click the icon in your Taskbar.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (notifyIcon != null)
                notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            try
            {
                Distro.StopApp();
            }
            finally
            {
                WslDistro.FreeConsole(); //clean up the console to ensure it's closed alongside the app
                base.OnExit(e);
            }
        }

        private static Version GetVersion()
        {
            var version = new DesktopBridge.Helpers().IsRunningAsUwp() ? Package.Current.Id.Version : new PackageVersion();
            return new Version(version.Major, version.Minor, version.Build, version.Revision);
        }

        public static void ShowMessageDialog(string content, string button, IntPtr window = new IntPtr())
        {
            var msg = new MessageDialog(content, "LANraragi");
            msg.Commands.Add(new UICommand(button));
            if (window == IntPtr.Zero)
                window = User32.GetDesktopWindow();
            ((IInitializeWithWindow)(object)msg).Initialize(window);
            msg.ShowAsync().GetAwaiter().GetResult();
        }
    }
}
