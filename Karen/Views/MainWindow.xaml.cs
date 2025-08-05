using Karen.Services;
using Karen.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
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

            unsafe
            {
                hWnd = WindowNative.GetWindowHandle(this);
                HWND hwnd = new HWND((void*)hWnd);
                var dpi = (float)(PInvoke.GetDpiForWindow(hwnd) / 96f);

                var presenter = AppWindow.Presenter.As<OverlappedPresenter>();
                presenter.PreferredMinimumWidth = (int)(800 * dpi);
                presenter.PreferredMinimumHeight = (int)(480 * dpi);
                AppWindow.Resize(new SizeInt32((int)(900 * dpi), (int)(680 * dpi)));
            }
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
