using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karen.Services;
using Microsoft.UI;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Threading.Tasks;

namespace Karen.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly Settings Settings;

        [ObservableProperty]
        public partial string ContentFolder { get; set; }

        partial void OnContentFolderChanged(string value) => Settings.ContentFolder = value;

        [ObservableProperty]
        public partial string ThumbnailFolder { get; set; }

        partial void OnThumbnailFolderChanged(string value) => Settings.ThumbnailFolder = value;

        [ObservableProperty]
        public partial int NetworkPort { get; set; }

        partial void OnNetworkPortChanged(int value) => Settings.NetworkPort = value;

        [ObservableProperty]
        public partial bool StartServerAutomatically { get; set; }

        partial void OnStartServerAutomaticallyChanged(bool value) => Settings.StartServerAutomatically = value;

        [ObservableProperty]
        public partial bool StartWithWindows { get; set; }

        partial void OnStartWithWindowsChanged(bool value)
        {
            if (value)
            {
                ActivationRegistrationManager.RegisterForStartupActivation("karen", string.Empty);
            }
            else
            {
                ActivationRegistrationManager.UnregisterForStartupActivation("karen");
            }
            Settings.StartWithWindows = value;
        }

        [ObservableProperty]
        public partial bool ForceDebugMode { get; set; }

        partial void OnForceDebugModeChanged(bool value) => Settings.ForceDebugMode = value;

        public MainWindowViewModel(Settings settings)
        {
            Settings = settings;
            ContentFolder = settings.ContentFolder;
            ThumbnailFolder = settings.ThumbnailFolder;
            NetworkPort = settings.NetworkPort;
            StartServerAutomatically = settings.StartServerAutomatically;
            StartWithWindows = settings.StartWithWindows;
            ForceDebugMode = settings.ForceDebugMode;
        }

        [RelayCommand]
        public async Task PickContentFolder(WindowId windowId)
        {
            var picker = new FolderPicker(windowId);
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.CommitButtonText = "Select your LANraragi Content Folder";

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
                ContentFolder = folder.Path;
        }

        [RelayCommand]
        public async Task PickThumbnailFolder(WindowId windowId)
        {
            var picker = new FolderPicker(windowId);
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.CommitButtonText = "Select your LANraragi Thumbnail Folder";

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
                ThumbnailFolder = folder.Path;
        }
    }
}
