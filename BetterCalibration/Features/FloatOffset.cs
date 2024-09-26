using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Tools;
using Newtonsoft.Json.Linq;
using UnityEngine;
using MethodInfo = System.Reflection.MethodInfo;

namespace BetterCalibration.Features;

public class FloatOffset : Feature {
    public static FloatOffset Instance;
    private static FloatOffsetSettings _settings;

    public float Offset {
        get => _settings.Offset.TryGetValue(scrConductor.currentPreset.outputName, out float offset) ? offset : scrConductor.currentPreset.inputOffset;
        set {
            if(scrConductor.currentPreset.inputOffset != (int) value) {
                scrConductor.currentPreset.inputOffset = (int) value;
                scrConductor.SaveCurrentPreset();
            }
            if(_settings.Offset.TryGetValue(scrConductor.currentPreset.outputName, out float f) && f == value) return;
            _settings.Offset[scrConductor.currentPreset.outputName] = value;
            Main.Instance.SaveSetting();
        }
    }

    public FloatOffset() : base(Main.Instance, nameof(FloatOffset), true, typeof(FloatOffset), typeof(FloatOffsetSettings)) {
        Instance = this;
    }

    protected override void OnEnable() {
        foreach(CalibrationPreset preset in scrConductor.userPresets)
            if(_settings.Offset.TryGetValue(preset.outputName, out float offset) && preset.inputOffset != (int) offset)
                _settings.Offset.Remove(preset.outputName);
    }

    protected override void OnGUI() {
        ref string offsetString = ref Main.offsetString;
        GUILayout.BeginHorizontal();
        GUILayout.Label(Main.Instance.Localization["InputOffset"]);
        GUILayout.Space(4f);
        float offset = Offset;
        if(GUILayout.Button("-", GUILayout.Width(25))) Offset = offset - 1;
        if(offsetString.IsNullOrEmpty() || !float.TryParse(offsetString, out float f) || f != offset) offsetString = offset.ToString();
        offsetString = GUILayout.TextField(offsetString);
        float resultFloat;
        try {
            resultFloat = offsetString.IsNullOrEmpty() ? offset : float.TryParse(offsetString, out f) ? f : offset;
        } catch (FormatException) {
            resultFloat = offset;
        }
        if(resultFloat != offset) Offset = resultFloat;
        GUILayout.Label("ms");
        if(GUILayout.Button("+", GUILayout.Width(25))) Offset = offset + 1;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    [JAPatch(typeof(scrConductor), "get_calibration_i", PatchType.Prefix, true)]
    private static bool GetCalibration(ref float __result) {
        if(!_settings.Offset.TryGetValue(scrConductor.currentPreset.outputName, out float offset)) return true;
        __result = offset / 1000f;
        return false;
    }

    [JAPatch(typeof(SettingsMenu), "UpdateSetting", PatchType.Prefix, false)]
    private static bool UpdateSetting(SettingsMenu __instance, PauseSettingButton setting, SettingsMenu.Interaction action) {
        if(setting.name != "inputOffset" || action is SettingsMenu.Interaction.ActivateInfo or SettingsMenu.Interaction.Activate) return true;
        __instance.offsetButton = setting;
        if(action == SettingsMenu.Interaction.Refresh) setting.CachedValue = null;
        else {
            float offset = Instance.Offset;
            float increment = 10;
            if(RDInput.holdingShift) increment /= 10;
            if(RDInput.holdingControl) increment /= 100;
            if(action == SettingsMenu.Interaction.Increment) {
                offset += increment;
                setting.PlayArrowAnimation(true);
            } else if(action == SettingsMenu.Interaction.Decrement) {
                offset -= increment;
                setting.PlayArrowAnimation(false);
            }
            scrController.instance.pauseMenu.PlayMenuSfx(SfxSound.MenuSquelch, 1.5f);
            Instance.Offset = offset;
        }
        Instance.SetOffsetSettingString(setting);
        return false;
    }

    public void SetOffsetSettingString(PauseSettingButton setting) {
        setting.valueLabel.text = Offset.ToString("0.##") + RDString.Get("editor.unit." + setting.unit);
    }

    [JAPatch(typeof(scrCalibrationPlanet), "PostSong", PatchType.Transpiler, false)]
    private static IEnumerable<CodeInstruction> PostSong(IEnumerable<CodeInstruction> instructions) {
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        while(enumerator.MoveNext()) {
            CodeInstruction current = enumerator.Current;
            if(current.opcode == OpCodes.Call && current.operand is MethodInfo method) {
                if(method == typeof(Math).Method("Round", typeof(double))) {
                    enumerator.MoveNext();
                    current = enumerator.Current;
                } else if(method == typeof(double).Method("ToString", [])) {
                    yield return new CodeInstruction(OpCodes.Ldstr, "0.##");
                    current.operand = typeof(double).Method("ToString", typeof(string));
                }
            } else if(current.opcode == OpCodes.Ldsflda && current.operand is FieldInfo field && field == typeof(scrConductor).Field("currentPreset")) {
                enumerator.MoveNext();
                CodeInstruction next = enumerator.Current;
                if(next.opcode == OpCodes.Ldfld) {
                    yield return current;
                    yield return next;
                    continue;
                }
                yield return new CodeInstruction(OpCodes.Ldsflda, typeof(FloatOffset).Field("Instance"));
                while(next.opcode != OpCodes.Call) {
                    yield return next;
                    enumerator.MoveNext();
                    next = enumerator.Current;
                }
                yield return new CodeInstruction(OpCodes.Call, typeof(FloatOffset).Setter("Offset"));
                do {
                    enumerator.MoveNext();
                    next = enumerator.Current;
                } while(next.opcode != OpCodes.Call || (MethodInfo) next.operand != typeof(scrConductor).Method("SaveCurrentPreset"));
                continue;
            }
            yield return current;
        }
    }

    [JAPatch(typeof(scrCalibrationPlanet), "PutDataPoint", PatchType.Transpiler, false)]
    private static IEnumerable<CodeInstruction> PutDataPoint(IEnumerable<CodeInstruction> instructions) {
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        while(enumerator.MoveNext()) {
            CodeInstruction current = enumerator.Current;
            if(current.opcode == OpCodes.Ldfld && current.operand is FieldInfo field && field == typeof(scrCalibrationPlanet).Field("txtLastOffset")) {
                while(current.opcode != OpCodes.Call) {
                    enumerator.MoveNext();
                    current = enumerator.Current;
                }
                do {
                    enumerator.MoveNext();
                } while(current.opcode != OpCodes.Call);
                yield return new CodeInstruction(OpCodes.Ldstr, "0.##");
                yield return new CodeInstruction(OpCodes.Call, typeof(double).Method("ToString", typeof(string)));
                enumerator.MoveNext();
                current = enumerator.Current;
            }
            yield return current;
        }
    }

    private class FloatOffsetSettings : JASetting {
        public Dictionary<string, float> Offset = new();

        public FloatOffsetSettings(JAMod mod, JObject jsonObject = null) : base(mod, jsonObject) {
            _settings = this;
        }
    }
}