namespace BetterCalibration {
    public class Values {
        public static Values Korean = new Values {
            Pitch = "보정 피치",
            Minimum = "최소 보정값",
            Popup = "오프셋 조정 팝업 표시",
            ChangeOffset = "입력 오프셋을 다음과 같이 바꾸겠습니까?\n{0} -> {1}"
        };

        public static Values English = new Values {
            Pitch = "Calibration Pitch",
            Minimum = "Minimum Calibration Value",
            Popup = "Show Offset Adjust Popup",
            ChangeOffset = "Do you want to set your input offset from\n{0}ms to {1}ms?"
        };

        public string Pitch;
        public string Minimum;
        public string Popup;
        public string ChangeOffset;
    }
}