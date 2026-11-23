using BepInEx.Configuration;
using UnityEngine;

namespace AlchemistMod
{
    /// <summary>Player-facing settings in BepInEx/config/swamie.Alchemist.cfg.</summary>
    internal static class AlchemistConfig
    {
        public static ConfigEntry<bool> ScrollWheelSwitching;
        public static ConfigEntry<bool> InvertScroll;
        public static ConfigEntry<KeyboardShortcut> NextPotionKey;
        public static ConfigEntry<KeyboardShortcut> PreviousPotionKey;
        public static ConfigEntry<bool> ShowPotionHud;

        public static void Init(ConfigFile config)
        {
            ScrollWheelSwitching = config.Bind("Controls", "Scroll wheel switching", true,
                "Scroll the mouse wheel to cycle through your three equipped potions.");
            InvertScroll = config.Bind("Controls", "Invert scroll", false,
                "Flip the scroll direction.");
            NextPotionKey = config.Bind("Controls", "Next potion key", new KeyboardShortcut(KeyCode.None),
                "Optional key to select the next potion (for controllers, bind this with a remapper).");
            PreviousPotionKey = config.Bind("Controls", "Previous potion key", new KeyboardShortcut(KeyCode.None),
                "Optional key to select the previous potion.");
            ShowPotionHud = config.Bind("UI", "Show potion belt", true,
                "Show the three equipped potions and which one is active near the bottom of the screen.");
        }
    }
}
