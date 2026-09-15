# Mobile parity matrix

Nguồn chuẩn chính: `D:/game/Bla_mobile_map/ExportedProject` (Unity 2022.3.62f2). Dự án đích: Unity 6000.5.9f1. Các kế hoạch trong Downloads chỉ dùng để kiểm tra phạm vi.

| Hạng mục | Bằng chứng nguồn mobile | Trạng thái dự án | Việc còn thiếu để đạt parity |
|---|---|---|---|
| Android touch art | `Texture2D/Attack`, `Dash`, `Jump`, `Parry`, `Flask`, `Prayer`, `RangeAttack`, `Map`, `Inventory`, hai joystick | Đã nhập đủ 11/11, point filter, ETC2, có multitouch và remap | Kiểm tra cảm giác trên thiết bị thật |
| Touch layout | `GenericElements.unity`, canvas tham chiếu 1920×1080 | Đã chép anchor, vị trí và kích thước sang canvas 1280×720; qua 16:9 đến 20:9 | So hình trên từng vùng safe-area thật |
| Màu/render | Mobile dùng Gamma (`m_ActiveColorSpace: 0`) | Builder đã chuyển target sang Gamma; URP 2D Sprite Unlit thay material Built-in cũ | So ảnh từng phòng sau reimport Gamma; phục hồi shader/mask đặc thù |
| Orientation | Mobile `defaultScreenOrientation: 4` (LandscapeRight) | Builder đã đặt LandscapeRight | Kiểm tra xoay và camera cutout trên thiết bị |
| Player base tuning | `Penitent.prefab`: Dash 13, ride 0.35, cooldown 1; `penitent_dodge_anim`: invulnerable 0.00, stop dust 0.54, vulnerable 0.72 | Dash dùng đúng tốc độ/thời gian/event; collider thấp, bụi và afterimage được kiểm thử độc lập; bất tử dash không còn kích hoạt màu nhấp nháy khi trúng đòn | Đối chiếu toàn bộ transition hiếm của controller mobile |
| Player animation | AnimationClip và texture mobile | Clip chính, combo, air/crouch/up attack, dash, ladder, throwback, execution và projectile đã phục hồi | Các transition hiếm và frame event còn lại |
| Execution | Acolyte/NewFlagellant: `IsParryable=1`, `ExecutionChance=100`, `StuntTime=5`; ACT bật `RepositionBeforeInteract`, không cho dùng khi nhảy và có animator trái/phải riêng | Parry mở cửa sổ 5 giây và hiện awareness/USE; sát thương thường không tự mở execution; composite đưa Player xuyên qua enemy và Player thật xuất hiện ở phía đối diện đúng frame cuối; camera, Fervour/Tears và SFX chạy theo event nguồn | Rumble phần cứng cần kiểm tra trên thiết bị Android thật |
| Prayer | 13 prefab inventory, cost/duration/effect component; clip beam/cherub/flame/toxic/guardian | Catalog giữ cost/duration và 100 component inventory; đang nối hành vi/clip Prayer | Hoàn thiện VFX, projectile, shield, guardian và audio theo từng Prayer |
| Map 1 | Năm scene D17 mobile và prefab/core mobile | Extractor dùng nguồn mobile; 5 phòng, 5 nền máu nguồn, collider/spawn/door/ladder và atlas rect migration qua test | So trực quan toàn bộ renderer, mask và camera region còn lại |
| HUD | Atlas HUD và font `MajesticExtended_Pixel_Scroll` mobile | Portrait, health/fervour/loss/end/division, flask, Tears và font gốc đã dùng | So chính xác scale/offset với HUD mobile ở nhiều DPI |
| Main Menu | Năm scene MainMenu mobile, background và Penitent sheet | Có entry scene, camera, EventSystem, background crop và 22 frame, New/Continue/Options/Extras | Save-slot flow, logo/title bitmap, transition và menu audio đầy đủ |
| Inventory | 68 prefab, icon, mô tả và effect component | Đủ 68 icon/mô tả; save v4; Prayer/Relic/Rosary/Sword Heart equip state | Hành vi đặc thù của mọi item và layout menu mobile đầy đủ |
| Audio | Mobile export có FMOD event IDs nhưng không có bank giải mã | Unity AudioSource dùng mẫu giải từ bank PC gốc; movement/combat/execution/range/prayer mẫu chính | Ambience/mixer/event graph và các Prayer/boss event còn thiếu |
| Save | Logic mobile bị phụ thuộc framework/FMOD/serializer cũ | Save JSON nguyên tử + backup, checkpoint/Prie Dieu, version 4 | Nhiều slot và metadata UI như menu mobile |
| Performance | Mobile yêu cầu 60 FPS | Pool 80 effect objects, không instantiate cho hit/dash/projectile; target 60 | Profiler draw calls/GC/RAM/thermal và thiết bị thật |

Không đánh dấu hoàn thành 100% khi cột cuối còn nội dung. Lần kiểm tra 2026-09-13: gameplay `PASSED` (179 mục), menu `PASSED` (10 mục), nguồn mobile `25/25`. `mobile-source-audit.json`, `playtest-results.txt`, `menu-playtest-results.txt` và ảnh `dash-active.png`, `execution-prompt.png`, `execution-active.png`, `map1-contact-sheet.png` trong `Documentation/Previews` là bằng chứng kiểm tra hiện tại.
