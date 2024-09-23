using System;
using BetterCalibration.Features;
using JALib.Core;
using JALib.Tools;
using UnityEngine;
using UnityModManagerNet;

namespace BetterCalibration;

public class Main : JAMod {
    public static Main Instance;
    public static SettingGUI SettingGUI;
    private static string offsetString;

    private Main(UnityModManager.ModEntry modEntry) : base(modEntry, true, gid: 1929334982) {
        Instance = this;
        SettingGUI = new SettingGUI(this);
        AddFeature(new CalibrationPopup(), new CalibrationDetail(), new CalibrationSong());
    }

    protected override void OnGUI() {
        GUILayout.BeginHorizontal();
        GUILayout.Label(Localization["Language"]);
        GUILayout.Space(4f);
        AddLanguageButton(GetSelectText(Localization["Language.Default"], CustomLanguage == null), null);
        AddLanguageButton(GetSelectText("한국어", CustomLanguage == SystemLanguage.Korean), SystemLanguage.Korean);
        AddLanguageButton(GetSelectText("English", CustomLanguage == SystemLanguage.English), SystemLanguage.English);
        AddLanguageButton(GetSelectText("日本語", CustomLanguage == SystemLanguage.Japanese), SystemLanguage.Japanese);
        AddLanguageButton(GetSelectText("Tiếng Việt", CustomLanguage == SystemLanguage.Vietnamese), SystemLanguage.Vietnamese);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    protected override void OnGUIBehind() {
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
        if(!GUILayout.Button(text)) return;
        CustomLanguage = lang;
    }

    private static string GetSelectText(string text, bool selected) {
        return string.Format(selected ? "<b>{0}</b>" : "{0}", text);
    }
}