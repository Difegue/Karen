using Karen.Services;
using Karen.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using System;
using Windows.Graphics;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using WinRT;
using WinRT.Interop;

namespace Karen.Views
{

    public sealed partial class MainWindow : Window
    {
        private MainWindowViewModel Data;
        private SUBCLASSPROC _subclassProc;
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
                var dpi = (float)(PInvoke.GetDpiForWindow(hwnd) / (float)PInvoke.USER_DEFAULT_SCREEN_DPI);

                var presenter = AppWindow.Presenter.As<OverlappedPresenter>();
                presenter.PreferredMinimumWidth = (int)(800 * dpi);
                presenter.PreferredMinimumHeight = (int)(480 * dpi);
                AppWindow.Resize(new SizeInt32((int)(900 * dpi), (int)(680 * dpi)));

                PInvoke.SetWindowSubclass(hwnd, _subclassProc = new SUBCLASSPROC(WindowSubclassProc), 0, 0);
            }
        }

        private LRESULT WindowSubclassProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
        {
            switch (uMsg)
            {
                case PInvoke.WM_DPICHANGED:
                    var dpi = (float)((wParam >> 16) / (float)PInvoke.USER_DEFAULT_SCREEN_DPI);
                    var presenter = AppWindow.Presenter.As<OverlappedPresenter>();
                    presenter.PreferredMinimumWidth = (int)(800 * dpi);
                    presenter.PreferredMinimumHeight = (int)(480 * dpi);
                    break;
            }
            return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // TODO Doing this prevents the window from closing but also makes it impossible to ever close properly afterwards.
            // Doesn't look like WinUI implements Closing, so we might just need to block server startup if a content folder isn't set. 
            //if (string.IsNullOrWhiteSpace(Data.ContentFolder))
            //args.Handled = true;

            Service.Settings.Save();
        }

        private async void Hyperlink_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store://pdp/?ProductId=9MZ6BWWVSWJH&amp;mode=mini"));
        }
    }
}
