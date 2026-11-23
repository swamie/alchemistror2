using System.Collections;
using System.Collections.Generic;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using UnityEngine;

namespace AlchemistMod.Content
{
    /// <summary>
    /// Collects everything the mod creates and hands it to the game as one content pack.
    /// Other files call the Add* helpers while they build their prefabs in Awake.
    /// </summary>
    internal class AlchemistContentPack : IContentPackProvider
    {
        public static readonly List<GameObject> BodyPrefabs = new List<GameObject>();
        public static readonly List<GameObject> MasterPrefabs = new List<GameObject>();
        public static readonly List<GameObject> ProjectilePrefabs = new List<GameObject>();
        public static readonly List<SurvivorDef> SurvivorDefs = new List<SurvivorDef>();
        public static readonly List<SkillDef> SkillDefs = new List<SkillDef>();
        public static readonly List<SkillFamily> SkillFamilies = new List<SkillFamily>();
        public static readonly List<System.Type> EntityStates = new List<System.Type>();
        public static readonly List<BuffDef> BuffDefs = new List<BuffDef>();
        public static readonly List<ItemDef> ItemDefs = new List<ItemDef>();
        public static readonly List<UnlockableDef> UnlockableDefs = new List<UnlockableDef>();
        public static readonly List<EffectDef> EffectDefs = new List<EffectDef>();

        private readonly ContentPack contentPack = new ContentPack();
        public string identifier => AlchemistPlugin.PluginGUID;

        public static void Init()
        {
            ContentManager.collectContentPackProviders += add => add(new AlchemistContentPack());
        }

        public static void AddEffect(GameObject effectPrefab)
        {
            EffectDefs.Add(new EffectDef(effectPrefab));
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            contentPack.identifier = identifier;
            contentPack.bodyPrefabs.Add(BodyPrefabs.ToArray());
            contentPack.masterPrefabs.Add(MasterPrefabs.ToArray());
            contentPack.projectilePrefabs.Add(ProjectilePrefabs.ToArray());
            contentPack.survivorDefs.Add(SurvivorDefs.ToArray());
            contentPack.skillDefs.Add(SkillDefs.ToArray());
            contentPack.skillFamilies.Add(SkillFamilies.ToArray());
            contentPack.entityStateTypes.Add(EntityStates.ToArray());
            contentPack.buffDefs.Add(BuffDefs.ToArray());
            contentPack.itemDefs.Add(ItemDefs.ToArray());
            contentPack.unlockableDefs.Add(UnlockableDefs.ToArray());
            contentPack.effectDefs.Add(EffectDefs.ToArray());
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
