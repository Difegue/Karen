using Karen.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Karen.Views
{
    public sealed partial class ConsoleWindow : Window
    {
        private VirtualConsole Data;

        public ConsoleWindow()
        {
            InitializeComponent();
            Data = Service.Services.GetRequiredService<VirtualConsole>();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            AppWindow.SetIcon("Assets/favicon.ico");

            if (Data.Lines.Count > 0)
                LogListView.ScrollIntoView(Data.Lines.Last());
        }

    }
}
