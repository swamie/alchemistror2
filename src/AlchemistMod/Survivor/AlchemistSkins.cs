using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using AlchemistMod.Content;
using RoR2;
using UnityEngine;

namespace AlchemistMod.Survivor
{
    /// <summary>
    /// Default look plus three unlockable skins.
    ///
    /// Until the real model exists the skins are colour palettes applied over the placeholder
    /// (see AlchemistSkinTint). Each SkinDef is a copy of the placeholder's default skin so it
    /// keeps working across game versions; the palette is chosen from the skin index.
    /// </summary>
    internal static class AlchemistSkins
    {
        internal class SkinInfo
        {
            public string key;
            public string name;
            public Color tint;
            public Color accent;
            public string unlockableName;  // null = always unlocked
            public UnlockableDef unlockable;
        }

        // Index in this list == skin index in the loadout.
        public static readonly List<SkinInfo> Skins = new List<SkinInfo>
        {
            new SkinInfo { key = "DEFAULT", name = "Apothecary", tint = new Color(0.82f, 0.9f, 0.86f), accent = new Color(0.45f, 0.72f, 0.35f) },
            new SkinInfo { key = "PLAGUEWARDEN", name = "Plaguewarden", tint = new Color(0.62f, 0.26f, 0.26f), accent = new Color(0.12f, 0.1f, 0.1f),
                unlockableName = "Skins.SwamieAlchemist.Plaguewarden" },
            new SkinInfo { key = "VERDIGRIS", name = "Verdigris", tint = new Color(0.5f, 0.82f, 0.66f), accent = new Color(0.72f, 0.5f, 0.25f),
                unlockableName = "Skins.SwamieAlchemist.Verdigris" },
            new SkinInfo { key = "ASHEN", name = "Ashen Herbalist", tint = new Color(0.95f, 0.92f, 0.84f), accent = new Color(0.4f, 0.4f, 0.42f),
                unlockableName = "Skins.SwamieAlchemist.Ashen" },
        };

        public static void Init()
        {
            if (!AlchemistSurvivor.BodyPrefab) return;
            var modelLocator = AlchemistSurvivor.BodyPrefab.GetComponent<ModelLocator>();
            Transform model = modelLocator ? modelLocator.modelTransform : null;
            if (!model) return;

            var controller = model.GetComponent<ModelSkinController>();
            if (!controller) controller = model.gameObject.AddComponent<ModelSkinController>();

            var skinDefs = new List<SkinDef>();
            foreach (SkinInfo info in Skins)
            {
                if (info.unlockableName != null)
                {
                    info.unlockable = ScriptableObject.CreateInstance<UnlockableDef>();
                    info.unlockable.cachedName = info.unlockableName;
                    info.unlockable.nameToken = AlchemistPlugin.Prefix + "SKIN_" + info.key + "_NAME";
                    info.unlockable.achievementIcon = MakeIcon(info);
                    string achievementKey = AlchemistPlugin.Prefix + "ACH_" + info.key;
                    info.unlockable.getHowToUnlockString = () => Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT",
                        Language.GetString(achievementKey + "_NAME"), Language.GetString(achievementKey + "_DESC"));
                    info.unlockable.getUnlockedString = () => Language.GetStringFormatted("UNLOCKED_FORMAT",
                        Language.GetString(achievementKey + "_NAME"), Language.GetString(achievementKey + "_DESC"));
                    AlchemistContentPack.UnlockableDefs.Add(info.unlockable);
                }

                SkinDef skin = CreateEmptySkinDef();
                skin.name = "skinAlchemist" + info.key;
                skin.nameToken = AlchemistPlugin.Prefix + "SKIN_" + info.key + "_NAME";
                skin.icon = MakeIcon(info);
                skin.rootObject = model.gameObject;
                skin.unlockableDef = info.unlockable;
                skinDefs.Add(skin);
            }

            controller.skins = skinDefs.ToArray();

            // The display is cloned before skins exist, so give it the same list.
            if (AlchemistSurvivor.DisplayPrefab)
            {
                var displayController = AlchemistSurvivor.DisplayPrefab.GetComponent<ModelSkinController>();
                if (displayController) displayController.skins = skinDefs.ToArray();
            }
        }

        /// <summary>
        /// SkinDef.Awake bakes the skin straight away and fails while rootObject is still null,
        /// so skip Awake while we create it. The game bakes it later on first use.
        /// The skin itself changes nothing on the model; AlchemistSkinTint does the recolour.
        /// </summary>
        private static SkinDef CreateEmptySkinDef()
        {
            var harmony = new Harmony(AlchemistPlugin.PluginGUID + ".skins");
            MethodInfo awake = AccessTools.Method(typeof(SkinDef), "Awake");
            if (awake != null)
                harmony.Patch(awake, prefix: new HarmonyMethod(typeof(AlchemistSkins), nameof(SkipAwake)));
            try
            {
                var skin = ScriptableObject.CreateInstance<SkinDef>();
                FillNullArrays(skin);

                // Newer game versions keep renderer / mesh data on a separate SkinDefParams asset.
                FieldInfo paramsField = AccessTools.Field(typeof(SkinDef), "skinDefParams");
                if (paramsField != null && typeof(ScriptableObject).IsAssignableFrom(paramsField.FieldType)
                    && paramsField.GetValue(skin) == null)
                {
                    var skinParams = ScriptableObject.CreateInstance(paramsField.FieldType);
                    FillNullArrays(skinParams);
                    paramsField.SetValue(skin, skinParams);
                }
                return skin;
            }
            finally
            {
                harmony.UnpatchSelf();
            }
        }

        private static bool SkipAwake() => false;

        private static void FillNullArrays(object target)
        {
            foreach (FieldInfo field in target.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.FieldType.IsArray && field.GetValue(target) == null)
                    field.SetValue(target, System.Array.CreateInstance(field.FieldType.GetElementType(), 0));
            }
        }

        public static SkinInfo Get(int index)
        {
            return index >= 0 && index < Skins.Count ? Skins[index] : Skins[0];
        }

        /// <summary>Simple two-tone round icon until real skin icons are painted.</summary>
        private static Sprite MakeIcon(SkinInfo info)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f, dy = y - r + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = d > r - 2f ? new Color(0, 0, 0, 0) : (x + y > size ? info.accent : info.tint);
                    if (d > r - 8f && d <= r - 2f) c = new Color(0.1f, 0.1f, 0.1f, 1f);
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }

    /// <summary>
    /// Recolours the placeholder model's materials to match the selected skin.
    /// Works on both the in-game model and the character select display.
    /// </summary>
    public class AlchemistSkinTint : MonoBehaviour
    {
        private CharacterModel characterModel;
        private ModelSkinController skinController;
        private int appliedSkin = -1;
        private readonly Dictionary<Material, Material> tintedToOriginal = new Dictionary<Material, Material>();
        private readonly Dictionary<(Material, int), Material> cache = new Dictionary<(Material, int), Material>();

        private void Awake()
        {
            characterModel = GetComponent<CharacterModel>();
            skinController = GetComponent<ModelSkinController>();
        }

        private void LateUpdate()
        {
            if (!characterModel || characterModel.baseRendererInfos == null) return;
            int skin = skinController ? skinController.currentSkinIndex : 0;
            bool skinChanged = skin != appliedSkin;
            appliedSkin = skin;

            for (int i = 0; i < characterModel.baseRendererInfos.Length; i++)
            {
                Material current = characterModel.baseRendererInfos[i].defaultMaterial;
                if (!current) continue;

                bool alreadyOurs = tintedToOriginal.TryGetValue(current, out Material original);
                if (alreadyOurs && !skinChanged) continue;
                if (!alreadyOurs) original = current;

                Material tinted = GetTinted(original, skin);
                characterModel.baseRendererInfos[i].defaultMaterial = tinted;
                if (characterModel.baseRendererInfos[i].renderer)
                    characterModel.baseRendererInfos[i].renderer.sharedMaterial = tinted;
                characterModel.materialsDirty = true;
            }
        }

        private Material GetTinted(Material original, int skin)
        {
            if (cache.TryGetValue((original, skin), out Material tinted)) return tinted;

            AlchemistSkins.SkinInfo info = AlchemistSkins.Get(skin);
            tinted = new Material(original) { name = original.name + "_Alchemist" + info.key };
            if (tinted.HasProperty("_Color")) tinted.SetColor("_Color", original.GetColor("_Color") * info.tint);
            if (tinted.HasProperty("_EmColor")) tinted.SetColor("_EmColor", info.accent * 0.4f);

            cache[(original, skin)] = tinted;
            tintedToOriginal[tinted] = original;
            return tinted;
        }
    }
}
