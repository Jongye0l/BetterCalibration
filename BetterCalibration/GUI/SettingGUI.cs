using System;
using UnityEngine;
using UnityModManagerNet;

namespace BetterCalibration.GUI {
    public class SettingGUI {

        private static readonly Settings Settings = Main.Settings;
        private static string offsetString;

        public static void OnGUI(UnityModManager.ModEntry modEntry) {
            Values values = Main.GetValues();
            AddSettingLanguage(values.Language, values.Default);
            AddSettingPitch(ref Settings.Pitch, 100, ref Settings.PitchString, values.Pitch);
            AddSettingToggleInt(ref Settings.Minimum, 0, ref Settings.UseMinimum, ref Settings.MinimumString, values.Minimum);
            AddSettingInt(ref Settings.RepeatSong, 0, ref Settings.RepeatString, values.RepeatSong);
            AddSettingToggle(ref Settings.Detail, values.UseDetail);
            AddSettingToggle(ref Settings.ShowPopup, values.Popup);
            AddSettingOffset(values.InputOffset);
        }

        private static void AddSettingLanguage(string text, string defaultText) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.Space(4f);
            AddLanguageButton(GetSelectText(defaultText, Settings.Values == null), null);
            AddLanguageButton(GetSelectText("한국어", Settings.Values == Values.Korean), Values.Korean);
            AddLanguageButton(GetSelectText("English", Settings.Values == Values.English), Values.English);
            AddLanguageButton(GetSelectText("日本語", Settings.Values == Values.Japanese), Values.Japanese);
            AddLanguageButton(GetSelectText("Tiếng Việt", Settings.Values == Values.Vietnamese), Values.Vietnamese);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void AddLanguageButton(string text, Values values) {
            if(!GUILayout.Button(text)) return;
            Settings.Values = values;
            Settings.Save();
        }

        private static string GetSelectText(string text, bool selected) {
            return string.Format(selected ? "<b>{0}</b>" : "{0}", text);
        }

        private static void AddSettingToggle(ref bool value, string text) {
            if(GUILayout.Toggle(value, text)) {
                if(!value) {
                    value = true;
                    Settings.Save();
                }
            } else if(value) {
                value = false;
                Settings.Save();
            }
        }

        private static void AddSettingToggleInt(ref int value, int defaultValue, ref bool value2, ref string valueString, string text) {
            GUILayout.BeginHorizontal();
            if(GUILayout.Toggle(value2, text)) {
                if(!value2) {
                    value2 = true;
                    Settings.Save();
                }
                GUILayout.Space(4f);
                if(valueString == null) valueString = value.ToString();
                valueString = GUILayout.TextField(valueString, GUILayout.Width(50));
                int resultInt;
                try {
                    resultInt = valueString.IsNullOrEmpty() ? defaultValue : int.Parse(valueString);
                } catch (FormatException) {
                    resultInt = defaultValue;
                    valueString = defaultValue.ToString();
                }
                if(resultInt != value) {
                    value = resultInt;
                    Settings.Save();
                }
                GUILayout.Label("ms");
            } else if(value2) {
                value2 = false;
                Settings.Save();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void AddSettingInt(ref int value, int defaultValue, ref string valueString, string text) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.Space(4f);
            if(valueString == null) valueString = value.ToString();
            valueString = GUILayout.TextField(valueString, GUILayout.Width(50));
            int resultInt;
            try {
                resultInt = valueString.IsNullOrEmpty() ? defaultValue : int.Parse(valueString);
                if(resultInt < 0) {
                    resultInt = 0;
                    valueString = "0";
                }
            } catch (FormatException) {
                resultInt = defaultValue;
                valueString = defaultValue.ToString();
            }
            if(resultInt != value) {
                value = resultInt;
                Settings.Save();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void AddSettingOffset(string text) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.Space(4f);
            int offset = scrConductor.currentPreset.inputOffset;
            if(offsetString.IsNullOrEmpty() || !int.TryParse(offsetString, out int i) || i != offset) offsetString = offset.ToString();
            offsetString = GUILayout.TextField(offsetString, GUILayout.Width(50));
            int resultInt;
            try {
                resultInt = offsetString.IsNullOrEmpty() ? offset : int.TryParse(offsetString, out i) ? i : offset;
            } catch (FormatException) {
                resultInt = offset;
            }
            if(resultInt != offset) {
                offset = resultInt;
                scrConductor.currentPreset.inputOffset = offset;
                scrConductor.SaveCurrentPreset();
            }
            GUILayout.Label("ms");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private static void AddSettingPitch(ref float value, float defaultValue, ref string valueString, string text) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(text);
            GUILayout.Space(4f);
            if(valueString == null) valueString = value.ToString();
            valueString = GUILayout.TextField(valueString, GUILayout.Width(50));
            float resultFloat;
            try {
                resultFloat = valueString.IsNullOrEmpty() ? defaultValue : float.Parse(valueString);
                if(resultFloat > 500) {
                    resultFloat = 500;
                    valueString = "500";
                } else if(resultFloat <= 0) {
                    resultFloat = defaultValue;
                    valueString = defaultValue.ToString();
                }
            } catch (FormatException) {
                resultFloat = defaultValue;
                valueString = defaultValue.ToString();
            }
            if(resultFloat != value) {
                value = resultFloat;
                Settings.Save();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}