using UnityEngine;

namespace Brotherhood
{
    /// <summary>
    /// Plays the supplied Red Earth Lavia pixel frames for the D17 trial boss.
    /// The source frames were aligned to a common torso anchor in the imported atlas.
    /// Damage and movement belong to BrotherhoodTrialLaviaAI, not to this renderer.
    /// </summary>
    public sealed class BrotherhoodTrialLaviaVisual : MonoBehaviour
    {
        const int BodyCellWidth = 416;
        const int BodyCellHeight = 256;
        const int BodyColumns = 6;
        const int BodyFrameCount = 39;
        const int ProjectileCell = 192;
        const int ProjectileFrameCount = 4;
        const float PixelsPerUnit = 96f;

        SpriteRenderer target;
        Sprite[] bodyFrames;
        string state = "Idle";
        int frameIndex;
        float frameClock;

        public bool IsReady { get; private set; }
        public float DeathDuration => 9 * .13f;
        public Sprite[] ProjectileSprites { get; private set; }

        public void Configure(SpriteRenderer renderer)
        {
            target = renderer;
            var bodyTexture = Resources.Load<Texture2D>("Trial/Lavia/lavia-body-atlas");
            var projectileTexture = Resources.Load<Texture2D>("Trial/Lavia/lavia-fireball-atlas");
            if (target == null || bodyTexture == null || projectileTexture == null ||
                bodyTexture.width < BodyCellWidth * BodyColumns ||
                bodyTexture.height < BodyCellHeight * 7 ||
                projectileTexture.width < ProjectileCell * ProjectileFrameCount ||
                projectileTexture.height < ProjectileCell)
            {
                Debug.LogError("Lavia sprite atlases are missing or have invalid dimensions.", this);
                return;
            }

            bodyTexture.filterMode = FilterMode.Point;
            projectileTexture.filterMode = FilterMode.Point;
            bodyFrames = new Sprite[BodyFrameCount];
            for (int i = 0; i < bodyFrames.Length; i++)
            {
                int column = i % BodyColumns;
                int row = i / BodyColumns;
                var rect = new Rect(column * BodyCellWidth,
                    bodyTexture.height - (row + 1) * BodyCellHeight,
                    BodyCellWidth, BodyCellHeight);
                bodyFrames[i] = Sprite.Create(bodyTexture, rect, Vector2.one * .5f,
                    PixelsPerUnit, 0, SpriteMeshType.FullRect);
                bodyFrames[i].name = "Lavia_" + i.ToString("00");
            }

            ProjectileSprites = new Sprite[ProjectileFrameCount];
            for (int i = 0; i < ProjectileSprites.Length; i++)
            {
                var rect = new Rect(i * ProjectileCell, 0, ProjectileCell, ProjectileCell);
                ProjectileSprites[i] = Sprite.Create(projectileTexture, rect,
                    Vector2.one * .5f, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                ProjectileSprites[i].name = "Lavia_Fireball_" + i;
            }

            IsReady = true;
            Play("Idle", true);
        }

        public void Play(string nextState, bool restart = false)
        {
            if (!IsReady || target == null) return;
            string normalized = Normalize(nextState);
            if (!restart && state == normalized) return;
            state = normalized;
            frameIndex = 0;
            frameClock = 0;
            target.sprite = bodyFrames[StartFrame(state)];
        }

        public void SetFacing(float direction)
        {
            if (target != null && Mathf.Abs(direction) > .001f)
                target.flipX = direction > 0; // Authored Lavia faces left.
        }

        public void SetTint(Color tint)
        {
            if (target != null) target.color = tint;
        }

        public void Show(bool visible)
        {
            if (target != null) target.enabled = visible;
        }

        void Update()
        {
            if (!IsReady || target == null || !target.enabled) return;
            frameClock += Time.deltaTime;
            float interval = FrameInterval(state);
            if (frameClock < interval) return;
            int count = FrameCount(state);
            while (frameClock >= interval)
            {
                frameClock -= interval;
                if (frameIndex + 1 < count) frameIndex++;
                else if (state == "Idle") frameIndex = 0;
                else { frameIndex = count - 1; frameClock = 0; break; }
            }
            target.sprite = bodyFrames[StartFrame(state) + frameIndex];
        }

        static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Idle";
            switch (value.ToLowerInvariant())
            {
                case "windup": return "Windup";
                case "dive": return "Dive";
                case "slash": return "Slash";
                case "cast": return "Cast";
                case "hurt": return "Hurt";
                case "death": return "Death";
                default: return "Idle";
            }
        }

        static int StartFrame(string value)
        {
            switch (value)
            {
                case "Windup": return 6;
                case "Dive": return 10;
                case "Slash": return 16;
                case "Cast": return 22;
                case "Hurt": return 27;
                case "Death": return 30;
                default: return 0;
            }
        }

        static int FrameCount(string value)
        {
            switch (value)
            {
                case "Windup": return 4;
                case "Dive": return 6;
                case "Slash": return 6;
                case "Cast": return 5;
                case "Hurt": return 3;
                case "Death": return 9;
                default: return 6;
            }
        }

        static float FrameInterval(string value)
        {
            switch (value)
            {
                case "Windup": return .12f;
                case "Dive": return .075f;
                case "Slash": return .09f;
                case "Cast": return .12f;
                case "Hurt": return .11f;
                case "Death": return .13f;
                default: return .11f;
            }
        }

        void OnDestroy()
        {
            if (bodyFrames != null)
                foreach (var sprite in bodyFrames)
                    if (sprite != null) Destroy(sprite);
            if (ProjectileSprites != null)
                foreach (var sprite in ProjectileSprites)
                    if (sprite != null) Destroy(sprite);
        }
    }
}
