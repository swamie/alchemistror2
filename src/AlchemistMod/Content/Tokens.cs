using AlchemistMod.Potions;
using AlchemistMod.Survivor;
using R2API;

namespace AlchemistMod.Content
{
    /// <summary>All English text. Kept in one place so it is easy to edit or translate.</summary>
    internal static class Tokens
    {
        private static void Add(string key, string text) => LanguageAPI.Add(AlchemistPlugin.Prefix + key, text);

        public static void Init()
        {
            // ---- Survivor ----
            Add("NAME", "Alchemist");
            Add("SUBTITLE", "Mistress of Maladies");
            Add("DESCRIPTION",
                "The Alchemist layers poisons, burns and curses until her enemies come apart.<color=#CCD3E0>\n\n" +
                "< ! > Scroll the mouse wheel to swap between your three equipped potions. Pick them on the Potion Belt rows in the loadout.\n\n" +
                "< ! > Cut with Cutthroat Knife, then hit the same enemy with any potion to infect the wound for heavy damage over time.\n\n" +
                "< ! > Plague Flask pools punish enemies that stand still. Pair them with Frostbite or Thrown Elbow.\n\n" +
                "< ! > Transmute cashes in every DoT you have applied. Stack as much as you can before pressing it.</color>\n");
            Add("OUTRO_FLAVOR", "..and so she left, her satchel heavier than when she arrived.");
            Add("OUTRO_FAILURE", "..and so she vanished, one last sample left unlabelled.");
            Add("LORE", "A field apothecary sent to catalogue Petrichor V's flora. Her notes stop being about medicine on day three.");

            // ---- Passive ----
            Add("PASSIVE_NAME", "Potion Belt");
            Add("PASSIVE_DESC",
                "Carry <style=cIsUtility>three potions</style> chosen in the loadout. <style=cIsUtility>Scroll</style> to swap the active one. " +
                "Starts with the <style=cIsUtility>Clockwork Crow</style>.");

            // ---- Keywords ----
            Add("KEYWORD_LACERATE",
                "<style=cKeywordName>Lacerate</style><style=cSub>Bleeds for <style=cIsDamage>80% base damage</style> per second for 5s. Stacks.</style>");
            Add("KEYWORD_INFECT",
                "<style=cKeywordName>Infect</style><style=cSub>Any potion that hits a <style=cIsDamage>lacerated</style> enemy consumes the cuts and inflicts " +
                "<style=cIsDamage>Infected Wound</style> for <style=cIsDamage>240% base damage</style> per second for 6s, <style=cIsDamage>+50%</style> per extra cut.</style>");
            Add("KEYWORD_CONFUSE",
                "<style=cKeywordName>Confuse</style><style=cSub>For 3s the target <style=cIsUtility>attacks its own allies</style> and can be hurt by them. Does not work on bosses.</style>");

            // ---- Skills ----
            Add("PRIMARY_NAME", "Throw Potion");
            Add("PRIMARY_DESC",
                "Lob your active potion for <style=cIsDamage>200% damage</style>. It shatters on impact and applies its effect. " +
                "<style=cIsUtility>Scroll</style> to switch potions.");

            Add("SECONDARY_KNIFE_NAME", "Cutthroat Knife");
            Add("SECONDARY_KNIFE_DESC",
                "<style=cIsDamage>Lacerate</style>. Slash an enemy right in front of you for <style=cIsDamage>360% damage</style>. " +
                "Follow up with a potion to <style=cIsDamage>Infect</style> the wound.");

            Add("SECONDARY_ELBOW_NAME", "Thrown Elbow");
            Add("SECONDARY_ELBOW_DESC",
                "<style=cIsDamage>Stunning</style>. <style=cIsUtility>Confuse</style>. Drive an elbow into a close enemy for <style=cIsDamage>250% damage</style>. " +
                "It turns on its allies for <style=cIsUtility>3s</style>.");

            Add("UTILITY_NAME", "Vapour Vault");
            Add("UTILITY_DESC",
                "Smash a fume vial at your feet and <style=cIsUtility>vault backwards</style>. The cloud <style=cIsUtility>slows enemies by 40%</style> for 5s " +
                "and you gain a burst of <style=cIsUtility>cloaked speed</style>.");

            Add("SPECIAL_NAME", "Transmute");
            Add("SPECIAL_DESC",
                "Detonate <style=cIsDamage>all of your damage over time</style> on enemies within <style=cIsUtility>25m</style>. " +
                "They instantly take <style=cIsDamage>150%</style> of the damage it had left, plus <style=cIsDamage>300% damage</style>.");

            // ---- Potions ----
            foreach (PotionDef potion in PotionCatalog.Potions)
            {
                LanguageAPI.Add(potion.NameToken, potion.name);
                LanguageAPI.Add(potion.DescToken, potion.description);
            }

            // ---- Clockwork Crow ----
            Add("ITEM_CROW_NAME", "Clockwork Crow");
            Add("ITEM_CROW_PICKUP", "A brass familiar that worsens your enemies' ailments.");
            Add("ITEM_CROW_DESC",
                "Every <style=cIsUtility>3.5s</style> the crow pecks the most afflicted enemy within <style=cIsUtility>45m</style> for " +
                "<style=cIsDamage>150% damage</style>, adding <style=cIsDamage>one more stack of every damage over time</style> on it " +
                "and extending its debuffs by <style=cIsUtility>1s</style>. <style=cStack>Alchemist only. Cannot be scrapped.</style>");
            Add("ITEM_CROW_LORE", "\"It doesn't eat. It doesn't sleep. It does, apparently, keep notes.\"");

            // ---- Skins ----
            foreach (AlchemistSkins.SkinInfo skin in AlchemistSkins.Skins)
                Add("SKIN_" + skin.key + "_NAME", skin.name);

            // ---- Achievements (skin unlocks) ----
            AddAchievement("PLAGUEWARDEN", AlchemistMasteryAchievement.Id,
                "Alchemist: Mastery", "As Alchemist, beat the game or obliterate on Monsoon.");
            AddAchievement("VERDIGRIS", AlchemistDebuffAchievement.Id,
                "Alchemist: Compound Interest", "As Alchemist, have 5 different debuffs on a single enemy at once.");
            AddAchievement("ASHEN", AlchemistTransmuteAchievement.Id,
                "Alchemist: Mass Conversion", "As Alchemist, kill 5 enemies with a single Transmute.");
        }

        private static void AddAchievement(string skinKey, string achievementId, string name, string desc)
        {
            Add("ACH_" + skinKey + "_NAME", name);
            Add("ACH_" + skinKey + "_DESC", desc);
            LanguageAPI.Add("ACHIEVEMENT_" + achievementId.ToUpperInvariant() + "_NAME", name);
            LanguageAPI.Add("ACHIEVEMENT_" + achievementId.ToUpperInvariant() + "_DESCRIPTION", desc);
        }
    }
}
