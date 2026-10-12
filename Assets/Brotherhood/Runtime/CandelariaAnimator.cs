using System;
using UnityEngine;

namespace Brotherhood
{
    /// <summary>
    /// Dedicated animator component for Candelaria that plays her 34-frame idle loop at 12 fps.
    /// Marked [ExecuteAlways] so she animates in Scene view, Edit mode, and Play mode.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CandelariaAnimator : MonoBehaviour
    {
        public SpriteRenderer visual;
        public Sprite[] frames = new Sprite[34];
        public float fps = 12f;
        public int currentFrame;

        float timer;
        float lastRealtime;

        void Awake()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            EnsureFramesLoaded();
        }

        void OnEnable()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            EnsureFramesLoaded();
            lastRealtime = Time.realtimeSinceStartup;
        }

        public void EnsureFramesLoaded()
        {
            bool hasAll = frames != null && frames.Length == 34;
            if (hasAll)
            {
                for (int i = 0; i < 34; i++)
                {
                    if (frames[i] == null) { hasAll = false; break; }
                }
            }

            if (!hasAll)
            {
                var loaded = new Sprite[34];
                int count = 0;
                for (int i = 0; i < 34; i++)
                {
                    string path = "Merchant/LadronaIdle/ladrona_" + (i < 10 ? ("0" + i) : i.ToString());
                    var sp = Resources.Load<Sprite>(path);
                    if (sp != null)
                    {
                        loaded[i] = sp;
                        count++;
                    }
                }
                if (count == 34)
                {
                    frames = loaded;
                }
            }

            if (visual != null && visual.sprite == null && frames != null && frames.Length > 0 && frames[0] != null)
            {
                visual.sprite = frames[0];
            }
        }

        void Update()
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (frames == null || frames.Length == 0 || frames[0] == null)
            {
                EnsureFramesLoaded();
                if (frames == null || frames.Length == 0 || frames[0] == null) return;
            }

            float dt;
            if (Application.isPlaying)
            {
                dt = Time.deltaTime;
            }
            else
            {
                float now = Time.realtimeSinceStartup;
                dt = now - lastRealtime;
                lastRealtime = now;
                if (dt < 0f || dt > 0.5f) dt = 0.0833f;
            }

            timer += dt;
            float frameDuration = fps > 0f ? (1f / fps) : 0.0833f;
            if (timer >= frameDuration)
            {
                int steps = (int)(timer / frameDuration);
                timer -= steps * frameDuration;
                currentFrame = (currentFrame + steps) % frames.Length;
                if (frames[currentFrame] != null)
                {
                    visual.sprite = frames[currentFrame];
                }
            }

            if (!visual.enabled) visual.enabled = true;
        }
    }
}
