using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Tools;
using MonsterLove.StateMachine;

namespace BetterCalibration.Features.Multi;

public class Timing : MultiFeature {
    private static float _lastTooEarly;
    private static float _lastTooLate;
    public static List<float> Timings;

    public Timing() : base(Main.Instance) {
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

    private static void SetTiming(float timing, HitMargin margin) {
        switch(margin) {
            case HitMargin.TooEarly: _lastTooEarly = timing;
                break;
            case (HitMargin) 6 /* TooLate */: _lastTooLate = timing;
                break;
            default:
                Timings.Add(timing);
                ResetLastTooJudge();
                break;
        }
    }

    private static void SetTimingR149(float timing, HitMargin margin) {
        switch(margin) {
            case HitMargin.TooEarly: _lastTooEarly = timing;
                break;
            case HitMargin.TooLate: _lastTooLate = timing;
                break;
            default:
                Timings.Add(timing);
                ResetLastTooJudge();
                break;
        }
    }
    
    [JAPatch(typeof(scrMisc), "GetHitMargin", PatchType.Postfix, false, MaxVersion = 140)]
    // ReSharper disable once InconsistentNaming
    private static void OnHitMarginChange(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, float conductorPitch, HitMargin __result) {
        if(RDC.auto || scrController.instance.currFloor.nextfloor && scrController.instance.currFloor.nextfloor.auto) return;
        float angle = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f;
        float timing = angle / 180 / bpmTimesSpeed / conductorPitch * 60000;
        SetTiming(timing, __result);
    }
    
    private static scrPlanet _planet;
    
    [JAPatch(typeof(scrPlanet), nameof(scrPlanet.SwitchChosen), PatchType.Prefix, true, MinVersion = 141, TryingCatch = false)]
    private static void SwitchChosenPrefix(scrPlanet __instance) => _planet = __instance;

    [JAPatch(typeof(scrPlanet), nameof(scrPlanet.SwitchChosen), PatchType.Finalizer, false, MinVersion = 141, TryingCatch = false)]
    private static void SwitchChosenFinalizer() => _planet = null;

    [JAPatch(typeof(scrMisc), "GetHitMargin", PatchType.Postfix, false, MinVersion = 141, MaxVersion = 148)]
    public static void OnHitMarginChangeR141(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, float conductorPitch, HitMargin __result) {
        if(!IsTimingAvailable(_planet)) return;

        float angle = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f;
        float timing = angle / 180 / bpmTimesSpeed / conductorPitch * 60000;
        SetTiming(timing, __result);
    }

    [JAPatch(typeof(scrMisc), nameof(scrMisc.GetHitMarginInDeg), PatchType.Postfix, false, MinVersion = 149)]
    private static void GetHitMarginInDegProxyR149(float hitAngle, float refAngle, bool clockwise, float floorBpm, float conductorPitch, HitMargin __result) {
        if(!IsTimingAvailable(_planet)) return;

        float angle = (hitAngle - refAngle) * (clockwise ? 1 : -1) * 57.29578f;
        float timing = angle / 180 / floorBpm / conductorPitch * 60000;
        SetTimingR149(timing, __result);
    }

    [JAPatch(typeof(scrMisc), nameof(scrMisc.GetHitMarginInSec), PatchType.Postfix, false, MinVersion = 149)]
    private static void GetHitMarginInSecProxyR149(double timeDiff, HitMargin __result) {
        if(!IsTimingAvailable(_planet)) return;

        float timing = (float) timeDiff * 1000;
        SetTimingR149(timing, __result);
    }

    private static bool IsTimingAvailable(scrPlanet planet) => !RDC.auto && !planet.player.auto && (!planet.currfloor.nextfloor || !planet.currfloor.nextfloor.auto);

    private static readonly HitMargin FailMiss = VersionControl.releaseNumber >= 149 ? HitMargin.FailMiss : (HitMargin) 8;

    [JAPatch(typeof(scrMistakesManager), "AddHit", PatchType.Postfix, false, MaxVersion = 140)]
    [JAPatch(nameof(scrMarginTracker), nameof(scrMarginTracker.AddHit), PatchType.Postfix, false, MinVersion = 141)]
    public static void MissCheck(HitMargin hit) {
        if(hit != FailMiss) return;
        if(float.IsNaN(_lastTooEarly) || float.IsNaN(_lastTooLate)) return;
        Timings.Add(_lastTooLate);
        Timings.Add(_lastTooEarly);
        ResetLastTooJudge();
    }

    public static float GetTrimmedMeanTiming() {
        if(Timings.Count == 0) return 0;

        int trimCount = (int) Math.Floor(Timings.Count * 0.1);
        if(trimCount == 0) return Timings.Average();

        return Timings.OrderBy(t => t).Skip(trimCount).Take(Timings.Count - 2 * trimCount).Average();
    }
}