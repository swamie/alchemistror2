using System.Collections.Generic;
using AlchemistMod.Content;
using AlchemistMod.Effects;
using AlchemistMod.SkillStates;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.DamageAPI;

namespace AlchemistMod.Potions
{
    /// <summary>One of the nine throwable potions.</summary>
    internal class PotionDef
    {
        public int index;
        public string key;               // token key, e.g. "PLAGUE"
        public string name;
        public string description;       // skill description shown in loadout / HUD tooltip
        public Color color;
        public float damageCoefficient;
        public float blastRadius;
        public ModdedDamageType? effect;
        public bool spawnsPool;

        public GameObject projectilePrefab;
        public SkillDef skillDef;

        public string NameToken => AlchemistPlugin.Prefix + "POTION_" + key + "_NAME";
        public string DescToken => AlchemistPlugin.Prefix + "POTION_" + key + "_DESC";
    }

    internal static class PotionCatalog
    {
        public static readonly List<PotionDef> Potions = new List<PotionDef>();
        private static readonly Dictionary<SkillDef, PotionDef> bySkill = new Dictionary<SkillDef, PotionDef>();

        public static GameObject PlaguePoolPrefab;

        // Impact damage requested: 180-220% base. Everything sits at 200% except the pure-burst potion.
        private const float ImpactDamage = 2f;
        private const float ImpactRadius = 6f;

        public static void Init()
        {
            Add("PLAGUE", "Plague Flask",
                "Shatters into a <style=cIsHealing>lingering plague pool</style> for 6s. Each tick inside adds a stack of <style=cIsDamage>Plague</style>, so the longer enemies stand in it, the harder it hurts.",
                new Color(0.4f, 0.75f, 0.2f), ImpactDamage, ImpactRadius, Debuffs.PlagueTick, spawnsPool: true);

            Add("HELLFIRE", "Hellfire Tonic",
                "<style=cIsDamage>Ignites</style> everything in the blast, burning for a portion of the hit.",
                new Color(1f, 0.45f, 0.1f), ImpactDamage, ImpactRadius, Debuffs.Hellfire);

            Add("VITRIOL", "Corrosive Vitriol",
                "Applies <style=cIsUtility>Corroded</style>: <style=cIsDamage>-6 armor</style> per stack, up to 10 stacks.",
                new Color(0.9f, 0.75f, 0.1f), ImpactDamage, ImpactRadius, Debuffs.Corrosive);

            Add("FROST", "Frostbite Draught",
                "Applies <style=cIsUtility>Chilled</style>, slowing by 12% per stack. At 5 stacks the target <style=cIsUtility>freezes</style>.",
                new Color(0.55f, 0.85f, 1f), ImpactDamage, ImpactRadius, Debuffs.Frostbite);

            Add("HEMLOCK", "Hemlock Philter",
                "Applies <style=cIsUtility>Enfeebled</style> for 6s: enemies deal <style=cIsHealth>30% less damage</style> and attack 25% slower.",
                new Color(0.55f, 0.3f, 0.7f), ImpactDamage, ImpactRadius, Debuffs.Hemlock);

            Add("CATALYST", "Catalyst Elixir",
                "Deals <style=cIsDamage>+50% damage per unique debuff</style> on the target and <style=cIsUtility>extends</style> all of its damage over time by 2s.",
                new Color(0.95f, 0.95f, 0.85f), ImpactDamage, ImpactRadius, Debuffs.Catalyst);

            Add("MIASMA", "Miasma Spores",
                "Applies <style=cIsUtility>Contagious</style> for 8s. If a contagious enemy dies, its <style=cIsDamage>debuffs and damage over time spread</style> to enemies within 15m.",
                new Color(0.55f, 0.6f, 0.25f), ImpactDamage, ImpactRadius, Debuffs.Miasma);

            Add("SANGUINE", "Sanguine Draught",
                "Applies <style=cIsUtility>Bloodlet</style> for 8s. Damage over time dealt to it <style=cIsHealing>heals you for 15%</style> of the damage.",
                new Color(0.65f, 0.05f, 0.15f), ImpactDamage, ImpactRadius, Debuffs.Sanguine);

            Add("VOLATILE", "Volatile Concoction",
                "No debuff. A big, unstable blast for <style=cIsDamage>320% damage</style> in a wider radius.",
                new Color(1f, 0.3f, 0.6f), 3.2f, 9f, null);

            BuildPlaguePool();
        }

        public static PotionDef FromSkillDef(SkillDef skillDef)
        {
            return skillDef != null && bySkill.TryGetValue(skillDef, out var def) ? def : null;
        }

        private static void Add(string key, string name, string description, Color color,
            float damageCoefficient, float blastRadius, ModdedDamageType? effect, bool spawnsPool = false)
        {
            var def = new PotionDef
            {
                index = Potions.Count,
                key = key,
                name = name,
                description = description,
                color = color,
                damageCoefficient = damageCoefficient,
                blastRadius = blastRadius,
                effect = effect,
                spawnsPool = spawnsPool,
            };

            def.projectilePrefab = BuildProjectile(def);
            def.skillDef = BuildSkillDef(def);

            Potions.Add(def);
            bySkill[def.skillDef] = def;
        }

        private static GameObject BuildProjectile(PotionDef def)
        {
            GameObject baseGrenade = GameAssets.Load<GameObject>(GameAssets.CommandoGrenade);
            if (!baseGrenade) return null;

            GameObject prefab = baseGrenade.InstantiateClone("AlchemistPotion" + def.key, true);

            // Commando's grenade bounces and waits on a fuse. Potions shatter on the first thing they touch.
            var impact = prefab.GetComponent<ProjectileImpactExplosion>();
            if (impact)
            {
                impact.destroyOnEnemy = true;
                impact.destroyOnWorld = true;
                impact.impactOnWorld = true;
                impact.timerAfterImpact = false;
                impact.lifetime = 6f;
                impact.explodeOnLifeTimeExpiration = true;
                impact.blastRadius = def.blastRadius;
                impact.blastDamageCoefficient = 1f;
                impact.blastProcCoefficient = 1f;
                impact.falloffModel = BlastAttack.FalloffModel.None;
                impact.fireChildren = false;
            }

            var damage = prefab.GetComponent<ProjectileDamage>();
            if (damage)
            {
                damage.damageType = DamageType.Generic;
                damage.damageType.AddModdedDamageType(Debuffs.PotionImpact);
                if (def.effect.HasValue)
                    damage.damageType.AddModdedDamageType(def.effect.Value);
            }

            if (def.spawnsPool)
                prefab.AddComponent<PotionPoolSpawner>();

            // Recolour the flying model so each potion is readable mid-air.
            var controller = prefab.GetComponent<ProjectileController>();
            if (controller && controller.ghostPrefab)
            {
                GameObject ghost = controller.ghostPrefab.InstantiateClone("AlchemistPotion" + def.key + "Ghost", false);
                Tint(ghost, def.color);
                controller.ghostPrefab = ghost;
            }

            AlchemistContentPack.ProjectilePrefabs.Add(prefab);
            return prefab;
        }

        private static SkillDef BuildSkillDef(PotionDef def)
        {
            var skill = ScriptableObject.CreateInstance<SkillDef>();
            skill.skillName = "AlchemistPotion" + def.key;
            (skill as ScriptableObject).name = skill.skillName;
            skill.skillNameToken = def.NameToken;
            skill.skillDescriptionToken = def.DescToken;
            skill.keywordTokens = new[] { AlchemistPlugin.Prefix + "KEYWORD_INFECT" };
            skill.icon = null; // TODO: potion icons. Filled with a vanilla placeholder on load.
            skill.activationStateMachineName = "Weapon";
            skill.activationState = new SerializableEntityStateType(typeof(ThrowPotion));
            skill.interruptPriority = InterruptPriority.Any;
            skill.baseRechargeInterval = 0f;
            skill.baseMaxStock = 1;
            skill.rechargeStock = 1;
            skill.requiredStock = 0;
            skill.stockToConsume = 0;
            skill.resetCooldownTimerOnUse = false;
            skill.fullRestockOnAssign = true;
            skill.dontAllowPastMaxStocks = false;
            skill.beginSkillCooldownOnSkillEnd = false;
            skill.cancelSprintingOnActivation = true;
            skill.canceledFromSprinting = false;
            skill.isCombatSkill = true;
            skill.mustKeyPress = false;

            AlchemistContentPack.SkillDefs.Add(skill);
            return skill;
        }

        private static void BuildPlaguePool()
        {
            GameObject baseAcid = GameAssets.Load<GameObject>(GameAssets.AcridAcidPool);
            if (!baseAcid) return;

            PlaguePoolPrefab = baseAcid.InstantiateClone("AlchemistPlaguePool", true);
            PlaguePoolPrefab.transform.localScale *= 1.25f;

            var zone = PlaguePoolPrefab.GetComponent<ProjectileDotZone>();
            if (zone)
            {
                zone.damageCoefficient = 0.15f;  // small direct tick, the Plague stacks do the real work
                zone.fireFrequency = 4f;
                zone.resetFrequency = 4f;
                zone.lifetime = 6f;
                zone.overlapProcCoefficient = 0.2f;
            }

            var damage = PlaguePoolPrefab.GetComponent<ProjectileDamage>();
            if (damage)
            {
                damage.damageType = DamageType.Generic;
                damage.damageType.AddModdedDamageType(Debuffs.PlagueTick);
            }

            AlchemistContentPack.ProjectilePrefabs.Add(PlaguePoolPrefab);
        }

        internal static void Tint(GameObject root, Color color)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!materials[i]) continue;
                    var copy = new Material(materials[i]);
                    if (copy.HasProperty("_Color")) copy.SetColor("_Color", color);
                    if (copy.HasProperty("_TintColor")) copy.SetColor("_TintColor", color);
                    if (copy.HasProperty("_EmColor")) copy.SetColor("_EmColor", color * 1.5f);
                    materials[i] = copy;
                }
                renderer.sharedMaterials = materials;
            }
        }
    }

    /// <summary>
    /// Drops an upright plague pool on the ground under wherever the flask shattered.
    /// </summary>
    internal class PotionPoolSpawner : MonoBehaviour, IProjectileImpactBehavior
    {
        private bool spawned;

        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            if (spawned || !NetworkServer.active || !PotionCatalog.PlaguePoolPrefab) return;
            spawned = true;

            Vector3 position = impactInfo.estimatedPointOfImpact;
            if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 40f, LayerIndex.world.mask))
                position = hit.point;

            var controller = GetComponent<ProjectileController>();
            var damage = GetComponent<ProjectileDamage>();

            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = PotionCatalog.PlaguePoolPrefab,
                position = position,
                rotation = Quaternion.identity,
                owner = controller ? controller.owner : null,
                damage = damage ? damage.damage : 0f,
                crit = damage && damage.crit,
                force = 0f,
                damageColorIndex = DamageColorIndex.Poison,
            });
        }
    }
}
