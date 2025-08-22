using H.NotifyIcon;
using H.NotifyIcon.Core;
using Karen.Services;
using Karen.Util;
using Karen.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppLifecycle;
using System;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinRT;
using WinRT.Interop;

namespace Karen
{

    public partial class App : Application
    {
        private KarenPopup Popup = null!;
        private TaskbarIcon TrayIcon = null!;

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
                await PopupUtils.ShowMessageDialog("LANraragi", "Another instance of the application is already running.", "Close");
                Exit();
                return;
            }

            TrayIcon = Resources["TrayIcon"].As<TaskbarIcon>();
            TrayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///Assets/favicon.ico"));
            TrayIcon.ForceCreate();

            TrayIcon.TrayIcon.MessageWindow.MouseEventReceived += MessageWindow_MouseEventReceived;

            if (Service.Settings.StartServerAutomatically)
            {
                TrayIcon.ShowNotification("LANraragi", "LANraragi is starting automagically...");
                await Service.Server.StartAsync();
            }
            else
            {
                TrayIcon.ShowNotification("LANraragi", "The Launcher is now running! Please click the icon in your Taskbar.");
            }

            Popup = new KarenPopup();
            Popup.Closed += (sender, args) =>
            {
                TrayIcon.Dispose();
            };

            if (Service.Settings.FirstLaunch)
            {
                await PopupUtils.ShowMessageDialog("LANraragi", "Looks like this is your first time running the app! Please setup your Content Folder in the Settings.", "OK");
                new MainWindow().Activate();
                Service.Settings.FirstLaunch = false;
            }
        }

        private void MessageWindow_MouseEventReceived(object? sender, MessageWindow.MouseEventReceivedEventArgs e)
        {
            if (e.MouseEvent == MouseEvent.IconLeftMouseUp || e.MouseEvent == MouseEvent.IconRightMouseUp)
            {
                unsafe
                {
                    var hwnd = new HWND((void*)WindowNative.GetWindowHandle(Popup));
                    var point = e.Point;
                    var area = DisplayArea.GetFromPoint(new PointInt32(point.X, point.Y), DisplayAreaFallback.Primary);

                    var dpi = Popup.Content.XamlRoot?.RasterizationScale ?? (float)(PInvoke.GetDpiForWindow(hwnd) / (float)PInvoke.USER_DEFAULT_SCREEN_DPI);

                    Popup.AppWindow.Resize(new SizeInt32((int)(266 * dpi), (int)(498 * dpi)));
                    var size = Popup.AppWindow.Size;

                    int xPosition = point.X - size.Width / 2;
                    int yPosition = point.Y - size.Height;

                    yPosition = Math.Clamp(yPosition, area.WorkArea.Y, area.WorkArea.Y + area.WorkArea.Height - size.Height);
                    xPosition = Math.Clamp(xPosition, area.WorkArea.X, area.WorkArea.X + area.WorkArea.Width - size.Width);

                    Popup.AppWindow.Move(new PointInt32(xPosition, yPosition));
                    Popup.Show();
                    Popup.Activate();

                    PInvoke.SetForegroundWindow(hwnd);
                }
            }
        }

    }
}
