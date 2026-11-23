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
        public static BuffDef Festering;     // stacks = seconds her DoTs have been running; scales their damage

        // ---- Damage over time ----
        public static DotController.DotIndex LacerationDot;
        public static DotController.DotIndex InfectionDot;
        public static DotController.DotIndex PlagueDot;

        // ---- Damage type tags, used to tell which attack caused a hit ----
        public static ModdedDamageType PotionImpact;
        public static ModdedDamageType PlagueTick;
        public static ModdedDamageType GasTick;         // tick from a lingering gas puff
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
        public const float BloodletHealFraction = 0.1f;

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
            Festering = NewBuff("Festering", new Color(0.75f, 0.95f, 0.3f), true);
            Festering.isDebuff = false; // a readout, not something to cleanse or count for Catalyst

            // Base numbers are deliberately low. Festering multiplies them the longer they run
            // (0.4x at the start, up to 4x), so fights start as a tickle and end in heavy ticks.
            // Laceration: 10% per 0.25s (40% base damage per second per stack).
            LacerationDot = DotAPI.RegisterDotDef(0.25f, 0.1f, DamageColorIndex.Bleed, Lacerated);
            // Infected Wound, the knife + potion payoff: 30% per 0.25s (120% per second).
            InfectionDot = DotAPI.RegisterDotDef(0.25f, 0.3f, DamageColorIndex.Poison, Infected);
            // Plague: weak per stack, the pool and gas keep adding stacks while enemies stay in it.
            PlagueDot = DotAPI.RegisterDotDef(0.25f, 0.05f, DamageColorIndex.Poison, Plagued);

            PotionImpact = ReserveDamageType();
            PlagueTick = ReserveDamageType();
            GasTick = ReserveDamageType();
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

            AssignIcons();
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

        private static void AssignIcons()
        {
            Lacerated.iconSprite = Icons.Get("buff_lacerated");
            Infected.iconSprite = Icons.Get("buff_infected");
            Plagued.iconSprite = Icons.Get("buff_plagued");
            Corroded.iconSprite = Icons.Get("buff_corroded");
            Chilled.iconSprite = Icons.Get("buff_chilled");
            Enfeebled.iconSprite = Icons.Get("buff_enfeebled");
            Contagious.iconSprite = Icons.Get("buff_contagious");
            Bloodlet.iconSprite = Icons.Get("buff_bloodlet");
            Gassed.iconSprite = Icons.Get("buff_gassed");
            Confused.iconSprite = Icons.Get("buff_confused");
            Festering.iconSprite = Icons.Get("buff_festering");
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
