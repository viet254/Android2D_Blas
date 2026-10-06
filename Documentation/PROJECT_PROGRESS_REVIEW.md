# Tiến độ Android2D_Blas và sai khác so với dự án gốc

Cập nhật: **29/09/2026, 10:39 (Asia/Saigon)**. Dự án đích: `D:/game/Android2D_Blas`, Unity **6000.5.9f1**, HEAD `63587a8cfc23a2e5420f3286d13f77e1f3448f70`. Báo cáo bao gồm các thay đổi chưa commit đã có trước lượt làm này.

## Kết quả hiện tại

**Đã triển khai các nhóm ưu tiên 2 trên tuyến đang phục dựng:** stat/heart và cost phóng kiếm, COMBO/VERTICAL, Prayer bổ sung, sự kiện PENANCE, Guilt, khám phá bản đồ/lưu pin, phục hồi save, setting, âm thanh vùng và cảnh Bloody Baptism sau Warden. Đã thêm adapter Tirso, Soledad/đổi knot và hai cherub trong các phòng hiện có.

Giữ **12 phòng cũ**, bổ sung **4 prefab riêng** D17Z01S04/S07/S08/S09; tổng runtime **16 phòng**. **Chưa đánh dấu hoàn thành toàn bộ ưu tiên 2:** đường tiếp cận nhánh ngầm vẫn thiếu các phòng trung gian; một số hiệu ứng vật phẩm và quest ngoài tuyến còn thiếu; lượt này tập trung hoàn thiện cơ chế của 17 Prayer. Danh sách cụ thể ở phần việc còn lại. Không coi 16 phòng hoặc đủ 221 icon vật phẩm là đã hoàn thành toàn game.

**Đã hoàn thiện cơ chế 17/17 kinh cầu nguyện và chạy lại kiểm chứng:** bộ Prayer **262 PASS**, P2 kèm hồi quy P1 **376 PASS**, bộ tổng mới **608 PASS**, menu **13 PASS**; cả bốn bộ **0 FAIL**. Các phép thử kiểm tra cast/cost, vùng đánh, damage/timer nguồn, projectile, buff, companion và dọn trạng thái khi chết/đổi phòng/tháo trang bị. Hình ảnh/âm thanh vẫn có các sai khác được ghi bên dưới; chưa build APK mới hoặc nghiệm thu trên điện thoại.

### Chỉnh màn khởi động theo phản hồi 29/09

Đã bỏ đoạn chữ “Blasphemous. Developed by The Game Kitchen…” ở màn Title và bỏ nhãn “v. 1.2” ở cả Title lẫn màn chọn chế độ. Code chỉ sửa phần tạo Text trong [MainMenuController.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/MainMenuController.cs:295); credit ở Extras vẫn giữ. Chạy lại `menuverify` lúc **09:53:16**: **13 PASS, 0 FAIL**. SHA đối chiếu đầu lượt xóa phiên bản xác nhận các file Assets/ProjectSettings khác giữ nguyên; log/ảnh menu và báo cáo tiến độ được cập nhật. Phạm vi SHA và danh sách file của lượt Prayer bên dưới là kết quả đã chốt trước các bước chỉnh menu này.

### Chỉnh bảng cheat và Godmode theo phản hồi 29/09

Đã bỏ dòng tiêu đề “CHEAT · TEST GAME”. Godmode tiếp tục bảo vệ người chơi và đưa người chơi rơi vực về điểm lưu; khi bật, một hit gây sát thương hợp lệ sẽ hạ quái ngay, gồm chém thường, đòn kỹ năng, projectile và Prayer. Bật nút không tự thay HP quái; chỉ xử lý lúc hit chạm.

Runtime chỉ sửa [DevelopmentDebugMenu.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/DevelopmentDebugMenu.cs:20) và thêm một điểm nối trong [EnemyController.Damage](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/EnemyController.cs:446) sau guard boss intro. Nhánh cheat vẫn chạy luồng chết/animation/thưởng hiện có, không thay AI/hurt/stun; khi tắt, giữ damage đầu vào bình thường. Các thay đổi này nằm sau thời điểm chốt audit Prayer bên dưới. Kiểm chứng lúc **10:06:04**: [bộ gameplay và Godmode](D:/game/Android2D_Blas/Documentation/godmode-playtest-results.txt) **608 PASS, 0 FAIL**, gồm 18 phép kiểm tra mới với nút thật, chém thường, đòn mạnh, Lorquiana, hit 0 damage, bảo vệ boss intro, thưởng một lần, tắt Godmode và mở Prayer. Hồi quy Prayer **262 PASS lúc 10:07:24**, P1/P2 **376 PASS lúc 10:08:16**, đều **0 FAIL**. Đã xem [ảnh bảng cheat](D:/game/Android2D_Blas/Documentation/Previews/cheat-panel.png). [Audit phạm vi Godmode](D:/game/Android2D_Blas/Documentation/godmode-change-audit.json) xác nhận chỉ hai file runtime nói trên và file Editor kiểm thử có thay đổi trong Assets/ProjectSettings.

## Đối chiếu yêu cầu Game Over, bản đồ, Win và NPC

Rà soát ngày **29/09/2026, 10:19 (Asia/Saigon)** bằng code, dữ liệu phòng và log Editor hiện có. Lượt này chỉ cập nhật báo cáo; không sửa code, scene, prefab, tài nguyên hoặc ProjectSettings, không chạy lại gameplay và không build APK.

**Kết luận:** Game Over, Win và các bản đồ/nhiệm vụ đã được triển khai trên tuyến/chương đầu, với điều kiện cách chấm chấp nhận phòng/nhiệm vụ là cấp độ và trang UI là màn hình riêng. **Yêu cầu 3 NPC thông minh chưa đạt: mới xác nhận 1/3, còn thiếu 2 NPC** theo tiêu chí hành vi đã được người dùng nêu. Ngoài ra, marker `completedRooms` chưa đại diện cho hoàn thành mục tiêu, một đường điều hướng Win chưa được bấm trong log hiện có và chưa kiểm nghiệm các luồng trên điện thoại.

| Yêu cầu | Đánh giá | Căn cứ và giới hạn |
|---|---|---|
| Game Over: thông báo/hình ảnh/âm thanh | Đã triển khai | Khi hết Life có thông báo gục ngã, animation chết và tiếng damage/pushback; sau 1,8 giây hiện tranh Game Over với fade. Có asset âm thanh, chưa kiểm tra trực tiếp tiếng đầu ra trong lượt rà soát này. |
| Game Over: ít nhất 3 nút hoạt động | Đã triển khai và có kiểm thử cả 3 | HỒI SINH → gameplay tại checkpoint; HOME → MainMenu; SETTINGS → màn tùy chọn. Settings là trang UI trong scene gameplay. |
| Ít nhất 3 bản đồ/nhiệm vụ khác nhau | Có 3 ví dụ trên tuyến thường | Arena Warden, hành lang cơ quan S03 và khu Holy Line/Deogracias khác bố cục, cảnh nền và hoạt động. Đây là phòng/nhiệm vụ trong thế giới liên thông, chưa phải 3 chương độc lập. |
| Lưu trạng thái sau từng mục tiêu | Có lưu cho 3 mục tiêu dưới đây; cần phân biệt với marker rời phòng | Lưu kết quả boss, cổng và Thorn; save còn chứa Life/Fervour/bình, vị trí, checkpoint, Tears, vật phẩm và kỹ năng. Chưa có kiểm thử riêng đóng/mở game sau mỗi mục tiêu trong một lượt chơi liên tục. |
| Win: mục tiêu và thông báo/hình ảnh/âm thanh | Đã triển khai ở chương đầu | Mục tiêu là hạ Warden of Silent Sorrow; lưu trạng thái thắng, hiệu ứng/corpse/tiếng boss chết, Bloody Baptism rồi tranh REQUIEM AETERNAM. |
| Win: ít nhất 3 nút hoạt động | Có đủ đường xử lý; còn thiếu kiểm thử nút HOME tại Win | CONTINUE → gameplay sau boss; HOME → lưu rồi MainMenu; ACHIEVEMENTS → trang thành tích AC01. Log đã bấm Achievements, Back và Continue; Home tại Win mới có bằng chứng code và tồn tại nút. |
| Ít nhất 3 NPC có hành vi thông minh | **Chưa đạt: xác nhận 1/3, còn thiếu 2** | Chỉ MudCrawler được tính với chuỗi phát hiện → xuất hiện → đuổi → ẩn khi mất dấu. Fool/WheelCarrier hiện có AI cơ bản đuổi/đánh/đứng, chưa đủ theo tiêu chí này. PASS hành vi không tự chứng minh đạt yêu cầu thông minh. |

### Ba nút của mỗi trạng thái dẫn tới đâu

| Trạng thái | Nút | Màn đích / hành động |
|---|---|---|
| Game Over | HỒI SINH | Khôi phục người chơi ở checkpoint, đóng bảng chết và trả điều khiển gameplay. |
| Game Over | HOME | Đóng bảng chết và tải scene MainMenu. |
| Game Over | SETTINGS | Mở màn tùy chọn; đóng tùy chọn trở lại Game Over, tiếp tục ẩn điều khiển gameplay. |
| Win | CONTINUE | Đóng bảng thắng, trở lại phòng boss đã được mở khóa và trả điều khiển. |
| Win | HOME | Ghi save, đóng bảng thắng và tải scene MainMenu. |
| Win | ACHIEVEMENTS | Đổi sang trang riêng hiển thị thành tích AC01; có Back về bảng thắng. |

Code điều hướng: [GameOverUI.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/GameOverUI.cs:74), [Respawn](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/BrotherhoodGame.cs:238), [BossDefeatedUI.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/BossDefeatedUI.cs:40). Build Settings hiện có **2 scene được bật: MainMenu và Brotherhood**. Settings/Achievements là màn UI riêng; nếu tiêu chí chấm bắt buộc mỗi đích phải là một Unity Scene độc lập thì phần đó chưa đáp ứng.

Thông báo/VFX/SFX của luồng chết nằm tại [PlayerController.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/PlayerController.cs:663) và fade tại [GameOverUI.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/GameOverUI.cs:101). Luồng thắng bắt đầu tại [BossDefeated](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/BrotherhoodGame.cs:239), rồi đi qua [SourceDeath](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/ElderBrotherEncounter.cs:135). Âm thanh phát trong chuỗi chết/thắng; hai hàm Show của bảng không phát một tiếng riêng. Bộ tổng bỏ qua playback CTS02 để kiểm thử UI; bộ P2 có phép thử riêng xác nhận Bloody Baptism bắt đầu phát.

### Ba bản đồ/nhiệm vụ và trạng thái được lưu

| Phòng | Khác biệt trong gameplay và cảnh | Kết quả mục tiêu được ghi vào save |
|---|---|---|
| D17Z01S11 — Warden | Arena với nền boss; intro, đòn AREA/JUMP, khóa cửa khi boss còn sống. | `wardenDefeated`, `achievementAC01`, `campaignWon`, ID phòng boss; SaveGame gọi ngay khi hạ boss. |
| D17Z01S03 — cơ quan/cổng | Hành lang có receiver ở cao; người chơi nhảy/chém trúng để mở cổng, hình cơ quan và cổng đổi trạng thái. | `shockGateOpened`; lưu ngay và khôi phục hình/collider cổng mở khi trở lại. |
| D01Z01S07 — Holy Line/Deogracias | Cửa Brotherhood và cảnh Holy Line; tương tác NPC, thoại/cinematic CTS07, nhận Thorn. | `deograciasMet`, `thornGranted`, quyền sở hữu `QI31`; lưu ngay sau khi nhận. |

Tuyến cửa thường nối **D17Z01S01 → S02 → S05 → S11 → S03 → D01Z01S07**, nên ba ví dụ này không phụ thuộc cheat. Cảnh riêng từng phòng có trong [brotherhood.json](D:/game/Android2D_Blas/Assets/Brotherhood/SourceData/brotherhood.json). Không dùng bốn phòng bổ sung ở nhánh ngầm làm bằng chứng tuyến hoàn chỉnh vì đường tiếp cận còn thiếu.

Điểm cần lưu ý: [ChangeRoom](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/BrotherhoodGame.cs:171) gọi `CompleteRoom` khi người chơi qua cửa. [CompleteRoom](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/PlayerProgress.cs:80) chỉ thêm ID, không kiểm tra mục tiêu. Vì vậy rời S03 qua cửa tây khi chưa mở cơ quan, hoặc rời S07 khi chưa nhận Thorn, vẫn có thể được ghi vào `completedRooms`. Để chứng minh hoàn thành ba nhiệm vụ phải dùng các cờ boss/cổng/Thorn ở bảng trên.

Điểm lưu mục tiêu thật: [boss](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/BrotherhoodGame.cs:242), [cổng](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/RoomState.cs:90), [Thorn](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/DeograciasEncounter.cs:187); dữ liệu người chơi được ghi tại [SaveGame](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/BrotherhoodGame.cs:321) và đọc tại [LoadSave](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/BrotherhoodGame.cs:341). Test cổng có đọc JSON đã ghi và kiểm tra trở lại phòng tại [BrotherhoodVerification.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Editor/BrotherhoodVerification.cs:911).

### Đánh giá hành vi NPC: mới xác nhận 1/3

Đánh giá cập nhật lúc **29/09/2026, 10:39** theo phản hồi của người dùng: đuổi, đổi hướng, đứng và đánh theo khoảng cách chưa đủ để được tính là hành vi thông minh. Trong ba loại NPC đã đưa làm bằng chứng, chỉ MudCrawler đạt. Các kiểm thử hiện có vẫn xác nhận hành vi code hoạt động; việc đáp ứng tiêu chí nghiệm thu được đánh giá riêng.

| NPC thù địch | Hành vi hiện tại | Đánh giá theo tiêu chí đã chốt | Bằng chứng |
|---|---|---|---|
| Fool | Nhận biết khoảng cách/độ cao và đường an toàn; đuổi, quay người khi đổi phía, gây sát thương khi áp sát. | **Chưa tính**: AI đuổi/đứng và phản ứng hướng cơ bản. | [EnemyController.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/EnemyController.cs:236); [PASS quay người](D:/game/Android2D_Blas/Documentation/godmode-playtest-results.txt:441). |
| WheelCarrier | Kiểm tra tầm nhìn, góc nhìn và vật cản; đuổi khi xa, chuẩn bị rồi tấn công khi đủ gần, có thời gian hồi phục. | **Chưa tính**: vẫn chọn đuổi/đánh/đứng theo khoảng cách và timer. | [EnemyController.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/EnemyController.cs:209); [PASS chuẩn bị/tầm đánh](D:/game/Android2D_Blas/Documentation/godmode-playtest-results.txt:439). |
| MudCrawler | Ẩn trước khi phát hiện người chơi; xuất hiện rồi đuổi; mất dấu 2 giây thì chìm xuống và trở về điểm xuất phát. | **Đạt**: có chuỗi thay đổi hành vi theo phát hiện và mất dấu mục tiêu. | [EnemyController.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/EnemyController.cs:270); [PASS chuỗi trạng thái](D:/game/Android2D_Blas/Documentation/godmode-playtest-results.txt:435). |

Ba loại này xuất hiện từ marker đang bật trên tuyến D01Z01S01/S02/S03. Kiểm tra sàn, tường và khoảng cách đồng loại tại [ForestPathClear](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/EnemyController.cs:192). Warden có luân phiên đòn và tính điểm đáp từ vị trí/vận tốc người chơi tại [EnemyController.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/EnemyController.cs:334); ghi nhận riêng, chưa dùng để bù hai NPC còn thiếu trong đánh giá này.

Nếu tiêu chí yêu cầu riêng NPC thân thiện: Tirso kiểm tra dược liệu và trả thưởng theo tiến độ; Soledad kiểm tra knot/slot và kết thúc quest; Deogracias đổi tương tác theo Warden và trạng thái đã nhận Thorn. Đây là hành vi phản ứng theo quest, chưa đủ bằng chứng để khẳng định cả ba có AI tự chủ về di chuyển/ra quyết định trong môi trường. Không tính dân làng hoặc Redento chỉ idle/hội thoại tĩnh để đủ số lượng.

### Phần còn thiếu hoặc cần kiểm chứng trước khi nghiệm thu

1. Nếu dùng `completedRooms` để hiển thị “hoàn thành cấp độ”, cần gắn việc đánh dấu với điều kiện mục tiêu; hiện nó là lịch sử rời phòng. Các cờ mục tiêu chuyên biệt vẫn được lưu đúng theo code.
2. Bấm HOME trực tiếp từ Win và kiểm tra màn đích/save; kiểm thử hiện có chỉ xác nhận nút tồn tại tại [BrotherhoodVerification.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Editor/BrotherhoodVerification.cs:703).
3. Chơi từ save mới qua đủ ba mục tiêu bằng tuyến thường, đóng/mở game sau từng mục tiêu và kiểm tra khôi phục. Các log hiện có kiểm tra từng phần bằng fixture; chưa chứng minh toàn bộ lượt chơi đó.
4. Kiểm tra trực tiếp hình/tiếng và các nút trên bản APK hiện hành, với Godmode tắt để kiểm luồng chết. Lượt rà soát này đánh giá code Unity và bằng chứng Editor.
5. Bổ sung hoặc nâng cấp hành vi cho **hai NPC nữa** theo tiêu chí đã chốt, có kích thích, lựa chọn hành động, chuyển trạng thái và kết quả quan sát rõ. Lượt phản hồi này chỉ sửa đánh giá trong báo cáo; chưa triển khai hành vi mới hoặc thay combat/quest.

Bằng chứng hiện có: [bộ tổng 608 PASS, 0 FAIL](D:/game/Android2D_Blas/Documentation/godmode-playtest-results.txt:542), [Win](D:/game/Android2D_Blas/Documentation/godmode-playtest-results.txt:571), [Home từ Game Over](D:/game/Android2D_Blas/Documentation/godmode-playtest-results.txt:611), [P1/P2 376 PASS, 0 FAIL](D:/game/Android2D_Blas/Documentation/priority-two-playtest-results.txt). Số PASS tổng không thay thế kiểm chứng từng yêu cầu và không có nghĩa toàn bộ game nguồn đã hoàn thiện.

## Bảo vệ các sửa đổi trước

Đã chụp SHA-256 **2.935 file** trước lượt Prayer lúc **28/09/2026, 20:41:33**, lưu bền tại [prayers-baseline.json](D:/game/Android2D_Blas/Tools/.verification/prayers-baseline.json). Lượt ưu tiên 2 trước đó có baseline **2.637 file** riêng. Kết quả so sánh lượt Prayer nằm tại [prayers-change-audit.json](D:/game/Android2D_Blas/Documentation/prayers-change-audit.json). Các thay đổi đã tồn tại trước đó được giữ, không reset Git, không dựng lại hai scene bằng Builder.

Trong lượt Prayer chốt lúc **29/09, 04:34**, các file sau **giống từng byte với đầu lượt Prayer**. Hai file `DevelopmentDebugMenu`/`EnemyController` được sửa riêng sau đó theo yêu cầu Godmode nêu trên:

- [DevelopmentDebugMenu.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/DevelopmentDebugMenu.cs): bảng cheat, Godmode, mở Prayer.
- [TouchControls.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/TouchControls.cs): nút chung Penance/ranged, vị trí HUD, touch/remap.
- [EnemyController.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/EnemyController.cs): ngắt đòn và sát thương va chạm khi hurt/stun.
- [RoomState.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Runtime/RoomState.cs): cơ quan/cổng S03, collider và thang của phòng cũ.
- [BrotherhoodBuilder.cs](D:/game/Android2D_Blas/Assets/Brotherhood/Editor/BrotherhoodBuilder.cs).
- [Brotherhood.unity](D:/game/Android2D_Blas/Assets/Brotherhood/Scenes/Brotherhood.unity), [MainMenu.unity](D:/game/Android2D_Blas/Assets/Brotherhood/Scenes/MainMenu.unity).
- [RestoredCatalog.asset](D:/game/Android2D_Blas/Assets/Brotherhood/Generated/RestoredCatalog.asset), [brotherhood.json của 12 phòng](D:/game/Android2D_Blas/Assets/Brotherhood/SourceData/brotherhood.json).
- Import setting của hai tiếng cổng cũ `BELL_RECEIVER_ACTIVATE.wav.meta`, `GATE_OPEN.wav.meta`. Đã khôi phục đúng SHA đầu lượt và loại chúng khỏi importer audio ưu tiên 2.

Không thay đổi ProjectSettings hoặc ghi vào các thư mục dự án gốc. PlayerController/BrotherhoodGame vẫn cần các điểm nối cho chức năng mới; **giữ file quái không đổi chưa đủ để bảo đảm combat**, nên đã chạy lại cả các kiểm tra hành vi hurt, HUD, Penance và cổng.

Báo cáo SHA kiểm tra đủ **28 file ProjectSettings** và **11 file cần bảo vệ** kể trên. Phạm vi code cũ của lượt Prayer là **8 file runtime + 4 file Editor**; các file còn lại đổi do cập nhật báo cáo/log/ảnh kiểm thử. Không có file đầu lượt bị xóa.

## Nguồn đối chiếu

- Dữ liệu mobile: `D:/game/Bla_mobile_map/ExportedProject` (Unity 2022.3.62f2), gồm scene/prefab/timeline/inventory/skill/map CvstodiaDLC3.
- Implementation PC: `D:/game/Blasphemous_PC_Source/Assembly-CSharp`, dùng đối chiếu stat, Guilt, Healing, familiar và cơ chế nền.
- IL2CPP mobile: `D:/game/Blasphemous_IL2CPP/Cpp2IL_ISIL/IsilDump/Assembly-CSharp`, dùng tra hành vi mới ở bản mobile. Các hàm C# export có thân rỗng không được coi là bằng chứng thực thi; dùng dữ liệu prefab và lời gọi trong ISIL, ghi giới hạn khi chưa đủ thông tin.
- Native Android: `D:/game/Blasphemous_IL2CPP/Input/libil2cpp.so`, đối chiếu ARM64 cho constant, loại hit và thời điểm tạo hit của Guardian/Miriam; audio DLC lấy từ `D:/game/Blasphemous_Android_Extracted/assets/Master Bank.bank`.

Các importer chỉ đọc những thư mục nguồn này và ghi tài nguyên bổ sung vào dự án đích.

## Ưu tiên 1 và các sửa theo phản hồi đã được giữ

| Hạng mục | Hành vi hiện tại |
|---|---|
| Animation nguồn | Giữ 124 key sprite rỗng trong 32 clip, 268 event/16 float track; áp dụng renderer/alpha/size/loop |
| New Game | 0 Tears, Mea Culpa 0, hai rosary slot; không cấp RANGED_1 miễn phí; giữ migration save cũ |
| Penance/ranged | Nút sáng từ đầu; giữ 0,25 s đổi máu, chạm nhanh phóng kiếm đã học; phím F cùng thao tác |
| Penance | Trừ 15 Life, `25 + 25 × cấp Mea Culpa` Tears; cộng 25 Fervour, có giới hạn; Life và Tears phải lớn hơn mức cần dùng |
| Quái hurt/stun | Hủy đòn đang chuẩn bị, dừng AI phù hợp và chặn sát thương va chạm; hết trạng thái thì hoạt động lại |
| Cơ quan S03 | Lưỡi kiếm chạm sensor nguồn có thể phá cơ quan và mở cổng; có hồi quy touch/keyboard/save |
| S05 | Không có sàn giả cũ; giữ hai nhóm hình học đóng/mở và thang theo trạng thái nguồn |
| Cheat | Nút cờ lê 48 × 48 cạnh MAP; Mea Culpa/Tears/tài nguyên/skill/thánh tích/chuyển phòng; Godmode bất tử + một đòn hạ quái; mở 17 Prayer vẫn có |

Mea Culpa tăng khi tương tác với **mỗi bàn mới**, không tăng lại khi quay lại cùng bàn. Bàn trong tuyến chính ở **D01Z02S06**; bàn thứ hai đã nhập ở **D17Z01S08**, nhưng đường tiếp cận tự nhiên tới nhánh này chưa đủ. Có thể dùng bảng cheat để tới S08 và kiểm tra. Phòng bổ sung được thêm vào danh sách phòng qua dữ liệu runtime, không sửa code bảng cheat.

## Ưu tiên 2 đã thực hiện

| Nhóm | Phần đã làm và kiểm chứng | Giới hạn |
|---|---|---|
| Stat/heart | Cộng giá trị rồi nhân modifier; hiệu ứng multiplier dù value bằng 0; điều kiện máu thấp/bình rỗng; giảm damage/Defense; Strength tăng 4 mỗi altar mới | Chưa thay toàn bộ damage/hitbox của adapter bằng hệ combat nguồn |
| Ranged | Cost nền **7 Fervour**; HE01 giảm 25% còn **5,25**; kiểm tra tiêu tài nguyên qua nút thật | Không đổi cách giữ Penance/chạm ranged |
| Skill | Mua được đủ **15 skill** theo tier/cha/cost nguồn; thêm COMBO_1–3 và VERTICAL_1–3 | Cấp cao vẫn phải đạt Mea Culpa tương ứng hoặc chỉnh bằng cheat |
| Prayer | Cơ chế riêng cho **17/17 Prayer**; thời điểm cast, cost, buff, familiar, collider và cleanup theo nguồn | Vẫn dùng SpriteActor/URP/AudioSource; giới hạn hình ảnh/âm thanh ghi riêng phía dưới |
| Event/item | PENANCE phát một lần và lưu số lần; stat event hit/update/damage/breakable/kill/death/healing; context execution/heavy kill | Chưa có toàn bộ subscriber quest/achievement gốc |
| Guilt | Fragment khi chết, nhóm fragment gần nhau, lưu/tải, tương tác thu hồi, curve Fervour/Tears nguồn, bead chặn rơi Guilt | Chưa có toàn bộ ngoại lệ level/chế độ penitence |
| Map | **993 ô nguồn**, **809 ô tính %**; vị trí, hình vuông, biên cửa/tường và phát hiện từng ô; pin lưu/xóa ngay | Chỉ vẽ các phòng đang nhập; chưa đủ icon/màu mọi zone |
| NPC/cherub | Tirso nhận sáu dược liệu; Soledad đổi sáu knot, tăng 2→8 ô; hai captor/cherub được cứu/lưu một lần | Quest deadline/boss, Redento và toàn bộ 38 cherub chưa hoàn chỉnh |
| UI/setting | Đồng bộ Screen Shake; áp dụng Achievement Popups/HowToPlay; sensor tutorial nguồn; FPS đã chọn được giữ; volume nhạc/SFX/voice video | Chưa nghiệm thu mọi tùy chọn hiển thị trên Android |
| Audio | Thêm **21 sample** từ bank nguồn; tiếng UI, vertical, cherub, Guilt, ambience/nhạc Brotherhood–Forest–Albero, CTS02 Foley | WAV/AudioSource vẫn thay FMOD; open/close dùng alias CHANGE_TAB |
| Save | Thử primary→`.bak`→`.tmp` sau validation; ghi file tạm, Flush, replace và giữ backup hợp lệ; tương thích save v6 | Fallback trên filesystem không hỗ trợ replace không có bảo đảm atomic tương đương |
| Hậu Warden | Nối **CTS02 Bloody Baptism** trước màn chọn tiếp tục; hoàn thành/skip xóa Guilt và lưu flag | Màn thắng chương vẫn là phần riêng của dự án |

### Skill và Prayer

COMBO mở lần lượt đòn kết thúc giữa, hướng lên và hướng xuống, theo ownership từng tier. VERTICAL cần giữ xuống khi đang trên không; tier/độ cao quyết định vùng đáp và cột hiệu ứng. Khóa hướng finisher chưa học; không cấp miễn phí kỹ năng khi New Game.

### Hoàn thiện 17 kinh cầu nguyện

Cast dùng `penitent_aura_transform`: phải đứng trên đất, bắt đầu animation rồi phát hiệu ứng/trừ Fervour đúng event **0,55 s**, recovery theo clip **1,33 s**. RB108 tăng tốc cả animation và event; thiếu mana hoặc bị ngắt trước event không mất mana. Tại event, hit đáp mạnh nguồn gây **30** damage trong vùng **2 × 0,5**, không cộng Fervour; đây là hit riêng với hiệu ứng từng Prayer. Không cho cast lặp trong pose hoặc khi timed Prayer còn hoạt động. Đổi phòng, chết, phục hồi hoặc đổi trang bị sẽ dọn hiệu ứng tạm thời.

| ID / Prayer | Cost Fervour | Cơ chế đã nối từ nguồn |
|---|---:|---|
| PR01 — Següiriya a Tus Luceros | 20 | Attack speed theo stat, giới hạn animation **1,5×**; trail tím; **10 s** |
| PR03 — Debla de las Luces | 40 | Cột cao **12**, rộng **6,8**; **55 × 4** tick, cách **0,28 s**; các pha warning/loop/fade và impact; bảo vệ cast cố định **2 s** |
| PR04 — Saeta Dolorosa | 40 | Hồi **15% DamageAmount** của hit thành công, gồm hit heavy và overkill; **10 s**; không hồi sau khi hết buff |
| PR05 — Campanillero a los Hijos de la Aurora | 60 | **7** cherub hữu hạn, deploy mỗi **0,5 s**, tìm mục tiêu/LOS, charge **0,6 s**, mỗi cherub bắn một lần rồi rời đi; base damage **40**, đời cố định **15 s** |
| PR07 — Lorquiana | 40 | Ba beam xuyên, cách **0,15 s**, range **15**, rộng **2**, damage **90**; giữ collision mask nguồn |
| PR08 — Zarabanda del Refugio Seguro | 40 | Hai shield quay bán kính **2**, collider tròn **0,75**, damage **18** khi chạm; chặn projectile tại shield, expand/contract; **không còn invulnerability toàn người chơi** |
| PR09 — Taranto a la Hermana Mía | 40 | Sáu cột tuần tự hai phía, ground snap/chặn tường, collider cao **20**; damage **20** mỗi **0,12 s** |
| PR10 — Soleá a la Excomunión | 60 | Melee **2,5×**, slash cấp 2, context heavy, Fervour gain **8**; **10 s**; không có AoE cast giả |
| PR11 — Tiento a Tus Cabellos Espinados | 40 | Invulnerability **8 s**, lady xuất hiện khi đỡ hit; trail xanh có timer riêng, không dùng vòng shield |
| PR12 — Cante Jondo de las Tres Hermanas | 60 | Chọn tối đa **20** target trong vòng tròn bán kính **20**; damage **210**, lần lượt mỗi **0,5 s** của bản mobile; invocation/impact nguồn |
| PR14 — Verdiales del Pueblo Olvidado | 20 | Hai crawler theo mặt đất/góc, polygon nguồn, damage **18**, đời **2 s** và explosion khi hết đời |
| PR15 — Ballad to the Crimson Mist | 40 | Thả cloud đứng yên mỗi **1 s** trong **10 s**; mỗi cloud sống **3 s**, damage **25** mỗi **0,5 s**, có birth/idle/fade; cloud cũ ở lại khi người chơi di chuyển |
| PR16 — Zambra de la Resplandeciente Corona | 40 | Tears **+30%** một lần, trail vàng, harvest **2 s** khi giết quái; stat **25 s**; trail và stop-Fervour dùng timer riêng |
| PR101 — Aubade of the Nameless Guardian | 100 | Xuất hiện/theo người chơi, đánh theo lệnh sword và event `WeaponAttack`; damage **180 × stat 15** qua capsule, hit **Heavy**, gain Fervour **8**, phá projectile trong vùng; guard từ lệnh parry bảo vệ trong đúng action; đời **20 s** và vanish |
| PR201 — Cantiña of the Blue Rose | 40 | Portal Miriam chờ lệnh đánh; polygon cú chém **100 × stat 15**, hit **OptionalStunt**, stun **5 s** quái thường, gain Fervour **4**, phá projectile trong vùng; tween lên/xuống, landing và **12** shard damage **40 × stat 15** |
| PR202 — Mirabrás of the Return to Port | 40 | Teleport sau **0,3 s** về Prie Dieu đã kích hoạt, phải grounded; không tự hồi Life/bình/mana |
| PR203 — Tirana of the Celestial Bastion | 90 | **7** core/**6** segment; reveal/charge/window **0,3 s**, damage **10**, ba pulse cách **1,2 s**; HE01 thêm `floor(5 × 0,6) = 3` pulse; fade tuần tự |

Damage/geometry của Prayer dùng vùng nguồn thay AoE chung: rectangle, circle, segment, capsule và polygon. PR12/PR14 cùng các vùng Prayer có thể giải thoát captor thuộc layer Enemy; không thay cách mở cơ quan S03. Hit Prayer đi qua hurt/stun hiện có và event trang bị; gain Fervour bị chặn khi component nguồn có flag stop-Fervour.

Guardian/Miriam tạo và giữ hit khi companion xuất hiện, đúng `OnStart/CreateHit` native: multiplier có điều kiện được chụp lúc đó, không tính lại khi Life thay đổi giữa những lần đánh. Shard Miriam tính multiplier tại lúc landing. Đã kiểm tra cả thay đổi Life sau cast và điều kiện máu thấp đã có trước cast; Miriam không dùng nhầm PrayerStrength stat 27.

Guardian đánh tức thời tại `WeaponAttack`. Miriam giữ polygon theo vị trí đang di chuyển từ `WeaponAttack` tới `WeaponAttackFinished`, gồm thời gian pause/resume của clip; mục tiêu bước vào muộn vẫn trúng, mỗi mục tiêu chỉ một lần. Phá projectile cũng hoạt động suốt cửa sổ đó. Hitbox đóng sau event kết thúc và bị dọn khi hủy Prayer.

HE01 cộng **5 s** chỉ cho component opt-in: Saeta/Soleá/Següiriya và các component có cờ thời lượng tương ứng. Không kéo dài bừa timer invulnerability **PR03/09/14**, cherub PR05, ghost PR11 hay trail/stop-Fervour PR16. Tiento có thể bảo vệ **13 s** với Heart nhưng trail xanh vẫn **8 s**; Zambra stat có thể **30 s** trong khi trail/stop-Fervour vẫn **25 s**. PR201 là invocation tức thời: companion và shard tự quản vòng đời.

Bổ sung riêng **46 clip, 489 sprite, 26 atlas** từ export mobile và **49 WAV/alias từ 44 sample duy nhất** trong bank Android nguồn; provenance audio ghi tại [prayers-audio-provenance.json](D:/game/Android2D_Blas/Assets/Brotherhood/Resources/Audio/prayers-audio-provenance.json). Một số ánh xạ event→sample vẫn suy luận theo vai trò, gồm `PRAYER_SHOT→MAGIC_SHOT` và `MIRIAM_SHARD_IN/OUT`. `PrayerClips.asset` được tạo riêng, không nhập lại catalog/scene chính. Native ARM64 được dùng xác nhận các constant không đọc tin cậy qua ISIL, gồm Miriam **0,5/0,2** và Tirana **0,6**.

**Mở Prayer bằng cheat không tự nâng FervourMax.** PR101/PR203 vẫn cần đủ mana tối đa; phép kiểm thử cấp tài nguyên cao trực tiếp trong save riêng để cô lập cost. Không sửa bảng cheat hay progression tài nguyên trong lượt này.

### Hiệu ứng trang bị đã nối thêm

- RB101 hồi Fervour theo chu kỳ một giây; RB202 đỡ từ phía sau; RB203 đổi tốc độ chạy.
- RB21 thêm xung khí thứ ba khi chém xuống trên không; bình thường có hai xung khí mỗi lần trên không. RB15 tăng tốc recovery đáp mạnh theo nguồn.
- RB42 hiện familiar bằng sprite/clip gốc, theo người chơi/cherub gần; tháo bead thì dọn familiar.
- HE06 chặn dùng bình; HE201 khóa tháo heart và dùng slash cấp 2. **HE201 không phải hiệu ứng chặn rơi Guilt.**
- RB28 bảo vệ khi đang dùng bình, kết thúc bảo vệ sau animation; RB103 dùng bình hồi Fervour thay Life, dùng được khi Life đầy nhưng thiếu Fervour.
- RB102 giảm hồi máu tức thời theo modifier nguồn, rồi hồi dần **1 Life/s tại cấp bình nền**, bị ngắt khi nhận damage/đầy máu/tháo bead. Chưa có nâng cấp bình và chế độ PE02 stocks-of-health.
- RB16 nhận đúng context từ execution/heavy kill để áp dụng xác suất nguồn; kill thường không nhận hai bonus này. Context được truyền từ player, giữ EnemyController nguyên vẹn.
- RB38→RB39→RB40→RB41 đổi stage khi chết và chỉ đổi một stage mỗi lần.

**Sửa lỗi đọc dữ liệu:** `ItemTemporalEffect.effects` là danh sách enum int32 serialized dưới dạng hex của Unity; không phải bitmask. Parser YAML trước có thể hiểu chuỗi này thành số octal. Đã nhập danh sách chính xác riêng cho 11 vật phẩm trong `temporal-effects-source.json`, không nhập lại catalog chính. PR03/09/11/14 có invulnerability tạm thời đúng enum; RB38/39/40 chặn Guilt; HE201 khóa tháo sword. Có kiểm tra damage thật và hết timer.

### Bốn phòng bổ sung, NPC và cửa nhánh

Bốn prefab có **869 node, 623 sprite definition, 41 atlas và 34 clip**, nhập riêng dưới `PriorityTwo/SourceData` và `Resources/Rooms/PriorityTwo`. Không có mesh node trong bốn phòng này; export thiếu 19 script PlayMaker ngoài nhưng dữ liệu FSM đã được dùng để đọc flag/quest.

| Phòng | Node | Vai trò |
|---|---:|---|
| D17Z01S04 | 246 | Cơ quan mở shortcut từ phía dưới; giữ Redento theo quest flag |
| D17Z01S07 | 307 | Phòng nối nhánh ngầm, parallax/camera/cửa nguồn |
| D17Z01S08 | 145 | Bàn Mea Culpa thứ hai, tăng cấp/Strength một lần |
| D17Z01S09 | 171 | Soledad nhận knot để tăng rosary slot |

Đánh cơ quan **LOGIC_188** ở S04 đặt `LEVEL_FLAGS/D17Z01S04_SHORTCUT`; S05 chỉ đổi nhóm đóng→mở/thang hoạt động khi có flag. Không áp dụng các lệnh DEBUG của scene nguồn vào New Game.

**Đường tiếp cận bình thường còn thiếu:** tới S04 từ tuyến hiện tại phải đi qua thêm khoảng 10 phòng của nhánh cống; có các family quái chưa được hỗ trợ như ReekLeader, Roller, BellCarrier và quái bay/boomerang. Bốn phòng bổ sung đã có thể test qua cheat và các cửa nối đã nhập; không thay cửa cũ bằng một đường tắt tự đặt để giả hoàn thành nhánh.

Tirso nhận **QI19/QI20/QI37/QI63/QI64/QI65**, tiêu item và lưu flag để tránh thưởng lặp; phần thưởng đầu QI66, các lần tiếp theo Tears theo FSM; đủ sáu dược liệu nhận QI56. Soledad dùng **QI44/QI52/QI53/QI54/QI55/QI56**, tiêu từng knot, tăng một slot và lưu riêng knot đã dùng; đủ tám slot thì hoàn thành/biến mất. **Chưa có luồng lấy mọi item này tự nhiên trên tuyến đang nhập**, chưa có deadline người bệnh theo các boss ngoài tuyến.

Cherub nguồn **06** ở phòng mở đầu và **07** ở D01Z01S03 đã nhận melee/ranged, giải thoát/animation bay và lưu ID để không cộng lại. Việc hiển thị `0 / 38` không có nghĩa 38 vị trí đã nhập.

### Map, Guilt, save và cutscene

Map dùng ô **16×16** nguồn để dựng hình vuông; kích thước khám phá trong world là **20×11**, hai tỷ lệ này có mục đích khác nhau. Chỉ ô đã đi vào được hiển thị; tên vùng lấy từ localization nguồn. % tính trên 809 ô của thế giới nguồn, nên đi hết tuyến 16 phòng vẫn không phải 100%. Save cũ chỉ ghi completedRooms được chuyển sang các ô của phòng đó; đây là migration xấp xỉ, không phục hồi lịch sử khám phá từng ô đã mất.

Guilt giữ curve Hermite nguồn, tối đa **7 fragment**, link gần **20 đơn vị**, hồi Life/Fervour khi thu hồi và lưu theo vị trí đất an toàn gần nhất. Khi không có override level trong export, dùng mặc định cho phép Guilt của LevelInitializer PC; chưa xác nhận đủ mọi ngoại lệ mobile.

SafeSaveFile kiểm cấu trúc/version/phòng/tọa độ và dữ liệu hữu hạn trước khi nhận file. Backup hỏng bị bỏ qua; không dùng save chính hỏng để ghi đè backup tốt. Cả gameplay và preview menu dùng cùng cơ chế phục hồi; thao tác xóa slot chỉ xóa ba file của slot đó.

CTS02 nguồn dài **23,45 s** đã được nối vào flow chết của Warden. Giữ video gốc; Windows Editor D3D12 dùng preview **352 frame, 15 FPS, 320×180** tách từ chính video để tránh lỗi native backend đã gặp. Đây là fallback giảm chất lượng; Android/backend khác dùng video. Không bỏ các đoạn đen vốn có trong video nguồn. Đổi phòng hủy playback mà không ghi hoàn thành giả; người chơi có thể skip theo thiết lập nguồn. Audio Foley tôn trọng SFX volume/mute, nhạc/ambience tạm dừng khi chiếu.

## File đã thay đổi trong lượt ưu tiên 2

**15 file runtime cũ có sửa**; phần sửa tập trung vào API và điểm nối của các chức năng trên:

- `BlasInventoryUI.cs`, `BlasMapUI.cs`, `BlasOptionsUI.cs`, `MainMenuController.cs`: skill, source map, setting và preview save.
- `BrotherhoodGame.cs`, `PlayerProgress.cs`, `PlayerController.cs`: progression, event, skill/Prayer/item, Guilt/save/cutscene và thêm phòng runtime.
- `InventoryCatalog.cs`, `InventoryModifiers.cs`, `SourceGameplayTuning.cs`: đọc setting effect, stat và cost nguồn.
- `RestoredCatalog.cs`, `RestoredAudio.cs`, `EffectPool.cs`: nạp resource bổ sung, vùng audio và điểm nối projectile.
- `ElderBrotherEncounter.cs`, `DeograciasEncounter.cs`: flow CTS02 và áp dụng volume voice video.

Module mới: `GameSettings`, `SafeSaveFile`, `SourceMap`, `GuiltRuntime`, `InventoryEffectRuntime`, `PriorityTwoPrayers`, `ProgressionEvents`, `SourceWorldRuntime`, `SourceWorldAnchor`, `SourceNPCDialogue`, `SourceBloodyBaptism`.

Editor: thêm `PriorityTwoAssets`, `PriorityTwoRoomAssets`, `PriorityTwoVerification`; cập nhật `BrotherhoodAutomation`, `BrotherhoodVerification`, `PriorityOneVerification` để chạy scene đã lưu, kiểm tra phòng/hiệu ứng mới và kỳ vọng nguồn. Không gọi rebuild toàn bộ qua `p1verify`.

Tools mới: `import_priority_two_audio.py`, `import_priority_two_cutscene.py`, `import_priority_two_effects.py`, `import_priority_two_item_flags.py`, `import_priority_two_rooms.py`, `import_priority_two_world.py`, `import_source_map.py`. Tài nguyên thêm nằm trong các thư mục bổ sung; map/Guilt/temporal có JSON riêng trong Resources/Inventory. Bộ effect có **34 clip, 520 sprite, 24 atlas**.

Các ảnh/log kiểm thử được cập nhật khi chạy test. Không xóa file đã có từ đầu lượt; không tạo commit/PR/APK.

### Phạm vi thay đổi riêng của lượt Prayer

Các file runtime có sẵn sửa trong lượt này: `PlayerController`, `BrotherhoodGame`, `EffectPool`, `RestoredCatalog`, `InventoryModifiers`, `InventoryEffectRuntime`, `PriorityTwoPrayers`, `SourceWorldRuntime`. Điểm nối `SourceWorldRuntime` chỉ thêm vùng đánh Prayer cho captor; đường melee/cơ quan cũ được giữ. Module mới: `PrayerCombat`, `LegacyPrayerEffects`, `PrayerBuffVisuals`.

Editor có `PrayerAssets`, `AllPrayersVerification`, `LegacyPrayerChecks`, `AdditionalPrayerChecks`; cập nhật command và fixture của P1/P2 theo event cast/giới hạn tốc độ nguồn. Importer mới `import_prayer_assets.py`, `import_prayer_audio.py` chỉ ghi resource bổ sung. Báo cáo SHA liệt kê mọi file đổi/thêm; không xóa file đầu lượt.

## Sai khác còn lại so với dự án gốc

1. **Thế giới/progression:** chỉ tuyến đầu và bốn phòng bổ sung; nhánh cống tiếp cận S04, các altar còn lại, nhiều pickup/NPC/quest/boss/ending/DLC chưa hoàn chỉnh. Redento chỉ có visibility gate, chưa có toàn quest; Tirso thiếu deadline theo boss; chưa nối AC26/AC35 và phần lớn achievement.
2. **Hiệu ứng vật phẩm:** còn thiếu CloisteredGem **RB105/RB107**, sprint **HE101**, chuông bí mật **RE02**, vùng độc **RE07** và rơi qua vực theo cherub **RE05**. Trail và harvest của **PR16** đã được nối trong lượt Prayer. Không coi số component metadata là số hiệu ứng đã làm xong.
3. **Prayer — phần hình ảnh/âm thanh còn khác:** collider Prayer đã dùng hình nguồn, nhưng enemy body vẫn là bounds của motor mới. Tint/trail dùng màu và fade tương ứng, chưa tái hiện toàn shader palette/material; shockwave hậu kỳ Cante Jondo chưa có shader gốc. Âm thanh dùng WAV/AudioSource, chưa có routing/mix FMOD; alias `PENITENT_MAGIC_DAMAGE` chưa xác nhận và đang dùng `PENITENT_SIMPLE_ENEMY_HIT` thay thế. Chưa có nghiệm thu pixel/audio parity từng frame trên Android.
4. **Combat/AI:** giữ motor và state machine mới; raw damage melee nền 18 của adapter vẫn khác PenitentStrengthBase 10 nguồn, dù modifier/altar đã nối. Hai encounter Acolyte/Flagellant ở Brotherhood là bố trí thêm; forest dùng marker nguồn. Không sửa bố trí/AI quái trong lượt này.
5. **Animation/đồ họa:** dữ liệu event/track đã giữ nhưng chưa đủ adapter cho toàn bộ 101 tên event, Animator/PlayMaker và shader/mask. FaithPlatform còn thiếu tween màu/sensor/particle/audio truyền giữa bệ; impact/blood còn có hạt tự dựng. Map chưa đủ màu/icon theo tất cả zone/type.
6. **Kiến trúc:** RoomState bật/tắt phòng thay loader nguồn; JSON/SpriteActor thay Animator/PlayMaker; URP unlit thay material Built-in; Input System thay Rewired; WAV/AudioSource thay FMOD. Inventory/Map dùng Caudex hoặc font hệ thống cho tiếng Việt, không hoàn toàn là bitmap font gốc.
7. **Thành phần riêng:** đoạn copyright ở màn Title và nhãn “v. 1.2” ở Title/chọn chế độ được bỏ theo yêu cầu ngày 29/09; credit ở Extras vẫn giữ. Cheat và icon cờ lê là công cụ test theo yêu cầu; ForbiddenZone/còi ở S02 là cơ chế bổ sung đã tồn tại; campaignWon/màn chọn sau Warden là mốc kết thúc chương phục dựng, không phải kết thúc toàn Blasphemous.
8. **Audio/video/save:** open/close inventory dùng sample CHANGE_TAB làm alias, chưa có hai event riêng được xác nhận; cutscene fallback giảm chất lượng; save có schema/slot/migration riêng và fallback replace có giới hạn filesystem.

## Bằng chứng kiểm chứng

| Bộ | Kết quả | Thời điểm 29/09/2026, giờ Việt Nam |
|---|---:|---|
| [17 Prayer](D:/game/Android2D_Blas/Documentation/prayers-playtest-results.txt) | **262 PASS, 0 FAIL** | **10:07:24** |
| [Ưu tiên 2 + hồi quy P1](D:/game/Android2D_Blas/Documentation/priority-two-playtest-results.txt) | **376 PASS, 0 FAIL** = **227 P2 + 149 P1** | **10:08:16** |
| [Gameplay + Godmode mới](D:/game/Android2D_Blas/Documentation/godmode-playtest-results.txt) | **608 PASS, 0 FAIL**, thêm 2 dòng thông tin | **10:06:04** |
| [Bộ tổng sau lượt Prayer](D:/game/Android2D_Blas/Documentation/prayers-regression-results.txt) | **590 PASS, 0 FAIL**, thêm 2 dòng thông tin | **04:32:10** |
| [Menu](D:/game/Android2D_Blas/Documentation/menu-playtest-results.txt) | **13 PASS, 0 FAIL** | **09:53:16** |
| [Đối chiếu nguồn ưu tiên 1](D:/game/Android2D_Blas/Documentation/priority-one-source-checks.txt) | 321 timeline, cost 17 Prayer và 30 effect Prayer; 0 sai lệch tại lần chạy P1 | Bằng chứng P1 trước lượt này |
| [Map validator của scene cũ](D:/game/Android2D_Blas/Documentation/map-validation.txt) | 0 lỗi tham chiếu ở lần rebuild P1 | **28/09, 12:09**, chưa rebuild lại |

Lượt Prayer lúc 04:32 ghi thành công vào [playtest-results.txt](D:/game/Android2D_Blas/Documentation/playtest-results.txt), đã chụp thêm bản cố định `prayers-regression-results.txt` kèm thời gian file UTC. `priority-two-regression-results.txt` là bằng chứng lượt P2 trước đó, không dùng thay kết quả Prayer cuối. Log tổng `checks=592` gồm **590 PASS + 2 dòng thông tin**, không phải 592 kiểm tra PASS. Lượt Godmode mới có `checks=610`: **608 PASS + 2 dòng thông tin**. File tổng chính bị khóa ghi ở lượt này; runner ghi fallback trong Temp, đã chụp kết quả bền vào `godmode-playtest-results.txt` và không dùng bản 590 PASS cũ để báo kết quả mới. Các bộ có phần kiểm tra trùng nhau; không cộng số PASS để tính số hành vi độc lập.

Fixture được sửa theo dữ liệu nguồn: kiểm sprite Soleá ngay lúc sprite còn sống; phân biệt hai link Tirana giao nhau và cửa sổ hit của từng link; đặt ownership Prayer rõ ràng; tiến qua event 0,55 s bằng bước 0,56 s để tránh sai số float. Các phép kiểm tra damage, cost, timer và không hit hai lần vẫn được giữ. Fixture P1/P2 trước đó đã sửa condition HE03, phòng bổ sung và cost ranged **20→7**; không sửa scene cũ để khớp expectation sai.

Đã xem trực quan [tutorial](D:/game/Android2D_Blas/Documentation/Previews/priority-two-tutorial.png), [map nguồn](D:/game/Android2D_Blas/Documentation/Previews/priority-two-source-map.png) và [CTS02 fallback](D:/game/Android2D_Blas/Documentation/Previews/priority-two-bloody-baptism.png). Ảnh map cho phép toàn bộ ô của 16 phòng trong fixture để xem hình dạng, không cấp khám phá này cho New Game.

Đã xem trực quan hiệu ứng [Debla](D:/game/Android2D_Blas/Documentation/Previews/Prayers/PR03-effect.png), [Zarabanda](D:/game/Android2D_Blas/Documentation/Previews/Prayers/PR08-effect.png), [Guardian](D:/game/Android2D_Blas/Documentation/Previews/Prayers/PR101-effect.png), [Tirana](D:/game/Android2D_Blas/Documentation/Previews/Prayers/PR203-effect.png) và [shard Miriam](D:/game/Android2D_Blas/Documentation/Previews/Prayers/PR201-shards.png). Ảnh chụp camera của fixture dùng quái cố định để kiểm hình và vùng; không phải nghiệm thu gameplay đầy đủ trên thiết bị.

Test dùng save riêng `brotherhood-verification.json` và file thử backup riêng, không load/ghi slot hành hương của người dùng. Timestep 30/60/120 là mô phỏng Editor. Chưa có pixel/audio parity từng frame, test native video trên Android hoặc số đo FPS/GC/RAM/nhiệt.

## Việc còn lại theo thứ tự

- [x] Ưu tiên 1 và hồi quy nút Penance/ranged, hurt/stun, cheat, cơ quan S03.
- [x] Ưu tiên 2: stat/heart/ranged cost, đủ mua 15 skill và COMBO/VERTICAL, bốn Prayer bổ sung, sửa PR07/09/10/16.
- [x] Ưu tiên 2: map khám phá/lưu pin, Guilt, event PENANCE, save recovery, nhóm setting/audio nêu trên và CTS02.
- [x] Ưu tiên 2: adapter Tirso/Soledad, hai cherub và flag shortcut S04, bàn S08; kiểm thử độc lập trên prefab bổ sung.
- [ ] Ưu tiên 2: đường tiếp cận nhánh ngầm với các phòng/quái nguồn; luồng nhận knot/dược liệu tự nhiên và quest ngoài tuyến.
- [x] Cơ chế **17/17 Prayer**: cast/cost, buff/timer riêng, entity/geometry/damage, projectile và cleanup; bộ Prayer và các bộ hồi quy đã chạy **0 FAIL**.
- [ ] Ưu tiên 2: các hiệu ứng vật phẩm ngoài Prayer còn thiếu được liệt kê ở trên và các subscriber quest/achievement cần chúng.
- [ ] Ưu tiên 3: đối chiếu cùng camera/frame, đầy đủ VFX/shader/mask/audio; khi có yêu cầu build thì tạo APK mới và nghiệm thu input/safe area/FPS/frame time/GC/RAM/nhiệt trên máy thật.

APK `Builds/Android/Brotherhood.apk` vẫn là bản **12/09/2026**, không đại diện cho những sửa ngày 28–29/09. Không gán tỷ lệ hoàn thành chung cho toàn game.
