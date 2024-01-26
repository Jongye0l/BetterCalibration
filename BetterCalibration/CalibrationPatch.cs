using System;
using HarmonyLib;
using UnityEngine;

namespace BetterCalibration {
    [HarmonyPatch]
    public class CalibrationPatch {
        private static readonly Settings Settings = Settings.Instance;
        private static float _bpm;
        private static float _lastTime;
        private static int _attempt;
        
        [HarmonyPatch(typeof(scrCalibrationPlanet), "Start")]
        [HarmonyPostfix]
        public static void SetPitch(scrConductor ___conductor) {
            ___conductor.song.pitch = Settings.Pitch / 100;
            _bpm = (float) (1.3 * Settings.Pitch); // (BPM) 130 / (Default Pitch) 100 = 1.3
            ___conductor.song.loop = Settings.RepeatSong > 0;
        }

        [HarmonyPatch(typeof(scrCalibrationPlanet), "CleanSlate")]
        [HarmonyPrefix]
        public static void SetRestartPitch(scrConductor ___conductor) {
            ___conductor.song.pitch = Settings.Pitch / 100;
            _attempt = 0;
            ___conductor.song.loop = Settings.RepeatSong > 0;
        }

        [HarmonyPatch(typeof(scrCalibrationPlanet), "PutDataPoint")]
        [HarmonyPrefix]
        public static void BpmPrefix(scrConductor ___conductor) {
            ___conductor.bpm = _bpm;
        }

        [HarmonyPatch(typeof(scrCalibrationPlanet), "PutDataPoint")]
        [HarmonyPostfix]
        public static void BpmPostfix(scrConductor ___conductor) {
            ___conductor.bpm = 130;
        }
        
        [HarmonyPatch(typeof(scrCalibrationPlanet), "PostSong")]
        [HarmonyPrefix]
        public static void ResetPitch(scrConductor ___conductor) {
            ___conductor.song.pitch = 1;
        }
        
        [HarmonyPatch(typeof(scrCalibrationPlanet), "GetOffset")]
        [HarmonyPostfix]
        public static void SetOffset(ref double __result) {
            if(!Settings.UseMinimum) return;
            double time360 = 30000 / _bpm;
            double result = __result * 1000;
            while(result < Settings.Minimum) result += time360;
            __result = result / 1000;
        }

        [HarmonyPatch(typeof(scrCalibrationPlanet), "Update")]
        [HarmonyPostfix]
        public static void CheckRepeat(scrConductor ___conductor) {
            if(_lastTime > ___conductor.song.time && ++_attempt >= Settings.RepeatSong) ___conductor.song.loop = false;
            _lastTime = ___conductor.song.time;
        }
    }
}