using System.Collections.Generic;
using System.Linq;
using JALib.Core;
using JALib.Core.Patch;
using UnityEngine;
using UnityEngine.UI;

namespace BetterCalibration.Features;

public class CalibrationDetail() : Feature(Main.Instance, nameof(CalibrationDetail), true, typeof(CalibrationDetail)) {
    
    private static Text _text;
    private static List<double> _timings;
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
    
    [JAPatch(typeof(scrCalibrationPlanet), "Start", PatchType.Postfix, true)]
    public static void Initialize(scrCalibrationPlanet __instance) {
        _text = __instance.txtResults;
        _timings = __instance.listOffsets;
    }
    
    [JAPatch(typeof(scrCalibrationPlanet), "GetOffset", PatchType.Postfix, true)]
    public static void SetMinMax(ref double __result) {
        float timing = (float) (__result * 1000);
        if(!_text) return;
        if(_max == null || timing > _max) _max = timing;
        if(_min == null || timing < _min) _min = timing;
    }

    [JAPatch(typeof(scrCalibrationPlanet), "PutDataPoint", PatchType.Postfix, true)]
    public static void ReloadText() {
        if(_text) _text.text = string.Format(Main.Instance.Localization.Get("Cablibration.Detail"), ToStringAuto(GetTimingAverage()), ToStringAuto(_max ?? 0), ToStringAuto(_min ?? 0));
    }

    [JAPatch(typeof(scrCalibrationPlanet), "SetMessageNumber", PatchType.Postfix, true)]
    public static void Setup(int n) {
        if(!_text) return;
        _text.fontSize = n == 1 ? 30 : 40;
        _max = null;
        _min = null;
    }

    private static string ToStringAuto(float f) {
        return FloatOffset.Instance.Enabled ? f.ToString("0.##") : Mathf.RoundToInt(f).ToString();
    }

    private static float GetTimingAverage() {
        return _timings.Count == 0 ? 0f : (float) (_timings.Sum() / _timings.Count) * 1000;
    }
}