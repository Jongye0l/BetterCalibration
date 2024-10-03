using System;
using System.Collections.Generic;
using JALib.Core.Patch;
using MonsterLove.StateMachine;

namespace BetterCalibration.DoubleFeaturePatch;

public class Timing : DoubleFeaturePatch {
    private static Timing _instance;
    private static float? _lastTooEarly;
    private static float? _lastTooLate;
    public static List<float> Timings;

    public static Timing Instance => _instance ??= new Timing();

    public override void OnEnable() {
        Timings = [];
    }

    public override void OnDisable() {
        _lastTooEarly = null;
        _lastTooLate = null;
        Timings.Clear();
        Timings = null;
    }

    [JAPatch(typeof(StateBehaviour), "ChangeState", PatchType.Postfix, true, ArgumentTypesType = [typeof(Enum)])]
    public static void OnChangeState(Enum newState) {
        if((States) newState != States.Fail2) ExitPlay();
        if((States) newState == States.Start) Timings.Clear();
    }

    [JAPatch(typeof(scrController), "TogglePauseGame", PatchType.Postfix, true)]
    public static void ExitPlay() {
        _lastTooEarly = null;
        _lastTooLate = null;
    }

    [JAPatch(typeof(scrMisc), "GetHitMargin", PatchType.Postfix, true)]
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
                _lastTooEarly = null;
                _lastTooLate = null;
                break;
        }
    }

    [JAPatch(typeof(scrMistakesManager), "AddHit", PatchType.Postfix, true)]
    public static void MissCheck(HitMargin hit) {
        if(hit != HitMargin.FailMiss) return;
        if(_lastTooEarly == null || _lastTooLate == null) return;
        Timings.Add((float) _lastTooLate);
        Timings.Add((float) _lastTooEarly);
        _lastTooLate = null;
        _lastTooEarly = null;
    }
}