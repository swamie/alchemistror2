using AlchemistMod.Content;
using R2API;
using RoR2;
using UnityEngine;
using static R2API.DamageAPI;

namespace AlchemistMod.Effects
{
    /// <summary>
    /// Every buff, damage-over-time and custom damage type the Alchemist uses.
    /// </summary>
    internal static class Debuffs
    {
        // ---- Debuffs shown on enemies ----
        public static BuffDef Lacerated;     // DoT from Cutthroat Knife
        public static BuffDef Infected;      // DoT when a potion hits a lacerated enemy
        public static BuffDef Plagued;       // DoT from standing in the Plague Flask pool
        public static BuffDef Corroded;      // -armor per stack
        public static BuffDef Chilled;       // slow per stack, freezes at max stacks
        public static BuffDef Enfeebled;     // deals less damage, attacks slower
        public static BuffDef Contagious;    // spreads its debuffs when it dies
        public static BuffDef Bloodlet;      // DoT damage it takes heals the Alchemist
        public static BuffDef Gassed;        // slowed by the utility's smoke cloud
        public static BuffDef Confused;      // attacking its own allies after the elbow

        // ---- Damage over time ----
        public static DotController.DotIndex LacerationDot;
        public static DotController.DotIndex InfectionDot;
        public static DotController.DotIndex PlagueDot;

        // ---- Damage type tags, used to tell which attack caused a hit ----
        public static ModdedDamageType PotionImpact;
        public static ModdedDamageType PlagueTick;
        public static ModdedDamageType Hellfire;
        public static ModdedDamageType Corrosive;
        public static ModdedDamageType Frostbite;
        public static ModdedDamageType Hemlock;
        public static ModdedDamageType Catalyst;
        public static ModdedDamageType Miasma;
        public static ModdedDamageType Sanguine;
        public static ModdedDamageType KnifeLacerate;
        public static ModdedDamageType ElbowConfuse;
        public static ModdedDamageType Fumes;
        public static ModdedDamageType Transmute;
        public static ModdedDamageType CrowPeck;

        // ---- Tuning ----
        public const int CorrodedMaxStacks = 10;
        public const float CorrodedArmorPerStack = 6f;
        public const int ChilledFreezeStacks = 5;
        public const float ChilledSlowPerStack = 0.12f;
        public const float EnfeebledDamagePenalty = 0.3f;
        public const float EnfeebledAttackSpeedPenalty = 0.25f;
        public const float GassedSlow = 0.4f;
        public const float BloodletHealFraction = 0.15f;

        public static void Init()
        {
            Lacerated = NewBuff("Lacerated", new Color(0.75f, 0.1f, 0.12f), true);
            Infected = NewBuff("Infected", new Color(0.55f, 0.75f, 0.15f), true);
            Plagued = NewBuff("Plagued", new Color(0.35f, 0.65f, 0.2f), true);
            Corroded = NewBuff("Corroded", new Color(0.85f, 0.7f, 0.15f), true);
            Chilled = NewBuff("Chilled", new Color(0.55f, 0.85f, 1f), true);
            Enfeebled = NewBuff("Enfeebled", new Color(0.5f, 0.3f, 0.65f), false);
            Contagious = NewBuff("Contagious", new Color(0.45f, 0.5f, 0.2f), false);
            Bloodlet = NewBuff("Bloodlet", new Color(0.6f, 0f, 0.15f), false);
            Gassed = NewBuff("Gassed", new Color(0.6f, 0.6f, 0.55f), false);
            Confused = NewBuff("Confused", new Color(1f, 0.85f, 0.3f), false);

            // Vanilla bleed is 20% per 0.25s. Laceration matches it.
            LacerationDot = DotAPI.RegisterDotDef(0.25f, 0.2f, DamageColorIndex.Bleed, Lacerated);
            // Infected Wound is the big payoff: 60% per 0.25s (240% per second).
            InfectionDot = DotAPI.RegisterDotDef(0.25f, 0.6f, DamageColorIndex.Poison, Infected);
            // Plague is weak per stack, but the pool keeps adding stacks while enemies stand in it.
            PlagueDot = DotAPI.RegisterDotDef(0.25f, 0.1f, DamageColorIndex.Poison, Plagued);

            PotionImpact = ReserveDamageType();
            PlagueTick = ReserveDamageType();
            Hellfire = ReserveDamageType();
            Corrosive = ReserveDamageType();
            Frostbite = ReserveDamageType();
            Hemlock = ReserveDamageType();
            Catalyst = ReserveDamageType();
            Miasma = ReserveDamageType();
            Sanguine = ReserveDamageType();
            KnifeLacerate = ReserveDamageType();
            ElbowConfuse = ReserveDamageType();
            Fumes = ReserveDamageType();
            Transmute = ReserveDamageType();
            CrowPeck = ReserveDamageType();

            RoR2Application.onLoad += AssignIcons;
        }

        private static BuffDef NewBuff(string name, Color color, bool canStack)
        {
            var buff = ScriptableObject.CreateInstance<BuffDef>();
            buff.name = "bdAlchemist" + name;
            buff.buffColor = color;
            buff.canStack = canStack;
            buff.isDebuff = true;
            buff.isHidden = false;
            buff.isCooldown = false;
            AlchemistContentPack.BuffDefs.Add(buff);
            return buff;
        }

        /// <summary>
        /// Placeholder icons: borrow vanilla buff icons once the game has loaded them.
        /// Each buff keeps its own tint so they stay readable.
        /// </summary>
        private static void AssignIcons()
        {
            Sprite Icon(BuffDef vanilla) => vanilla ? vanilla.iconSprite : null;

            Lacerated.iconSprite = Icon(RoR2Content.Buffs.Bleeding);
            Infected.iconSprite = Icon(RoR2Content.Buffs.Blight);
            Plagued.iconSprite = Icon(RoR2Content.Buffs.Poisoned);
            Corroded.iconSprite = Icon(RoR2Content.Buffs.Pulverized);
            Chilled.iconSprite = Icon(RoR2Content.Buffs.Slow80);
            Enfeebled.iconSprite = Icon(RoR2Content.Buffs.Weak);
            Contagious.iconSprite = Icon(RoR2Content.Buffs.Blight);
            Bloodlet.iconSprite = Icon(RoR2Content.Buffs.SuperBleed);
            Gassed.iconSprite = Icon(RoR2Content.Buffs.Slow50);
            Confused.iconSprite = Icon(RoR2Content.Buffs.Cripple);
        }

        /// <summary>How many different debuffs (including DoTs) the body has right now.</summary>
        public static int CountUniqueDebuffs(CharacterBody body)
        {
            if (!body || BuffCatalog.debuffBuffIndices == null) return 0;
            int count = 0;
            foreach (BuffIndex index in BuffCatalog.debuffBuffIndices)
            {
                if (body.HasBuff(index)) count++;
            }
            return count;
        }
    }
}
