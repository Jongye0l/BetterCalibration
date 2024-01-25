namespace BetterCalibration {
    public class Values {
        public static readonly Values Korean = new Values {
            Pitch = "보정 피치",
            Minimum = "최소 보정값",
            Popup = "오프셋 조정 팝업 표시",
            ChangeOffset = "입력 오프셋을 다음과 같이 바꾸겠습니까?\n{0} -> {1}",
            Language = "언어",
            Default = "기본값"
        };

        public static readonly Values English = new Values {
            Pitch = "Calibration Pitch",
            Minimum = "Minimum Calibration Value",
            Popup = "Show Offset Adjust Popup",
            ChangeOffset = "Do you want to set your input offset from\n{0}ms to {1}ms?", 
            Language = "Language",
            Default = "Default"
        };

        public static readonly Values Japanese = new Values {
            Pitch = "較正ピッチ",
            Minimum = "最小較正値",
            Popup = "オフセット調整ポップアップを表示",
            ChangeOffset = "入力オフセットを変更しますか？\n{0} -> {1}",
            Language = "言語",
            Default = "既定値"
        };

        public string Pitch;
        public string Minimum;
        public string Popup;
        public string ChangeOffset;
        public string Language;
        public string Default;
    }
}
