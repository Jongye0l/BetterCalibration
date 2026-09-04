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
    private static Func<float, float, bool, float, float, double, HitMargin> _getHitMarginR141;

    public Timing() : base(Main.Instance) {
        Patcher.AddPatch(typeof(Timing));
        if(VersionControl.releaseNumber >= 149) {
            int founded = 0;
            foreach(MethodInfo methodInfo in typeof(scrPlanet).Methods()) {
                if(methodInfo.Name.StartsWith("<SwitchChosen>") && methodInfo.Name.Contains("GetHitMargin")) {
                    Patcher.AddPatch(GetHitMargin, new JAPatchAttribute(methodInfo, PatchType.Transpiler, false));
                    founded++;
                }
            }

            if(founded < 1) Main.Instance.Error("Failed to find the method for transpiler patching: scrPlanet.<SwitchChosen>g__GetHitMargin|");
            else if(founded != 1) Main.Instance.Warning($"Found {founded} methods for transpiler patching: scrPlanet.<SwitchChosen>g__GetHitMargin|. Expected 1.");
        } else if(VersionControl.releaseNumber >= 141) {
            _getHitMarginR141 = (Func<float, float, bool, float, float, double, HitMargin>) Delegate.CreateDelegate(typeof(Func<float, float, bool, float, float, double, HitMargin>), typeof(scrMisc).Method("GetHitMargin"));
        }
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

    [JAPatch(typeof(scrPlanet), nameof(scrPlanet.SwitchChosen), PatchType.Transpiler, false, MinVersion = 141, MaxVersion = 148)]
    private static IEnumerable<CodeInstruction> GetHitMarginR141(IEnumerable<CodeInstruction> instructions) {
        List<CodeInstruction> list = instructions.ToList();
        for(int i = 0; i < list.Count; i++) {
            CodeInstruction codeInstruction = list[i];
            if(codeInstruction.operand is not MethodInfo { Name: "GetHitMargin" }) continue;
            list[i] = new CodeInstruction(OpCodes.Ldarg_0);
            list.Insert(++i, new CodeInstruction(OpCodes.Call, ((Delegate) GetHitMarginProxy).Method));
        }
        return list;
    }

    public static HitMargin GetHitMarginProxy(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, float conductorPitch, double marginScale, scrPlanet planet) {
        HitMargin result = _getHitMarginR141(hitangle, refangle, isCW, bpmTimesSpeed, conductorPitch, marginScale);
        try {
            if(IsTimingAvailable(planet)) {
                float angle = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f;
                float timing = angle / 180 / bpmTimesSpeed / conductorPitch * 60000;
                SetTiming(timing, result);
            }
        } catch (Exception e) {
            Main.Instance.LogReportException("Failed to calculate timing", e);
        }
        return result;
    }

    private static IEnumerable<CodeInstruction> GetHitMargin(IEnumerable<CodeInstruction> instructions) {
        List<CodeInstruction> list = instructions.ToList();
        for(int i = 0; i < list.Count; i++) {
            CodeInstruction codeInstruction = list[i];
            if(codeInstruction.operand is not MethodInfo methodInfo) continue;
            switch(methodInfo.Name) {
                case "GetHitMarginInDeg":
                    list[i] = new CodeInstruction(OpCodes.Ldarg_0);
                    list.Insert(++i, new CodeInstruction(OpCodes.Call, ((Delegate) GetHitMarginInDegProxyR149).Method));
                    break;
                case "GetHitMarginInSec":
                    list[i] = new CodeInstruction(OpCodes.Ldarg_0);
                    list.Insert(++i, new CodeInstruction(OpCodes.Call, ((Delegate) GetHitMarginInSecProxyR149).Method));
                    break;
            }
        }
        return list;
    }

    private static HitMargin GetHitMarginInDegProxyR149(Difficulty difficulty, float hitAngle, float refAngle, bool clockwise,
                                                        float floorBpm, float conductorPitch, double marginScale, scrPlanet planet) {
        HitMargin result = scrMisc.GetHitMarginInDeg(difficulty, hitAngle, refAngle, clockwise, floorBpm, conductorPitch, marginScale);
        try {
            if(IsTimingAvailable(planet)) {
                float angle = (hitAngle - refAngle) * (clockwise ? 1 : -1) * 57.29578f;
                float timing = angle / 180 / floorBpm / conductorPitch * 60000;
                SetTimingR149(timing, result);
            }
        } catch (Exception e) {
            Main.Instance.LogReportException("Failed to calculate hit margin in degrees", e);
        }
        return result;
    }

    private static HitMargin GetHitMarginInSecProxyR149(Difficulty difficulty, double timeDiff, float floorBpm,
                                                        float conductorPitch, double marginScale, scrPlanet planet) {
        HitMargin result = scrMisc.GetHitMarginInSec(difficulty, timeDiff, floorBpm, conductorPitch, marginScale);
        try {
            if(IsTimingAvailable(planet)) {
                float timing = (float) timeDiff * 1000;
                SetTimingR149(timing, result);
            }
        } catch (Exception e) {
            Main.Instance.LogReportException("Failed to calculate hit margin in seconds", e);
        }
        return result;
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