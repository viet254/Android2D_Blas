# Kế hoạch triển khai Android2D_Blas — cập nhật 12/09/2026

## Mục tiêu và phạm vi

Phục hồi tuyến Brotherhood **S01 → S02 → S05 → S11 → S03** bằng tài nguyên từ `D:/game/Bla_loc_map/ExportedProject`, đồng thời lấy UI và cấu hình dành riêng cho Android từ `D:/game/Bla_mobile_map/ExportedProject`. Dự án chạy trên Unity 6000.5.9f1 và nhắm Android ARM64. Các bản nguồn luôn chỉ đọc. Không nhập nguyên DLL, script gameplay hoặc ProjectSettings cũ vào dự án mới.

S01 là phòng khởi đầu. S11 chứa ElderBrother. S01 và S05 có Prie Dieu trong dữ liệu nguồn. Các cửa tới S10, S04, phòng relic và vùng ngoài tuyến phải được đánh dấu ngoài phạm vi, không lấp bằng tường hoặc bịa phòng nối.

## Thay đổi từ các tài liệu gửi thêm

### Bệ máu

- Mặc định dùng cơ chế có điều kiện: chưa sở hữu/trang bị relic thì collider tắt, chạy animation `bloodplatform_no_relic_anim`.
- Code nguồn `FaithPlatform.cs` kiểm tra `REVEAL_FAITH_PLATFORMS`: bệ `firstPlatform` xuất hiện trước; tiếp xúc truyền kích hoạt tới danh sách `target`; các bệ sau có `deactivationDelay`. Không đơn giản bật cả chuỗi cùng lúc.
- S01 có 3 bệ, trong đó 1 bệ đầu và 2 liên kết tiếp theo; nhập kích thước/offset collider và liên kết từ source fileID.
- Lưu trạng thái sở hữu/trang bị; có menu Editor để thử bật/tắt. `startWithBloodRelic` chỉ là cấu hình thử nghiệm, mặc định tắt. Việc kiếm relic và UI túi đồ đầy đủ nằm ngoài tuyến chính hiện tại.
- Kiểm tra: chưa trang bị → xuyên qua; trang bị → chỉ bệ đầu bật; đứng trên bệ → bật bệ tiếp; tháo relic → mọi collider tắt; kiểm tra lại sau chuyển phòng và tải save.

### Khoảng trống trên tường và kiểm tra import

Hai ảnh là vị trí cần điều tra, không đủ để kết luận nguyên nhân. Phòng S01 có cửa E tại khoảng (-950, 8) và cửa NE tại (-950, 32); phải phân biệt lối đi có chủ ý với mất mảnh tường. Không cam kết 'tường kín 100%' vì có thể làm mất đường đi nguyên bản.

- Giữ transform, hierarchy, Sorting Layer ID/order, flip, màu và trạng thái renderer từ nguồn. **Không ép tất cả Z về 0 hoặc chuyển hết về Default/Playfield.**
- Phát hiện cụ thể khi rà soát: 17 SpriteRenderer nguồn dùng `Tiled`; một mảnh tường S01 có `size.y = 12`. Bộ chuyển đổi ban đầu chưa giữ drawMode/size. Bổ sung drawMode, size, tileMode, adaptive threshold và sprite border; validator phải kiểm tra các trường này. Đây là lỗi chuyển đổi có bằng chứng, khác với giả định mọi khoảng trống đều do sprite null.
- Gắn định danh `room + section + source Transform fileID` cho từng đối tượng. Kiểm tra bằng định danh và GUID thay vì đoán theo tọa độ/tên trùng.
- Đối chiếu số node và cả 5 lớp của từng phòng; phát hiện mất node, parent sai, sprite null bất thường, rect/pivot/PPU sai, layer thiếu và thứ tự hiển thị lệch.
- Phân biệt sprite vốn null/renderer ẩn trong nguồn với tham chiếu có GUID nhưng không khôi phục được.
- Sau kiểm tra dữ liệu, chụp riêng vùng tường và bệ máu để kiểm tra bằng mắt. Dữ liệu khớp không chứng minh shader/mask hay trạng thái runtime đúng.
- Script `MapImportValidator.cs` đính kèm được dùng làm ý tưởng cho bản kiểm tra mới, bỏ sửa Z hàng loạt và kết luận quá mức 'không có tường trống'.
- `BlasphemousRoomExporter.cs` là phương án xuất tham chiếu tùy chọn trên **bản sao** dự án Unity gốc khi dự án biên dịch được. ExportPackage giữ các tham chiếu hiện có nhưng không sửa shader dummy, script thiếu hoặc asset đã thiếu trước export; IncludeDependencies có thể kéo theo thư viện cũ. Không chạy tự động lên bản nguồn.

## Trình tự thực hiện

1. **Tính đúng của map trước:** bộ chuyển đổi lặp lại được, validator theo nguồn, bệ máu đúng điều kiện, điều tra hai vùng ảnh. Không vá bằng mảnh tự đoán.
2. **Vòng chơi nền tảng:** motor kinematic, dốc, nhảy, one-way/drop-through, leo mép, input cảm ứng + bàn phím + tay cầm, spawn và chuyển phòng. Nghiệm thu bằng playtest thay vì chỉ biên dịch.
3. **Nhân vật và checkpoint:** combo, parry, dash cooldown, plunge, hồi máu, chết/hồi sinh; lưu checkpoint và relic. Bộ chiêu là code mới dùng hình ảnh gốc; cần tinh chỉnh timing trước khi gọi là giống bản gốc.
4. **Quái/boss:** Acolyte và Flagellant trong bản dựng là encounter được bố trí theo kế hoạch, chưa xác minh là spawn gốc. ElderBrother dùng sprite/clip gốc, HP nguồn 400, AI mới cần cân bằng và kiểm tra trận đấu. Bổ sung intro, execution và đoạn kết sau khi vòng chiến đấu ổn định.
5. **Hình ảnh/âm thanh:** giữ ảnh gốc, shader sprite thay thế hoạt động; phục hồi palette/mask theo từng trường hợp. Có 35 mẫu âm thanh đã giải mã từ bank bằng vgmstream, phát qua AudioSource pool, không dùng FMOD DLL runtime. HUD hiện là giao diện mới, chưa phải toàn bộ HUD Gothic gốc.
6. **Android:** build APK ARM64, kiểm tra đầu vào đa chạm/safe area/tạm dừng/lưu game trên điện thoại. Nén texture theo profile Android sau khi kiểm tra chất lượng; đo bộ nhớ, CPU/GPU/frame time, giật khi chuyển phòng. 60 FPS là mục tiêu đo trên thiết bị, không suy ra từ số scene hoặc draw call.

## UI di động và menu nhân vật

Hai tài liệu UI/character-menu được dùng làm đặc tả tham khảo cho giao diện mới; hình ảnh và animation vẫn lấy từ dữ liệu gốc, còn logic được viết lại trong dự án này.

- Tách `Static UI Canvas` và `Dynamic Controls Canvas`, cùng áp dụng `Screen.safeArea` và tỉ lệ tham chiếu 1280×720.
- HUD có chân dung, health/fervour/flask, map/túi đồ, thông báo và thanh boss. Cụm nút bên phải theo vòng cung của ảnh tham chiếu; joystick bên trái xuất hiện ở điểm bắt đầu chạm.
- Mỗi nút/joystick giữ `pointerId` riêng để chạy, nhảy và đánh đồng thời. Mất focus/pause phải xóa toàn bộ trạng thái giữ để tránh kẹt nút.
- `USE` chỉ xuất hiện khi ở gần Prie Dieu; flask ẩn khi số lượng bằng 0. Prayer/special tiếp tục ẩn cho tới khi gameplay tương ứng tồn tại.
- Map/túi đồ hiện mở bảng nhân vật trong game với room, health, fervour, flask và relic. Đây là bước nền; lưới trang bị, mô tả item và bản đồ khám phá sẽ được bổ sung khi dữ liệu progression đã xác minh.
- Logic mở khóa kỹ năng cần lưu bằng cờ progression trong save. Không giả định điểm nhận Mea Culpa, flask đầu tiên hay prayer chỉ từ ảnh UI; phải đối chiếu event/flag nguồn hoặc đặt rõ đó là thiết kế mới trước khi khóa attack/flask trong bản chơi.
- Menu chính hiện là scene đầu tiên trong Build Settings, có Begin/Continue, loading async và safe area. Editor sau rebuild cũng mở scene này để Play đi đúng luồng. Menu dùng lại `MainMenuBackground`, hình Penitent và màu/khung chọn từ dữ liệu gốc; animation/title/save-slot đầy đủ của menu gốc vẫn là phần cần phục hồi tiếp.

## Bằng chứng và tình trạng

- Đã chuyển 5 phòng, 1.398 node nguồn, 3.035 sprite, 231 clip và 190 texture; có `source-audit.json`.
- Lần kiểm tra nền tảng trước cập nhật bệ máu đã qua các bước spawn, chạy/nhảy/tiếp đất, parry, sát thương, hồi máu và trạng thái hạ boss (`playtest-results.txt`). Đây chưa phải bằng chứng hoàn thành mọi hành vi gameplay.
- APK Development mới có Main Menu, progression/save v2, HUD cảm ứng và Debug Menu đã build thành công ngày 12/09/2026, 0 lỗi. APK là 77.630.052 byte; tổng build output Unity là 1.056.839.011 byte.
- HUD hai canvas, joystick nổi, input đa chạm và bảng nhân vật đã được triển khai. HUD hiện dùng các crop gốc cho chân dung Penitent, Health/Fervour, Bile Flask và khung Tears; nút hành động hiện theo cờ progression. Playtest và ảnh Game View mới đã nghiệm thu phần này.
- Validator theo source và playtest mới đã qua: bệ máu, audio, leo mép, one-way drop-through, progression gate, hai canvas UI, checkpoint/death/respawn và boss gate.
- Chưa có thiết bị ADB kết nối trong lần kiểm tra trước; chưa chứng minh hiệu năng 60 FPS hoặc cảm giác điều khiển trên điện thoại.

## Các kiểm tra bắt buộc trước bàn giao

- Biên dịch C# không lỗi; import không mất sprite có tham chiếu; đủ 5 lớp và đúng hierarchy/sorting/transform.
- Kiểm tra có chủ ý làm mất sprite hoặc lệch layer phải bị validator phát hiện.
- Ảnh kiểm tra S01 tổng thể, tường phải, chuỗi bệ máu khi tắt/bật; không lấp cửa đi đúng nguồn.
- Spawn trên đất ở cả 5 phòng; cửa nối đi được; không lặp teleport; bệ máu tắt thật sự không đỡ nhân vật.
- Sát thương, parry, heal, checkpoint, chết/hồi sinh và mở cổng sau boss; save kiểm thử tách save người chơi.
- Build Android mới thành công; phần chưa kiểm tra trên thiết bị hoặc chưa phục hồi đầy đủ phải ghi rõ.

## Rà soát trực quan ngày 12/09/2026

Sáu ảnh playtest của người dùng đã chứng minh bộ test tự động trước đó chưa bao phủ đủ hành vi và bố cục thực tế. Các lỗi được xử lý trong source nhưng phải kiểm tra lại trực quan trong Game View trước khi đánh dấu hoàn tất:

- Đứng yên trên dốc bị trượt: motor giữ vận tốc dọc bằng 0 khi đã grounded và không có input ngang.
- Nhân vật lơ lửng: importer chỉ tạo nền vật lý từ collider LAYOUT thuộc layer địa hình/one-way; collider LOGIC/interactable không còn biến thành sàn.
- Không leo thang: nhập `LadderTrigger` nguồn thành `LadderZone`, hỗ trợ lên/xuống bằng joystick, bàn phím và gamepad, dùng animation thang gốc.
- Menu lệch góc: bảng nhân vật luôn neo chính giữa safe area; cần kiểm tra thêm trên 16:9 và màn hình dài.
- Prie Dieu: giữ tương tác 0,9 giây, chạy `penitent_priedieu_kneeling_anim`, sau đó mới hồi phục, đặt checkpoint và ghi save.
- Background: bổ sung ảnh hưởng parallax theo trục Y từ dữ liệu nguồn; tiếp tục đối chiếu các vùng rỗng bằng ảnh trước/sau vì một số phòng có `influenceY = 0` theo nguồn.
- UI chữ tạm đã được thay bằng icon vẽ runtime cho Map, Bag, Jump, Attack, Dash, Parry, Flask và Use. Khung/chân dung/font Gothic gốc vẫn chưa hoàn thành; không được mô tả UI là giống bản mobile gốc ở giai đoạn này.
- Theo yêu cầu người dùng, không build APK trong các lượt sửa này cho tới khi được yêu cầu rõ ràng.

### Kết quả kiểm tra lại lúc 17:49

- Xóa offset hình ảnh cộng cứng `+0.65`: pivot nguồn của `Player_Idle` nằm đúng tại chân. Player, Acolyte, Flagellant và Elder Brother hiện qua kiểm tra bounds chân so với motor/điểm đất.
- S01 bắt đầu từ marker nguồn `DEBUG_Start Point`; Begin xóa save cũ và vào `D17Z01S01`. Continue chỉ bật khi thực sự có save.
- Đồng nhất vùng tương tác Prie Dieu theo hộp gần vật thể, tránh chênh ngưỡng giữa nút Use và thao tác lưu.
- `playtest-results.txt` mới: toàn bộ kiểm tra qua, gồm 5 phòng, spawn/tiếp đất, dốc, thang, bệ máu, Prie Dieu, damage, flask, respawn và boss.
- HUD playtest mới kiểm tra asset gốc, safe area và trạng thái ẩn/hiện nút Dash; nút DEBUG phát triển đã được ẩn mặc định và chỉ mở bằng F1 trong Editor/Development Build.
- Execution Acolyte/Flagellant giờ dùng clip composite gốc: renderer Player thật được ẩn đúng toàn bộ 4,2–4,51 giây rồi phục hồi, tránh xuất hiện hai Penitent.
- Dash dùng capsule thấp 0,62 m để đi qua khe thấp và chỉ trở về 1,15 m khi có đủ khoảng trống phía trên. Dash đồng thời phát `Penitent_stop_running_dust` và afterimage từ pool, không tạo GameObject theo từng lần lướt.
- Ảnh capture 1280×720 của S01/S05 không còn vùng nền chữ nhật xám từng thấy trong ảnh lỗi; khoảng mở có chủ ý và phần chưa phủ camera vẫn phải tiếp tục đối chiếu theo từng phòng.

## Công cụ và nguồn

- `Tools/extract_brotherhood.py`: chuyển dữ liệu nguồn, không thay đổi source.
- `Tools/augment_faith_platforms.py`: đọc thêm liên kết bệ máu.
- `Tools/extract_audio.py`: giải mã một tập mẫu âm thanh có provenance.
- Decoder: https://github.com/vgmstream/vgmstream — chỉ dùng lúc chuẩn bị tài nguyên, không đóng gói công cụ vào game.
