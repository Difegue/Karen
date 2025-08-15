using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
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
        private Window _window = null!;
        private TaskbarIcon TrayIcon = null!;

        public App()
        {
            InitializeComponent();
            Service.BuildServices();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var instance = AppInstance.FindOrRegisterForKey("Karen");
            if (!instance.IsCurrent)
            {
                PopupUtils.ShowMessageDialog("LANraragi", "Another instance of the application is already running.", "Close");
                Exit();
                return;
            }

            TrayIcon = Resources["TrayIcon"].As<TaskbarIcon>();
            TrayIcon.LeftClickCommand = OpenPopupCommand;
            TrayIcon.RightClickCommand = OpenPopupCommand;
            TrayIcon.IconSource = new BitmapImage(new Uri("ms-appx:///Assets/favicon.ico"));
            TrayIcon.ForceCreate();

            if (Service.Settings.StartServerAutomatically)
            {
                TrayIcon.ShowNotification("LANraragi", "LANraragi is starting automagically...");
                Service.Server.Start();
            }
            else
            {
                TrayIcon.ShowNotification("LANraragi", "The Launcher is now running! Please click the icon in your Taskbar.");
            }

            _window = new KarenPopup();
            _window.Closed += (sender, args) =>
            {
                TrayIcon.Dispose();
            };

            if (Service.Settings.FirstLaunch)
            {
                PopupUtils.ShowMessageDialog("LANraragi", "Looks like this is your first time running the app! Please setup your Content Folder in the Settings.", "OK");
                new MainWindow().Activate();
                Service.Settings.FirstLaunch = false;
            }
        }

        [RelayCommand]
        private void OpenPopup()
        {
            unsafe
            {
                var hwnd = new HWND((void*)WindowNative.GetWindowHandle(_window));
                var area = DisplayArea.GetFromWindowId(_window.AppWindow.Id, DisplayAreaFallback.Primary);
                PInvoke.GetCursorPos(out var point);

                var dpi = (float)(PInvoke.GetDpiForWindow(hwnd) / (float)PInvoke.USER_DEFAULT_SCREEN_DPI);

                _window.AppWindow.Resize(new SizeInt32((int)(266 * dpi), (int)(498 * dpi)));
                var size = _window.AppWindow.Size;

                int xPosition = point.X - size.Width / 2;
                int yPosition = point.Y - size.Height;

                yPosition = Math.Clamp(yPosition, area.WorkArea.Y, area.WorkArea.Y + area.WorkArea.Height - size.Height);
                xPosition = Math.Clamp(xPosition, area.WorkArea.X, area.WorkArea.X + area.WorkArea.Width - size.Width);

                _window.AppWindow.Move(new PointInt32(xPosition, yPosition));
                _window.Show();
                _window.Activate();

                PInvoke.SetForegroundWindow(hwnd);
            }
        }


    }
}
