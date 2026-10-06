# Audit khắc phục migration Unity 2017 -> Unity 6

Ngày kiểm tra: 13/09/2026. Kế hoạch tải xuống được dùng làm tài liệu tham khảo; trạng thái dưới đây được xác minh trực tiếp trong dự án.

- URP `17.6.0` và toàn bộ package 2D cần thiết đã được cài. Quality Settings trỏ tới URP asset.
- Input System `1.20.0` đang là backend duy nhất (`activeInputHandler: 1`). Đây là chủ ý vì runtime đã thay toàn bộ `Input.GetKey` cũ bằng Input System; bật `Both` chỉ cần thiết khi còn giữ code legacy.
- Touch runtime giữ pointer ID riêng cho từng nút/joystick, hỗ trợ thao tác đồng thời, safe area và gamepad. Việc dùng implementation riêng thay `OnScreenStick` vẫn theo cùng mô hình pointer/drag của Input System và tránh tạo virtual-device event trùng.
- Sprite map dùng material thay thế tương thích URP; validator kiểm tra shader hỗ trợ trên cả năm phòng. Pixel art dùng unlit để giữ màu atlas gốc, thay vì tự động đổi mọi sprite sang lit và làm sai độ sáng.
- Rewired, Odin và FMOD runtime không được nhập. Input dùng Unity Input System, save dùng cấu trúc `[Serializable]`, 60 WAV giải mã từ bank gốc phát qua AudioSource pool.
- Không nhập script 2017 vào assembly Unity 6 nên API Updater không phải đường chính. Script gốc chỉ dùng để lấy serialized settings, state timing và Animation Event.

## HUD nguồn

HUD trước đây chỉ dùng ảnh crop gốc nhưng tự vẽ khung thanh bằng Panel/Outline và kéo health lên 272 px. Scene nguồn `GenericElements.unity` và `PlayerHealth.cs` xác nhận health fill rộng 100 px ở tỉ lệ gốc, `Image.Type.Filled`, speed 8, nền giữa 78,01 px và có lớp Health Loss.

HUD mới dùng `PortraitFrame`, `HealthFill`, `HealthLoss`, `FervourFill`, `BarMid`, `BarEnd`, flask và Tears frame gốc. Tỉ lệ hiển thị 2x là health 200 px và fervour 140 px; health loss nội suy riêng. Các thành phần vẫn nằm trong safe area còn nút cảm ứng ở canvas độc lập.
