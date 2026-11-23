using System.Collections.Generic;
using AlchemistMod.Content;
using AlchemistMod.Potions;
using AlchemistMod.SkillStates;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace AlchemistMod.Survivor
{
    /// <summary>
    /// Builds the Alchemist body, her skills and her survivor entry.
    ///
    /// The body is a clone of Bandit as a PLACEHOLDER. Bandit is the closest vanilla match to the
    /// plague doctor silhouette (wide hat, long coat). Swap the model once the real one exists;
    /// see README "Replacing the placeholder model".
    /// </summary>
    internal static class AlchemistSurvivor
    {
        public const string BodyName = "AlchemistBody";

        public static GameObject BodyPrefab;
        public static GameObject DisplayPrefab;
        public static SurvivorDef SurvivorDef;

        public static readonly Color ThemeColor = new Color(0.45f, 0.72f, 0.35f);

        public static SkillDef PrimaryThrow, Knife, Elbow, Vault, TransmuteSkill;

        private static BodyIndex cachedBodyIndex = BodyIndex.None;
        public static BodyIndex BodyIndex
        {
            get
            {
                if (cachedBodyIndex == BodyIndex.None) cachedBodyIndex = BodyCatalog.FindBodyIndex(BodyName);
                return cachedBodyIndex;
            }
        }

        private static string T(string key) => AlchemistPlugin.Prefix + key;

        public static void Init()
        {
            GameObject bandit = GameAssets.Load<GameObject>(GameAssets.BanditBody);
            if (!bandit)
            {
                AlchemistPlugin.Log.LogError("Could not build the Alchemist: base body missing.");
                return;
            }

            BodyPrefab = bandit.InstantiateClone(BodyName, true);

            ConfigureBody();
            Dictionary<SkillSlot, Sprite> placeholderIcons = StripBanditSkills();
            BuildSkills(placeholderIcons);
            ConfigureStateMachines();
            BodyPrefab.AddComponent<PotionSelector>();

            VapourVault.BuildAssets();
            BuildDisplay();
            BuildSurvivorDef();

            AlchemistContentPack.BodyPrefabs.Add(BodyPrefab);
            AlchemistContentPack.EntityStates.AddRange(new[]
            {
                typeof(ThrowPotion), typeof(CutthroatKnife), typeof(ThrowElbow), typeof(VapourVault), typeof(Transmute),
            });
        }

        private static void ConfigureBody()
        {
            var body = BodyPrefab.GetComponent<CharacterBody>();
            body.name = BodyName;
            body.baseNameToken = T("NAME");
            body.subtitleNameToken = T("SUBTITLE");
            body.bodyColor = ThemeColor;

            // Squishy mid-range caster, a touch tankier than Bandit to survive the knife range.
            body.baseMaxHealth = 110f;
            body.levelMaxHealth = 33f;
            body.baseRegen = 1f;
            body.levelRegen = 0.2f;
            body.baseDamage = 12f;
            body.levelDamage = 2.4f;
            body.baseMoveSpeed = 7f;
            body.baseArmor = 0f;
            body.baseCrit = 1f;
            body.baseAttackSpeed = 1f;
            body.baseJumpCount = 1;
        }

        /// <summary>Removes Bandit's skill slots, keeping their icons as placeholders.</summary>
        private static Dictionary<SkillSlot, Sprite> StripBanditSkills()
        {
            var icons = new Dictionary<SkillSlot, Sprite>();
            var locator = BodyPrefab.GetComponent<SkillLocator>();

            void Grab(SkillSlot slot, GenericSkill skill)
            {
                if (skill && skill.skillFamily && skill.skillFamily.variants != null && skill.skillFamily.variants.Length > 0)
                    icons[slot] = skill.skillFamily.variants[0].skillDef ? skill.skillFamily.variants[0].skillDef.icon : null;
            }

            if (locator)
            {
                Grab(SkillSlot.Primary, locator.primary);
                Grab(SkillSlot.Secondary, locator.secondary);
                Grab(SkillSlot.Utility, locator.utility);
                Grab(SkillSlot.Special, locator.special);
            }

            foreach (GenericSkill skill in BodyPrefab.GetComponents<GenericSkill>())
                Object.DestroyImmediate(skill);

            return icons;
        }

        private static void BuildSkills(Dictionary<SkillSlot, Sprite> icons)
        {
            Sprite Icon(SkillSlot slot) => icons.TryGetValue(slot, out var s) ? s : null;
            var locator = BodyPrefab.GetComponent<SkillLocator>();

            // Passive
            locator.passiveSkill = new SkillLocator.PassiveSkill
            {
                enabled = true,
                skillNameToken = T("PASSIVE_NAME"),
                skillDescriptionToken = T("PASSIVE_DESC"),
                keywordToken = T("KEYWORD_INFECT"),
                icon = Icon(SkillSlot.Special),
            };

            // Primary. The slot shows the generic "Throw Potion"; in-game it is overridden by the active potion.
            PrimaryThrow = NewSkill("ThrowPotion", T("PRIMARY_NAME"), T("PRIMARY_DESC"), Icon(SkillSlot.Primary),
                typeof(ThrowPotion), "Weapon", 0f, InterruptPriority.Any, combat: true);
            PrimaryThrow.keywordTokens = new[] { T("KEYWORD_INFECT") };
            foreach (PotionDef potion in PotionCatalog.Potions) potion.skillDef.icon = Icon(SkillSlot.Primary);

            // Secondaries
            Knife = NewSkill("CutthroatKnife", T("SECONDARY_KNIFE_NAME"), T("SECONDARY_KNIFE_DESC"), Icon(SkillSlot.Secondary),
                typeof(CutthroatKnife), "Weapon", 4f, InterruptPriority.Skill, combat: true);
            Knife.keywordTokens = new[] { T("KEYWORD_LACERATE"), T("KEYWORD_INFECT") };

            Elbow = NewSkill("ThrowElbow", T("SECONDARY_ELBOW_NAME"), T("SECONDARY_ELBOW_DESC"), Icon(SkillSlot.Secondary),
                typeof(ThrowElbow), "Weapon", 7f, InterruptPriority.Skill, combat: true);
            Elbow.keywordTokens = new[] { "KEYWORD_STUNNING", T("KEYWORD_CONFUSE") };

            // Utility
            Vault = NewSkill("VapourVault", T("UTILITY_NAME"), T("UTILITY_DESC"), Icon(SkillSlot.Utility),
                typeof(VapourVault), "Body", 7f, InterruptPriority.PrioritySkill, combat: false);

            // Special
            TransmuteSkill = NewSkill("Transmute", T("SPECIAL_NAME"), T("SPECIAL_DESC"), Icon(SkillSlot.Special),
                typeof(Transmute), "Weapon", 12f, InterruptPriority.Skill, combat: true);

            locator.primary = AddSlot("AlchemistPrimary", new[] { PrimaryThrow }, 0);
            locator.secondary = AddSlot("AlchemistSecondary", new[] { Knife, Elbow }, 0);
            locator.utility = AddSlot("AlchemistUtility", new[] { Vault }, 0);
            locator.special = AddSlot("AlchemistSpecial", new[] { TransmuteSkill }, 0);

            // Potion belt: three extra loadout rows, each picking any of the nine potions.
            var potionSkills = new List<SkillDef>();
            foreach (PotionDef potion in PotionCatalog.Potions) potionSkills.Add(potion.skillDef);
            for (int i = 0; i < PotionSelector.BeltSize; i++)
            {
                AddSlot(PotionSelector.BeltSkillNames[i], potionSkills.ToArray(), (uint)Mathf.Min(i, potionSkills.Count - 1));
            }
        }

        private static SkillDef NewSkill(string name, string nameToken, string descToken, Sprite icon,
            System.Type state, string machine, float cooldown, InterruptPriority priority, bool combat)
        {
            var skill = ScriptableObject.CreateInstance<SkillDef>();
            skill.skillName = "Alchemist" + name;
            (skill as ScriptableObject).name = skill.skillName;
            skill.skillNameToken = nameToken;
            skill.skillDescriptionToken = descToken;
            skill.icon = icon;
            skill.activationState = new SerializableEntityStateType(state);
            skill.activationStateMachineName = machine;
            skill.baseRechargeInterval = cooldown;
            skill.baseMaxStock = 1;
            skill.rechargeStock = 1;
            skill.requiredStock = 1;
            skill.stockToConsume = cooldown > 0f ? 1 : 0;
            skill.interruptPriority = priority;
            skill.isCombatSkill = combat;
            skill.cancelSprintingOnActivation = combat;
            skill.canceledFromSprinting = false;
            skill.mustKeyPress = cooldown > 0f;
            skill.fullRestockOnAssign = true;
            skill.beginSkillCooldownOnSkillEnd = false;
            if (cooldown <= 0f) skill.requiredStock = 0;

            AlchemistContentPack.SkillDefs.Add(skill);
            return skill;
        }

        private static GenericSkill AddSlot(string slotName, SkillDef[] skills, uint defaultIndex)
        {
            var family = ScriptableObject.CreateInstance<SkillFamily>();
            (family as ScriptableObject).name = slotName + "Family";
            family.variants = new SkillFamily.Variant[skills.Length];
            for (int i = 0; i < skills.Length; i++)
            {
                family.variants[i] = new SkillFamily.Variant
                {
                    skillDef = skills[i],
                    viewableNode = new ViewablesCatalog.Node(slotName + "_" + skills[i].skillName, false, null),
                };
            }
            family.defaultVariantIndex = defaultIndex;

            var slot = BodyPrefab.AddComponent<GenericSkill>();
            slot.skillName = slotName;
            slot._skillFamily = family;
            slot.hideInCharacterSelect = false;

            AlchemistContentPack.SkillFamilies.Add(family);
            return slot;
        }

        private static void ConfigureStateMachines()
        {
            foreach (EntityStateMachine machine in BodyPrefab.GetComponents<EntityStateMachine>())
            {
                if (machine.customName == "Body")
                {
                    machine.mainStateType = new SerializableEntityStateType(typeof(GenericCharacterMain));
                }
                else
                {
                    machine.initialStateType = new SerializableEntityStateType(typeof(Idle));
                    machine.mainStateType = new SerializableEntityStateType(typeof(Idle));
                }
            }
        }

        private static void BuildDisplay()
        {
            var modelLocator = BodyPrefab.GetComponent<ModelLocator>();
            if (!modelLocator || !modelLocator.modelTransform) return;

            modelLocator.modelTransform.gameObject.AddComponent<AlchemistSkinTint>();

            DisplayPrefab = modelLocator.modelTransform.gameObject.InstantiateClone("AlchemistDisplay", false);
            if (!DisplayPrefab.GetComponent<AlchemistSkinTint>()) DisplayPrefab.AddComponent<AlchemistSkinTint>();
        }

        private static void BuildSurvivorDef()
        {
            SurvivorDef = ScriptableObject.CreateInstance<SurvivorDef>();
            SurvivorDef.cachedName = "Alchemist";
            (SurvivorDef as ScriptableObject).name = "Alchemist";
            SurvivorDef.bodyPrefab = BodyPrefab;
            SurvivorDef.displayPrefab = DisplayPrefab ? DisplayPrefab : BodyPrefab;
            SurvivorDef.displayNameToken = T("NAME");
            SurvivorDef.descriptionToken = T("DESCRIPTION");
            SurvivorDef.outroFlavorToken = T("OUTRO_FLAVOR");
            SurvivorDef.mainEndingEscapeFailureFlavorToken = T("OUTRO_FAILURE");
            SurvivorDef.primaryColor = ThemeColor;
            SurvivorDef.desiredSortPosition = 100f;
            SurvivorDef.hidden = false;
            AlchemistContentPack.SurvivorDefs.Add(SurvivorDef);
        }
    }
}
