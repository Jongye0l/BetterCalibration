using JALib.Core;
using JALib.Core.Setting;
using Newtonsoft.Json.Linq;

namespace BetterCalibration {
    public class Settings : JASetting {
        public static Settings Instance;
        public float Pitch = 100;
        [SettingIgnore] public string PitchString;
        public int Minimum = 0;
        [SettingIgnore] public string MinimumString;
        public bool UseMinimum = false;
        public bool ShowPopup = true;
        public int RepeatSong = 0;
        [SettingIgnore] public string RepeatString;
        public bool Detail = true;
        
        public Settings(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
            Instance ??= this;
        }
    }
}