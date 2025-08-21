using H.NotifyIcon;
using Karen.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using WinRT;
using WinRT.Interop;

namespace Karen.Views
{

    public sealed partial class KarenPopup : Window
    {

        private SUBCLASSPROC _subclassProc;

        public KarenPopup()
        {
            InitializeComponent();

            AppWindow.IsShownInSwitchers = false;
            var presenter = AppWindow.Presenter.As<OverlappedPresenter>();
            presenter.SetBorderAndTitleBar(true, false);
            presenter.IsAlwaysOnTop = true;
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;

            unsafe
            {
                HWND hwnd = new HWND((void*)WindowNative.GetWindowHandle(this));
                PInvoke.SetWindowSubclass(hwnd, _subclassProc = new SUBCLASSPROC(WindowSubclassProc), 0, 0);
            }
        }

        private LRESULT WindowSubclassProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
        {
            switch (uMsg)
            {
                case PInvoke.WM_ACTIVATE:
                    if (wParam == 0)
                    {
                        this.Hide();
                    }
                    break;
            }
            return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        private async void Button_Tapped(object sender, TappedRoutedEventArgs e)
        {
            sender.As<Button>().IsEnabled = false;
            await Service.Server.StopAsync();
            Close();
        }
    }
}
