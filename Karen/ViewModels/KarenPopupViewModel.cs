using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karen.Services;
using Karen.Views;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Karen.ViewModels
{
    public partial class KarenPopupViewModel : ObservableObject
    {
        private readonly Settings Settings;
        private readonly Server Server;

        [ObservableProperty]
        public partial bool IsRunning { get; set; }

        public KarenPopupViewModel(Settings settings, Server server)
        {
            Settings = settings;
            Server = server;
            IsRunning = server.IsRunning;
        }

        [RelayCommand]
        public void Start()
        {
            Server.Start();
            IsRunning = true;
        }

        [RelayCommand]
        public async Task Stop()
        {
            await Server.Stop();
            IsRunning = false;
        }

        [RelayCommand]
        public void ShowConsole()
        {
            Server.ShowConsole();
        }

        [RelayCommand]
        public async Task OpenClient()
        {
            await Launcher.LaunchUriAsync(new Uri($"http://localhost:{Settings.NetworkPort}"));
        }

        [RelayCommand]
        public async Task OpenAppFolder()
        {
            await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(AppContext.BaseDirectory));
        }

        [RelayCommand]
        public void OpenSettings()
        {
            new MainWindow().Activate();
        }
    }
}
