using Karen.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Karen.Services;

public class Settings
{

    private Dictionary<string, object> settings = new Dictionary<string, object>();

    private string SettingsPath = Path.Combine(AppContext.BaseDirectory, "settings.json");

    public Settings()
    {
        if (File.Exists(SettingsPath))
            settings = JsonSerializer.Deserialize(File.ReadAllText(SettingsPath), JsonSourceGenerationContext.Default.DictionaryStringObject)!;
        else
            MigrateUserConfig();
    }

    public void Save()
    {
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonSourceGenerationContext.Default.DictionaryStringObject));
    }

    public T? GetObject<T>([CallerMemberName] string? key = null) => GetObject<T>(default, key);

    [return: NotNullIfNotNull("def")]
    public T? GetObject<T>(T? def, [CallerMemberName] string? key = null)
    {
        if (!settings.ContainsKey(key!))
            return def;
        var val = settings[key!];
        return val != null ? (T)val : def;
    }

    public void StoreObject(object? obj, [CallerMemberName] string key = "")
    {
        if (obj != null)
            settings[key!] = obj;
    }

    public string ContentFolder
    {
        get => GetObject("");
        set => StoreObject(value);
    }
    public string ThumbnailFolder
    {
        get => GetObject("");
        set => StoreObject(value);
    }
    public bool StartServerAutomatically
    {
        get => GetObject(false);
        set => StoreObject(value);
    }
    public bool StartWithWindows
    {
        get => GetObject(false);
        set => StoreObject(value);
    }
    public int NetworkPort
    {
        get => GetObject(3000);
        set => StoreObject(value);
    }
    public bool FirstLaunch
    {
        get => GetObject(true);
        set => StoreObject(value);
    }
    public bool ForceDebugMode
    {
        get => GetObject(false);
        set => StoreObject(value);
    }

    public void MigrateUserConfig()
    {
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

        foreach (var element in configXml.Element("configuration")!.Element("userSettings")!.Element("Karen.Properties.Settings")!.Elements("setting"))
        {
            var name = element.Attribute("name")!.Value;
            var value = element.Value;
            switch (name)
            {
                case "ContentFolder":
                    ContentFolder = value;
                    break;
                case "StartServerAutomatically":
                    StartServerAutomatically = bool.Parse(value);
                    break;
                case "StartWithWindows":
                    StartWithWindows = bool.Parse(value);
                    break;
                case "NetworkPort":
                    NetworkPort = int.Parse(value);
                    break;
                case "ForceDebugMode":
                    ForceDebugMode = bool.Parse(value);
                    break;
                case "ThumbnailFolder":
                    ThumbnailFolder = value;
                    break;
            }
        }

        FirstLaunch = false;

        Save();
        //foreach (var f in files)
        //f.Delete();
    }

}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, WriteIndented = true, NumberHandling = JsonNumberHandling.AllowReadingFromString, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, Converters = new Type[] { typeof(ObjectToInferredTypesConverter) })]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
public partial class JsonSourceGenerationContext : JsonSerializerContext
{

}
