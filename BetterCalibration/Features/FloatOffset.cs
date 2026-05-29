using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using JALib.Core;
using JALib.Core.Patch;
using JALib.Core.Setting;
using JALib.Tools;
using Newtonsoft.Json.Linq;
using UnityEngine;
using MethodInfo = System.Reflection.MethodInfo;
using CodeInstruction = HarmonyLib.CodeInstruction;

namespace BetterCalibration.Features;

public class FloatOffset : Feature {
    public static FloatOffset Instance;
    private static FloatOffsetSettings _settings;

    public float Offset {
        get => _settings.Offset.TryGetValue(scrConductor.currentPreset.outputName, out float offset) ? offset : scrConductor.currentPreset.inputOffset;
        set {
            int cur = Mathf.RoundToInt(value);
            if(scrConductor.currentPreset.inputOffset != cur) {
                scrConductor.currentPreset.inputOffset = cur;
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
        ref string offsetString = ref Main.OffsetString;
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

    [JAPatch(typeof(scrConductor), "get_calibration_i", PatchType.Replace, true)]
    private static float GetCalibration() => Instance.Offset / 1000f;

    [JAPatch(typeof(SettingsMenu), nameof(SettingsMenu.UpdateSetting), PatchType.Prefix, false)]
    private static bool UpdateSetting(ref PauseSettingButton ___offsetButton, PauseSettingButton setting, SettingsMenu.Interaction action) {
        if(setting.name != "inputOffset" || action is SettingsMenu.Interaction.ActivateInfo or SettingsMenu.Interaction.Activate) return true;
        ___offsetButton = setting;
        if(action == SettingsMenu.Interaction.Refresh) {
            setting.CachedValue = null;
            setting.initialValue = Instance.Offset;
        } else {
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
        setting.valueLabel.text = Offset.ToString("0.##") + Main.RdStringGet("editor.unit." + setting.unit);
    }

    [JAPatch("scrCalibrationPlanet", "PostSong", PatchType.Transpiler, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "Calibrated", PatchType.Transpiler, false, MinVersion = 141)]
    private static IEnumerable<CodeInstruction> PostSong(IEnumerable<CodeInstruction> instructions) {
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        while(enumerator.MoveNext()) {
            CodeInstruction current = enumerator.Current!;
            // ---- original code C# ----
            // double num = Math.Round(averageTimeOffset * 1000.0);
            // ---- replaced code C# ----
            // double num = Math.Round(averageTimeOffset * 1000.0, 2);
            // ---- original code IL ----
            // IL_0107: ldarg.0      // this
            // IL_0108: ldfld        float64 scrCalibrationPlanet::averageTimeOffset
            // IL_010d: ldc.r8       1000
            // IL_0116: mul
            // IL_0117: call         float64 [mscorlib]System.Math::Round(float64)
            // IL_011c: stloc.2      // V_2
            // ---- replaced code IL ----
            // ldarg.0      // this
            // ldfld        float64 scrCalibrationPlanet::averageTimeOffset
            // ldc.r8       1000
            // mul
            // ldc.i4.2
            // call         float64 [mscorlib]System.Math::Round(float64, int32)
            // stloc.2      // V_2
            if(current.opcode == OpCodes.Call && current.operand is MethodInfo { Name: "Round" }) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                yield return new CodeInstruction(OpCodes.Call, typeof(Math).Method("Round", typeof(double), typeof(int)));
                continue;
            }
            if(current.opcode == OpCodes.Ldsflda && current.operand is FieldInfo { Name: "currentPreset" }) {
                enumerator.MoveNext();
                CodeInstruction next = enumerator.Current!;
                if(next.opcode == OpCodes.Ldfld) {
                    yield return current;
                    yield return next;
                    continue;
                }
                // ---- original code C# ----
                // scrConductor.currentPreset.inputOffset = Mathf.RoundToInt((float)(averageTimeOffset * 1000.0));
                // ---- replaced code C# ----
                // FloatOffset.Instance.Offset = (float)(averageTimeOffset * 1000.0);
                // ---- original code IL ----
                // IL_0395: ldsflda      valuetype CalibrationPreset scrConductor::currentPreset
                // IL_039a: ldarg.0      // this
                // IL_039b: ldfld        float64 scrCalibrationPlanet::averageTimeOffset
                // IL_03a0: ldc.r8       1000
                // IL_03a9: mul
                // IL_03aa: conv.r4
                // IL_03ab: call         int32 [UnityEngine.CoreModule]UnityEngine.Mathf::RoundToInt(float32)
                // IL_03b0: stfld        int32 CalibrationPreset::inputOffset
                // ---- replaced code IL ----
                // ldsfld       class BetterCalibration.Features.FloatOffset BetterCalibration.Features.FloatOffset::Instance
                // ldarg.0      // this
                // ldfld        float64 scrCalibrationPlanet::averageTimeOffset
                // ldc.r8       1000
                // mul
                // conv.r4
                // call         instance void class BetterCalibration.Features.FloatOffset::set_Offset(float32)
                yield return new CodeInstruction(OpCodes.Ldsfld, typeof(FloatOffset).Field("Instance"));
                while(next!.opcode != OpCodes.Call) {
                    yield return next;
                    enumerator.MoveNext();
                    next = enumerator.Current;
                }
                yield return new CodeInstruction(OpCodes.Call, typeof(FloatOffset).Setter("Offset"));
                enumerator.MoveNext();
                enumerator.MoveNext();
                continue;
            }
            yield return current;
        }
    }

    [JAPatch("scrCalibrationPlanet", "PutDataPoint", PatchType.Transpiler, false, MaxVersion = 140)]
    [JAPatch(nameof(scnCalibration), "CheckConsistency", PatchType.Transpiler, false, MinVersion = 141)]
    private static IEnumerable<CodeInstruction> PutDataPoint(IEnumerable<CodeInstruction> instructions) {
        using IEnumerator<CodeInstruction> enumerator = instructions.GetEnumerator();
        while(enumerator.MoveNext()) {
            CodeInstruction current = enumerator.Current!;
            // ---- original code C# ----
            // txtLastOffset.text = Math.Round(offset * 1000.0) + RDString.Get("editor.unit.ms");
            // ---- replaced code C# ----
            // txtLastOffset.text = Math.Round(offset * 1000.0, 2) + RDString.Get("editor.unit.ms");
            // ---- original code IL ----
            // IL_0053: ldarg.0      // this
            // IL_0054: ldfld        class [UnityEngine.UI]UnityEngine.UI.Text scrCalibrationPlanet::txtLastOffset
            // IL_0059: ldloc.0      // V_0
            // IL_005a: ldc.r8       1000
            // IL_0063: mul
            // IL_0064: call         float64 [mscorlib]System.Math::Round(float64)
            // IL_0069: stloc.3      // V_3
            // IL_006a: ldloca.s     V_3
            // IL_006c: call         instance string [mscorlib]System.Double::ToString()
            // IL_0071: ldstr        "editor.unit.ms"
            // IL_0076: ldnull
            // IL_0077: ldc.i4.0
            // IL_0078: call         string RDString::Get(string, class [mscorlib]System.Collections.Generic.Dictionary`2<string, object>, valuetype ['Assembly-CSharp-firstpass']SA.GoogleDoc.LangSection)
            // IL_007d: call         string [mscorlib]System.String::Concat(string, string)
            // ---- replaced code IL ----
            // ldarg.0      // this
            // ldfld        class [UnityEngine.UI]UnityEngine.UI.Text scrCalibrationPlanet::txtLastOffset
            // ldloc.0      // V_0
            // ldc.r8       1000
            // mul
            // ldc.i4.2
            // call         float64 [mscorlib]System.Math::Round(float64, int32)
            // stloc.3      // V_3
            // ldloca.s     V_3
            // call         instance string [mscorlib]System.Double::ToString()
            // ldstr        "editor.unit.ms"
            // ldnull
            // ldc.i4.0
            // call         string RDString::Get(string, class [mscorlib]System.Collections.Generic.Dictionary`2<string, object>, valuetype ['Assembly-CSharp-firstpass']SA.GoogleDoc.LangSection)
            // call         string [mscorlib]System.String::Concat(string, string)
            if(current.opcode == OpCodes.Call && current.operand is MethodInfo { Name: "Round" }) {
                yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                yield return new CodeInstruction(OpCodes.Call, typeof(Math).Method("Round", typeof(double), typeof(int)));
                continue;
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