using System;
using System.Linq;
using BetterCalibration.DoubleFeaturePatch;
using JALib.Core;
using JALib.Core.Patch;
using MonsterLove.StateMachine;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BetterCalibration.Features;

public class CalibrationPopup() : Feature(Main.Instance, nameof(CalibrationPopup), true, typeof(CalibrationPopup)) {
    private static GameObject _gameObject;
    private static Text _popupText;
    private static int _changeOffset;

    protected override void OnEnable() => Timing.Instance.AddPatch(this);

    protected override void OnDisable() {
        Timing.Instance.RemovePatch(this);
        Hide();
        _popupText = null;
        _gameObject = null;
    }

    [JAPatch(typeof(StateBehaviour), "ChangeState", PatchType.Postfix, true, ArgumentTypesType = [typeof(Enum)])]
    public static void OnChangeState(Enum newState) {
        if((States) newState == States.Fail2) Show();
        else Hide();
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
        _popupText.text = string.Format(Main.Instance.Localization["Popup.ChangeOffset"], original, _changeOffset);
    }

    private static void Yes() {
        scrConductor.currentPreset.inputOffset = _changeOffset;
        scrConductor.SaveCurrentPreset();
        Hide();
    }

    private static int GetTimingAverage() => Timing.Timings.Count == 0 ? 0 : Mathf.RoundToInt(Timing.Timings.Average());

    private static void Show() {
        if(!_gameObject) Initialize();
        SetupText();
        Object.DontDestroyOnLoad(_gameObject);
        Cursor.visible = true;
    }

    [JAPatch(typeof(scrController), "TogglePauseGame", PatchType.Postfix, true)]
    private static void Hide() {
        if(!_gameObject) return;
        Object.DestroyImmediate(_gameObject);
        Cursor.visible = !Persistence.GetHideCursorWhilePlaying();
    }
}