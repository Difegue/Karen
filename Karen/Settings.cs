using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Windows.Storage;

namespace Karen
{
    public class Settings : INotifyPropertyChanged
    {
        private static Settings _instance = new Settings();
        private ApplicationDataContainer _msixSettings = new DesktopBridge.Helpers().IsRunningAsUwp() ? ApplicationData.Current.LocalSettings : null;

        public static Settings Default => _instance;

        public event PropertyChangedEventHandler PropertyChanged;

        public T GetObjectLocal<T>([CallerMemberName] string key = "") => GetObjectLocal<T>(default, key);

        public T GetObjectLocal<T>(T def, [CallerMemberName] string key = "")
        {
            // If msixSettings are null, we don't have package identity -> fallback to user.config
            var val = _msixSettings?.Values[key] ?? Properties.Settings.Default[key];
            return val != null ? (T)val : def;
        }

        public void StoreObjectLocal(object obj, [CallerMemberName] string key = "")
        {
            if (_msixSettings != null)
            {
                _msixSettings.Values[key] = obj;
            }
            else
            {
                Properties.Settings.Default[key] = obj;
                Properties.Settings.Default.Save();
            }
        } 

        public string ContentFolder
        {
            get => GetObjectLocal("");
            set
            {
                StoreObjectLocal(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ContentFolder"));
            }
        }
        public string ThumbnailFolder
        {
            get => GetObjectLocal("");
            set
            {
                StoreObjectLocal(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ThumbnailFolder"));
            }
        }
        public bool StartServerAutomatically
        {
            get => GetObjectLocal(false);
            set => StoreObjectLocal(value);
        }
        public bool StartWithWindows
        {
            get => GetObjectLocal(false);
            set => StoreObjectLocal(value);
        }
        public string NetworkPort
        {
            get => GetObjectLocal("3000");
            set => StoreObjectLocal(value);
        }
        public bool FirstLaunch
        {
            get => GetObjectLocal(true);
            set => StoreObjectLocal(value);
        }
        public bool ForceDebugMode
        {
            get => GetObjectLocal(false);
            set => StoreObjectLocal(value);
        }
        public bool UseWSL2
        {
            get => GetObjectLocal(false);
            set => StoreObjectLocal(value);
        }
        public string Version
        {
            get => GetObjectLocal("");
            set => StoreObjectLocal(value);
        }
        public bool Karen
        {
            get => GetObjectLocal(false);
            set => StoreObjectLocal(value);
        }

        public void MigrateUserConfigToMSIX()
        {
            if (_msixSettings == null)
            {
                // Not running under package identity, do not migrate
                return;
            }

            var searchRoot = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Karen"));

            if (!searchRoot.Exists)
                return;

            var files = searchRoot.GetFiles("user.config", SearchOption.AllDirectories).OrderByDescending(f => f.LastWriteTime);
            if (files.Count() == 0)
                return;

            var file = files.FirstOrDefault();
            if (file == null)
                return;

            var configXml = XDocument.Load(file.FullName);

            _instance.FirstLaunch = false;

            foreach (var element in configXml.Element("configuration").Element("userSettings").Element("Karen.Properties.Settings").Elements("setting"))
            {
                var name = element.Attribute("name").Value;
                var value = element.Value;
                switch (name)
                {
                    case "ContentFolder":
                        _instance.ContentFolder = value;
                        break;
                    case "StartServerAutomatically":
                        _instance.StartServerAutomatically = bool.Parse(value);
                        break;
                    case "StartWithWindows":
                        _instance.StartWithWindows = bool.Parse(value);
                        break;
                    case "NetworkPort":
                        _instance.NetworkPort = value;
                        break;
                    case "ForceDebugMode":
                        _instance.ForceDebugMode = bool.Parse(value);
                        break;
                    case "UseWSL2":
                        _instance.UseWSL2 = bool.Parse(value);
                        break;
                    case "ThumbnailFolder":
                        _instance.ThumbnailFolder = value;
                        break;
                }
            }
            foreach (var f in files)
                f.Delete();
        }

    }

}