using System;
using System.IO;
using System.Text.Json;

namespace NextCmixGui.Core
{
    public class AppSettings
    {
        public string Action { get; set; } = "Compress";
        public string Version { get; set; } = "";
        public bool UseDict { get; set; } = true;
        public bool ShowCmd { get; set; } = false;
        public string Language { get; set; } = "English";
    }

    public static class ConfigManager
    {
        private static readonly string SettingsFile = "settings.json";

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch
            {
            }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch
            {
            }
        }
    }
}
