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
    public static string OffsetString;

    protected override void OnSetup() {
        SettingGUI = new SettingGUI(this);
        AddFeature(new CalibrationPopup(), new CalibrationDetail(), new CalibrationSong(), new TimingLogger(), new FloatOffset());
        Patcher.AddPatch(ShowSettingsMenu);
    }

    protected override void OnEnable() {
    }

    protected override void OnDisable() {
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
        if(OffsetString.IsNullOrEmpty() || !int.TryParse(OffsetString, out int i) || i != offset) OffsetString = offset.ToString();
        OffsetString = GUILayout.TextField(OffsetString);
        int resultInt;
        try {
            resultInt = OffsetString.IsNullOrEmpty() ? offset : int.TryParse(OffsetString, out i) ? i : offset;
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

    protected override void OnHideGUI() => OffsetString = null;

    private void AddLanguageButton(string text, SystemLanguage? lang) {
        if(!GUILayout.Button(GetSelectText(text, CustomLanguage == lang))) return;
        CustomLanguage = lang;
    }

    private static string GetSelectText(string text, bool selected) {
        return selected ? $"<b>{text}</b>" : text;
    }

    [JAPatch(typeof(SettingsMenu), nameof(SettingsMenu.Show), PatchType.Prefix, true)]
    private static void ShowSettingsMenu(PauseSettingButton ___offsetButton) {
        if(!___offsetButton) return;
        if(FloatOffset.Instance.Enabled) FloatOffset.Instance.SetOffsetSettingString(___offsetButton);
        else ___offsetButton.valueLabel.text = scrConductor.currentPreset.inputOffset + RDString.Get("editor.unit." + ___offsetButton.unit);
    }
}