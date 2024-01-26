using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterCalibration.GUI {
    public class CalibrationDetail {
        private static Text _text;
        private static List<double> _timings;
        private static float? _max;
        private static float? _min;
        
        public static void Initialize(scrCalibrationPlanet instance) {
            _text = instance.txtResults;
            _timings = instance.listOffsets;
        }

        public static void LoadText() {
            _text.text = string.Format(Main.GetValues().CalibrationDetail, GetTimingAverage(), Mathf.RoundToInt(_max ?? 0), Mathf.RoundToInt(_min ?? 0));
        }

        private static int GetTimingAverage() {
            return _timings.Count == 0 ? 0 : Mathf.RoundToInt((float) (_timings.Sum() / _timings.Count) * 1000);
        }

        public static void Setup(bool b) {
            if(_text == null) return;
            _text.fontSize = b ? 30 : 40;
            _max = null;
            _min = null;
        }

        public static void CheckTiming(double timing) {
            if(_max == null || timing > _max) _max = (float) timing;
            if(_min == null || timing < _min) _min = (float) timing;
        }
    }
}