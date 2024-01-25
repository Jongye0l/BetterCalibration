using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace BetterCalibration {
    public class Main {
        public static UnityModManager.ModEntry.ModLogger Logger;
        public static UnityModManager.ModEntry ModEntry;
        private static Harmony _harmony;
        public static bool Enabled;
        public static Settings Settings;
        private static Assembly _assembly;

        public static void Setup(UnityModManager.ModEntry modEntry) {
            ModEntry = modEntry;
            Logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            _assembly = Assembly.GetExecutingAssembly();
            Settings = Settings.CreateInstance();
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value) {
            Enabled = value;
            if(value) {
                _harmony = new Harmony(modEntry.Info.Id);
                _harmony.PatchAll(_assembly);
            } else {
                _harmony.UnpatchAll(modEntry.Info.Id);
            }
            return true;
        }
        
        private static void OnGUI(UnityModManager.ModEntry modEntry) {
            Values values = GetValues();
            AddSettingPitch(ref Settings.Pitch, 100, ref Settings.PitchString, values.Pitch);
            AddSettingToggleInt(ref Settings.Minimum, 0, ref Settings.UseMinimum, ref Settings.MinimumString, values.Minimum);
            AddSettingToggle(ref Settings.ShowPopup, values.Popup);
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

        public static Values GetValues() {
            return RDString.language == SystemLanguage.Korean ? Values.Korean : 
                RDString.language == SystemLanguage.Japanese ? Values.Japanese : Values.English;
        }
    }
}