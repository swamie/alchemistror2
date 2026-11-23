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
        public string iconName;          // embedded icon, e.g. "potion_plague"
        public string name;
        public string description;
        public Color color;
        public float damageCoefficient;
        public float blastRadius;
        public ModdedDamageType? effect;
        public bool spawnsPool;

        public GameObject projectilePrefab;
        public GameObject gasPrefab;
        public SkillDef skillDef;

        public string NameToken => AlchemistPlugin.Prefix + "POTION_" + key + "_NAME";
        public string DescToken => AlchemistPlugin.Prefix + "POTION_" + key + "_DESC";
    }

    /// <summary>
    /// Potion skills share one throw cooldown, so swapping potions can't be used to throw faster.
    /// </summary>
    public class PotionSkillDef : SkillDef
    {
        public override bool IsReady(GenericSkill skillSlot)
        {
            return base.IsReady(skillSlot) && SharedCooldownReady(skillSlot);
        }

        public override bool CanExecute(GenericSkill skillSlot)
        {
            return base.CanExecute(skillSlot) && SharedCooldownReady(skillSlot);
        }

        private static bool SharedCooldownReady(GenericSkill skillSlot)
        {
            var selector = skillSlot ? skillSlot.GetComponent<PotionSelector>() : null;
            return !selector || selector.ThrowReady;
        }
    }

    internal static class PotionCatalog
    {
        public static readonly List<PotionDef> Potions = new List<PotionDef>();
        private static readonly Dictionary<SkillDef, PotionDef> bySkill = new Dictionary<SkillDef, PotionDef>();

        public static GameObject PlaguePoolPrefab;

        // Requested range was 180-220%. Sits at the low end; the scaling comes from Festering DoTs.
        private const float ImpactDamage = 1.8f;
        private const float ImpactRadius = 5f;

        // Gas puffs left behind by every flask (Terraria style). Tiny direct damage, they exist to apply effects.
        public const int GasPuffs = 3;
        public const float GasRadius = 2.6f;
        public const float GasLifetime = 3f;
        public const float GasTickRate = 2f;
        public const float GasDamageCoefficient = 0.1f;

        public static void Init()
        {
            Add("PLAGUE", "Plague Flask",
                "Bursts into plague gas and leaves a <style=cIsHealing>lingering plague pool</style> for 6s. Every tick inside adds a <style=cIsDamage>Plague</style> stack, so the longer they stay, the worse it gets.",
                new Color(0.4f, 0.75f, 0.2f), ImpactDamage, ImpactRadius, Debuffs.PlagueTick, spawnsPool: true);

            Add("HELLFIRE", "Hellfire Tonic",
                "<style=cIsDamage>Ignites</style> everything in the blast and the burning gas it leaves behind.",
                new Color(1f, 0.45f, 0.1f), ImpactDamage, ImpactRadius, Debuffs.Hellfire);

            Add("VITRIOL", "Corrosive Vitriol",
                "Applies <style=cIsUtility>Corroded</style>: <style=cIsDamage>-6 armor</style> per stack, up to 10 stacks. The gas keeps eating through.",
                new Color(0.9f, 0.75f, 0.1f), ImpactDamage, ImpactRadius, Debuffs.Corrosive);

            Add("FROST", "Frostbite Draught",
                "Applies <style=cIsUtility>Chilled</style>, slowing by 12% per stack. At 5 stacks the target <style=cIsUtility>freezes</style>.",
                new Color(0.55f, 0.85f, 1f), ImpactDamage, ImpactRadius, Debuffs.Frostbite);

            Add("HEMLOCK", "Hemlock Philter",
                "Applies <style=cIsUtility>Enfeebled</style>: enemies deal <style=cIsHealth>30% less damage</style> and attack 25% slower.",
                new Color(0.55f, 0.3f, 0.7f), ImpactDamage, ImpactRadius, Debuffs.Hemlock);

            Add("CATALYST", "Catalyst Elixir",
                "Hits deal <style=cIsDamage>+25% damage per unique debuff</style> on the target. The flask itself <style=cIsUtility>extends</style> your damage over time on it by 2s.",
                new Color(0.95f, 0.95f, 0.85f), ImpactDamage, ImpactRadius, Debuffs.Catalyst);

            Add("MIASMA", "Miasma Spores",
                "Applies <style=cIsUtility>Contagious</style> for 8s. If a contagious enemy dies, its <style=cIsDamage>debuffs and damage over time spread</style> to enemies within 15m.",
                new Color(0.55f, 0.6f, 0.25f), ImpactDamage, ImpactRadius, Debuffs.Miasma);

            Add("SANGUINE", "Sanguine Draught",
                "Applies <style=cIsUtility>Bloodlet</style> for 8s. Damage over time dealt to it <style=cIsHealing>heals you for 10%</style> of the damage.",
                new Color(0.65f, 0.05f, 0.15f), ImpactDamage, ImpactRadius, Debuffs.Sanguine);

            Add("VOLATILE", "Volatile Concoction",
                "No debuff and no gas. A big, unstable blast for <style=cIsDamage>280% damage</style> in a wider radius.",
                new Color(1f, 0.3f, 0.6f), 2.8f, 8f, null);

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
                iconName = "potion_" + key.ToLowerInvariant(),
                name = name,
                description = description,
                color = color,
                damageCoefficient = damageCoefficient,
                blastRadius = blastRadius,
                effect = effect,
                spawnsPool = spawnsPool,
            };

            if (effect.HasValue) def.gasPrefab = BuildGas(def);
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
                impact.impactEffect = PotionFx.BuildShatterEffect(def.key, def.color);
            }

            var damage = prefab.GetComponent<ProjectileDamage>();
            if (damage)
            {
                damage.damageType = DamageType.Generic;
                damage.damageType.AddModdedDamageType(Debuffs.PotionImpact);
                if (def.effect.HasValue)
                    damage.damageType.AddModdedDamageType(def.effect.Value);
            }

            var spawner = prefab.AddComponent<PotionShatterSpawner>();
            spawner.potionIndex = Potions.Count;

            // Recolour the flying bottle and make it tumble.
            var controller = prefab.GetComponent<ProjectileController>();
            if (controller && controller.ghostPrefab)
            {
                GameObject ghost = controller.ghostPrefab.InstantiateClone("AlchemistPotion" + def.key + "Ghost", false);
                Tint(ghost, def.color);
                ghost.AddComponent<FlaskSpin>();
                controller.ghostPrefab = ghost;
            }

            AlchemistContentPack.ProjectilePrefabs.Add(prefab);
            return prefab;
        }

        /// <summary>A drifting gas puff that keeps applying the potion's effect for a few seconds.</summary>
        private static GameObject BuildGas(PotionDef def)
        {
            GameObject baseAcid = GameAssets.Load<GameObject>(GameAssets.AcridAcidPool);
            if (!baseAcid) return null;

            GameObject gas = baseAcid.InstantiateClone("AlchemistGas" + def.key, true);
            gas.transform.localScale = Vector3.one;
            PotionFx.StripVisuals(gas);
            PotionFx.SetHitBox(gas, new Vector3(GasRadius * 2f, GasRadius * 1.6f, GasRadius * 2f));
            PotionFx.AddCloud(gas, def.color, GasRadius);
            gas.AddComponent<GasDrift>();

            var zone = gas.GetComponent<ProjectileDotZone>();
            if (zone)
            {
                zone.damageCoefficient = GasDamageCoefficient;
                zone.fireFrequency = GasTickRate;
                zone.resetFrequency = GasTickRate;
                zone.lifetime = GasLifetime;
                zone.overlapProcCoefficient = 0.1f;
            }

            var damage = gas.GetComponent<ProjectileDamage>();
            if (damage)
            {
                damage.damageType = DamageType.Generic;
                damage.damageType.AddModdedDamageType(Debuffs.GasTick);
                damage.damageType.AddModdedDamageType(def.effect.Value);
            }

            AlchemistContentPack.ProjectilePrefabs.Add(gas);
            return gas;
        }

        private static SkillDef BuildSkillDef(PotionDef def)
        {
            var skill = ScriptableObject.CreateInstance<PotionSkillDef>();
            skill.skillName = "AlchemistPotion" + def.key;
            (skill as ScriptableObject).name = skill.skillName;
            skill.skillNameToken = def.NameToken;
            skill.skillDescriptionToken = def.DescToken;
            skill.keywordTokens = new[] { AlchemistPlugin.Prefix + "KEYWORD_INFECT", AlchemistPlugin.Prefix + "KEYWORD_FESTER" };
            skill.icon = Icons.Get(def.iconName);
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
                zone.damageCoefficient = 0.1f;  // tiny direct tick, the Plague stacks do the real work
                zone.fireFrequency = 2f;
                zone.resetFrequency = 2f;
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
    /// When the flask shatters: release drifting gas puffs (and the plague pool for Plague Flask).
    /// </summary>
    internal class PotionShatterSpawner : MonoBehaviour, IProjectileImpactBehavior
    {
        public int potionIndex;
        private bool spawned;

        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            if (spawned || !NetworkServer.active) return;
            spawned = true;
            if (potionIndex < 0 || potionIndex >= PotionCatalog.Potions.Count) return;
            PotionDef potion = PotionCatalog.Potions[potionIndex];

            var controller = GetComponent<ProjectileController>();
            var damage = GetComponent<ProjectileDamage>();
            GameObject owner = controller ? controller.owner : null;
            float baseDamage = damage ? damage.damage / Mathf.Max(0.01f, potion.damageCoefficient) : 0f; // back to the owner's damage stat
            bool crit = damage && damage.crit;

            Vector3 point = impactInfo.estimatedPointOfImpact;
            Vector3 ground = point;
            if (Physics.Raycast(point + Vector3.up, Vector3.down, out RaycastHit hit, 40f, LayerIndex.world.mask))
                ground = hit.point;

            if (potion.gasPrefab)
            {
                float startYaw = Random.Range(0f, 360f);
                for (int i = 0; i < PotionCatalog.GasPuffs; i++)
                {
                    float yaw = startYaw + i * (360f / PotionCatalog.GasPuffs);
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = potion.gasPrefab,
                        position = Vector3.Lerp(point, ground, 0.5f) + Vector3.up * 0.8f,
                        rotation = Quaternion.Euler(0f, yaw, 0f),
                        owner = owner,
                        damage = baseDamage,
                        crit = crit,
                        force = 0f,
                        damageColorIndex = DamageColorIndex.Poison,
                    });
                }
            }

            if (potion.spawnsPool && PotionCatalog.PlaguePoolPrefab)
            {
                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = PotionCatalog.PlaguePoolPrefab,
                    position = ground,
                    rotation = Quaternion.identity,
                    owner = owner,
                    damage = baseDamage,
                    crit = crit,
                    force = 0f,
                    damageColorIndex = DamageColorIndex.Poison,
                });
            }
        }
    }
}
