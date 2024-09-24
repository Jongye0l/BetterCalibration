using System;
using BetterCalibration.Features;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Tools;
using UnityEngine;
using UnityModManagerNet;

namespace BetterCalibration;

public class Main : JAMod {
    public static Main Instance;
    public static SettingGUI SettingGUI;
    public static string offsetString;
    private static JAPatcher Patcher;

    private Main(UnityModManager.ModEntry modEntry) : base(modEntry, true, gid: 1929334982) {
        Instance = this;
        SettingGUI = new SettingGUI(this);
        AddFeature(new CalibrationPopup(), new CalibrationDetail(), new CalibrationSong(), new TimingLogger(), new FloatOffset());
        Patcher = new JAPatcher(this).AddPatch(ShowSettingsMenu);
    }

    protected override void OnEnable() {
        Patcher.Patch();
    }

    protected override void OnDisable() {
        Patcher.Unpatch();
    }

    protected override void OnGUI() {
        GUILayout.BeginHorizontal();
        GUILayout.Label(Localization["Language"]);
        GUILayout.Space(4f);
        AddLanguageButton(Localization["Language.Default"], null);
        AddLanguageButton("한국어", SystemLanguage.Korean);
        AddLanguageButton("English", SystemLanguage.English);
        AddLanguageButton("日本語", SystemLanguage.Japanese);
        AddLanguageButton("Tiếng Việt", SystemLanguage.Vietnamese);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    protected override void OnGUIBehind() {
        if(FloatOffset.Instance.Enabled) return;
        GUILayout.BeginHorizontal();
        GUILayout.Label(Localization["InputOffset"]);
        GUILayout.Space(4f);
        if(GUILayout.Button("-", GUILayout.Width(25))) {
            scrConductor.currentPreset.inputOffset--;
            scrConductor.SaveCurrentPreset();
        }
        int offset = scrConductor.currentPreset.inputOffset;
        if(offsetString.IsNullOrEmpty() || !int.TryParse(offsetString, out int i) || i != offset) offsetString = offset.ToString();
        offsetString = GUILayout.TextField(offsetString);
        int resultInt;
        try {
            resultInt = offsetString.IsNullOrEmpty() ? offset : int.TryParse(offsetString, out i) ? i : offset;
        } catch (FormatException) {
            resultInt = offset;
        }
        if(resultInt != offset) {
            scrConductor.currentPreset.inputOffset = resultInt;
            scrConductor.SaveCurrentPreset();
        }
        GUILayout.Label("ms");
        if(GUILayout.Button("+", GUILayout.Width(25))) {
            scrConductor.currentPreset.inputOffset++;
            scrConductor.SaveCurrentPreset();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    protected override void OnHideGUI() => offsetString = null;

    private void AddLanguageButton(string text, SystemLanguage? lang) {
        if(!GUILayout.Button(GetSelectText(text, CustomLanguage == lang))) return;
        CustomLanguage = lang;
    }

    private static string GetSelectText(string text, bool selected) {
        return string.Format(selected ? "<b>{0}</b>" : "{0}", text);
    }

    [JAPatch(typeof(PauseMenu), "ShowSettingsMenu", PatchType.Prefix, true)]
    private static void ShowSettingsMenu(PauseMenu __instance) {
        PauseSettingButton offset = __instance.settingsMenu.offsetButton;
        if(!offset) return;
        if(FloatOffset.Instance.Enabled) FloatOffset.Instance.SetOffsetSettingString(offset);
        else offset.valueLabel.text = scrConductor.currentPreset.inputOffset + RDString.Get("editor.unit." + offset.unit);
    }
}