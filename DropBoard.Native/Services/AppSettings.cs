using System;
using System.IO;
using System.Text.Json;

namespace DropBoard.Native.Services
{
    public class AppSettings
    {
        public int OpacityPercent { get; set; } = 0;
        public bool IsPinned { get; set; } = false;
        public double WindowLeft { get; set; } = 100;
        public double WindowTop { get; set; } = 100;
        public double WindowWidth { get; set; } = 1350;
        public double WindowHeight { get; set; } = 850;
        public string WindowState { get; set; } = "Normal";
        public double ArrangeGap { get; set; } = 24.0;
        public bool AutoHideDock { get; set; } = false;
        public string DockPosition { get; set; } = "top"; // "top" | "left"
        public string NavMode { get; set; } = "macos"; // "macos" | "windows" | "mouse"
        public double PanSensitivity { get; set; } = 1.0;
        public double ZoomSensitivity { get; set; } = 1.0;
        public bool InvertPan { get; set; } = false;
        public bool AutoSaveEnabled { get; set; } = true;
        public bool TransparentTitlebar { get; set; } = false;

        private static string SettingsFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DropBoard", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsFilePath)!;
                Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
        }
    }
}
