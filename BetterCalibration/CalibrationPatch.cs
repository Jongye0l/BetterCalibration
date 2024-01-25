using System;
using HarmonyLib;
using UnityEngine;

namespace BetterCalibration {
    [HarmonyPatch]
    public class CalibrationPatch {
        private static readonly Settings Settings = Settings.Instance;
        private static float _bpm;
        
        [HarmonyPatch(typeof(scrCalibrationPlanet), "Start")]
        [HarmonyPostfix]
        public static void SetPitch(scrConductor ___conductor) {
            ___conductor.song.pitch = Settings.Pitch / 100;
            _bpm = (float) (1.3 * Settings.Pitch); // (BPM) 130 / (Default Pitch) 100 = 1.3
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
    }
}