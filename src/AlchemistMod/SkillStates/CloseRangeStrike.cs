using AlchemistMod.Effects;
using EntityStates;
using R2API;
using RoR2;
using UnityEngine;

namespace AlchemistMod.SkillStates
{
    /// <summary>
    /// Shared base for the two secondaries: a very short range single-target hit in front of you.
    /// </summary>
    public abstract class CloseRangeStrike : BaseSkillState
    {
        protected abstract float BaseDuration { get; }
        protected abstract float DamageCoefficient { get; }
        protected abstract float Range { get; }
        protected abstract string SoundString { get; }
        protected virtual float HitFraction => 0.35f;
        protected virtual float Force => 300f;

        protected abstract void ConfigureAttack(BulletAttack attack);

        private float duration;
        private bool hasHit;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = BaseDuration / attackSpeedStat;
            StartAimMode(duration + 0.5f);
            PlayAnimation("Gesture, Additive", "SlashBlade", "SlashBlade.playbackRate", duration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!hasHit && fixedAge >= duration * HitFraction) Strike();
            if (fixedAge >= duration && isAuthority) outer.SetNextStateToMain();
        }

        private void Strike()
        {
            hasHit = true;
            Util.PlaySound(SoundString, gameObject);
            if (!isAuthority) return;

            Ray aimRay = GetAimRay();
            var attack = new BulletAttack
            {
                owner = gameObject,
                weapon = gameObject,
                origin = aimRay.origin,
                aimVector = aimRay.direction,
                minSpread = 0f,
                maxSpread = 0f,
                bulletCount = 1,
                damage = damageStat * DamageCoefficient,
                damageColorIndex = DamageColorIndex.Default,
                damageType = DamageType.Generic,
                falloffModel = BulletAttack.FalloffModel.None,
                force = Force,
                hitMask = LayerIndex.CommonMasks.bullet,
                isCrit = RollCrit(),
                maxDistance = Range,
                procCoefficient = 1f,
                radius = 1.4f,
                smartCollision = true,
                hitEffectPrefab = null,
                tracerEffectPrefab = null,
            };
            ConfigureAttack(attack);
            attack.Fire();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }

    /// <summary>
    /// Secondary A. Close range slash that always inflicts Laceration.
    /// Hitting a lacerated enemy with any potion infects the wound (see DebuffHooks).
    /// </summary>
    public class CutthroatKnife : CloseRangeStrike
    {
        protected override float BaseDuration => 0.45f;
        protected override float DamageCoefficient => 3f;
        protected override float Range => 5.5f;
        protected override string SoundString => "Play_bandit2_m2_slash";

        protected override void ConfigureAttack(BulletAttack attack)
        {
            attack.damageColorIndex = DamageColorIndex.Bleed;
            attack.AddModdedDamageType(Debuffs.KnifeLacerate);
        }
    }

    /// <summary>
    /// Secondary B. A dirty elbow that stuns and confuses, turning the target on its own allies briefly.
    /// </summary>
    public class ThrowElbow : CloseRangeStrike
    {
        protected override float BaseDuration => 0.5f;
        protected override float DamageCoefficient => 2.2f;
        protected override float Range => 4.5f;
        protected override string SoundString => "Play_loader_m1_impact";
        protected override float Force => 1200f;

        protected override void ConfigureAttack(BulletAttack attack)
        {
            attack.damageType = DamageType.Stun1s;
            attack.AddModdedDamageType(Debuffs.ElbowConfuse);
        }
    }
}
