using System;
using HarmonyLib;
using MonsterLove.StateMachine;

namespace BetterCalibration {
    [HarmonyPatch]
    public class DeathCaliPatch {

        private static float? _lastTiming;
        
        [HarmonyPatch(typeof (StateBehaviour), "ChangeState", typeof(Enum))]
        [HarmonyPostfix]
        public static void OnRestart(Enum newState) {
            if((States) newState == States.Fail2) ShowCalibration.Show();
            else ShowCalibration.Hide();
            if((States) newState == States.Start) ShowCalibration.Timings.Clear();
        }

        [HarmonyPatch(typeof(scrController), "TogglePauseGame")]
        [HarmonyPostfix]
        public static void ExitPlay() {
            ShowCalibration.Hide();
        }

        [HarmonyPatch(typeof(scrMisc), "GetHitMargin")]
        [HarmonyPostfix]
        public static void GetTiming(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, float conductorPitch, HitMargin __result) {
            float angle = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f;
            float timing = angle / 180 / bpmTimesSpeed / conductorPitch * 60000;
            if(__result == HitMargin.TooEarly) _lastTiming = timing;
            else {
                ShowCalibration.Timings.Add(timing);
                _lastTiming = null;
            }
        }

        [HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
        [HarmonyPostfix]
        public static void MissCheck(HitMargin hit) {
            if(_lastTiming == null || hit != HitMargin.FailMiss) return;
            ShowCalibration.Timings.Add((float) _lastTiming);
            _lastTiming = null;
        }
    }
}