using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace BetterCalibration.Features;

public class CalibrationDetail() : Feature(Main.Instance, nameof(CalibrationDetail), true, typeof(CalibrationDetail)) {
    private static Text _text;
    private static IList _timings;
    private static float? _max;
    private static float? _min;

    protected override void OnDisable() {
        if(_text) {
            _text.text = "";
            _text.fontSize = 40;
        }
        _text = null;
        _timings = null;
        _max = null;
        _min = null;
    }
    
    [JAPatch("scrCalibrationPlanet", "Start", PatchType.Postfix, true, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "Start", PatchType.Postfix, true, MinVersion = 141)]
    public static void Initialize(Text ___txtResults, IList ___listOffsets) {
        _text = ___txtResults;
        _timings = ___listOffsets;
    }
    
    [JAPatch("scrCalibrationPlanet", "GetOffset", PatchType.Postfix, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "GetOffset", PatchType.Postfix, false, MinVersion = 141)]
    public static void SetMinMax(double __result) {
        float timing = (float) (__result * 1000);
        if(!_text) return;
        if(_max == null || timing > _max) _max = timing;
        if(_min == null || timing < _min) _min = timing;
    }

    [JAPatch("scrCalibrationPlanet", "PutDataPoint", PatchType.Postfix, false, MaxVersion = 140)]
    public static void ReloadTextR136() {
        if(_text) _text.text = string.Format(Main.Instance.Localization.Get("Cablibration.Detail"), ToStringAuto(GetTimingAverage()), ToStringAuto(_max ?? 0), ToStringAuto(_min ?? 0));
    }

    [JAPatch(nameof(scnCalibration), "PutDataPoint", PatchType.Postfix, false, MinVersion = 141)]
    public static void ReloadTextR141(bool ___calibrated) {
        if(!___calibrated && _text) _text.text = string.Format(Main.Instance.Localization.Get("Cablibration.Detail"), ToStringAuto(GetTimingAverage()), ToStringAuto(_max ?? 0), ToStringAuto(_min ?? 0));
    }

    [JAPatch("scrCalibrationPlanet", "SetMessageNumber", PatchType.Postfix, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "SetMessageNumber", PatchType.Postfix, false, MinVersion = 141)]
    public static void Setup(int n) {
        if(!_text) return;
        _text.fontSize = n == 1 ? 30 : 40;
        _max = null;
        _min = null;
    }

    private static string ToStringAuto(float f) {
        return FloatOffset.Instance.Enabled ? Math.Round(f, 2).ToString("0.##") : Mathf.RoundToInt(f).ToString();
    }

    private static float GetTimingAverage() => VersionControl.releaseNumber < 141 ? GetTimingAverageR136() : GetTimingAverageR141();

    private static float GetTimingAverageR136() {
        List<double> timings = _timings.AsUnsafe<List<double>>();
        return timings.Count == 0 ? 0f : (float) (timings.Sum() / timings.Count) * 1000;
    }

    private static float GetTimingAverageR141() {
        List<scnCalibration.OffsetPair> timings = _timings.AsUnsafe<List<scnCalibration.OffsetPair>>();
        return timings.Count == 0 ? 0f : (float) (timings.Sum(t => t.offset) / timings.Count) * 1000;
    }
}