using Karen.Services;
using Karen.Util;
using Karen.Views;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;
using WinUIEx;

namespace Karen
{

    public partial class App : Application
    {
        private KarenPopup Popup = null!;
        private TrayIcon TrayIcon = null!;

        public App()
        {
            InitializeComponent();
            Service.BuildServices();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            var instance = AppInstance.FindOrRegisterForKey("Karen");
            if (!instance.IsCurrent)
            {
                await WinUIUtils.ShowMessageDialog("LANraragi", "Another instance of the application is already running.", "Close");
                Exit();
                return;
            }

            AppNotificationManager.Default.NotificationInvoked += (sender, e) =>
            {
                // Do nothing?
            };

            AppNotificationManager.Default.Register();

            Popup = new KarenPopup();
            Popup.Closed += (sender, args) =>
            {
                TrayIcon.Dispose();
                AppNotificationManager.Default.Unregister();
            };

            TrayIcon = new(1, "Assets/favicon.ico", "LANraragi for Windows")
            {
                IsVisible = true
            };
            TrayIcon.Selected += Selected;
            TrayIcon.ContextMenu += Selected;

            if (Service.Settings.StartServerAutomatically)
            {
                WinUIUtils.ShowNotification("LANraragi", "LANraragi is starting automagically...");
                await Service.Server.StartAsync();
            }
            else
            {
                WinUIUtils.ShowNotification("LANraragi", "The Launcher is now running! Please click the icon in your Taskbar.");
            }

            if (Service.Settings.FirstLaunch)
            {
                await WinUIUtils.ShowMessageDialog("LANraragi", "Looks like this is your first time running the app! Please setup your Content Folder in the Settings.", "OK");
                new MainWindow().Activate();
                Service.Settings.FirstLaunch = false;
            }
        }

        private void Selected(TrayIcon sender, TrayIconEventArgs e)
        {
            unsafe
            {
                var hwnd = new HWND((void*)WindowNative.GetWindowHandle(Popup));
                PInvoke.GetCursorPos(out var point);

                var dpi = Popup.Content.XamlRoot?.RasterizationScale ?? (float)(PInvoke.GetDpiForWindow(hwnd) / (float)PInvoke.USER_DEFAULT_SCREEN_DPI);

                Popup.AppWindow.Resize(new SizeInt32((int)(266 * dpi), (int)(498 * dpi)));
                var size = Popup.AppWindow.Size;
                PInvoke.CalculatePopupWindowPosition(point, new SIZE(size.Width, size.Height), (uint)(TRACK_POPUP_MENU_FLAGS.TPM_CENTERALIGN | TRACK_POPUP_MENU_FLAGS.TPM_BOTTOMALIGN | TRACK_POPUP_MENU_FLAGS.TPM_WORKAREA), null, out var popupWindowPosition);

                Popup.AppWindow.Move(new PointInt32(popupWindowPosition.X, popupWindowPosition.Y));
                Popup.Show();
                Popup.Activate();

                PInvoke.SetForegroundWindow(hwnd);
            }
            e.Handled = true;
        }

    }
}
