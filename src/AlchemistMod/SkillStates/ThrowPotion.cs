using AlchemistMod.Potions;
using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace AlchemistMod.SkillStates
{
    /// <summary>
    /// Primary: lob the active potion. Uses Commando's grenade arc and speed, shatters on impact.
    /// </summary>
    public class ThrowPotion : BaseSkillState
    {
        public static float baseDuration = 0.5f;
        public static float fireFraction = 0.3f;
        /// <summary>Shared between all potions, so swapping can't skip it.</summary>
        public static float sharedCooldown = 0.75f;

        private float duration;
        private bool hasFired;
        private PotionDef potion;
        private PotionSelector selector;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;

            selector = GetComponent<PotionSelector>();
            potion = selector ? selector.CurrentPotion : null;
            if (potion == null && activatorSkillSlot) potion = PotionCatalog.FromSkillDef(activatorSkillSlot.skillDef);
            if (potion == null && PotionCatalog.Potions.Count > 0) potion = PotionCatalog.Potions[0];

            StartAimMode(duration + 1f);
            PlayAnimation("Gesture, Additive", "SlashBlade", "SlashBlade.playbackRate", duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!hasFired && fixedAge >= duration * fireFraction) Fire();

            if (fixedAge >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        private void Fire()
        {
            hasFired = true;
            Util.PlaySound("Play_commando_M2_grenade_throw", gameObject);
            if (selector) selector.StartThrowCooldown(sharedCooldown / attackSpeedStat);

            if (!isAuthority || potion == null || !potion.projectilePrefab) return;

            Ray aimRay = GetAimRay();
            // A small upward tilt so the lob feels like a throw, not a shot.
            Vector3 direction = Vector3.RotateTowards(aimRay.direction, Vector3.up, 4f * Mathf.Deg2Rad, 0f);

            ProjectileManager.instance.FireProjectile(
                potion.projectilePrefab,
                aimRay.origin,
                Util.QuaternionSafeLookRotation(direction),
                gameObject,
                damageStat * potion.damageCoefficient,
                0f,
                RollCrit());
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            // Other skills can cancel the throw animation; another potion has to wait for it to end.
            return InterruptPriority.Skill;
        }
    }
}
