using BetterCalibration.Features;
using JALib.Core;
using UnityModManagerNet;

namespace BetterCalibration {
    public class Main : JAMod {
        public static Main Instance;

        private static void Setup(UnityModManager.ModEntry modEntry) {
            Instance = new Main(modEntry);
            //ModEntry = modEntry;
            //Logger = modEntry.Logger;
            //modEntry.OnToggle = OnToggle;
            //modEntry.OnGUI = SettingGUI.OnGUI;
            //_assembly = Assembly.GetExecutingAssembly();
            //Settings = Settings.CreateInstance();
        }

        private Main(UnityModManager.ModEntry modEntry) : base(modEntry, true, settingType: typeof(Settings)) {
            AddFeature(new CalibrationPopup());
            AddFeature(new CalibrationDetail());
            AddFeature(new CalibrationSong());
        }
    }
}