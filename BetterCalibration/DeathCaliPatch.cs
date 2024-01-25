using System;
using HarmonyLib;
using MonsterLove.StateMachine;

namespace BetterCalibration {
    [HarmonyPatch]
    public class DeathCaliPatch {

        private static float? _lastTooEarly;
        private static float? _lastTooLate;
        
        [HarmonyPatch(typeof (StateBehaviour), "ChangeState", typeof(Enum))]
        [HarmonyPostfix]
        public static void OnChangeState(Enum newState) {
            if((States) newState == States.Fail2) ShowCalibration.Show();
            else {
                ShowCalibration.Hide();
                _lastTooEarly = null;
                _lastTooLate = null;
            }
            if((States) newState == States.Start) ShowCalibration.Timings.Clear();
        }

        [HarmonyPatch(typeof(scrController), "TogglePauseGame")]
        [HarmonyPostfix]
        public static void ExitPlay() {
            ShowCalibration.Hide();
            _lastTooEarly = null;
            _lastTooLate = null;
        }

        [HarmonyPatch(typeof(scrMisc), "GetHitMargin")]
        [HarmonyPostfix]
        public static void GetTiming(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, float conductorPitch, HitMargin __result) {
            float angle = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f;
            float timing = angle / 180 / bpmTimesSpeed / conductorPitch * 60000;
            if(__result == HitMargin.TooEarly) _lastTooEarly = timing;
            else if(__result == HitMargin.TooLate) _lastTooLate = timing;
            else {
                ShowCalibration.Timings.Add(timing);
                _lastTooEarly = null;
                _lastTooLate = null;
            }
        }

        [HarmonyPatch(typeof(scrMistakesManager), "AddHit")]
        [HarmonyPostfix]
        public static void MissCheck(HitMargin hit) {
            if(hit != HitMargin.FailMiss) return;
            if(_lastTooEarly == null || _lastTooLate == null) return;
            ShowCalibration.Timings.Add((float) _lastTooLate);
            ShowCalibration.Timings.Add((float) _lastTooEarly);
            _lastTooLate = null;
            _lastTooEarly = null;
        }
    }
}