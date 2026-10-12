# Android2D_Blas

Dự án tái hiện và tối ưu hóa trải nghiệm hành động Metroidvania 2D phong cách *Blasphemous* trên nền tảng di động (Android ARM64) sử dụng **Unity 6**.

---

## 📌 Tổng quan dự án

`Android2D_Blas` tập trung vào việc phục hồi tuyến bản đồ Brotherhood (**S01 → S02 → S05 → S11 → S03**), xây dựng lại hệ thống điều khiển cảm ứng chuyên dụng cho thiết bị di động, cơ chế di chuyển/chiến đấu mượt mà, và hệ thống tối ưu hóa tài nguyên cho kiến trúc di động.

- **Unity Version:** `6000.5.9f1`
- **Nền tảng mục tiêu:** Android (ARM64) & Windows (Testing)
- **Độ phân giải tham chiếu:** `1280 × 720` (hỗ trợ `Screen.safeArea` cho màn hình tai thỏ/nốt ruồi)

---

## 🎮 Các tính năng chính

### 1. Cơ chế di chuyển & Vật lý 2D
- **Kinematic Motor:** Xử lý va chạm mượt mà trên địa hình phẳng, dốc (slopes), nền tảng một chiều (one-way / drop-through platforms).
- **Kỹ năng cơ bản:** Di chuyển, nhảy, leo mép tường/thang, trượt (dash).
- **Cơ chế bệ máu (Faith Platforms):** Kích hoạt có điều kiện theo trạng thái trang bị Relic, tự động kích hoạt bệ tiếp theo theo chuỗi.

### 2. Chiến đấu & Kỹ năng
- **Mea Culpa Combo:** Chuỗi đòn đánh cận chiến, animation và hitbox chính xác.
- **Phản đòn (Parry) & Lướt (Dash):** Đỡ đòn đúng thời điểm, dash hồi chiêu cân bằng.
- **Đòn cắm kiếm (Plunge Attack):** Tấn công từ trên không xuống đất.
- **Hồi phục & Trạng thái:** Sử dụng bình Bile Flask hồi máu, thanh Fervour (Nhiệt thành), cơ chế tử vong và hồi sinh tại Prie Dieu (Checkpoint).

### 3. Kẻ địch & Boss
- **Quái thường:** Acolyte, Flagellant với AI tuần tra và phản ứng khi phát hiện người chơi.
- **Boss Encounter:** Elder Brother of the Silent Sorrow (Phòng S11) với thanh máu Boss riêng biệt, các pha tấn công đặc trưng.

### 4. Giao diện người dùng di động (Mobile UI)
- **Hệ thống điều khiển cảm ứng:**
  - Joystick động (Dynamic Touch Joystick) xuất hiện linh hoạt tại vị trí ngón tay chạm.
  - Cụm nút bấm hành động (Tấn công, Nhảy, Dash, Dùng Flask, Tương tác) bố trí công thái học theo vòng cung ngón tay cái.
  - Hỗ trợ đa chạm (Multi-touch với `pointerId` độc lập).
- **Giao diện HUD:** Chân dung nhân vật, thanh Máu (Health), Nhiệt thành (Fervour), Bình thuốc (Flasks), thanh máu Boss.
- **Menu nhân vật / Túi đồ:** Xem thông tin nhân vật, phòng hiện tại, vật phẩm và di vật.

### 5. Âm thanh (Audio System)
- Hệ thống Audio Pool phát hiệu ứng âm thanh (SFX) từ nguồn gốc đã giải mã, tối ưu không phụ thuộc vào thư viện bên ngoài ở runtime.

---

## 📁 Cấu trúc thư mục

```text
Android2D_Blas/
├── Assets/                 # Toàn bộ mã nguồn C#, Scenes, Sprites, Prefabs, Animations
├── Packages/               # Quản lý Package của Unity (manifest.json, ...)
├── ProjectSettings/        # Cấu hình dự án Unity (Physics2D, Graphics, Tags, Layers...)
├── Tools/                  # Bộ công cụ Python xử lý asset, map và dữ liệu
│   ├── .verification/      # Dữ liệu đối soát & baseline
│   ├── vgmstream/          # Bộ giải mã âm thanh
│   └── *.py                # Scripts trích xuất, import sprite, map, audio, inventory
├── Documentation/          # Tài liệu thiết kế, kế hoạch triển khai và nhật ký kiểm thử
├── .gitignore              # Cấu hình bỏ qua file tạm cho Unity, IDE và Python
└── README.md               # Tài liệu tổng quan dự án
```

---

## 🛠 Hướng dẫn cài đặt & Phát triển

### Yêu cầu
1. **Unity Hub** và **Unity Editor `6000.5.9f1`** (kèm module *Android Build Support*, *OpenJDK*, *Android SDK & NDK Tools*).
2. **Visual Studio 2022** hoặc **Visual Studio Code** với C# Dev Kit / Unity Extension.
3. *(Tùy chọn cho Tools)*: Python 3.10+ nếu cần chạy các script xử lý trong thư mục `Tools/`.

### Khởi động dự án
1. Mở **Unity Hub** -> Chọn **Add project from disk** -> Trỏ tới thư mục `Android2D_Blas`.
2. Chờ Unity nạp Packages và biên dịch Shader/Assets lần đầu.
3. Mở scene bắt đầu trong thư mục `Assets/Scenes/`.

---

## 🕹 Phím điều khiển khi thử nghiệm trên PC (Editor)

| Thao tác | Bàn phím | Tay cầm (Gamepad / Xbox) |
| :--- | :--- | :--- |
| **Di chuyển** | `A` / `D` hoặc `Phím mũi tên` | D-Pad / Cần gạt trái (Left Stick) |
| **Nhìn lên / Leo** | `W` / `Mũi tên lên` | Cần gạt lên |
| **Cúi / Xuống nền** | `S` / `Mũi tên xuống` | Cần gạt xuống |
| **Nhảy** | `Space` / `K` | Nút `A` |
| **Tấn công** | `J` | Nút `X` |
| **Lướt (Dash)** | `L` / `Left Shift` | Nút `RT` / `RB` |
| **Đỡ đòn (Parry)** | `I` | Nút `LT` / `LB` |
| **Hồi máu (Bile Flask)** | `U` | Nút `B` / `Y` |
| **Tương tác (Prie Dieu)** | `E` | Nút `Y` / `Up` |

---

## 📦 Hướng dẫn Build Android

1. Mở **File** > **Build Profiles** (hoặc **Build Settings**).
2. Chuyển Platform sang **Android** (Switch Platform).
3. Đảm bảo cấu hình trong **Player Settings**:
   - **Scripting Backend:** `IL2CPP`
   - **Target Architectures:** `ARM64` (bỏ chọn ARMv7 nếu không cần thiết)
   - **Minimum API Level:** Android 8.0 (API Level 26) trở lên
4. Nhấn **Build** hoặc **Build and Run** (nếu đang kết nối thiết bị qua USB/ADB).
