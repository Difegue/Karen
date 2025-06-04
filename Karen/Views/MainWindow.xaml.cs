using Karen.Services;
using Karen.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT;
using WinRT.Interop;

namespace Karen.Views
{

    public sealed partial class MainWindow : Window
    {
        private MainWindowViewModel Data;

        private long hWnd;

        public MainWindow()
        {
            InitializeComponent();
            Data = Service.Services.GetRequiredService<MainWindowViewModel>();

            AppWindow.SetIcon("Assets/favicon.ico");
            var presenter = AppWindow.Presenter.As<OverlappedPresenter>();
            presenter.PreferredMinimumWidth = 800;
            presenter.PreferredMinimumHeight = 480;
            AppWindow.Resize(new SizeInt32(900, 680));
            hWnd = WindowNative.GetWindowHandle(this);
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(Data.ContentFolder))
                args.Handled = true;

            Service.Settings.Save();
        }
    }
}
