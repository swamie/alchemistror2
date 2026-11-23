using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace AlchemistMod.Potions
{
    /// <summary>
    /// Lives on the Alchemist body. Tracks which of the three equipped potions is active,
    /// lets the local player cycle them with the scroll wheel, and swaps the primary skill
    /// to match so the HUD icon, name and tooltip always show the active potion.
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

        public int SelectedSlot { get; private set; }

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

            // The belt can be filled in late, so keep the primary in sync every frame.
            SyncPrimary();

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
                Util.PlaySound("Play_bandit2_m2_alt_throw", gameObject);
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
        }

        // ---- Simple on-screen belt. Placeholder until a proper HUD element is built. ----

        private static GUIStyle boxStyle;

        private void OnGUI()
        {
            if (!AlchemistConfig.ShowPotionHud.Value || !body || !body.hasEffectiveAuthority || !body.isPlayerControlled) return;
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            }

            const float width = 130f, height = 36f, gap = 6f;
            float total = BeltSize * width + (BeltSize - 1) * gap;
            float x = (Screen.width - total) / 2f;
            float y = Screen.height * 0.78f;

            for (int i = 0; i < BeltSize; i++)
            {
                PotionDef potion = GetPotion(i);
                string label = potion != null ? potion.name : "-";
                Color old = GUI.color;
                GUI.color = i == SelectedSlot
                    ? (potion != null ? Color.Lerp(potion.color, Color.white, 0.35f) : Color.white)
                    : new Color(1f, 1f, 1f, 0.45f);
                GUI.Box(new Rect(x + i * (width + gap), y, width, height), (i == SelectedSlot ? "> " : "") + label, boxStyle);
                GUI.color = old;
            }
        }
    }
}
