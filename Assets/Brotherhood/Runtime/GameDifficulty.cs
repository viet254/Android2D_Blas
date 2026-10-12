using UnityEngine;

namespace Brotherhood
{
    /// <summary>
    /// Difficulty chosen once when a new pilgrimage (save slot) is created.
    /// The value is persisted in <see cref="PlayerProgress.difficulty"/>, so each
    /// save slot keeps its own difficulty. Older saves without the field load as Normal.
    /// </summary>
    public static class GameDifficulty
    {
        public const int Easy = 0, Normal = 1, Hard = 2;

        /// <summary>PlayerPrefs hand-off from the main menu to the gameplay scene.</summary>
        public const string PendingKey = "BrotherhoodNewDifficulty";

        public static int Clamp(int value) => value < Easy ? Easy : value > Hard ? Hard : value;

        public static string Name(int value)
        {
            switch (Clamp(value))
            {
                case Easy: return "EASY";
                case Hard: return "HARD";
                default: return "NORMAL";
            }
        }

        /// <summary>Multiplier applied to every hit the Penitent One receives.</summary>
        public static float PlayerDamageTaken(int value)
        {
            switch (Clamp(value))
            {
                case Easy: return .6f;
                case Hard: return 1.5f;
                default: return 1f;
            }
        }

        /// <summary>Multiplier applied to every hit an enemy or boss receives.</summary>
        public static float EnemyDamageTaken(int value)
        {
            switch (Clamp(value))
            {
                case Easy: return 1.3f;
                case Hard: return .8f;
                default: return 1f;
            }
        }
    }

    /// <summary>
    /// Short, rate-limited vibration. Handheld.Vibrate() on Android lasts ~0.5 s
    /// and cannot be shortened, which made every button press buzz the phone.
    /// This uses the Android Vibrator service with an explicit duration instead.
    /// </summary>
    public static class Haptics
    {
        const string PreferenceKey = "BrotherhoodHaptics";
        static float nextAllowed;
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static int sdk = -1;
        static bool unavailable;
#endif

        public static bool Enabled => PlayerPrefs.GetInt(PreferenceKey, 1) == 1;

        public static void Pulse(long milliseconds = 40, float minInterval = .2f)
        {
            if (!Enabled || Application.platform != RuntimePlatform.Android) return;
            if (Time.unscaledTime < nextAllowed) return;
            nextAllowed = Time.unscaledTime + minInterval;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (unavailable) return;
            try
            {
                if (vibrator == null)
                {
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        sdk = version.GetStatic<int>("SDK_INT");
                }
                if (vibrator == null) { unavailable = true; return; }
                if (sdk >= 26)
                {
                    using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, -1))
                        vibrator.Call("vibrate", effect);
                }
                else vibrator.Call("vibrate", milliseconds);
            }
            catch (System.Exception error)
            {
                unavailable = true;
                Debug.LogWarning("Short haptic pulse unavailable: " + error.Message);
                // Reference keeps the VIBRATE permission in the generated manifest.
                Handheld.Vibrate();
            }
#endif
        }
    }
}
