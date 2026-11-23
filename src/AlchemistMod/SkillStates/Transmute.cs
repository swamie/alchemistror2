using System.Collections.Generic;
using AlchemistMod.Content;
using AlchemistMod.Effects;
using EntityStates;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AlchemistMod.SkillStates
{
    /// <summary>
    /// Special: cash in. Every enemy within range has all of YOUR damage over time detonated at once,
    /// taking 150% of what was left to tick, plus a flat hit. The DoTs are used up.
    /// Rewards stacking as much as possible before pressing it.
    /// </summary>
    public class Transmute : BaseSkillState
    {
        public static float baseDuration = 0.6f;
        public static float detonateFraction = 0.4f;
        public static float radius = 25f;
        public static float remainingDamageMultiplier = 1.5f;
        public static float flatDamageCoefficient = 3f;

        private static GameObject explosionEffect;

        private float duration;
        private bool hasDetonated;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            PlayAnimation("Gesture, Additive", "SlashBlade", "SlashBlade.playbackRate", duration);
            Util.PlaySound("Play_croco_R_explode", gameObject);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!hasDetonated && fixedAge >= duration * detonateFraction)
            {
                hasDetonated = true;
                if (NetworkServer.active) DetonateServer();
            }
            if (fixedAge >= duration && isAuthority) outer.SetNextStateToMain();
        }

        private void DetonateServer()
        {
            if (!explosionEffect) explosionEffect = GameAssets.Load<GameObject>(GameAssets.ExplosionVfx);

            TeamIndex myTeam = GetTeam();
            Vector3 center = characterBody.corePosition;
            float radiusSqr = radius * radius;
            bool crit = RollCrit();

            var targets = new List<CharacterBody>();
            for (TeamIndex team = 0; team < TeamIndex.Count; team++)
            {
                if (team == myTeam) continue;
                foreach (TeamComponent member in TeamComponent.GetTeamMembers(team))
                {
                    CharacterBody body = member ? member.body : null;
                    if (!body || !body.healthComponent || !body.healthComponent.alive) continue;
                    if ((body.corePosition - center).sqrMagnitude > radiusSqr) continue;
                    targets.Add(body);
                }
            }

            foreach (CharacterBody victim in targets)
            {
                float remaining = ConsumeOwnDots(victim);
                float damage = damageStat * flatDamageCoefficient + remaining * remainingDamageMultiplier;

                var info = new DamageInfo
                {
                    attacker = gameObject,
                    inflictor = gameObject,
                    damage = damage,
                    crit = crit,
                    procCoefficient = 1f,
                    position = victim.corePosition,
                    damageColorIndex = DamageColorIndex.Poison,
                    damageType = DamageType.Generic,
                    force = Vector3.zero,
                    procChainMask = default,
                };
                info.AddModdedDamageType(Debuffs.Transmute);

                victim.healthComponent.TakeDamage(info);
                GlobalEventManager.instance.OnHitEnemy(info, victim.gameObject);
                GlobalEventManager.instance.OnHitAll(info, victim.gameObject);

                if (explosionEffect)
                {
                    EffectManager.SpawnEffect(explosionEffect,
                        new EffectData { origin = victim.corePosition, scale = Mathf.Max(2f, victim.radius * 1.5f) }, true);
                }
            }
        }

        /// <summary>Removes every DoT stack this Alchemist owns on the victim and returns the damage they still had to deal.</summary>
        private float ConsumeOwnDots(CharacterBody victim)
        {
            DotController dot = DotController.FindDotController(victim.gameObject);
            if (!dot || dot.dotStackList == null) return 0f;

            float total = 0f;
            for (int i = dot.dotStackList.Count - 1; i >= 0; i--)
            {
                DotController.DotStack stack = dot.dotStackList[i];
                if (stack == null || stack.attackerObject != gameObject || stack.dotDef == null) continue;
                float ticksLeft = stack.dotDef.interval > 0f ? stack.timer / stack.dotDef.interval : 0f;
                total += stack.damage * Mathf.Max(0f, ticksLeft);
                dot.RemoveDotStackAtServer(i);
            }
            return total;
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Frozen;
    }
}
