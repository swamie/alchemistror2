using System.Collections.Generic;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AlchemistMod.Effects
{
    /// <summary>
    /// Server-side reactions to the Alchemist's hits: applying debuffs, infecting wounds,
    /// Catalyst bonus damage, Sanguine lifesteal and Miasma spreading on death.
    /// </summary>
    internal static class DebuffHooks
    {
        public const float LacerationDuration = 5f;
        public const float InfectionDuration = 6f;
        public const float InfectionBonusPerLaceration = 0.35f;
        public const float PlagueStackDuration = 3f;
        public const float ConfuseDuration = 3f;
        public const float MiasmaSpreadRadius = 15f;

        public static void Init()
        {
            GlobalEventManager.onServerDamageDealt += OnServerDamageDealt;
            GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
            RecalculateStatsAPI.GetStatCoefficients += OnGetStatCoefficients;
        }

        private static void OnServerDamageDealt(DamageReport report)
        {
            if (!NetworkServer.active || report == null) return;
            DamageInfo info = report.damageInfo;
            CharacterBody victim = report.victimBody;
            CharacterBody attacker = report.attackerBody;
            if (info == null || !victim) return;

            // Sanguine: damage over time on a Bloodlet target heals whoever dealt it.
            if (info.dotIndex != DotController.DotIndex.None && attacker && attacker.healthComponent && victim.HasBuff(Debuffs.Bloodlet))
            {
                attacker.healthComponent.Heal(report.damageDealt * Debuffs.BloodletHealFraction, default);
            }

            if (!report.victim || !report.victim.alive) return;
            GameObject attackerObject = info.attacker;

            if (info.HasModdedDamageType(Debuffs.KnifeLacerate))
            {
                Inflict(victim, attackerObject, Debuffs.LacerationDot, LacerationDuration);
            }

            if (info.HasModdedDamageType(Debuffs.PotionImpact))
            {
                InfectWound(victim, attackerObject);
            }

            if (info.HasModdedDamageType(Debuffs.PlagueTick))
            {
                Inflict(victim, attackerObject, Debuffs.PlagueDot, PlagueStackDuration);
            }

            if (info.HasModdedDamageType(Debuffs.Hellfire))
            {
                var dot = new InflictDotInfo
                {
                    victimObject = victim.gameObject,
                    attackerObject = attackerObject,
                    dotIndex = DotController.DotIndex.Burn,
                    duration = 4f,
                    damageMultiplier = 1f,
                    totalDamage = info.damage * 0.5f,
                };
                DotController.InflictDot(ref dot);
            }

            if (info.HasModdedDamageType(Debuffs.Corrosive))
            {
                victim.AddTimedBuff(Debuffs.Corroded, 6f, Debuffs.CorrodedMaxStacks);
            }

            if (info.HasModdedDamageType(Debuffs.Frostbite))
            {
                victim.AddTimedBuff(Debuffs.Chilled, 4f, Debuffs.ChilledFreezeStacks);
                if (victim.GetBuffCount(Debuffs.Chilled) >= Debuffs.ChilledFreezeStacks)
                {
                    var stateOnHurt = victim.GetComponent<SetStateOnHurt>();
                    if (stateOnHurt && stateOnHurt.canBeFrozen)
                    {
                        stateOnHurt.SetFrozen(2f);
                        victim.ClearTimedBuffs(Debuffs.Chilled);
                    }
                }
            }

            if (info.HasModdedDamageType(Debuffs.Hemlock))
            {
                victim.AddTimedBuff(Debuffs.Enfeebled, 6f);
            }

            if (info.HasModdedDamageType(Debuffs.Catalyst))
            {
                CatalystBonus(report, victim, attackerObject, extendDots: !info.HasModdedDamageType(Debuffs.GasTick));
            }

            if (info.HasModdedDamageType(Debuffs.Miasma))
            {
                victim.AddTimedBuff(Debuffs.Contagious, 8f);
            }

            if (info.HasModdedDamageType(Debuffs.Sanguine))
            {
                victim.AddTimedBuff(Debuffs.Bloodlet, 8f);
            }

            if (info.HasModdedDamageType(Debuffs.Fumes))
            {
                victim.AddTimedBuff(Debuffs.Gassed, 1.5f);
            }

            if (info.HasModdedDamageType(Debuffs.ElbowConfuse))
            {
                Confusion.Apply(victim, ConfuseDuration);
            }

            if (info.HasModdedDamageType(Debuffs.CrowPeck))
            {
                Items.ClockworkCrow.OnPeck(victim, attackerObject);
            }
        }

        internal static void Inflict(CharacterBody victim, GameObject attacker, DotController.DotIndex index, float duration, float multiplier = 1f)
        {
            var dot = new InflictDotInfo
            {
                victimObject = victim.gameObject,
                attackerObject = attacker,
                dotIndex = index,
                duration = duration,
                damageMultiplier = multiplier,
            };
            DotController.InflictDot(ref dot);
        }

        /// <summary>
        /// A potion hitting a lacerated enemy gets "inside the wound": all Laceration is
        /// converted into a much stronger Infected Wound DoT that scales with how many cuts there were.
        /// </summary>
        private static void InfectWound(CharacterBody victim, GameObject attacker)
        {
            int cuts = victim.GetBuffCount(Debuffs.Lacerated);
            if (cuts <= 0) return;

            DotController dot = DotController.FindDotController(victim.gameObject);
            if (dot && dot.dotStackList != null)
            {
                for (int i = dot.dotStackList.Count - 1; i >= 0; i--)
                {
                    if (dot.dotStackList[i] != null && dot.dotStackList[i].dotIndex == Debuffs.LacerationDot)
                        dot.RemoveDotStackAtServer(i);
                }
            }

            Inflict(victim, attacker, Debuffs.InfectionDot, InfectionDuration, 1f + InfectionBonusPerLaceration * (cuts - 1));
            Util.PlaySound("Play_acrid_R_infect", victim.gameObject);
        }

        private static void CatalystBonus(DamageReport report, CharacterBody victim, GameObject attacker, bool extendDots)
        {
            int debuffs = Debuffs.CountUniqueDebuffs(victim);

            DotController dot = DotController.FindDotController(victim.gameObject);
            if (extendDots && dot && dot.dotStackList != null)
            {
                foreach (DotController.DotStack stack in dot.dotStackList)
                {
                    if (stack != null && stack.attackerObject == attacker) stack.timer += 2f;
                }
            }

            if (debuffs <= 0) return;
            var bonus = new DamageInfo
            {
                attacker = attacker,
                inflictor = attacker,
                damage = report.damageInfo.damage * 0.25f * debuffs,
                crit = report.damageInfo.crit,
                procCoefficient = 0f,
                position = victim.corePosition,
                damageColorIndex = DamageColorIndex.WeakPoint,
                damageType = DamageType.Generic,
                procChainMask = report.damageInfo.procChainMask,
            };
            victim.healthComponent.TakeDamage(bonus);
        }

        /// <summary>Miasma Spores: a contagious enemy passes its DoTs and debuffs to nearby allies when it dies.</summary>
        private static void OnCharacterDeath(DamageReport report)
        {
            if (!NetworkServer.active || report == null) return;
            CharacterBody victim = report.victimBody;
            if (!victim || !victim.HasBuff(Debuffs.Contagious)) return;

            var stacks = new List<DotController.DotStack>();
            DotController dot = DotController.FindDotController(victim.gameObject);
            if (dot && dot.dotStackList != null) stacks.AddRange(dot.dotStackList);

            int corroded = victim.GetBuffCount(Debuffs.Corroded);
            int chilled = victim.GetBuffCount(Debuffs.Chilled);
            bool enfeebled = victim.HasBuff(Debuffs.Enfeebled);
            bool bloodlet = victim.HasBuff(Debuffs.Bloodlet);

            TeamIndex team = victim.teamComponent ? victim.teamComponent.teamIndex : TeamIndex.Monster;
            Vector3 center = victim.corePosition;
            float radiusSqr = MiasmaSpreadRadius * MiasmaSpreadRadius;

            var spreadTo = new List<CharacterBody>();
            foreach (TeamComponent member in TeamComponent.GetTeamMembers(team))
            {
                CharacterBody other = member ? member.body : null;
                if (!other || other == victim || !other.healthComponent || !other.healthComponent.alive) continue;
                if ((other.corePosition - center).sqrMagnitude <= radiusSqr) spreadTo.Add(other);
            }

            foreach (CharacterBody other in spreadTo)
            {
                foreach (DotController.DotStack stack in stacks)
                {
                    if (stack == null || stack.dotDef == null || !stack.attackerObject) continue;
                    float duration = Mathf.Max(1f, stack.timer);
                    var info = new InflictDotInfo
                    {
                        victimObject = other.gameObject,
                        attackerObject = stack.attackerObject,
                        dotIndex = stack.dotIndex,
                        duration = duration,
                        damageMultiplier = 1f,
                    };
                    if (stack.dotIndex == DotController.DotIndex.Burn || stack.dotIndex == DotController.DotIndex.StrongerBurn)
                        info.totalDamage = stack.damage * (duration / Mathf.Max(0.01f, stack.dotDef.interval));
                    DotController.InflictDot(ref info);
                }

                for (int i = 0; i < corroded; i++) other.AddTimedBuff(Debuffs.Corroded, 5f, Debuffs.CorrodedMaxStacks);
                for (int i = 0; i < chilled; i++) other.AddTimedBuff(Debuffs.Chilled, 4f, Debuffs.ChilledFreezeStacks);
                if (enfeebled) other.AddTimedBuff(Debuffs.Enfeebled, 5f);
                if (bloodlet) other.AddTimedBuff(Debuffs.Bloodlet, 5f);
            }

            if (spreadTo.Count > 0) Util.PlaySound("Play_acrid_m2_bite_hit", victim.gameObject);
        }

        private static void OnGetStatCoefficients(CharacterBody body, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (!body) return;

            int corroded = body.GetBuffCount(Debuffs.Corroded);
            if (corroded > 0) args.armorAdd -= Debuffs.CorrodedArmorPerStack * corroded;

            int chilled = body.GetBuffCount(Debuffs.Chilled);
            if (chilled > 0) args.moveSpeedReductionMultAdd += Debuffs.ChilledSlowPerStack * chilled;

            if (body.HasBuff(Debuffs.Enfeebled))
            {
                args.damageMultAdd -= Debuffs.EnfeebledDamagePenalty;
                args.attackSpeedReductionMultAdd += Debuffs.EnfeebledAttackSpeedPenalty;
            }

            if (body.HasBuff(Debuffs.Gassed)) args.moveSpeedReductionMultAdd += Debuffs.GassedSlow;
        }
    }
}
