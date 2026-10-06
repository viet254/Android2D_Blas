using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Brotherhood
{
    /// <summary>
    /// Reads the Vietnamese strings already present in the "English" column
    /// of the supplied Bla_loc_map I2 localization export.
    /// </summary>
    public sealed class VietnameseSource : MonoBehaviour
    {
        [Serializable] sealed class Entry
        {
            public string group, term, english, vietnamese;
        }
        [Serializable] sealed class Catalog { public Entry[] entries; }

        static Dictionary<string, string> byTerm, byEnglish, inventoryFields;
        static Font font;
        Text[] labels = Array.Empty<Text>();
        float nextScan;

        // Only reconstructed UI has no original localization term. Source
        // I2 strings always take precedence over these small fallback labels.
        static readonly Dictionary<string, string> Fallback =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["HOME"] = "TRANG CHỦ", ["SETTINGS"] = "CÀI ĐẶT",
            ["ACHIEVEMENTS"] = "THÀNH TỰU", ["BACK"] = "QUAY LẠI",
            ["EXTRAS"] = "NỘI DUNG THÊM", ["EMPTY"] = "TRỐNG",
            ["DELETE"] = "XÓA", ["NEW GAME"] = "TRÒ CHƠI MỚI",
            ["GAME OVER"] = "TRÒ CHƠI KẾT THÚC",
            ["REQUIEM AETERNAM"] = "AN NGHỈ NGÀN THU",
            ["HỒI SINH"] = "HỒI SINH",
            ["A LONG PATH AHEAD"] = "CHẶNG ĐƯỜNG DÀI PHÍA TRƯỚC",
            ["Defeat the Warden of Silent Sorrow."] = "Đánh bại Kẻ Canh Giữ Đau Khổ Lặng Im.",
            ["TOUCH THE SCREEN OR PRESS ANY BUTTON"] = "CHẠM MÀN HÌNH HOẶC NHẤN PHÍM BẤT KỲ",
            ["GAME"] = "TRÒ CHƠI", ["AUDIO"] = "ÂM THANH",
            ["VIDEO"] = "HÌNH ẢNH", ["HOW TO PLAY"] = "CÁCH CHƠI",
            ["TOUCH CONTROLS"] = "ĐIỀU KHIỂN CẢM ỨNG",
            ["HAPTIC FEEDBACK"] = "RUNG PHẢN HỒI",
            ["ACHIEVEMENT POPUPS"] = "THÔNG BÁO THÀNH TỰU",
            ["FRAME RATE"] = "TỐC ĐỘ KHUNG HÌNH",
            ["RESOLUTION MODE"] = "CHẾ ĐỘ HIỂN THỊ",
            ["MASTER VOLUME"] = "ÂM LƯỢNG CHUNG",
            ["VOICEOVER VOLUME"] = "ÂM LƯỢNG GIỌNG NÓI",
            ["JOYSTICK"] = "CẦN ĐIỀU KHIỂN",
            ["FIXED"] = "CỐ ĐỊNH", ["FLOATING"] = "LINH HOẠT",
            ["CONTROL SIZE"] = "KÍCH CỠ NÚT",
            ["RESTORE DEFAULT"] = "KHÔI PHỤC MẶC ĐỊNH",
            ["RESET"] = "ĐẶT LẠI",
            ["TUTORIAL POPUPS"] = "THÔNG BÁO HƯỚNG DẪN",
            ["ON"] = "BẬT", ["OFF"] = "TẮT",
            ["GAME SETTINGS"] = "CÀI ĐẶT TRÒ CHƠI",
            ["AUDIO SETTINGS"] = "CÀI ĐẶT ÂM THANH",
            ["MUSIC PLAYBACK"] = "PHÁT NHẠC",
            ["SFX PLAYBACK"] = "PHÁT HIỆU ỨNG",
            ["MUSIC OFF"] = "TẮT NHẠC", ["MUSIC ON"] = "BẬT NHẠC",
            ["SOUND OFF"] = "TẮT TIẾNG", ["SOUND ON"] = "BẬT TIẾNG",
            ["VIBRATION / HAPTICS"] = "RUNG / PHẢN HỒI",
            ["JOYSTICK MODE"] = "CHẾ ĐỘ CẦN ĐIỀU KHIỂN",
            ["TOUCH CONTROLS SCALE"] = "KÍCH CỠ NÚT CẢM ỨNG",
            ["CUSTOMIZE BUTTON LAYOUT"] = "TÙY CHỈNH VỊ TRÍ NÚT",
            ["GAMEPLAY & COMBAT TIPS"] = "MẸO CHƠI VÀ CHIẾN ĐẤU",
            ["ACCESSIBILITY"] = "HỖ TRỢ TIẾP CẬN",
            ["PIXEL PERFECT"] = "PIXEL CHUẨN", ["SCALE"] = "CO GIÃN",
            ["OPEN"] = "MỞ", ["CANCEL"] = "HỦY",
            ["CORRUPTED SAVE"] = "BẢN LƯU BỊ HỎNG",
            ["CREDITS"] = "GHI CÔNG",
            ["ROSARY BEADS"] = "HẠT CHUỖI MÂN CÔI",
            ["RELICS"] = "THÁNH TÍCH",
            ["QUEST ITEMS"] = "VẬT PHẨM NHIỆM VỤ",
            ["MEA CULPA HEARTS"] = "TRÁI TIM MEA CULPA",
            ["PRAYERS"] = "LỜI CẦU NGUYỆN",
            ["ABILITIES"] = "KỸ NĂNG",
            ["COLLECTIBLES"] = "VẬT PHẨM SƯU TẦM",
            ["Lore"] = "TRUYỆN TÍCH",
            ["ACTIVE"] = "ĐANG DÙNG", ["UPGRADE"] = "NÂNG CẤP",
            ["UNEQUIP"] = "THÁO RA", ["EQUIP"] = "TRANG BỊ",
            ["SUBURBS"] = "VÙNG NGOẠI Ô",
            ["THE HOLY LINE"] = "DÒNG THÁNH",
            ["YOU HAVE FALLEN"] = "BẠN ĐÃ GỤC NGÃ",
            ["Defeat the Warden to open the gate"] = "Đánh bại Kẻ Canh Giữ để mở cổng",
            ["End of restored route"] = "Đã đến cuối tuyến đường được khôi phục",
            ["Unable to save progress"] = "Không thể lưu tiến độ",
            ["PRIE DIEU · progress saved"] = "PRIE DIEU · ĐÃ LƯU TIẾN ĐỘ",
            ["REQUIEM AETERNAM · the way is open"] = "AN NGHỈ NGÀN THU · LỐI ĐI ĐÃ MỞ",
            ["FORBIDDEN ZONE"] = "VÙNG CẤM",
            ["FERVOROUS BLOOD"] = "MÁU CUỒNG TÍN",
            ["MAP"] = "BẢN ĐỒ", ["INVENTORY"] = "TÚI ĐỒ",
            ["SKILLS"] = "KỸ NĂNG", ["PREVIOUS"] = "TRƯỚC",
            ["NEXT"] = "SAU", ["CONTROLS"] = "ĐIỀU KHIỂN",
            ["CLOSE"] = "ĐÓNG", ["SELECT ITEM"] = "CHỌN VẬT PHẨM",
            ["NOT OWNED"] = "CHƯA SỞ HỮU", ["NOT FOUND"] = "CHƯA TÌM THẤY",
            ["EQUIPPED"] = "ĐÃ TRANG BỊ", ["OWNED"] = "ĐÃ SỞ HỮU",
            ["UNLOCKED"] = "ĐÃ MỞ", ["LOCKED"] = "CHƯA MỞ",
            ["JUMP"] = "NHẢY", ["ATTACK"] = "TẤN CÔNG",
            ["DASH"] = "LƯỚT", ["PARRY"] = "ĐỠ ĐÒN",
            ["FLASK"] = "BÌNH MÁU", ["PRAYER"] = "CẦU NGUYỆN",
            ["SPECIAL"] = "ĐẶC BIỆT", ["USE"] = "DÙNG",
            ["BAG"] = "TÚI ĐỒ", ["DONE"] = "XONG",
            ["TOUCH LAYOUT SAVED"] = "ĐÃ LƯU BỐ CỤC NÚT",
            ["PAGE"] = "TRANG", ["DEOGRACIAS"] = "DEOGRACIAS",
            ["THORN"] = "GAI",
            ["Deosgracias' Farewell"] = "Lời tạm biệt của Deosgracias",
            ["Tutorial prompts explain movement, combat and interaction\nduring the pilgrimage."] =
                "Hướng dẫn giải thích cách di chuyển, chiến đấu và tương tác\ntrong hành trình.",
            ["The selected save slot and its backup will be erased."] =
                "Ô lưu đã chọn và bản sao lưu của nó sẽ bị xóa.",
            ["DELETE PILGRIMAGE?"] = "XÓA HÀNH TRÌNH?",
            ["PRAYER EQUIPPED\nConsumes Fervour and invokes its source effect."] =
                "ĐÃ TRANG BỊ LỜI CẦU NGUYỆN\nTiêu hao Nhiệt huyết để kích hoạt hiệu ứng.",
            ["NO PRAYER FOUND"] = "CHƯA CÓ LỜI CẦU NGUYỆN",
            ["OWNED · READY TO EQUIP"] = "ĐÃ CÓ · SẴN SÀNG TRANG BỊ",
            ["rosarybead"] = "HẠT MÂN CÔI", ["relic"] = "THÁNH TÍCH",
            ["questitem"] = "VẬT PHẨM NHIỆM VỤ", ["sword"] = "KIẾM",
            ["ability"] = "KỸ NĂNG",
            ["collectibleitem"] = "VẬT PHẨM SƯU TẦM"
        };

        static void Load()
        {
            if (byTerm != null) return;
            byTerm = new Dictionary<string, string>(StringComparer.Ordinal);
            byEnglish = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            inventoryFields = new Dictionary<string, string>(StringComparer.Ordinal);
            var asset = Resources.Load<TextAsset>("Localization/SourceVietnamese");
            if (asset == null) { Debug.LogWarning("Source Vietnamese I2 catalog missing"); return; }
            var catalog = JsonUtility.FromJson<Catalog>(asset.text);
            if (catalog == null || catalog.entries == null) return;
            foreach (var entry in catalog.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.vietnamese)) continue;
                byTerm[entry.term] = entry.vietnamese;
                if (!string.IsNullOrEmpty(entry.english) && !byEnglish.ContainsKey(entry.english))
                    byEnglish.Add(entry.english, entry.vietnamese);
                if (entry.group == "inventory")
                {
                    int slash = entry.term.LastIndexOf('/');
                    if (slash >= 0) inventoryFields[entry.term.Substring(slash + 1)] = entry.vietnamese;
                }
            }
        }

        public static string Term(string key, string fallback = "")
        {
            Load();
            return byTerm.TryGetValue(key, out var result) ? result : fallback;
        }

        public static string Inventory(string id, string field, string fallback)
        {
            Load();
            return inventoryFields.TryGetValue(id + "_" + field, out var result) ? result : fallback;
        }

        public static string Display(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;
            Load();
            if (byEnglish.TryGetValue(source, out var result) &&
                (!string.Equals(result, source, StringComparison.OrdinalIgnoreCase) || !Fallback.ContainsKey(source)))
                return result;
            if (Fallback.TryGetValue(source, out result)) return result;
            if (source.StartsWith("PAGE ", StringComparison.Ordinal))
                return "TRANG " + source.Substring(5);
            if (source.StartsWith("PILGRIMAGE SAVED", StringComparison.Ordinal))
                return source.Replace("PILGRIMAGE SAVED", "ĐÃ LƯU HÀNH TRÌNH")
                    .Replace("% COMPLETED", "% HOÀN THÀNH");
            if (source.Contains("CURRENT ROOM") || source.Contains("SOURCE CATALOG") ||
                source.Contains("CHARGED ATTACK") || source.StartsWith("INVENTORY\n", StringComparison.Ordinal))
                return source.Replace("BROTHERHOOD OF THE SILENT SORROW", Term("Map/D17", "Hội Anh Em Đau Khổ Lặng Im"))
                    .Replace("AWAKENING", "TỈNH GIẤC").Replace("HALLWAY", "HÀNH LANG")
                    .Replace("WARDEN", "KẺ CANH GIỮ").Replace("THE HOLY LINE", Term("Map/D01_Z01", "Thánh Tuyến"))
                    .Replace("CURRENT ROOM", "PHÒNG HIỆN TẠI").Replace("RESTORED ROOMS", "PHÒNG ĐÃ KHÔI PHỤC")
                    .Replace("INVENTORY", "TÚI ĐỒ").Replace("HEALTH", "SINH LỰC")
                    .Replace("FERVOUR", "NHIỆT HUYẾT").Replace("BILE FLASKS", "BÌNH MẬT")
                    .Replace("TEARS OF ATONEMENT", "GIỌT LỆ SÁM HỐI")
                    .Replace("SOURCE CATALOG", "DANH MỤC GỐC").Replace("ITEMS", "VẬT PHẨM")
                    .Replace("CHARGED ATTACK", "ĐÒN TÍCH LỰC")
                    .Replace("SACRED ONSLAUGHT", "XUNG KÍCH THÁNH")
                    .Replace("FERVOROUS BLOOD", "MÁU CUỒNG TÍN")
                    .Replace("SWORD HEARTS", "TRÁI TIM KIẾM")
                    .Replace("ROSARY BEADS", "HẠT MÂN CÔI")
                    .Replace("TIER", "BẬC").Replace("UNLOCKED", "ĐÃ MỞ")
                    .Replace("LOCKED", "CHƯA MỞ");
            return source;
        }

        static bool NeedsVietnameseFont(string value)
        {
            foreach (char c in value)
                if (c == 'Đ' || c == 'đ' || c >= '\u1EA0' && c <= '\u1EF9' ||
                    c == 'Ă' || c == 'ă' || c == 'Â' || c == 'â' ||
                    c == 'Ê' || c == 'ê' || c == 'Ô' || c == 'ô' ||
                    c == 'Ơ' || c == 'ơ' || c == 'Ư' || c == 'ư')
                    return true;
            return false;
        }

        static Font VietnameseFont
        {
            get
            {
                if (font == null)
                {
                    font = UnityEngine.Font.CreateDynamicFontFromOSFont(
                        new[] { "Roboto", "Arial", "Noto Sans", "Segoe UI" }, 20);
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        public static Font DynamicFont => VietnameseFont;

        public static void Apply(Text label)
        {
            if (label == null || string.IsNullOrEmpty(label.text)) return;
            string value = Display(label.text);
            if (value != label.text) label.text = value;
            if (NeedsVietnameseFont(label.text) && VietnameseFont != null && label.font != VietnameseFont)
            {
                label.font = VietnameseFont;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = Mathf.Min(12, label.fontSize);
                label.resizeTextMaxSize = label.fontSize;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindAnyObjectByType<VietnameseSource>() != null) return;
            var go = new GameObject("Source Vietnamese UI");
            DontDestroyOnLoad(go);
            go.AddComponent<VietnameseSource>();
        }

        void LateUpdate()
        {
            if (Time.unscaledTime >= nextScan)
            {
                labels = FindObjectsByType<Text>(FindObjectsInactive.Include);
                nextScan = Time.unscaledTime + .4f;
            }
            foreach (var label in labels) Apply(label);
        }
    }
}
