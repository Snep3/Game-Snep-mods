using BepInEx.Configuration;
using UnityEngine;

namespace SnepMods.Features.AutoShield
{
    // Auto re-equips the player's last shield when they switch to a one-handed weapon.
    internal static class AutoShieldFeature
    {
        private const string Category = "AutoShield";

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyboardShortcut> ToggleKey;

        // Remembers, per local session, the prefab name of the last shield the player
        // deliberately equipped, so it can be restored when switching weapons.
        internal static string LastShieldName;

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind(
                Category,
                "Enabled",
                true,
                "When enabled, equipping a one-handed weapon will automatically re-equip your last shield.");

            ToggleKey = config.Bind(
                Category,
                "ToggleKey",
                new KeyboardShortcut(KeyCode.F7),
                "Key to toggle AutoShield on/off while in-game.");
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
                    "AutoShield: " + (Enabled.Value ? "On" : "Off"));
            }
        }
    }
}
