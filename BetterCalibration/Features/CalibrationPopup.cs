﻿using System;
using System.Collections.Generic;
using System.Linq;
using JALib.Core;
using JALib.Core.Patch;
using MonsterLove.StateMachine;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BetterCalibration.Features;

public class CalibrationPopup() : Feature(Main.Instance, nameof(CalibrationPopup), patchClass: typeof(CalibrationPopup)) {
    private static float? _lastTooEarly;
    private static float? _lastTooLate;
    private static GameObject _gameObject;
    private static Text _popupText;
    private static List<float> _timings;
    private static int _changeOffset;

    public override void OnEnable() {
        _timings = [];
    }

    public override void OnDisable() {
        _lastTooEarly = null;
        _lastTooLate = null;
        _timings.Clear();
        _timings = null;
        Hide();
        _popupText = null;
        _gameObject = null;
    }

    [JAPatch("CalibrationPopup.ChageState", typeof(StateBehaviour), "ChangeState", PatchType.Postfix, true, argumentTypesType: [typeof(Enum)])]
    public static void OnChangeState(Enum newState) {
        if((States) newState == States.Fail2) Show();
        else {
            Hide();
            _lastTooEarly = null;
            _lastTooLate = null;
        }
        if((States) newState == States.Start) _timings.Clear();
    }

    [JAPatch("CalibrationPopup.ExitPlay", typeof(scrController), "TogglePauseGame", PatchType.Postfix, true)]
    public static void ExitPlay() {
        Hide();
        _lastTooEarly = null;
        _lastTooLate = null;
    }
    
    [JAPatch("CalibrationPopup.GetTiming", typeof(scrMisc), "GetHitMargin", PatchType.Postfix, true)]
    public static void GetTiming(float hitangle, float refangle, bool isCW, float bpmTimesSpeed, float conductorPitch, HitMargin __result) {
        float angle = (hitangle - refangle) * (isCW ? 1 : -1) * 57.29578f;
        float timing = angle / 180 / bpmTimesSpeed / conductorPitch * 60000;
        switch(__result) {
            case HitMargin.TooEarly:
                _lastTooEarly = timing;
                break;
            case HitMargin.TooLate:
                _lastTooLate = timing;
                break;
            default:
                _timings.Add(timing);
                _lastTooEarly = null;
                _lastTooLate = null;
                break;
        }
    }

    [JAPatch("CalibrationPopup.CheckMiss", typeof(scrMistakesManager), "AddHit", PatchType.Postfix, true)]
    public static void MissCheck(HitMargin hit) {
        if(hit != HitMargin.FailMiss) return;
        if(_lastTooEarly == null || _lastTooLate == null) return;
        _timings.Add((float) _lastTooLate);
        _timings.Add((float) _lastTooEarly);
        _lastTooLate = null;
        _lastTooEarly = null;
    }

    private static void Initialize() {
        _gameObject = new GameObject("Calibration Popup Canvas");
        CreateCanvas();
        _gameObject.AddComponent<GraphicRaycaster>();
        CreateBackground();
        GameObject popupObject = CreatePopupObject();
        CreateText(popupObject);
        AddButton(popupObject, "Yes", Yes, -140);
        AddButton(popupObject, "No", Hide, 140);
    }

    private static void CreateCanvas() {
        Canvas canvas = _gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12345;
        CanvasScaler canvasScaler = _gameObject.AddComponent<CanvasScaler>();
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
    }

    private static void CreateBackground() {
        GameObject backgroundPanel = new("BackgroundPanel");
        RectTransform panelTransform = backgroundPanel.AddComponent<RectTransform>();
        backgroundPanel.transform.SetParent(_gameObject.transform, false);
        panelTransform.sizeDelta = new Vector2(600, 300);
        panelTransform.anchoredPosition = Vector2.zero;
        Image panelImage = backgroundPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
    }

    private static GameObject CreatePopupObject() {
        GameObject popupObject = new("Popup");
        RectTransform popupTransform = popupObject.AddComponent<RectTransform>();
        popupObject.transform.SetParent(_gameObject.transform, false);
        popupTransform.sizeDelta = new Vector2(600, 300);
        popupTransform.anchoredPosition = Vector2.zero;
        return popupObject;
    }

    private static void CreateText(GameObject popupObject) {
        GameObject textObject = new("Popup Text");
        RectTransform textTransform = textObject.AddComponent<RectTransform>();
        textObject.transform.SetParent(popupObject.transform, false);
        textTransform.anchoredPosition = new Vector2(0, 50);
        textTransform.sizeDelta = new Vector2(600, 300);
        _popupText = textObject.AddComponent<Text>();
        _popupText.font = RDString.GetFontDataForLanguage(RDString.language).font;
        _popupText.fontSize = 48;
        _popupText.alignment = TextAnchor.MiddleCenter;
    }

    private static void AddButton(GameObject popupObject, string buttonText, UnityEngine.Events.UnityAction buttonAction, float x) {
        GameObject buttonObject = new(buttonText + "Button");
        buttonObject.transform.SetParent(popupObject.transform, false);
        RectTransform buttonTransform = buttonObject.AddComponent<RectTransform>();
        Text buttonTextComponent = buttonObject.AddComponent<Text>();
        buttonTextComponent.text = buttonText;
        buttonTextComponent.font = RDString.GetFontDataForLanguage(RDString.language).font;
        buttonTextComponent.fontSize = 40;
        buttonTextComponent.alignment = TextAnchor.MiddleCenter;
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(buttonAction);
        buttonTransform.sizeDelta = new Vector2(150, 50);
        buttonTransform.anchoredPosition = new Vector2(x, -100);
    }

    private static void SetupText() {
        int original = scrConductor.currentPreset.inputOffset;
        _changeOffset = original + GetTimingAverage();
        _popupText.text = string.Format(Main.Instance.Localization.Get("Popup.ChangeOffset"), original, _changeOffset);
    }

    private static void Yes() {
        scrConductor.currentPreset.inputOffset = _changeOffset;
        scrConductor.SaveCurrentPreset();
        Hide();
    }

    private static int GetTimingAverage() {
        return _timings.Count == 0 ? 0 : Mathf.RoundToInt(_timings.Sum() / _timings.Count);
    }

    private static void Show() {
        if(!Settings.Instance.ShowPopup) return;
        if(!_gameObject) Initialize();
        SetupText();
        Object.DontDestroyOnLoad(_gameObject);
        Cursor.visible = true;
    }

    private static void Hide() {
        if(!_gameObject) return;
        Object.DestroyImmediate(_gameObject);
        Cursor.visible = !Persistence.GetHideCursorWhilePlaying();
    }
}