using AlchemistMod.Content;
using AlchemistMod.Effects;
using AlchemistMod.Potions;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace AlchemistMod.SkillStates
{
    /// <summary>
    /// Utility: smash a fume vial at your feet and vault backwards out of it.
    /// The cloud slows enemies inside it and you get a short burst of cloaked speed.
    /// </summary>
    public class VapourVault : BaseSkillState
    {
        public static float duration = 0.35f;
        public static float backwardSpeed = 26f;
        public static float upwardSpeed = 14f;
        public static float cloakDuration = 1.5f;
        public static float cloudDamageCoefficient = 0.5f;

        public static GameObject FumesCloudPrefab;

        internal static void BuildAssets()
        {
            GameObject baseAcid = GameAssets.Load<GameObject>(GameAssets.AcridAcidPool);
            if (!baseAcid) return;

            FumesCloudPrefab = baseAcid.InstantiateClone("AlchemistFumesCloud", true);
            FumesCloudPrefab.transform.localScale *= 1.6f;
            PotionCatalog.Tint(FumesCloudPrefab, new Color(0.65f, 0.65f, 0.6f));

            var zone = FumesCloudPrefab.GetComponent<ProjectileDotZone>();
            if (zone)
            {
                zone.damageCoefficient = 1f;
                zone.fireFrequency = 2f;
                zone.resetFrequency = 2f;
                zone.lifetime = 5f;
                zone.overlapProcCoefficient = 0.1f;
            }

            var damage = FumesCloudPrefab.GetComponent<ProjectileDamage>();
            if (damage)
            {
                damage.damageType = DamageType.Generic;
                damage.damageType.AddModdedDamageType(Debuffs.Fumes);
            }

            AlchemistContentPack.ProjectilePrefabs.Add(FumesCloudPrefab);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            Util.PlaySound("Play_bandit2_shift_enter", gameObject);

            Vector3 origin = characterBody.footPosition;

            if (EntityStates.Bandit2.StealthMode.smokeBombEffectPrefab)
            {
                EffectManager.SpawnEffect(EntityStates.Bandit2.StealthMode.smokeBombEffectPrefab,
                    new EffectData { origin = origin, scale = 1f }, false);
            }

            if (isAuthority)
            {
                if (FumesCloudPrefab)
                {
                    ProjectileManager.instance.FireProjectile(FumesCloudPrefab, origin, Quaternion.identity,
                        gameObject, damageStat * cloudDamageCoefficient, 0f, false);
                }

                if (characterMotor)
                {
                    Vector3 forward = inputBank ? inputBank.aimDirection : transform.forward;
                    forward.y = 0f;
                    forward.Normalize();
                    characterMotor.Motor.ForceUnground();
                    characterMotor.velocity = -forward * backwardSpeed + Vector3.up * upwardSpeed;
                }

                characterBody.AddTimedBuffAuthority(RoR2Content.Buffs.CloakSpeed.buffIndex, cloakDuration);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= duration && isAuthority) outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill;
    }
}
