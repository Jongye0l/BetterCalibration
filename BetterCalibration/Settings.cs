using System.IO;
using Newtonsoft.Json;

namespace BetterCalibration {
    public class Settings {
        private static readonly string SettingPath = Path.Combine(Main.ModEntry.Path, "Settings.json");
        public static Settings Instance;
        public float Pitch = 100;
        [JsonIgnore]
        public string PitchString;
        public int Minimum = 0;
        [JsonIgnore]
        public string MinimumString;
        public bool UseMinimum = false;
        public bool ShowPopup = true;
        
        public static Settings CreateInstance() {
            Instance = File.Exists(SettingPath) ? JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SettingPath)) : new Settings();
            return Instance;
        }

        public void Save() {
            File.WriteAllText(SettingPath, JsonConvert.SerializeObject(Instance, Formatting.Indented));
        }
    }
}