using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using WinRT;
using WinUIEx;

namespace Karen.Views.Dialogs
{

    public sealed partial class GenericDialog : Window
    {

        public object DialogContent { get; set; }
        public object PrimaryButtonText { get; set; }

        private TaskCompletionSource tsc = new();

        public GenericDialog(string title, object content, string primaryButtonText)
        {
            Title = title;
            DialogContent = content;
            PrimaryButtonText = primaryButtonText;

            InitializeComponent();

            ExtendsContentIntoTitleBar = true;

            AppWindow.SetIcon("Assets/favicon.ico");

            var presenter = AppWindow.Presenter.As<OverlappedPresenter>();
            presenter.SetBorderAndTitleBar(true, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        public Task ShowAsync()
        {
            return tsc.Task;
        }

        private void BackgroundElement_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.CenterOnScreen(e.NewSize.Width + 16, e.NewSize.Height + 9);
            Activate();
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            tsc.SetResult();
        }
    }
}
