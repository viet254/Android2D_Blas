# Đối chiếu gameplay, VFX và audio với dữ liệu gốc

Ngày rà soát: 12/09/2026. Nguồn: `D:/game/Bla_loc_map/ExportedProject`.

## Dữ liệu dash đã xác nhận

Các giá trị dưới đây được đọc trực tiếp từ `Assets/Resources/core/Penitent.prefab`, component `Dash` và `Dash.cs` gốc:

- `DashDrag = 2`
- `DashMaxWalkingSpeed = 13`
- `DashCooldownBase = 1 giây`
- `DashRideBase = 0,35 giây`
- `CompletionBeforeDash = 0,25`
- `DashCollisionCenter = (0, 0,36)`
- `DashCollisionSize = (0,5, 0,6)`
- `DamageAreaDashHeight = 0,8`, offset Y `0,4`
- `penitent_dodge_anim`: `RaiseStopDust` tại `0,54 giây`, `SetDashVulnerable` tại `0,72 giây`

Runtime mới đọc các giá trị tương ứng từ `SourceGameplayTuning`; các số phục dựng `11,5`, `0,3`, `0,7` và `0,62` đã được loại bỏ. Dữ liệu gốc không có một giá trị "quãng đường dash" cố định: quãng đường là kết quả của tốc độ, thời gian ride, drag và va chạm. Mốc dừng bụi và hết bất tử hiện cũng theo đúng Animation Event gốc.

## Execution gốc

- `EntityExecution.InstantiateExecution()` phát `PenitentEnemyStunt` và tạo prefab execution.
- Prefab tạo `ExecutionAwareness` trên enemy; `Execution.cs` chỉ thực hiện khi `InteractionTriggered`, Player đứng cùng mặt sàn và không đang bị thương.
- Acolyte: `ExecutionChance = 100`, activation sound `AcolyteExecution`, clip âm thanh giải mã `ACOLYTE_EXECUTION_2` dài 4,92 giây.
- Flagellant: `ExecutionChance = 100`; animation gọi âm thanh ở 0,88 giây, đổi nhịp ở 1,54 và 2,22 giây, dừng ở 4,23 giây.
- Runtime hiện hiển thị animation `flagellant_execution_awareness_anim` trên đầu, phát `ENEMY_STUNT`, hiện nút USE, rồi mới chạy execution khi Player bấm tương tác.
- Clip execution là sprite composite chứa cả enemy và Penitent. Player thật bị ẩn trong thời gian clip; khi clip kết thúc visual composite của enemy được tắt trước khi Player thật hiện lại. Điều này ngăn frame cuối giữ hình Penitent và tạo Player thứ hai.

## Kiểm kê Animation Events

Đã quét 219 clip có tiền tố Player/Penitent/Acolyte/Flagellant/ElderBrother: 274 Animation Events, 92 tên hàm riêng.

Đã có runtime tương đương cho các nhóm chính: hitbox combo, dash collision/ghost/dust, run start/stop, jump/landing/hard landing, hai đòn đánh trên không, đánh ngồi, đánh hướng lên, footsteps, âm thanh leo thang, parry spark/dust, pushback dust/audio, flask/healing explosion, plunge, Prie Dieu, damage, execution Acolyte/Flagellant và các đòn Warden nền.

Motor hiện kiểm tra toàn bộ capsule tại vị trí đứng cuối cùng sau dash và tại đích leo mép. Player không được đổi sang tư thế đứng hoặc teleport lên mép nếu capsule đầy đủ còn chồng vào tường. Fixture kiểm thử giữ Player trong một khe thấp dài hơn toàn bộ quãng dash để ngăn lỗi đứng xuyên giữa tường tái xuất hiện.

Warden hiện dùng các mốc Animation Event nguồn cho đòn chuông: chuẩn bị `0,89 giây`, hit sau `0,12 giây`; jump preparation `0,63 giây` và landing sound/hit sau `0,07 giây`. Voice, pre-attack, attack-hit và corpse-wave sound được gắn vào đúng các bước này.

Các nhóm còn thiếu hoặc mới phục dựng một phần:

- Charged/lunge dùng timing, tốc độ, damage factor, clip và audio nguồn. Tier 3 tạo projectile pooled theo prefab gốc, đi 4 m trong 0,3 giây, dùng hitbox nguồn và chuyển đúng clip impact/vanish khi trúng enemy, tường hoặc hết tầm.
- Landing/hard landing theo từng vật liệu; Map 1 hiện dùng âm đá mặc định.
- Sword slash renderer đã nối từ event `SetSwordSlash` tại 0 giây; throwback transition, quỹ đạo và ground-contact dust đã có. Còn thiếu camera shake và một số event phụ theo từng clip.
- Prayer aura, ranged projectile và wall-climb dust.
- Warden voice/pre-attack/attack-hit/corpse wave chưa gắn đủ vào từng mốc animation.

## Audio đã giải mã bổ sung

Đã bổ sung từ `Master Bank.bank`: `ENEMY_STUNT`, `ACOLYTE_EXECUTION_2`, `EXECUTION_EFFECT`, `EXECUTION_EFFECT_END`, ba execution hit, attack/hit của Flagellant, charge/release của Acolyte, Penitent jump/landing/run-stop, ba nhịp leo thang, parry hit, pushback, hard landing, healing explosion và vertical attack. Tất cả được đưa vào AudioSource pool khi scene được tạo lại.

## Chuẩn tham khảo Android chính thức

Trang Blasphemous Mobile của The Game Kitchen và listing Google Play xác nhận bản mobile giữ đầy đủ trải nghiệm gameplay, hỗ trợ touch hoặc gamepad. Listing mobile hiện còn xác nhận joystick nổi có thể bật/tắt và remap nút gameplay. Vì vậy runtime giữ thông số gameplay từ dữ liệu gốc; lớp Android chỉ thay input, safe area, tỷ lệ màn hình và cách bố trí/remap nút.

AssetRipper phục hồi sprite, clip, prefab và mã C# đã dịch ngược, nhưng không tự nối lại FMOD event graph, PlayMaker state, service manager và runtime stat pipeline. Vì vậy chỉ chép `Assets` không tạo lại hành vi hoàn chỉnh; exporter ban đầu của dự án cũng chỉ chọn map renderer, animation catalog và 35 sample audio. Quy trình từ đây phải đọc serialized component, mã gốc và Animation Event trước khi đặt thông số runtime.
