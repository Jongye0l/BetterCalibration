using System.Reflection;
using BetterCalibration.GUI;
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
            modEntry.OnGUI = SettingGUI.OnGUI;
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

        public static Values GetValues() {
            return Settings.Values ??
                   (RDString.language == SystemLanguage.Korean ? Values.Korean :
                       RDString.language == SystemLanguage.Japanese ? Values.Japanese : Values.English);
        }
    }
}