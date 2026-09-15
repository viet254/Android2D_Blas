# Kiểm kê tiến độ theo các kế hoạch tham khảo

Ngày rà soát: 13/09/2026. Chỉ đánh dấu hoàn thành khi có kiểm tra trong Unity hoặc bằng chứng dữ liệu cụ thể.

## Đã hoàn thành và có kiểm tra

- Nhập 5 scene D17Z01S01, S02, S05, S11 và S03; giữ hierarchy, transform, sprite rect/pivot/PPU, sorting và collider địa hình.
- Player spawn/grounded tại cả 5 phòng; chân Player, Acolyte, Flagellant và Elder Brother khớp điểm đất.
- Motor kinematic: đi, dốc không tự trượt, nhảy biến thiên, rơi, one-way drop-through, dash, ledge climb và ladder.
- Combat nền: combo, input buffer, parry, riposte, plunge, damage, i-frame, flask và death/respawn.
- Prie Dieu: giữ tương tác, animation, hồi máu/flask, checkpoint, save và reset enemy.
- Blood platform: relic gate, chuỗi kích hoạt, collider và thời gian biến mất.
- Touch nền: pointer ID riêng, joystick nổi, nút giữ/nhấn, bàn phím và gamepad.
- Hai canvas HUD/control, safe area, menu pause giữa màn hình.
- Main Menu là scene đầu: nền 640×360 được cắt đúng rect nguồn, animation Penitent 22 frame, Begin/Continue, loading async, touch/keyboard/controller EventSystem và safe area.
- Validator hiện báo 896 renderer có tham chiếu, 37 tham chiếu rỗng có chủ ý, 0 lỗi import.

## Đã có nhưng chưa đạt đặc tả 100%

- Player dùng sprite/clip gốc nhưng state machine là code phục dựng; chưa khớp toàn bộ timing/transitions của `Player.controller` gốc.
- Acolyte và Flagellant có nhịp wind-up, tầm đánh và sát thương riêng; execution gọi đúng clip gốc từng họ. Vị trí encounter vẫn được bố trí theo kế hoạch, chưa phục hồi toàn bộ patrol/spawn gốc.
- Elder Brother có intro rơi, hai đòn AREA/JUMP luân phiên theo cấu hình D17, phase dưới 50% tạo shockwave, boss gate, death/corpse và nhịp blood cleansing nền. Cinematic camera chi tiết vẫn cần so khớp từng shot nguồn.
- HUD dùng khung/chân dung/health/fervour/health-loss/bar-end/vạch fervour/flask/Tears từ atlas gốc và tỉ lệ lấy từ scene UI nguồn. Toàn bộ nhãn HUD, Character Menu và Main Menu dùng font bitmap `MajesticExtended_Pixel_Scroll` gốc; đủ 68 icon trang bị và kéo-thả vị trí từng nút touch có lưu cấu hình.
- Menu chính đã dùng đúng background rect và animation gốc; có New Game, Continue, Options, Extras, volume/kích thước touch/kiểu joystick/haptic. Extras có luồng purge save được bảo vệ bằng màn hình cảnh báo và xác nhận. Chưa có title logo bitmap, nhiều save slot và audio/menu transition đầy đủ như scene gốc.
- Có 75 mẫu audio gốc; Fervorous Blood đã nối âm thanh thi triển, bay, trúng, nổ và biến mất. Chưa khôi phục đầy đủ event graph FMOD, mixer, ambience và toàn bộ âm thanh theo state.
- Parallax đọc dữ liệu nguồn; shader/mask/lighting đặc thù vẫn cần so hình từng camera region.

## Chưa hoàn thành

- Đã có crouch attack, hai air attack, upward attack, charged attack và lunge theo tier nguồn. Projectile charged tier 3 dùng đúng ba clip gốc, bay 4 m trong 0,3 giây, va enemy/tường và dùng effect pool; còn thiếu các special skill khác.
- Prayer và Sacred Onslaught đã có input, progression gate và nút mobile. Character Menu có 68 mục gốc (13 prayer, 7 relic, 39 rosary bead, 9 sword heart), 68 icon và mô tả lấy từ prefab nguồn, phân trang 7 ô, chọn/trang bị/tháo và trạng thái trang bị được lưu trong save phiên bản 4. Catalog giữ 100 component hiệu ứng nguồn; các modifier stat chung đã nối vào Life, Fervour, Flask, Strength, phòng thủ thường, Speed, DashRide, DashCooldown, Prayer cost/duration/strength và AttackSpeed tạm thời. Các hiệu ứng đặc thù như beam, cherub, shield, toxic cloud và guardian vẫn chưa hoàn chỉnh.
- Execution hiện đã riêng cho Acolyte và Flagellant; còn thiếu căn camera/timing/VFX kết liễu chi tiết như bản gốc.
- Đã có dust chạy/dừng/landing, dash ghost, parry/pushback/plunge, throwback arc/ground-contact dust và sword-slash renderer riêng cho các hướng đánh; còn thiếu VFX theo đủ từng frame event còn lại.
- Boss cinematic camera chi tiết và trình tự blood cleansing đầy đủ theo scene nguồn.
- Hiệu ứng gameplay riêng cho toàn bộ prayer/relic/rosary/sword heart.
- Cỡ nút, fixed/floating joystick, haptic và vị trí kéo-thả riêng của từng touch control đã có và được lưu.
- Đo draw call `<= 25`, profiler frame time/GC, RAM/thermal và xác nhận 60 FPS trên thiết bị Android thật.
- Kiểm tra các tỉ lệ trên thiết bị Android thật; bộ kiểm tra Editor đã qua 16:9, 18:9, 19.5:9 và 20:9.

## Các vấn đề kỹ thuật tìm thấy

1. Asset menu từng bị dùng sai: cả atlas `MainMenuBackground.png` được kéo lên UI. Rect nguồn đúng là `(0, 664, 640, 360)`; đã cắt lại.
2. Animation menu từng dùng một ảnh tĩnh khác. Nguồn thật là sheet 2048×4096 với 22 sprite và timing dài 3,2 giây; đã phục hồi chuỗi frame.
3. Main Menu từng không có `EventSystem`, nên Button không nhận touch. Đã thêm `InputSystemUIInputModule` và chọn Begin mặc định cho gamepad/keyboard.
3a. Sau khi thêm Options/Extras, module Input System vẫn chưa được gán default actions và các nhãn Text nằm trên Button còn chặn raycast. Đã gán pointer/touch/navigation actions, tắt raycast của nhãn và thêm Camera menu để Unity không phủ `No cameras rendering`.
4. Player/enemy từng cộng offset hình ảnh `+0.65` dù pivot sprite nguồn đã đặt tại chân. Đã bỏ offset và thêm kiểm tra bounds.
5. Điểm bắt đầu bị hiểu nhầm do Play mở thẳng gameplay và đọc save. Rebuild hiện để Editor ở Main Menu; Begin xóa save và vào S01.
6. Prie Dieu dùng hai ngưỡng tương tác khác nhau, làm thao tác giữ bị hủy. Đã dùng chung một vùng tương tác.
7. Hệ automation từng so timestamp runtime source với editor DLL và refresh vô hạn; đã sửa cách chọn assembly mới nhất.
8. Enter Play Mode bỏ Domain Reload làm callback test không ổn định; bộ kiểm tra ép cấu hình domain reload cho kết quả lặp lại được.
9. Clip execution enemy là composite đã chứa Penitent; giữ renderer Player thật tạo hình nhân vật thứ hai. Runtime giờ ẩn Player theo đúng duration clip và bật lại sau khi kết liễu xong.
10. Dash trước đây chỉ đổi vận tốc với capsule đứng cao 1,15 m nên mắc ở khe thấp. Dash giờ hạ capsule xuống 0,62 m, kiểm tra khoảng trống trước khi đứng lại, phát bụi gốc và afterimage qua object pool.
11. Charged tier 3 trước đây chỉ quét một vùng trúng tức thời. Runtime giờ phục dựng projectile từ prefab nguồn: 4 m/0,3 giây, ba animation flight/impact/vanish, hitbox, terrain blocking, audio và object pool cố định.

## Bằng chứng

- `Documentation/playtest-results.txt`: gameplay `RESULT: PASSED`.
- `Documentation/menu-playtest-results.txt`: menu `RESULT: PASSED`.
- `Documentation/map-validation.txt`: import renderer `errors: 0`.
- `Documentation/Previews/main-menu-playtest.png`: ảnh menu 1280×720 từ Play Mode.
- `Documentation/Previews/hud-playtest.png`: HUD Play Mode mới dùng portrait, gauge, flask và Tears frame trích đúng từ dữ liệu gốc; overlay DEBUG không còn che góc trái.
- HUD/progression: các nút Jump, Attack, Dash, Parry, Flask, Map và Bag được ràng buộc với trạng thái mở khóa; kiểm thử tự động xác nhận Dash khóa thì ẩn và mở khóa thì hiện.
- `Documentation/menu-playtest-results.txt` mới xác nhận New Game, Continue, Options, Extras và overlay tùy chỉnh đều được tạo.
- Menu test mới còn xác nhận input module có action pointer/touch/navigation và mọi nhãn menu đều cho raycast đi xuyên xuống Button.
- Playtest gameplay xác nhận execution Acolyte/Flagellant dùng clip nguyên bản theo đúng họ enemy.
- Playtest gameplay xác nhận không còn duplicate Player trong execution, collider thấp đi qua tunnel, collider đứng phục hồi, bụi dash và afterimage đều được kích hoạt; toàn suite `RESULT: PASSED`.
- Playtest gameplay xác nhận projectile charged tier 3 được lấy từ pool, bay trúng enemy và dừng khi va terrain; toàn suite vẫn `RESULT: PASSED`.
- Playtest xác nhận Warden hoàn tất intro, vào phase hai ở 50% máu và chuyển sang clip corpse sau death. Character Menu năm trang và các nút Prayer/Special theo trạng thái mở khóa cũng đã qua kiểm tra.
- Playtest xác nhận ba sword-slash gốc được import và renderer slash xuất hiện cùng đòn đánh. Heavy damage dùng throwback transition và quỹ đạo hất tung. Menu test xác nhận có purge save được bảo vệ.
- `Documentation/Previews/character-menu-playtest.png` xác nhận Character Menu nằm giữa màn hình, ẩn HUD/control gameplay khi mở và hiển thị icon vật phẩm nguồn; `playtest-results.txt` xác nhận đủ 68 icon và trang bị prayer dùng chung trạng thái gameplay.
- Save phiên bản 4 lưu prayer, sword heart, tối đa ba relic và hai rosary bead theo số ô HUD đầu game; playtest xác nhận rosary bead có thể trang bị/tháo mà không mất quyền sở hữu.
- `Tools/enrich_inventory_effects.py` đọc trực tiếp prefab inventory Unity 2017 và ghi `fervourNeeded`, `EffectTime`, `StatsTypes`, value/multiplier cùng tên component đặc thù vào catalog. Playtest xác nhận PR01 dùng đúng 20 Fervour, thời lượng 10 giây và tăng AttackSpeed; RB01 giảm 10% sát thương thường; HE03 cộng 8 Strength.
- Bộ nút và joystick hiện lấy trực tiếp từ AssetRipper Android `D:/game/Bla_mobile_map/ExportedProject`: Attack, Dash, Jump, Parry, Flask, Prayer, RangeAttack, Map, Inventory, HD_BaseJoystick và HD_ControlJoystick. Playtest xác nhận toàn bộ resource mobile tải thành công.

## Đối chiếu bản Android chính thức

- Trang Google Play chính thức xác nhận bản Android hỗ trợ touch và gamepad, chơi offline, không quảng cáo/vi giao dịch; Update 1.9 có sửa touch controls.
- Vì điều khiển cảm ứng là phần được nhà phát hành tiếp tục vá sau phát hành, dự án giữ hai lựa chọn joystick fixed/floating, ba cỡ nút và lưu cấu hình bằng PlayerPrefs. Các mục này vẫn cần kiểm tra trên thiết bị thật trước khi nghiệm thu cuối.

Chưa được phép dùng từ “hoàn thành 100%” cho toàn kế hoạch khi các mục trong hai phần “chưa đạt” và “chưa hoàn thành” còn tồn tại.
