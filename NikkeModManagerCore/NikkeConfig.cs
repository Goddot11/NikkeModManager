using System;
using System.IO;
using System.Text.Json;

namespace NikkeModManagerCore;

public static class NikkeConfig {

    private static string _configFile = "";
    static ConfigData _config { get; set; }

    static NikkeConfig() {
        LoadConfigData();
    }

    public static void LoadConfig(string path) {
        _configFile = path;
        LoadConfigData();
    }

    public static void LoadConfig(ConfigData data) {
        _config = data;
    }

    public static string GameDataDirectory {
        get => _config.GameDirectory;
        set {
            _config.GameDirectory = value;
            SaveConfigData();
        }
    }

    public static string ModDirectory {
        get => _config.ModDirectory;
        set { 
            _config.ModDirectory = value;
            SaveConfigData();
        }
    }

    public static string CacheDirectory {
        get => _config.CacheDirectory;
        set { 
            _config.CacheDirectory = value;
            SaveConfigData();
        }
    }

    public static bool UseMultiThreading {
        get => _config.UseMultiThreading;
        set {
            _config.UseMultiThreading = value;
            SaveConfigData();
        }
    }

    public static bool LoadGameData {
        get => _config.LoadGameData;
        set {
            _config.LoadGameData = value;
            SaveConfigData();
        }
    }

    private static void LoadConfigData() {
        if (_configFile != "" && File.Exists(_configFile)) {
            using FileStream file = File.Open(_configFile, FileMode.Open, FileAccess.Read);
            _config = JsonSerializer.Deserialize<ConfigData>(file);
        } else {
            _config = new ConfigData() {
                GameDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow"), "com_proximabeta", "NIKKE", "eb"),
                ModDirectory = "mods",
                CacheDirectory = "cache",

                UseMultiThreading = true,
                LoadGameData = true
            };
            SaveConfigData();
        }
    }

    private static void SaveConfigData() {
        if (_configFile == "") return;
        File.WriteAllText(_configFile, JsonSerializer.Serialize(_config));
    }

    public class ConfigData {
        public string ModDirectory { get; set; }
        public string CacheDirectory { get; set; }
        public string GameDirectory { get; set; }

        public bool UseMultiThreading{ get; set; }
        public bool LoadGameData{ get; set; }
    }
}