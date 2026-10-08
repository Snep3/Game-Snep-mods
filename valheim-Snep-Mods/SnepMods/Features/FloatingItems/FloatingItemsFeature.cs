using BepInEx.Configuration;
using UnityEngine;

namespace SnepMods.Features.FloatingItems
{
    // Config + toggle logic for floating items. The actual component-adding logic lives in FloatingItemsPatch.
    internal static class FloatingItemsFeature
    {
        private const string Category = "FloatingItems";

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyboardShortcut> ToggleKey;
        public static ConfigEntry<float> Force;

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind(
                Category,
                "Enabled",
                true,
                "When enabled, dropped items float on water instead of sinking. Only affects items your client currently owns physics for (usually whichever items are nearest to you).");

            ToggleKey = config.Bind(
                Category,
                "ToggleKey",
                new KeyboardShortcut(KeyCode.F10),
                "Key to toggle floating items on/off while in-game.");

            Force = config.Bind(
                Category,
                "Force",
                0.5f,
                new ConfigDescription("Buoyancy strength pushing floating items back to the surface.", new AcceptableValueRange<float>(0.1f, 2f)));
        }

        public static void Update()
        {
            if (!ToggleKey.Value.IsDown())
            {
                return;
            }

            Enabled.Value = !Enabled.Value;
            if ((bool)MessageHud.instance)
            {
                MessageHud.instance.ShowMessage(
                    MessageHud.MessageType.Center,
                    "Floating Items: " + (Enabled.Value ? "On" : "Off"));
            }
        }
    }
}
