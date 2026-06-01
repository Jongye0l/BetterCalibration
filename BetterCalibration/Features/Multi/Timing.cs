using System;
using System.Collections.Generic;
using JALib.Core;
using JALib.Core.Patch;
using MonsterLove.StateMachine;

namespace BetterCalibration.Features.Multi;

public class Timing : MultiFeature {
    private static float _lastTooEarly;
    private static float _lastTooLate;
    public static List<float> Timings;

    public Timing(JAMod mod) : base(mod) {
        Patcher.AddPatch(typeof(Timing));
    }

    protected override void OnEnable() {
        Timings = [];
    }

    protected override void OnDisable() {
        ResetLastTooJudge();
        Timings.Clear();
        Timings = null;
    }

    [JAPatch(typeof(StateBehaviour), "ChangeState", PatchType.Postfix, true, ArgumentTypesType = [typeof(Enum)])]
    public static void OnChangeState(Enum newState) {
        if((States) newState != States.Fail2) ResetLastTooJudge();
        if((States) newState == States.Start) Timings.Clear();
    }

    [JAPatch(typeof(scrController), "TogglePauseGame", PatchType.Postfix, false)]
    public static void ResetLastTooJudge() {
        _lastTooEarly = float.NaN;
        _lastTooLate = float.NaN;
    }

    [JAPatch(typeof(scrMisc), "GetHitMargin", PatchType.Postfix, false)]
    public static void GetTiming(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, float conductorPitch, HitMargin __result) {
        if(RDC.auto || scrController.instance.currFloor.nextfloor?.auto == true) return;
        float angle = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f;
        float timing = angle / 180 / bpmTimesSpeed / conductorPitch * 60000;
        switch(__result) {
            case HitMargin.TooEarly:
                _lastTooEarly = timing;
                break;
            case HitMargin.TooLate:
                _lastTooLate = timing;
                break;
            default:
                Timings.Add(timing);
                ResetLastTooJudge();
                break;
        }
    }

    [JAPatch(typeof(scrMistakesManager), "AddHit", PatchType.Postfix, false, MaxVersion = 140)]
    [JAPatch(nameof(scrMarginTracker), nameof(scrMarginTracker.AddHit), PatchType.Postfix, false, MinVersion = 141)]
    public static void MissCheck(HitMargin hit) {
        if(hit != HitMargin.FailMiss) return;
        if(float.IsNaN(_lastTooEarly) || float.IsNaN(_lastTooLate)) return;
        Timings.Add((float) _lastTooLate);
        Timings.Add((float) _lastTooEarly);
        ResetLastTooJudge();
    }
}