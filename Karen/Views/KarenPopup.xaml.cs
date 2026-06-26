using DesktopFlyouts;
using Karen.Services;
using Karen.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using WinRT;

namespace Karen.Views
{

    public sealed partial class KarenPopup : DesktopFlyout
    {

        private readonly KarenPopupViewModel Data;
        private readonly Server Server;

        public KarenPopup()
        {
            InitializeComponent();
            Data = Service.Services.GetRequiredService<KarenPopupViewModel>();
            Server = Service.Services.GetRequiredService<Server>();
        }

        private async void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            sender.As<Button>().IsEnabled = false;
            Hide();
            ((App)Application.Current).TrayIcon.Dispose();
            await Task.WhenAll(Data.Quit(), Task.Delay(267)); // Wait for server stop and/or animation duration
            Dispose();
            AppInstance.GetCurrent().UnregisterKey();
            AppNotificationManager.Default.Unregister();
            DispatcherQueue.EnqueueEventLoopExit();
        }
    }
}
