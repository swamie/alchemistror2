using System.Security;
using System.Security.Permissions;
using AlchemistMod.Content;
using AlchemistMod.Effects;
using AlchemistMod.Items;
using AlchemistMod.Potions;
using AlchemistMod.Survivor;
using BepInEx;
using BepInEx.Logging;
using R2API;
using R2API.Utils;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
[module: UnverifiableCode]
// Lets RoR2 find our [RegisterAchievement] classes.
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace AlchemistMod
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(DotAPI.PluginGUID)]
    [BepInDependency(DamageAPI.PluginGUID)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class AlchemistPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "swamie.Alchemist";
        public const string PluginName = "Alchemist";
        public const string PluginVersion = "0.1.0";

        /// <summary>Prefix for every language token and asset name we register.</summary>
        public const string Prefix = "SWAMIE_ALCHEMIST_";

        public static AlchemistPlugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            AlchemistConfig.Init(Config);

            // Order matters: debuffs and damage types first, then the things that use them.
            Debuffs.Init();
            PotionCatalog.Init();
            ClockworkCrow.Init();
            AlchemistSurvivor.Init();
            AlchemistSkins.Init();
            Tokens.Init();

            DebuffHooks.Init();
            ClockworkCrow.InitHooks();
            AlchemistContentPack.Init();

            Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
        }
    }
}
