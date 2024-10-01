using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ADOFAI;
using BetterCalibration.DoubleFeaturePatch;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Tools;
using JALib.Tools.ByteTool;
using MonsterLove.StateMachine;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BetterCalibration.Features;

public class TimingLogger() : Feature(Main.Instance, nameof(TimingLogger), true, typeof(TimingLogger), typeof(TimingLoggerSettings)) {
    private static bool _logging;
    private static Dictionary<byte[], List<float>> _timings;
    private static long _lastUseTime;
    private static TimingLoggerSettings _settings;
    private string _maxTimings;
    private string _maxTimingsPerMap;

    protected override void OnEnable() {
        Timing.Instance.AddPatch(this);
    }

    protected override void OnDisable() {
        Timing.Instance.RemovePatch(this);
    }

    protected override void OnGUI() {
        JALocalization localization = Main.Instance.Localization;
        SettingGUI settingGUI = Main.SettingGUI;
        settingGUI.AddSettingInt(ref _settings.MaxTimings, 15, ref _maxTimings, localization["TimingLogger.MaxTimings"]);
        settingGUI.AddSettingInt(ref _settings.MaxTimingsPerMap, 5, ref _maxTimingsPerMap, localization["TimingLogger.MaxTimingsPerMap"]);
        bool inGame = ADOBase.controller && ADOBase.controller.gameworld;
        List<float> mapTimings = !inGame ? null : GetTiming(GetMapHash());
        GUILayout.BeginHorizontal();
            GUILayout.Label(localization["TimingLogger.PrevOffset"] + ": " +
                            (inGame ? mapTimings.Count == 0 ? localization["TimingLogger.NoTimings"] : mapTimings[0] + "" :
                                 localization["TimingLogger.NotOpenMap"]));
            if(inGame && mapTimings.Count > 0 && GUILayout.Button(localization["TimingLogger.SetTiming"])) {
                if(FloatOffset.Instance.Enabled) {
                    FloatOffset.Instance.Offset = mapTimings[0];
                    return;
                }
                if(scrConductor.currentPreset.inputOffset != (int) mapTimings[0]) {
                    scrConductor.currentPreset.inputOffset = (int) mapTimings[0];
                    scrConductor.SaveCurrentPreset();
                }
            }
            GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(localization["TimingLogger.AllTimings"]);
                    GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                List<float> allTimings = GetTiming([]);
                GUIContent buttonContent = new(localization["TimingLogger.SetTiming"]);
                Vector2 buttonSize = GUI.skin.button.CalcSize(buttonContent);
                GUILayout.BeginVertical(new GUIStyle(GUI.skin.box));
                    if(allTimings.Count <= 1) {
                        GUILayout.BeginVertical();
                            GUILayout.Label(localization["TimingLogger.NoTimings"]);
                        GUILayout.EndVertical();
                    } else {
                        GUILayout.BeginHorizontal();
                            GUILayout.BeginVertical();
                                foreach(float timing in allTimings.Skip(1)) GUILayout.Label(FloatOffset.Instance.Enabled ? timing.ToString("0.##")
                                                                                        : Mathf.RoundToInt(timing).ToString(), GUILayout.Height(buttonSize.y));
                            GUILayout.EndVertical();
                            GUILayout.FlexibleSpace();
                            GUILayout.BeginVertical();
                                foreach(float timing in allTimings.Skip(1).Where(_ => GUILayout.Button(buttonContent))) {
                                    if(FloatOffset.Instance.Enabled) {
                                        FloatOffset.Instance.Offset = timing;
                                        continue;
                                    }
                                    int roundedTiming = Mathf.RoundToInt(timing);
                                    if(scrConductor.currentPreset.inputOffset == roundedTiming) continue;
                                    scrConductor.currentPreset.inputOffset = roundedTiming;
                                    scrConductor.SaveCurrentPreset();
                                }
                            GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                    }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(localization["TimingLogger.MapTimings"]);
                    GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.BeginVertical(new GUIStyle(GUI.skin.box));
                    if(!inGame) {
                        GUILayout.BeginVertical();
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(localization["TimingLogger.NotOpenMap"]);
                            GUILayout.FlexibleSpace();
                        GUILayout.EndVertical();
                    } else if(mapTimings.Count <= 1) {
                        GUILayout.BeginVertical();
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(localization["TimingLogger.NoTimings"]);
                            GUILayout.FlexibleSpace();
                        GUILayout.EndVertical();
                    } else {
                        GUILayout.BeginHorizontal();
                            GUILayout.BeginVertical();
                                foreach(float timing in mapTimings.Skip(1)) GUILayout.Label(FloatOffset.Instance.Enabled ? timing.ToString("0.##")
                                                                                        : Mathf.RoundToInt(timing).ToString(), GUILayout.Height(buttonSize.y));
                            GUILayout.EndVertical();
                            GUILayout.FlexibleSpace();
                            GUILayout.BeginVertical();
                                foreach(float timing in mapTimings.Skip(1).Where(_ => GUILayout.Button(buttonContent))) {
                                    if(FloatOffset.Instance.Enabled) {
                                        FloatOffset.Instance.Offset = timing;
                                        continue;
                                    }
                                    int roundedTiming = Mathf.RoundToInt(timing);
                                    if(scrConductor.currentPreset.inputOffset == roundedTiming) continue;
                                    scrConductor.currentPreset.inputOffset = roundedTiming;
                                    scrConductor.SaveCurrentPreset();
                                }
                            GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                    }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    [JAPatch(typeof(StateBehaviour), "ChangeState", PatchType.Postfix, true, ArgumentTypesType = [typeof(Enum)])]
    public static void OnChangeState(Enum newState) {
        States states = (States) newState;
        if(states == States.Start) {
            _logging = false;
            GetTiming(GetMapHash())[0] = FloatOffset.Instance.Enabled ? FloatOffset.Instance.Offset : scrConductor.currentPreset.inputOffset;
        } else if(states != States.Fail2) LogTiming();
    }

    [JAPatch(typeof(scrController), "TogglePauseGame", PatchType.Postfix, true)]
    public static void LogTiming() {
        if(_logging || Timing.Timings.Count == 0) return;
        byte[] mapHash = GetMapHash();
        float timing = scrConductor.currentPreset.inputOffset + Timing.Timings.Average();
        AddTiming(mapHash, timing, _settings.MaxTimingsPerMap);
        AddTiming([], timing, _settings.MaxTimings);
        _logging = true;
    }

    private static byte[] GetMapHash() {
        return MD5.Create().ComputeHash(ADOBase.isOfficialLevel ? Encoding.UTF8.GetBytes(ADOBase.currentLevel) : GetHash());
    }

    private static byte[] GetHash() {
        using MemoryStream memoryStream = new();
        scrLevelMaker lm = ADOBase.lm;
        if(lm.isOldLevel) memoryStream.WriteUTF(lm.leveldata);
        else memoryStream.WriteObject(lm.floorAngles);
        foreach(LevelEvent levelEvent in ADOBase.customLevel.events) {
            switch(levelEvent.eventType) {
                case LevelEventType.SetSpeed:
                    memoryStream.WriteInt(levelEvent.floor);
                    memoryStream.WriteByte(0);
                    levelEvent.Get<SpeedType>("speedType");
                    memoryStream.WriteByte((byte) levelEvent["speedType"]);
                    if((SpeedType) levelEvent["speedType"] == SpeedType.Bpm) memoryStream.WriteFloat((float) levelEvent["beatsPerMinute"]);
                    else memoryStream.WriteFloat((float) levelEvent["bpmMultiplier"]);
                    break;
                case LevelEventType.Twirl:
                    memoryStream.WriteInt(levelEvent.floor);
                    memoryStream.WriteByte(1);
                    break;
                case LevelEventType.Hold:
                    memoryStream.WriteInt(levelEvent.floor);
                    memoryStream.WriteByte(2);
                    memoryStream.WriteFloat((float) levelEvent["duration"]);
                    break;
                case LevelEventType.MultiPlanet:
                    memoryStream.WriteInt(levelEvent.floor);
                    memoryStream.WriteByte(3);
                    memoryStream.WriteByte((byte) levelEvent["planets"]);
                    break;
                case LevelEventType.Pause:
                    memoryStream.WriteInt(levelEvent.floor);
                    memoryStream.WriteByte(4);
                    memoryStream.WriteFloat((float) levelEvent["duration"]);
                    break;
                case LevelEventType.AutoPlayTiles:
                    memoryStream.WriteInt(levelEvent.floor);
                    memoryStream.WriteByte(5);
                    memoryStream.WriteBoolean((bool) levelEvent["enabled"]);
                    break;
                case LevelEventType.ScaleMargin:
                    memoryStream.WriteInt(levelEvent.floor);
                    memoryStream.WriteByte(6);
                    memoryStream.WriteFloat((float) levelEvent["scale"]);
                    break;
                case LevelEventType.Multitap:
                    memoryStream.WriteInt(levelEvent.floor);
                    memoryStream.WriteByte(7);
                    memoryStream.WriteFloat((float) levelEvent["taps"]);
                    break;
                case LevelEventType.KillPlayer:
                    memoryStream.WriteInt(levelEvent.floor);
                    memoryStream.WriteByte(8);
                    break;
            }
        }
        return memoryStream.ToArray();
    }

    public static Dictionary<byte[], List<float>> GetTimings() {
        if(_timings == null) {
            _timings = new Dictionary<byte[], List<float>>();
            if(File.Exists(Path.Combine(Main.Instance.Path, "Timings.dat"))) {
                using FileStream fileStream = File.OpenRead(Path.Combine(Main.Instance.Path, "Timings.dat"));
                try {
                    using Stream stream = Zipper.UnDeflateToMemoryStream(fileStream);
                    int count = stream.ReadInt();
                    for(int i = 0; i < count; i++) {
                        byte[] key = stream.ReadBytes(16);
                        List<float> value = [];
                        int valueCount = stream.ReadInt();
                        for(int j = 0; j < valueCount; j++) value.Add(stream.ReadFloat());
                        _timings[key] = value;
                    }
                } catch (Exception e) {
                    Main.Instance.LogException(e);
                    SaveTiming();
                }
            }
            Deleter();
        }
        _lastUseTime = DateTime.Now.Ticks;
        return _timings;
    }

    private static async void Deleter() {
        await Task.Delay(60000);
        long currentTime = DateTime.Now.Ticks;
        while(currentTime - _lastUseTime < 600000000) {
            await Task.Delay((int) (60000 - (currentTime - _lastUseTime) / 10000));
            currentTime = DateTime.Now.Ticks;
        }
        _timings = null;
    }

    public static void AddTiming(byte[] mapHash, float timing, int maxTiming) {
        _timings = GetTimings();
        List<float> timings;
        if(!_timings.TryGetValue(mapHash, out List<float> timing1)) timings = _timings[mapHash] = [0f];
        else timings = timing1;
        timings.Add(timing);
        if(timings.Count > maxTiming + 1) timings.RemoveAt(1);
        SaveTiming();
    }

    public static List<float> GetTiming(byte[] mapHash) {
        return GetTimings().TryGetValue(mapHash, out List<float> timing) ? timing : [0f];
    }

    public static void SaveTiming() {
        using MemoryStream memoryStream = new();
        memoryStream.WriteInt(_timings.Count);
        foreach(KeyValuePair<byte[],List<float>> valuePair in _timings) {
            memoryStream.Write(valuePair.Key);
            memoryStream.WriteInt(valuePair.Value.Count);
            foreach(float timing in valuePair.Value) memoryStream.WriteFloat(timing);
        }
        using FileStream fileStream = File.OpenWrite(Path.Combine(Main.Instance.Path, "Timings.dat"));
        fileStream.Deflate(memoryStream);
    }

    private class TimingLoggerSettings : JASetting {
        public int MaxTimings = 15;
        public int MaxTimingsPerMap = 5;

        public TimingLoggerSettings(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
            _settings = this;
        }
    }
}