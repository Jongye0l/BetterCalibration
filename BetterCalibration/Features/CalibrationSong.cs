using JALib.Core;
using JALib.Core.Patch;

namespace BetterCalibration.Features;

public class CalibrationSong() : Feature(Main.Instance, nameof(CalibrationSong), false, typeof(CalibrationSong)) {
    
    private static Settings Settings => Settings.Instance;
    private static float _bpm;
    private static float _lastTime;
    private static int _attempt;
    
    [JAPatch("CalibrationSong.SetPitch", typeof(scrCalibrationPlanet), "Start", PatchType.Postfix, false)]
    public static void SetPitch(scrConductor ___conductor) {
        ___conductor.song.pitch = Settings.Pitch / 100;
        _bpm = (float) (1.3 * Settings.Pitch); // (BPM) 130 / (Default Pitch) 100 = 1.3
        ___conductor.song.loop = Settings.RepeatSong > 0;
        _attempt = 0;
    }

    [JAPatch("CalibrationSong.SetRestartPitch", typeof(scrCalibrationPlanet), "CleanSlate", PatchType.Prefix, false)]
    public static void SetRestartPitch(scrConductor ___conductor) {
        ___conductor.song.pitch = Settings.Pitch / 100;
        _attempt = 0;
        ___conductor.song.loop = Settings.RepeatSong > 0;
    }

    [JAPatch("CalibrationSong.SetBpm", typeof(scrCalibrationPlanet), "PutDataPoint", PatchType.Prefix, false)]
    public static void BpmPrefix(scrConductor ___conductor) {
        ___conductor.bpm = _bpm;
    }

    [JAPatch("CalibrationSong.SetBpm", typeof(scrCalibrationPlanet), "PutDataPoint", PatchType.Postfix, false)]
    public static void BpmPostfix(scrConductor ___conductor) {
        ___conductor.bpm = 130;
    }
    
    [JAPatch("CalibrationSong.ResetPitch", typeof(scrCalibrationPlanet), "PostSong", PatchType.Prefix, false)]
    public static void ResetPitch(scrConductor ___conductor) {
        ___conductor.song.pitch = 1;
    }
    
    [JAPatch("CalibrationSong.SetOffset", typeof(scrCalibrationPlanet), "GetOffset", PatchType.Postfix, false)]
    public static void SetOffset(ref double __result) {
        double time360 = 30000 / _bpm;
        double result = __result * 1000;
        while(result < Settings.Minimum) result += time360;
        __result = result / 1000;
    }
    
    [JAPatch("CalibrationSong.CheckRepeat", typeof(scrCalibrationPlanet), "Update", PatchType.Postfix, false)]
    public static void CheckRepeat(scrConductor ___conductor) {
        if(_lastTime > ___conductor.song.time && ++_attempt >= Settings.RepeatSong) ___conductor.song.loop = false;
        _lastTime = ___conductor.song.time;
    }
}