using AlchemistMod.Content;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AlchemistMod.Potions
{
    /// <summary>
    /// Lives on the Alchemist body. Tracks which of the three equipped potions is active,
    /// lets the local player cycle them with the scroll wheel, owns the shared throw cooldown,
    /// and swaps the primary skill to match so the HUD icon and tooltip show the active potion.
    /// </summary>
    public class PotionSelector : MonoBehaviour
    {
        public const int BeltSize = 3;
        public static readonly string[] BeltSkillNames = { "AlchemistPotionBelt1", "AlchemistPotionBelt2", "AlchemistPotionBelt3" };

        private CharacterBody body;
        private SkillLocator skillLocator;
        private readonly GenericSkill[] belt = new GenericSkill[BeltSize];
        private SkillDef appliedOverride;
        private float scrollCooldown;
        private float throwReadyTime;

        private PotionBeltHud hud;

        public int SelectedSlot { get; private set; }

        /// <summary>True when the shared potion throw cooldown has finished.</summary>
        public bool ThrowReady => Time.time >= throwReadyTime;

        public float ThrowCooldownRemaining => Mathf.Max(0f, throwReadyTime - Time.time);

        public void StartThrowCooldown(float seconds)
        {
            throwReadyTime = Time.time + seconds;
        }

        internal PotionDef CurrentPotion => GetPotion(SelectedSlot);

        internal PotionDef GetPotion(int slot)
        {
            GenericSkill skill = belt[slot];
            return skill ? PotionCatalog.FromSkillDef(skill.baseSkill) : null;
        }

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            skillLocator = GetComponent<SkillLocator>();
            foreach (GenericSkill skill in GetComponents<GenericSkill>())
            {
                for (int i = 0; i < BeltSize; i++)
                {
                    if (skill.skillName == BeltSkillNames[i]) belt[i] = skill;
                }
            }
        }

        private void Update()
        {
            // Only the owning player swaps skills. The server never overrides, so nothing it
            // syncs back can fight the local choice.
            if (!body || !body.hasEffectiveAuthority) return;

            SyncPrimary();
            if (body.isPlayerControlled && AlchemistConfig.ShowPotionHud.Value && !hud)
                hud = PotionBeltHud.TryCreate(this, body);

            if (Time.timeScale == 0f) return;

            scrollCooldown -= Time.unscaledDeltaTime;
            int step = 0;

            if (AlchemistConfig.ScrollWheelSwitching.Value && scrollCooldown <= 0f)
            {
                float scroll = Input.mouseScrollDelta.y;
                if (AlchemistConfig.InvertScroll.Value) scroll = -scroll;
                if (scroll > 0.01f) step = -1;
                else if (scroll < -0.01f) step = 1;
            }

            if (AlchemistConfig.NextPotionKey.Value.IsDown()) step = 1;
            if (AlchemistConfig.PreviousPotionKey.Value.IsDown()) step = -1;

            if (step != 0)
            {
                scrollCooldown = 0.08f; // avoid skipping two potions on one fast notch
                SelectedSlot = (SelectedSlot + step + BeltSize) % BeltSize;
                SyncPrimary();
                Util.PlaySound("Play_UI_menuClick", gameObject);
            }
        }

        private void SyncPrimary()
        {
            if (!skillLocator || !skillLocator.primary) return;
            PotionDef potion = CurrentPotion;
            SkillDef wanted = potion != null ? potion.skillDef : null;
            if (wanted == appliedOverride) return;

            if (appliedOverride)
                skillLocator.primary.UnsetSkillOverride(this, appliedOverride, GenericSkill.SkillOverridePriority.Upgrade);
            if (wanted)
                skillLocator.primary.SetSkillOverride(this, wanted, GenericSkill.SkillOverridePriority.Upgrade);
            appliedOverride = wanted;
        }

        private void OnDestroy()
        {
            if (appliedOverride && skillLocator && skillLocator.primary)
                skillLocator.primary.UnsetSkillOverride(this, appliedOverride, GenericSkill.SkillOverridePriority.Upgrade);
            if (hud) Destroy(hud.gameObject);
        }
    }

    /// <summary>
    /// Three potion icons sitting just above the skill bar in the real game HUD.
    /// The active potion is full size and bright, the others are smaller and dimmed.
    /// </summary>
    internal class PotionBeltHud : MonoBehaviour
    {
        private PotionSelector selector;
        private RectTransform[] slots;
        private Image[] icons;
        private Image[] frames;
        private Image cooldownShade;

        public static PotionBeltHud TryCreate(PotionSelector selector, CharacterBody body)
        {
            foreach (HUD hudInstance in HUD.readOnlyInstanceList)
            {
                if (!hudInstance || hudInstance.targetBodyObject != body.gameObject) continue;
                if (hudInstance.skillIcons == null || hudInstance.skillIcons.Length == 0 || !hudInstance.skillIcons[0]) continue;
                return Build(selector, hudInstance);
            }
            return null;
        }

        private static PotionBeltHud Build(PotionSelector selector, HUD hudInstance)
        {
            var primaryIcon = (RectTransform)hudInstance.skillIcons[0].transform;
            RectTransform parent = (RectTransform)primaryIcon.parent;

            var root = new GameObject("AlchemistPotionBelt", typeof(RectTransform));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(parent, false);
            var layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true; // don't get pushed around by the skill bar's layout group

            var hud = root.AddComponent<PotionBeltHud>();
            hud.selector = selector;
            hud.slots = new RectTransform[PotionSelector.BeltSize];
            hud.icons = new Image[PotionSelector.BeltSize];
            hud.frames = new Image[PotionSelector.BeltSize];

            // Sit right above the primary skill icon, same size, laid out to the right like the skill bar.
            Vector2 iconSize = primaryIcon.rect.size;
            if (iconSize.x < 1f) iconSize = new Vector2(64f, 64f);
            float slotSize = iconSize.x * 0.8f;
            float gap = iconSize.x * 0.12f;

            rootRect.anchorMin = primaryIcon.anchorMin;
            rootRect.anchorMax = primaryIcon.anchorMax;
            rootRect.pivot = new Vector2(0f, 0f);
            rootRect.sizeDelta = new Vector2(slotSize * 3 + gap * 2, slotSize);
            Vector2 primaryBottomLeft = primaryIcon.anchoredPosition - Vector2.Scale(primaryIcon.pivot, iconSize);
            rootRect.anchoredPosition = primaryBottomLeft + new Vector2(0f, iconSize.y + gap * 1.5f);

            for (int i = 0; i < PotionSelector.BeltSize; i++)
            {
                var slot = new GameObject("Slot" + i, typeof(RectTransform));
                var rect = (RectTransform)slot.transform;
                rect.SetParent(rootRect, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(slotSize, slotSize);
                rect.anchoredPosition = new Vector2(slotSize * 0.5f + i * (slotSize + gap), 0f);

                var frame = slot.AddComponent<Image>();
                frame.color = new Color(0f, 0f, 0f, 0.6f);

                var iconGo = new GameObject("Icon", typeof(RectTransform));
                var iconRect = (RectTransform)iconGo.transform;
                iconRect.SetParent(rect, false);
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(3f, 3f);
                iconRect.offsetMax = new Vector2(-3f, -3f);
                var icon = iconGo.AddComponent<Image>();
                icon.preserveAspect = true;

                hud.slots[i] = rect;
                hud.frames[i] = frame;
                hud.icons[i] = icon;
            }

            // dark fill over the active slot while the shared throw cooldown runs
            var shadeGo = new GameObject("Cooldown", typeof(RectTransform));
            var shadeRect = (RectTransform)shadeGo.transform;
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
            hud.cooldownShade = shadeGo.AddComponent<Image>();
            hud.cooldownShade.color = new Color(0f, 0f, 0f, 0.55f);
            hud.cooldownShade.type = Image.Type.Filled;
            hud.cooldownShade.fillMethod = Image.FillMethod.Vertical;
            hud.cooldownShade.fillOrigin = 0;

            return hud;
        }

        private void LateUpdate()
        {
            if (!selector)
            {
                Destroy(gameObject);
                return;
            }

            for (int i = 0; i < PotionSelector.BeltSize; i++)
            {
                PotionDef potion = selector.GetPotion(i);
                bool active = i == selector.SelectedSlot;
                icons[i].sprite = potion != null ? Icons.Get(potion.iconName) : null;
                icons[i].enabled = icons[i].sprite;
                icons[i].color = active ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.8f);
                frames[i].color = active && potion != null
                    ? new Color(potion.color.r, potion.color.g, potion.color.b, 0.95f)
                    : new Color(0f, 0f, 0f, 0.6f);
                slots[i].localScale = Vector3.one * (active ? 1f : 0.8f);
            }

            RectTransform activeSlot = slots[selector.SelectedSlot];
            if (cooldownShade.transform.parent != activeSlot) cooldownShade.transform.SetParent(activeSlot, false);
            float remaining = selector.ThrowCooldownRemaining;
            cooldownShade.enabled = remaining > 0f;
            cooldownShade.fillAmount = Mathf.Clamp01(remaining / Mathf.Max(0.01f, SkillStates.ThrowPotion.sharedCooldown));
        }
    }
}
