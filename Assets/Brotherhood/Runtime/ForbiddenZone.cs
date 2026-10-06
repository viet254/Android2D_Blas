using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Brotherhood
{
    /// <summary>
    /// Vùng cấm (Forbidden Zone) trên bản đồ game.
    /// Khi Đối tượng B (quái vật / NPC) bắt đầu di chuyển vào vùng cấm,
    /// hệ thống sẽ phát tiếng cảnh báo liên tục từ 3 đến 6 lần (mặc định 5 lần)
    /// kèm hiệu ứng hình ảnh cảnh báo.
    /// Module này được thiết kế độc lập, dễ dàng bật/tắt hoặc tháo gỡ theo yêu cầu.
    /// </summary>
    public sealed class ForbiddenZone : MonoBehaviour
    {
        public BrotherhoodGame game;

        [Header("Zone Configuration")]
        public string targetRoomId = "D17Z01S02"; // Phòng có quái vật Acolyte tuần tra
        public Vector2 center = new Vector2(-938.5f, 8.5f);
        public Vector2 size = new Vector2(3.5f, 2.5f);

        [Header("Alarm Settings")]
        [Range(3, 6)]
        public int alarmRepeatCount = 5; // Cảnh báo từ 3 đến 6 lần theo yêu cầu
        public float alarmInterval = 0.35f; // Khoảng cách giữa các lần kêu (giây)

        [Header("Visuals")]
        public Color normalColor = new Color(1f, 0.15f, 0.15f, 0.28f);
        public Color alertColor = new Color(1f, 0.85f, 0.15f, 0.65f);

        private readonly HashSet<EnemyController> enemiesInside = new HashSet<EnemyController>();
        private GameObject visualRoot;
        private SpriteRenderer fillRenderer;
        private SpriteRenderer[] borderRenderers;
        private TextMesh labelMesh;
        private AudioSource alarmSource;
        private AudioClip alarmClip;
        private Coroutine alarmCoroutine;

        void Awake()
        {
            alarmSource = gameObject.AddComponent<AudioSource>();
            alarmSource.playOnAwake = false;
            alarmSource.loop = false;
            alarmClip = CreateAlarmClip();
            BuildVisuals();
        }

        public void Tick(float dt)
        {
            if (game == null || game.Current == null)
            {
                SetVisualActive(false);
                return;
            }

            bool inTargetRoom = game.Current.id == targetRoomId;
            SetVisualActive(inTargetRoom);
            if (!inTargetRoom)
            {
                enemiesInside.Clear();
                return;
            }

            // Kiểm tra các NPC / kẻ địch trong phòng hiện tại
            var enemies = game.Current.enemies;
            if (enemies == null || enemies.Length == 0) return;

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.Dead)
                {
                    if (enemy != null && enemiesInside.Contains(enemy))
                        enemiesInside.Remove(enemy);
                    continue;
                }

                bool isInside = Contains(enemy.transform.position);

                if (isInside)
                {
                    // Đối tượng B BẮT ĐẦU di chuyển vào vùng cấm từ bên ngoài
                    if (!enemiesInside.Contains(enemy))
                    {
                        enemiesInside.Add(enemy);
                        OnEnemyEntered(enemy);
                    }
                }
                else
                {
                    // Đối tượng B đã rời khỏi vùng cấm
                    if (enemiesInside.Contains(enemy))
                    {
                        enemiesInside.Remove(enemy);
                    }
                }
            }
        }

        public bool Contains(Vector2 point)
        {
            return Mathf.Abs(point.x - center.x) <= size.x * 0.5f &&
                   Mathf.Abs(point.y - center.y) <= size.y * 0.5f;
        }

        private void OnEnemyEntered(EnemyController enemy)
        {
            if (alarmCoroutine != null)
                StopCoroutine(alarmCoroutine);

            alarmCoroutine = StartCoroutine(AlarmSequenceRoutine(enemy));
        }

        private IEnumerator AlarmSequenceRoutine(EnemyController enemy)
        {
            string enemyName = enemy != null ? enemy.family.ToUpper() : "ENEMY";

            for (int i = 1; i <= alarmRepeatCount; i++)
            {
                // 1. Phát âm thanh cảnh báo (tôn trọng thiết lập bật/tắt SFX trong Option)
                PlayAlarmSound();

                // 2. Hiển thị thông báo trên màn hình HUD
                if (game != null)
                {
                    game.Message($"⚠️ CẢNH BÁO: {enemyName} XÂM NHẬP VÙNG CẤM! ({i}/{alarmRepeatCount})");
                    game.Shake(0.12f);

                    if (game.effects != null && enemy != null)
                    {
                        game.effects.Burst(enemy.transform.position + Vector3.up * 0.8f, Color.red);
                        game.effects.Burst(center, Color.yellow);
                    }
                }

                // 3. Nhấp nháy màu vùng cấm
                SetVisualAlert(true);
                yield return new WaitForSeconds(alarmInterval * 0.5f);
                SetVisualAlert(false);
                yield return new WaitForSeconds(alarmInterval * 0.5f);
            }

            alarmCoroutine = null;
        }

        private void PlayAlarmSound()
        {
            // Kiểm tra trạng thái Mute SFX từ Option
            bool sfxMuted = (game != null && game.audioBank != null)
                ? game.audioBank.SfxMuted
                : PlayerPrefs.GetInt("BrotherhoodSfxMuted", 0) == 1;

            if (sfxMuted) return;

            float sfxVol = PlayerPrefs.GetFloat("BrotherhoodSfxVol", 1f);
            if (alarmSource != null && alarmClip != null)
            {
                alarmSource.volume = 0.85f * sfxVol;
                alarmSource.pitch = 1.05f;
                alarmSource.PlayOneShot(alarmClip);
            }

            // Đồng thời phát kèm tiếng stunt từ bank âm thanh gốc nếu có
            if (game != null)
            {
                game.Sfx("ENEMY_STUNT", 0.6f);
            }
        }

        private void SetVisualActive(bool active)
        {
            if (visualRoot != null && visualRoot.activeSelf != active)
                visualRoot.SetActive(active);
        }

        private void SetVisualAlert(bool alert)
        {
            if (fillRenderer != null)
                fillRenderer.color = alert ? alertColor : normalColor;

            if (borderRenderers != null)
            {
                Color bColor = alert ? Color.yellow : new Color(1f, 0.25f, 0.2f, 0.9f);
                foreach (var r in borderRenderers)
                    if (r != null) r.color = bColor;
            }
        }

        private void BuildVisuals()
        {
            visualRoot = new GameObject("ForbiddenZone_Visuals");
            visualRoot.transform.SetParent(transform);
            visualRoot.transform.position = new Vector3(center.x, center.y, 0);

            var whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);

            // 1. Mặt nền bán trong suốt
            var fillObj = new GameObject("ZoneFill", typeof(SpriteRenderer));
            fillObj.transform.SetParent(visualRoot.transform, false);
            fillRenderer = fillObj.GetComponent<SpriteRenderer>();
            fillRenderer.sprite = whiteSprite;
            fillRenderer.color = normalColor;
            fillRenderer.sortingOrder = 500;
            fillObj.transform.localScale = new Vector3(size.x, size.y, 1);

            // 2. Viền cảnh báo 4 cạnh
            borderRenderers = new SpriteRenderer[4];
            float thickness = 0.08f;
            Color borderColor = new Color(1f, 0.25f, 0.2f, 0.9f);

            // Top border
            borderRenderers[0] = MakeBorderLine("TopBorder", whiteSprite, new Vector3(0, size.y * 0.5f, 0), new Vector3(size.x + thickness, thickness, 1), borderColor);
            // Bottom border
            borderRenderers[1] = MakeBorderLine("BottomBorder", whiteSprite, new Vector3(0, -size.y * 0.5f, 0), new Vector3(size.x + thickness, thickness, 1), borderColor);
            // Left border
            borderRenderers[2] = MakeBorderLine("LeftBorder", whiteSprite, new Vector3(-size.x * 0.5f, 0, 0), new Vector3(thickness, size.y, 1), borderColor);
            // Right border
            borderRenderers[3] = MakeBorderLine("RightBorder", whiteSprite, new Vector3(size.x * 0.5f, 0, 0), new Vector3(thickness, size.y, 1), borderColor);

            // 3. Nhãn chữ cảnh báo trên vùng cấm
            var labelObj = new GameObject("ZoneLabel", typeof(TextMesh));
            labelObj.transform.SetParent(visualRoot.transform, false);
            labelObj.transform.localPosition = new Vector3(0, size.y * 0.5f + 0.35f, 0);
            labelMesh = labelObj.GetComponent<TextMesh>();
            labelMesh.text = VietnameseSource.Display("FORBIDDEN ZONE");
            labelMesh.font = VietnameseSource.DynamicFont;
            var labelRenderer=labelObj.GetComponent<MeshRenderer>();
            if(labelRenderer!=null&&labelMesh.font!=null)labelRenderer.sharedMaterial=labelMesh.font.material;
            labelMesh.fontSize = 20;
            labelMesh.characterSize = 0.06f;
            labelMesh.alignment = TextAlignment.Center;
            labelMesh.anchor = TextAnchor.MiddleCenter;
            labelMesh.color = new Color(1f, 0.85f, 0.35f);
            var textRenderer = labelObj.GetComponent<MeshRenderer>();
            if (textRenderer != null) textRenderer.sortingOrder = 550;

            visualRoot.SetActive(false);
        }

        private SpriteRenderer MakeBorderLine(string name, Sprite sprite, Vector3 pos, Vector3 scale, Color color)
        {
            var obj = new GameObject(name, typeof(SpriteRenderer));
            obj.transform.SetParent(visualRoot.transform, false);
            obj.transform.localPosition = pos;
            obj.transform.localScale = scale;
            var sr = obj.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 510;
            return sr;
        }

        private AudioClip CreateAlarmClip()
        {
            // Tạo sóng âm thanh bíp cảnh báo tần số cao 880Hz (chuẩn nốt A5 cảnh báo)
            int sampleRate = 44100;
            float duration = 0.16f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float freq = 880f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Hàm bao sin mượt để không bị tiếng pop/click
                float envelope = Mathf.Sin(Mathf.PI * (t / duration));
                samples[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * envelope * 0.75f;
            }

            var clip = AudioClip.Create("AlarmWarningClip", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        void OnDestroy()
        {
            if (alarmCoroutine != null)
                StopCoroutine(alarmCoroutine);

            if (visualRoot != null)
                Destroy(visualRoot);
        }
    }
}
