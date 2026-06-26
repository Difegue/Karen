using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karen.Services;
using Karen.Views;
using Windows.Storage;
using Windows.System;

namespace Karen.ViewModels
{
    public partial class KarenPopupViewModel : ObservableObject
    {
        private readonly Settings Settings;
        private readonly Server Server;
        private readonly VirtualConsole VirtualConsole;

        private SettingsWindow? SettingsWindow;

        public KarenPopupViewModel(Settings settings, Server server, VirtualConsole virtualConsole)
        {
            Settings = settings;
            Server = server;
            VirtualConsole = virtualConsole;
        }

        [RelayCommand]
        public async Task StartAsync()
        {
            await Server.StartAsync();
        }

        [RelayCommand]
        public async Task StopAsync()
        {
            await Server.StopAsync();
        }

        [RelayCommand]
        public async Task StartStopAsync(bool startStop)
        {
            if (startStop)
            {
                await Server.StartAsync();
            }
            else
            {
                await Server.StopAsync();
            }
        }

        [RelayCommand]
        public void ShowConsole()
        {
            VirtualConsole.ShowConsole();
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
            if (SettingsWindow != null)
            {
                SettingsWindow.Activate();
            }
            else
            {
                SettingsWindow = new();
                SettingsWindow.Closed += (sender, e) =>
                {
                    SettingsWindow = null;
                };
                SettingsWindow.Activate();
            }
        }

        public async Task Quit()
        {
            SettingsWindow?.Close();
            VirtualConsole.CloseConsole();
            await Server.StopAsync();
        }

    }
}
