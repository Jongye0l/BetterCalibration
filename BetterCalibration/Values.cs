namespace BetterCalibration {
    public class Values {
        public static readonly Values Korean = new Values {
            Key = 0,
            Pitch = "보정 피치",
            Minimum = "최소 보정값",
            Popup = "오프셋 조정 팝업 표시",
            ChangeOffset = "입력 오프셋을 다음과 같이 바꾸겠습니까?\n{0} -> {1}",
            Language = "언어",
            Default = "기본값",
            RepeatSong = "보정 노래 반복횟수",
            CalibrationDetail = "평균: {0}밀리초\n최대: {1}밀리초\n최소: {2}밀리초",
            UseDetail = "보정 자세한 표시"
        };

        public static readonly Values English = new Values {
            Key = 1,
            Pitch = "Calibration Pitch",
            Minimum = "Minimum Calibration Value",
            Popup = "Show Offset Adjust Popup",
            ChangeOffset = "Do you want to set your input offset from\n{0}ms to {1}ms?",
            Language = "Language",
            Default = "Default",
            RepeatSong = "Calibration Song Repeat",
            CalibrationDetail = "Avg: {0}ms\nMax: {1}ms\nMin: {2}ms",
            UseDetail = "Show Calibration Detail"
        };

        public static readonly Values Japanese = new Values {
            Key = 2,
            Pitch = "較正ピッチ",
            Minimum = "最小較正値",
            Popup = "オフセット調整ポップアップを表示",
            ChangeOffset = "入力オフセットを変更しますか？\n{0} -> {1}",
            Language = "言語",
            Default = "既定値",
            RepeatSong = "較正曲の繰り返し数",
            CalibrationDetail = "平均：{0}ms\n最大：{1}ms\n最小：{2}ms",
            UseDetail = "較正の詳細結果を表示"
        };

        public static readonly Values Vietnamese = new Values {
            Key = 3,
            Pitch = "Tốc Độ Nhạc Cân Bằng",
            Minimum = "Giá Trị Cân Bằng Nhỏ Nhất",
            Popup = "Hiện Yêu Cầu Đổi Độ Trễ",
            ChangeOffset = "Bạn Có Muốn Đổi Độ Trễ Của Bạn từ\n{0}ms sang {1}ms?",
            Language = "Ngôn Ngữ",
            Default = "Mặc Định",
            RepeatSong = "Lặp Lại Nhạc Cân Bằng",
            CalibrationDetail = "Tr.Bình: {0}ms\nCao Nhất: {1}ms\nNhỏ Nhất: {2}ms",
            UseDetail = "Hiện Thông Tin Quá Trình Cân Bằng"
        };

        public int Key;
        public string Pitch;
        public string Minimum;
        public string Popup;
        public string ChangeOffset;
        public string Language;
        public string Default;
        public string RepeatSong;
        public string CalibrationDetail;
        public string UseDetail;
    }
}
