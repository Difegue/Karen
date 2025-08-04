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

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            Data = Service.Services.GetRequiredService<MainWindowViewModel>();

            AppWindow.SetIcon("Assets/favicon.ico");
            var presenter = AppWindow.Presenter.As<OverlappedPresenter>();
            presenter.PreferredMinimumWidth = 800;
            presenter.PreferredMinimumHeight = 480;
            AppWindow.Resize(new SizeInt32(900, 980));
            hWnd = WindowNative.GetWindowHandle(this);
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // TODO Doing this prevents the window from closing but also makes it impossible to ever close properly afterwards.
            // Doesn't look like WinUI implements Closing, so we might just need to block server startup if a content folder isn't set. 
            //if (string.IsNullOrWhiteSpace(Data.ContentFolder))
                //args.Handled = true;

            Service.Settings.Save();
        }
    }
}
