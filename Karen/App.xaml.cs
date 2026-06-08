using DesktopFlyouts;
using Karen.Services;
using Karen.Util;
using Karen.Views;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using Windows.Win32;
using Windows.Win32.UI.Shell;
using WinUIEx;

namespace Karen
{

    public partial class App : Application
    {
        private KarenPopup Popup = null!;
        public TrayIcon TrayIcon { get; private set; } = null!;

        public App()
        {
            InitializeComponent();
            DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown;
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
                new SettingsWindow().Activate();
                Service.Settings.FirstLaunch = false;
            }
        }

        private void Selected(TrayIcon sender, TrayIconEventArgs e)
        {
            unsafe
            {
                // Figure out where the taskbar is and place the flyout in the correct location
                APPBARDATA appBarData = new()
                {
                    cbSize = (uint)sizeof(APPBARDATA)
                };
                PInvoke.SHAppBarMessage(PInvoke.ABM_GETTASKBARPOS, &appBarData);

                switch(appBarData.uEdge)
                {
                    case PInvoke.ABE_LEFT:
                        Popup.PopupDirection = DesktopFlyoutPopupDirection.LeftToRight;
                        Popup.Placement = DesktopFlyoutPlacementMode.BottomLeft;
                        break;
                    case PInvoke.ABE_TOP:
                        Popup.PopupDirection = DesktopFlyoutPopupDirection.TopToBottom;
                        Popup.Placement = DesktopFlyoutPlacementMode.TopRight;
                        break;
                    case PInvoke.ABE_RIGHT:
                        Popup.PopupDirection = DesktopFlyoutPopupDirection.RightToLeft;
                        Popup.Placement = DesktopFlyoutPlacementMode.BottomRight;
                        break;
                    case PInvoke.ABE_BOTTOM:
                        Popup.PopupDirection = DesktopFlyoutPopupDirection.BottomToTop;
                        Popup.Placement = DesktopFlyoutPlacementMode.BottomRight;
                        break;
                }
            }

            if (Popup.IsOpen)
                Popup.Hide();
            else
                Popup.Show();
            e.Handled = true;
        }

    }
}
