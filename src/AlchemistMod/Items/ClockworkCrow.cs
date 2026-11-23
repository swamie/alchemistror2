using System.Collections.Generic;
using AlchemistMod.Content;
using AlchemistMod.Effects;
using AlchemistMod.Potions;
using AlchemistMod.Survivor;
using R2API;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace AlchemistMod.Items
{
    /// <summary>
    /// Clockwork Crow: red-tier item exclusive to the Alchemist. She starts every run with it.
    /// It cannot be scrapped, printed, rerolled, stolen, copied or dropped by anything.
    ///
    /// The crow orbits her shoulder and every few seconds dives at the most-afflicted enemy
    /// nearby: a peck that adds one more stack of every DoT already on it and extends its debuffs.
    /// </summary>
    internal static class ClockworkCrow
    {
        public static ItemDef ItemDef;
        public static GameObject CrowModelPrefab;

        public const float BaseInterval = 3.5f;
        public const float Range = 45f;
        public const float PeckDamageCoefficient = 1f;
        public const float DebuffExtension = 1f;

        public static void Init()
        {
            ItemDef = ScriptableObject.CreateInstance<ItemDef>();
            ItemDef.name = "AlchemistClockworkCrow";
            ItemDef.nameToken = AlchemistPlugin.Prefix + "ITEM_CROW_NAME";
            ItemDef.pickupToken = AlchemistPlugin.Prefix + "ITEM_CROW_PICKUP";
            ItemDef.descriptionToken = AlchemistPlugin.Prefix + "ITEM_CROW_DESC";
            ItemDef.loreToken = AlchemistPlugin.Prefix + "ITEM_CROW_LORE";
#pragma warning disable CS0618
            ItemDef.deprecatedTier = ItemTier.Tier3;
#pragma warning restore CS0618
            ItemDef.canRemove = false;     // scrappers, printers, cleansing pools and Bazaar can't take it
            ItemDef.hidden = false;
            ItemDef.tags = new[]
            {
                ItemTag.WorldUnique,       // never in chests or any drop table
                ItemTag.CannotSteal,       // Void Fields / Mithrix item steal
                ItemTag.CannotCopy,        // Engi turrets, Happiest Mask ghosts etc.
                ItemTag.CannotDuplicate,   // 3D printers / duplicators
                ItemTag.AIBlacklist,       // Evolution / Vengeance can't hand it to monsters
                ItemTag.BrotherBlacklist,
                ItemTag.Utility,
            };

            // Placeholder look: Wax Quail's bird, painted gunmetal.
            GameObject quail = GameAssets.Load<GameObject>(GameAssets.WaxQuailPickup);
            if (quail)
            {
                CrowModelPrefab = quail.InstantiateClone("AlchemistClockworkCrowModel", false);
                PotionCatalog.Tint(CrowModelPrefab, new Color(0.35f, 0.37f, 0.42f));
                ItemDef.pickupModelPrefab = CrowModelPrefab;
            }

            ItemDef.pickupIconSprite = Icons.Get("item_crow");
            AlchemistContentPack.ItemDefs.Add(ItemDef);
            RoR2Application.onLoad += () =>
            {
                if (!ItemDef.pickupIconSprite && RoR2Content.Items.JumpBoost)
                    ItemDef.pickupIconSprite = RoR2Content.Items.JumpBoost.pickupIconSprite;
            };
        }

        public static void InitHooks()
        {
            // Give the crow to the Alchemist when her body first spawns in a run.
            CharacterBody.onBodyStartGlobal += body =>
            {
                if (!NetworkServer.active || !body || !body.inventory) return;
                if (body.bodyIndex != AlchemistSurvivor.BodyIndex) return;
                if (body.inventory.GetItemCount(ItemDef) > 0) return;
                body.inventory.GiveItem(ItemDef);
            };

            CharacterBody.onBodyInventoryChangedGlobal += body =>
            {
                if (!body || !body.inventory) return;
                body.AddItemBehavior<CrowBehavior>(body.inventory.GetItemCount(ItemDef));
            };
        }

        /// <summary>Server: called from DebuffHooks when a crow peck lands.</summary>
        public static void OnPeck(CharacterBody victim, GameObject owner)
        {
            DotController dot = DotController.FindDotController(victim.gameObject);
            if (dot && dot.dotStackList != null && dot.dotStackList.Count > 0)
            {
                var seen = new HashSet<DotController.DotIndex>();
                var copies = new List<DotController.DotStack>(dot.dotStackList);
                foreach (DotController.DotStack stack in copies)
                {
                    if (stack == null || !seen.Add(stack.dotIndex)) continue;
                    // Burn types need a damage total, everything else scales off the owner.
                    if (stack.dotIndex == DotController.DotIndex.Burn || stack.dotIndex == DotController.DotIndex.StrongerBurn)
                        continue;
                    DebuffHooks.Inflict(victim, owner, stack.dotIndex, Mathf.Max(2f, stack.timer));
                }
                foreach (DotController.DotStack stack in dot.dotStackList)
                {
                    if (stack != null) stack.timer += DebuffExtension;
                }
            }

            if (victim.timedBuffs != null)
            {
                foreach (CharacterBody.TimedBuff buff in victim.timedBuffs)
                {
                    BuffDef def = BuffCatalog.GetBuffDef(buff.buffIndex);
                    if (def && def.isDebuff) buff.timer += DebuffExtension;
                }
            }
        }
    }

    /// <summary>Runs on every copy of a body holding the crow. Visual everywhere, attacks only on the server.</summary>
    internal class CrowBehavior : CharacterBody.ItemBehavior
    {
        private GameObject crowVisual;
        private float peckTimer;
        private float orbitAngle;

        private void Start()
        {
            peckTimer = 1f;
            if (ClockworkCrow.CrowModelPrefab && body)
            {
                crowVisual = Instantiate(ClockworkCrow.CrowModelPrefab);
                crowVisual.transform.localScale = Vector3.one * 0.6f;
                foreach (Collider c in crowVisual.GetComponentsInChildren<Collider>()) Destroy(c);
            }
        }

        private void Update()
        {
            if (!crowVisual || !body) return;
            orbitAngle += Time.deltaTime * 90f;
            Vector3 offset = Quaternion.Euler(0f, orbitAngle, 0f) * new Vector3(1.2f, 0f, 0f);
            Vector3 anchor = body.corePosition + Vector3.up * (body.radius + 0.8f);
            crowVisual.transform.position = Vector3.Lerp(crowVisual.transform.position, anchor + offset, Time.deltaTime * 8f);
            crowVisual.transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0f, 90f, 0f) * offset.normalized);
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || !body || !body.healthComponent || !body.healthComponent.alive) return;

            peckTimer -= Time.fixedDeltaTime;
            if (peckTimer > 0f) return;

            // Extra crows (if another mod ever hands one out) make it peck faster.
            float interval = ClockworkCrow.BaseInterval / (1f + 0.25f * Mathf.Max(0, stack - 1));
            peckTimer = interval;

            CharacterBody target = FindTarget();
            if (!target) return;

            var orb = new LightningOrb
            {
                origin = crowVisual ? crowVisual.transform.position : body.corePosition + Vector3.up,
                attacker = body.gameObject,
                inflictor = body.gameObject,
                teamIndex = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.Player,
                damageValue = body.damage * ClockworkCrow.PeckDamageCoefficient,
                isCrit = body.RollCrit(),
                procCoefficient = 0.5f,
                bouncesRemaining = 0,
                bouncedObjects = new List<HealthComponent>(),
                lightningType = LightningOrb.LightningType.TreePoisonDart,
                damageColorIndex = DamageColorIndex.Poison,
                range = ClockworkCrow.Range,
                target = target.mainHurtBox,
                damageType = DamageType.Generic,
            };
            orb.AddModdedDamageType(Debuffs.CrowPeck);
            OrbManager.instance.AddOrb(orb);
            Util.PlaySound("Play_treebot_m1_impact", body.gameObject);
        }

        /// <summary>The enemy carrying the most debuffs in range. Falls back to the nearest one.</summary>
        private CharacterBody FindTarget()
        {
            TeamIndex myTeam = body.teamComponent ? body.teamComponent.teamIndex : TeamIndex.Player;
            CharacterBody best = null;
            int bestDebuffs = -1;
            float bestDist = float.MaxValue;
            float rangeSqr = ClockworkCrow.Range * ClockworkCrow.Range;

            for (TeamIndex team = 0; team < TeamIndex.Count; team++)
            {
                if (team == myTeam || team == TeamIndex.Neutral) continue;
                foreach (TeamComponent member in TeamComponent.GetTeamMembers(team))
                {
                    CharacterBody other = member ? member.body : null;
                    if (!other || !other.mainHurtBox || !other.healthComponent || !other.healthComponent.alive) continue;
                    float dist = (other.corePosition - body.corePosition).sqrMagnitude;
                    if (dist > rangeSqr) continue;
                    int debuffs = Debuffs.CountUniqueDebuffs(other);
                    if (debuffs > bestDebuffs || (debuffs == bestDebuffs && dist < bestDist))
                    {
                        best = other;
                        bestDebuffs = debuffs;
                        bestDist = dist;
                    }
                }
            }
            return best;
        }

        private void OnDestroy()
        {
            if (crowVisual) Destroy(crowVisual);
        }
    }
}
