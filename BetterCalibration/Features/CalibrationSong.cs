using JALib.Core;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Tools;
using Newtonsoft.Json.Linq;

namespace BetterCalibration.Features;

public class CalibrationSong() : Feature(Main.Instance, nameof(CalibrationSong), false, typeof(CalibrationSong), typeof(CalibrationSongSettings)) {
    private static CalibrationSongSettings _settings;
    private static float _bpm;
    private static float _lastTime;
    private static int _attempt;
    private string _pitch;
    private string _minimum;
    private string _repeat;

    protected override void OnGUI() {
        JALocalization localization = Main.Instance.Localization;
        SettingGUI settingGUI = Main.SettingGUI;
        settingGUI.AddSettingFloat(ref _settings.Pitch, 100, ref _pitch, localization["Song.Pitch"], 0);
        settingGUI.AddSettingToggleInt(ref _settings.Minimum, 0, ref _settings.UseMinimum, ref _minimum, localization["Song.Minimum"]);
        settingGUI.AddSettingInt(ref _settings.RepeatSong, 0, ref _repeat, localization["Song.Repeat"], 0);
    }

    [JAPatch("scrCalibrationPlanet", "Start", PatchType.Postfix, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "Start", PatchType.Postfix, false, MinVersion = 141)]
    public static void SetPitch(scrConductor ___conductor) {
        ___conductor.song.pitch = _settings.Pitch / 100;
        _bpm = (float) (1.3 * _settings.Pitch); // (BPM) 130 / (Default Pitch) 100 = 1.3
        ___conductor.song.loop = _settings.RepeatSong > 0;
        _attempt = 0;
    }

    [JAPatch("scrCalibrationPlanet", "CleanSlate", PatchType.Prefix, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "CleanSlate", PatchType.Prefix, false, MinVersion = 141)]
    public static void SetRestartPitch(scrConductor ___conductor) {
        ___conductor.song.pitch = _settings.Pitch / 100;
        _attempt = 0;
        ___conductor.song.loop = _settings.RepeatSong > 0;
    }

    [JAPatch("scrCalibrationPlanet", "PutDataPoint", PatchType.Prefix, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "PutDataPoint", PatchType.Prefix, false, MinVersion = 141)]
    public static void BpmPrefix(scrConductor ___conductor) {
        ___conductor.bpm = _bpm;
    }

    [JAPatch("scrCalibrationPlanet", "PutDataPoint", PatchType.Postfix, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "PutDataPoint", PatchType.Postfix, false, MinVersion = 141)]
    public static void BpmPostfix(scrConductor ___conductor) {
        ___conductor.bpm = 130;
    }
    
    [JAPatch("scrCalibrationPlanet", "PostSong", PatchType.Prefix, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "Calibrated", PatchType.Prefix, false, MinVersion = 141)]
    public static void ResetPitch(scrConductor ___conductor) {
        ___conductor.song.pitch = 1;
    }
    
    [JAPatch("scrCalibrationPlanet", "GetOffset", PatchType.Postfix, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "GetOffset", PatchType.Postfix, false, MinVersion = 141)]
    public static void SetOffset(ref double __result) {
        double time360 = 30000 / _bpm;
        double result = __result * 1000;
        while(result < _settings.Minimum) result += time360;
        __result = result / 1000;
    }
    
    [JAPatch("scrCalibrationPlanet", "Update", PatchType.Postfix, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "Update", PatchType.Postfix, false, MinVersion = 141)]
    public static void CheckRepeat(scrConductor ___conductor) {
        if(_lastTime > ___conductor.song.time && ++_attempt >= _settings.RepeatSong) ___conductor.song.loop = false;
        _lastTime = ___conductor.song.time;
    }

    private class CalibrationSongSettings : JASetting {
        public float Pitch = 100;
        public int Minimum;
        public bool UseMinimum;
        public int RepeatSong;

        public CalibrationSongSettings(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
            _settings = this;
        }
    }
}