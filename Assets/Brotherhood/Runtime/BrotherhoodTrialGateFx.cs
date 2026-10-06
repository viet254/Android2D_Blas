using System.Collections.Generic;
using UnityEngine;

namespace Brotherhood
{
    // The imported S09 rubble is visual-only. Keep the source wall collision and
    // the route's walk trigger intact while clearing the art after the boss dies.
    public sealed class BrotherhoodTrialGateFx : MonoBehaviour
    {
        static readonly Rect[] SourcePieces =
        {
            new Rect(151, 55, 37, 37), new Rect(58, 55, 46, 46),
            new Rect(0, 116, 56, 56), new Rect(0, 172, 53, 57),
            new Rect(0, 60, 57, 56), new Rect(58, 0, 59, 55),
            new Rect(0, 0, 58, 60), new Rect(117, 0, 54, 54),
            new Rect(171, 0, 50, 48), new Rect(104, 55, 47, 41)
        };

        sealed class Shard
        {
            public Transform node;
            public SpriteRenderer visual;
            public Vector2 velocity;
            public Color tint;
            public float life, duration, spin;
        }

        readonly List<Shard> shards = new List<Shard>();
        SpriteRenderer blockingArt;
        BrotherhoodGame game;
        Sprite[] pieces;
        float waveClock;
        int nextWave;

        public bool IsOpen { get; private set; }
        public bool BlockingArtVisible => blockingArt != null && blockingArt.enabled;
        public bool SourceTextureLoaded => pieces != null && pieces.Length == SourcePieces.Length;
        public int BurstCount { get; private set; }
        public int ActiveShardCount => shards.Count;

        public void Configure(BrotherhoodGame owner, SpriteRenderer rockArt, bool bossDefeated)
        {
            game = owner;
            blockingArt = rockArt;
            var texture = Resources.Load<Texture2D>("Effects/wallCrumble_dust_big");
            if (texture == null)
                Debug.LogError("Missing original wallCrumble_dust_big rubble effect for the S09 boss gate");
            else
            {
                pieces = new Sprite[SourcePieces.Length];
                for (int i = 0; i < pieces.Length; i++)
                    pieces[i] = Sprite.Create(texture, SourcePieces[i], Vector2.one * .5f, 32);
            }
            Sync(bossDefeated);
        }

        // Save/load and room entry restore the permanent art state silently.
        public void Sync(bool bossDefeated)
        {
            ClearTransient();
            IsOpen = bossDefeated;
            if (blockingArt != null) blockingArt.enabled = !bossDefeated;
        }

        // Called only for the first live boss defeat, after its save flag is set.
        public void OpenWithCrumble()
        {
            if (blockingArt == null) return;
            ClearTransient();
            IsOpen = true;
            blockingArt.enabled = false;
            BurstCount++;
            if (SourceTextureLoaded)
            {
                EmitWave(0);
                nextWave = 1;
                waveClock = .1f;
            }
            game?.Sfx("CHARGED_ATTACK_PROJECTILE_HIT_WALL", .85f);
            game?.Shake(.24f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (nextWave > 0 && nextWave < 5)
            {
                waveClock -= dt;
                while (waveClock <= 0 && nextWave < 5)
                {
                    EmitWave(nextWave++);
                    waveClock += .1f;
                }
            }
            for (int i = shards.Count - 1; i >= 0; i--)
            {
                var shard = shards[i];
                shard.life -= dt;
                if (shard.life <= 0)
                {
                    Destroy(shard.node.gameObject);
                    shards.RemoveAt(i);
                    continue;
                }
                shard.velocity += Vector2.down * (4.5f * dt);
                shard.node.position += (Vector3)(shard.velocity * dt);
                shard.node.Rotate(0, 0, shard.spin * dt);
                var tint = shard.tint;
                tint.a *= Mathf.Clamp01(shard.life / (shard.duration * .45f));
                shard.visual.color = tint;
            }
        }

        void EmitWave(int wave)
        {
            // The source BREAKFLOORFX emits five fragments per burst, five
            // bursts 0.1 seconds apart. Narrow that spread to the S09 arch.
            var center = new Vector2(-887.8f, -11.1f);
            for (int i = 0; i < 5; i++)
            {
                var node = new GameObject("Source wall crumble fragment");
                node.transform.SetParent(transform, true);
                node.transform.position = center + new Vector2(Random.Range(-.55f, .55f), Random.Range(-1.2f, 1.3f));
                node.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-32f, 32f));
                node.transform.localScale = Vector3.one * Random.Range(.42f, .72f);
                var visual = node.AddComponent<SpriteRenderer>();
                visual.sprite = pieces[(wave * 5 + i) % pieces.Length];
                visual.sortingLayerID = blockingArt.sortingLayerID;
                visual.sortingOrder = blockingArt.sortingOrder + 2;
                var duration = Random.Range(.8f, 1.2f);
                shards.Add(new Shard
                {
                    node = node.transform,
                    visual = visual,
                    velocity = new Vector2(Random.Range(.5f, 3.3f), Random.Range(-.3f, 2.7f)),
                    tint = Color.white,
                    life = duration,
                    duration = duration,
                    spin = Random.Range(-160f, 160f)
                });
            }
        }

        void ClearTransient()
        {
            nextWave = 0;
            waveClock = 0;
            foreach (var shard in shards)
                if (shard.node != null) Destroy(shard.node.gameObject);
            shards.Clear();
        }

        void OnDestroy()
        {
            ClearTransient();
            if (pieces == null) return;
            foreach (var piece in pieces)
                if (piece != null) Destroy(piece);
        }
    }
}
