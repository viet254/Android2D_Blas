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
        public const float WalkSpeed=5f;
        public const float JumpSpeed=10f;
        public const float ParryWindow=0.3f;
        public const float AcolyteAttackHitTime=0.69f;
        public const float FlagellantAttackHitTime=0.52f;
        public const float FlagellantSecondAttackHitTime=1.1f;
        public const float FlagellantAttackDamage=10f;
        // NewFlagellantHurtState.hurtRecoveryTime in the original controller.
        public const float FlagellantHurtRecoveryTime=1.2f;
        public const float FlagellantHeavyAttackHitTime=0.73f;
        public const float AcolyteParryReactionTime=1.0f;
        public const float FlagellantParryReactionTime=0.52f;
        // Animation events in penitent_dodge_anim.anim.
        public const float DashStopDustEventTime=0.54f;
        public const float DashVulnerableEventTime=0.72f;
        public const float ChargedTier1Time=1.25f;
        public const float ChargedTier2Time=0.75f;
        // PlatformCharacterInput.timeInputAttackHold in the original PC source.
        public const float AttackHoldThreshold=0.5f;
        // Rewired's Default input behavior and FervourPenance on the mobile prefab.
        public const float PenanceHoldThreshold=0.25f;
        public const float PenanceLifeCost=15f;
        public const float PenanceFervourRestored=25f;
        // RangeAttack component, including its HE01 reference, on Penitent.prefab.
        public const float RangedFervourCost=7f;
        public const float RangedHeartCostMultiplier=.75f;
        public const float VerticalHoldThreshold=.3f;
        public const float VerticalMinimumHeight=1f;
        public const float VerticalBeamHeight=3f;
        public const float VerticalDamageFactor=3f;
        public static float PenanceTearsCost(int meaCulpaLevel){return 25f+25f*meaCulpaLevel;}
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
