using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BetterCalibration {
    public class ShowCalibration {
        private static GameObject _gameObject;
        private static Text _popupText;
        public static List<float> Timings = new List<float>();
        private static int ChangeOffset;

        public static void Initialize() {
            _gameObject = new GameObject("BetterCalibration Object");
            Canvas canvas = CreateCanvas();
            _gameObject.AddComponent<GraphicRaycaster>();
            CreateBackground(canvas);
            GameObject popupObject = CreatePopupObject(canvas);
            CreateText(popupObject);
            AddButton(popupObject, "Yes", Yes, -140);
            AddButton(popupObject, "No", Hide, 140);
        }

        private static Canvas CreateCanvas() {
            Canvas canvas = _gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12345;
            CanvasScaler canvasScaler = _gameObject.AddComponent<CanvasScaler>();
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            return canvas;
        }

        private static void CreateBackground(Canvas canvas) {
            GameObject backgroundPanel = new GameObject("BackgroundPanel");
            RectTransform panelTransform = backgroundPanel.AddComponent<RectTransform>();
            backgroundPanel.transform.SetParent(canvas.transform, false);
            panelTransform.sizeDelta = new Vector2(600, 300);
            panelTransform.anchoredPosition = Vector2.zero;
            Image panelImage = backgroundPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
        }

        private static GameObject CreatePopupObject(Canvas canvas) {
            GameObject popupObject = new GameObject("Popup");
            RectTransform popupTransform = popupObject.AddComponent<RectTransform>();
            popupObject.transform.SetParent(canvas.transform, false);
            popupTransform.sizeDelta = new Vector2(600, 300);
            popupTransform.anchoredPosition = Vector2.zero;
            return popupObject;
        }

        private static void CreateText(GameObject popupObject) {
            GameObject textObject = new GameObject("Popup Text");
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
            GameObject buttonObject = new GameObject(buttonText + "Button");
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
            ChangeOffset = original + GetTimingAverage();
            _popupText.text = string.Format(Main.GetValues().ChangeOffset, original, ChangeOffset);
        }

        private static void Yes() {
            scrConductor.currentPreset.inputOffset = ChangeOffset;
            scrConductor.SaveCurrentPreset();
            Hide();
        }

        private static int GetTimingAverage() {
            return Timings.Count == 0 ? 0 : Mathf.RoundToInt(Timings.Sum() / Timings.Count);
        }

        public static void Show() {
            if(_gameObject == null) Initialize();
            SetupText();
            Object.DontDestroyOnLoad(_gameObject);
        }

        public static void Hide() {
            if(_gameObject == null) return;
            Object.DestroyImmediate(_gameObject);
        }
    }
}