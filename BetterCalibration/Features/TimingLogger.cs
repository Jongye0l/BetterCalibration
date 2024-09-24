﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ADOFAI;
using BetterCalibration.DoubleFeaturePatch;
using HarmonyLib;
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
        GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                    GUILayout.Label(localization["TimingLogger.AllTimings"]);
                GUILayout.EndHorizontal();
                List<float> allTimings = GetTiming([]);
                bool inGame = ADOBase.controller && ADOBase.controller.gameworld;
                List<float> mapTimings = !inGame ? null : GetTiming(GetMapHash());
                GUIContent buttonContent = new(localization["TimingLogger.SetTiming"]);
                Vector2 buttonSize = GUI.skin.button.CalcSize(buttonContent);
                float width = 100f;
                float height = buttonSize.y * Math.Max(allTimings.Count, mapTimings?.Count ?? 1);
                GUILayout.BeginArea(new Rect(0, 0, width, height), new GUIStyle(GUI.skin.box));
                    GUILayout.BeginHorizontal();
                        GUILayout.BeginVertical();
                            foreach(float timing in allTimings) GUILayout.Label(timing.ToString("N0"), GUILayout.Height(buttonSize.y));
                        GUILayout.EndVertical();
                        GUILayout.FlexibleSpace();
                        GUILayout.BeginVertical();
                            foreach(float timing in allTimings.Where(_ => GUILayout.Button(buttonContent))) {
                                if(scrConductor.currentPreset.inputOffset == (int) timing) continue;
                                scrConductor.currentPreset.inputOffset = (int) timing;
                                scrConductor.SaveCurrentPreset();
                            }
                        GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                GUILayout.EndArea();
                GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                    GUILayout.Label(localization["TimingLogger.MapTimings"]);
                GUILayout.EndHorizontal();
                GUILayout.BeginArea(new Rect(0, 0, width, height), new GUIStyle(GUI.skin.box));
                    if(inGame) {
                        GUILayout.BeginHorizontal();
                            GUILayout.BeginVertical();
                                foreach(float timing in allTimings) GUILayout.Label(timing.ToString("N0"), GUILayout.Height(buttonSize.y));
                            GUILayout.EndVertical();
                            GUILayout.FlexibleSpace();
                            GUILayout.BeginVertical();
                                foreach(float timing in allTimings.Where(_ => GUILayout.Button(buttonContent))) {
                                    if(scrConductor.currentPreset.inputOffset == (int) timing) continue;
                                    scrConductor.currentPreset.inputOffset = (int) timing;
                                    scrConductor.SaveCurrentPreset();
                                }
                            GUILayout.EndVertical();
                        GUILayout.EndHorizontal();
                    } else {
                        GUILayout.BeginVertical();
                            GUILayout.Label(localization["TimingLogger.NotOpenMap"]);
                        GUILayout.EndVertical();
                    }
                GUILayout.EndArea();
                GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    [JAPatch(typeof(StateBehaviour), "ChangeState", PatchType.Postfix, true, ArgumentTypesType = [typeof(Enum)])]
    public static void OnChangeState(Enum newState) {
        States states = (States) newState;
        if(states == States.Start) _logging = false;
        else if(states != States.Fail2) LogTiming();
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
        return ADOBase.isOfficialLevel ? Encoding.UTF8.GetBytes(ADOBase.currentLevel) : GetHash();;
    }

    private static byte[] GetHash() {
        using MemoryStream memoryStream = new();
        scrLevelMaker lm = ADOBase.lm;
        if(lm.isOldLevel) memoryStream.WriteUTF(lm.leveldata);
        else memoryStream.WriteObject(lm.floorAngles);
        foreach(LevelEvent levelEvent in ADOBase.customLevel.events) {
            switch(levelEvent.eventType) {
                case LevelEventType.SetSpeed:
                case LevelEventType.Twirl:
                case LevelEventType.Hold:
                case LevelEventType.MultiPlanet:
                case LevelEventType.Pause:
                case LevelEventType.AutoPlayTiles:
                case LevelEventType.ScaleMargin:
                case LevelEventType.Multitap:
                case LevelEventType.KillPlayer:
                    memoryStream.WriteInt((int) levelEvent.eventType);
                    memoryStream.WriteInt(levelEvent.floor);
                    foreach(object o in levelEvent.data.Values) {
                        switch(o) {
                            case int i:
                                memoryStream.WriteInt(i);
                                break;
                            case long l:
                                memoryStream.WriteLong(l);
                                break;
                            case float f:
                                memoryStream.WriteFloat(f);
                                break;
                            case double d:
                                memoryStream.WriteDouble(d);
                                break;
                            case bool b:
                                memoryStream.WriteBoolean(b);
                                break;
                            case string s:
                                memoryStream.WriteUTF(s);
                                break;
                            case Enum e:
                                memoryStream.WriteUTF(e.ToString());
                                break;
                        }
                    }
                    break;
            }
        }
        return MD5.Create().ComputeHash(memoryStream.ToArray());
    }

    public static Dictionary<byte[], List<float>> GetTimings() {
        if(_timings == null) {
            if(File.Exists("Timings.dat")) {
                using FileStream fileStream = File.OpenRead("Timings.dat");
                try {
                    _timings = fileStream.ReadObject<Dictionary<byte[], List<float>>>();
                } catch (Exception e) {
                    _timings = new Dictionary<byte[], List<float>>();
                    Main.Instance.LogException(e);
                    SaveTiming();
                }
            } else _timings = new Dictionary<byte[], List<float>>();
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
        if(!_timings.TryGetValue(mapHash, out List<float> timing1)) timings = _timings[mapHash] = [];
        else timings = timing1;
        timings.Add(timing);
        if(timings.Count > maxTiming) timings.RemoveAt(0);
        SaveTiming();
    }

    public static List<float> GetTiming(byte[] mapHash) {
        return GetTimings().TryGetValue(mapHash, out List<float> timing) ? timing : [];
    }

    public static void SaveTiming() {
        using FileStream fileStream = File.Create("Timings.dat");
        fileStream.WriteObject(_timings);
    }

    private class TimingLoggerSettings : JASetting {
        public int MaxTimings = 15;
        public int MaxTimingsPerMap = 5;

        public TimingLoggerSettings(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
            _settings = this;
        }
    }
}