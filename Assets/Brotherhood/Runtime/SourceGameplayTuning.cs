using UnityEngine;
namespace Brotherhood
{
    // Values transcribed from ExportedProject/Assets/Resources/core/Penitent.prefab.
    public static class SourceGameplayTuning
    {
        public const float DashSpeed=13f;
        public const float DashDrag=2f;
        public const float DashRide=0.35f;
        public const float DashCooldown=1f;
        public const float DashCompletionBeforeRepeat=0.25f;
        public const float ExecutionStunTime=5f;
        public const float ParryWindow=0.35f;
        public const float AcolyteAttackHitTime=0.69f;
        public const float FlagellantAttackHitTime=0.52f;
        public const float FlagellantHeavyAttackHitTime=0.73f;
        public const float AcolyteParryReactionTime=1.0f;
        public const float FlagellantParryReactionTime=0.52f;
        // Animation events in penitent_dodge_anim.anim.
        public const float DashStopDustEventTime=0.54f;
        public const float DashVulnerableEventTime=0.72f;
        public const float ChargedTier1Time=1.25f;
        public const float ChargedTier2Time=0.75f;
        public const float ChargedHitEventTime=0.36f;
        public const float ChargedAreaWidth=3.5f;
        public const float LungeDuration=0.2f;
        public const float LungeDrag=2f;
        public static float LungeSpeed(int tier){return tier>=3?14f:tier==2?12f:10f;}
        public static float LungeDamage(int tier){return tier>=3?72f:tier==2?63f:54f;}
        public static readonly Vector2 DashCollisionCenter=new Vector2(0,.36f);
        public static readonly Vector2 DashCollisionSize=new Vector2(.5f,.6f);
    }
}
