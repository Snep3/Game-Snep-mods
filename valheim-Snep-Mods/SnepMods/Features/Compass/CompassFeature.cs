using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace SnepMods.Features.Compass
{
    // Config + toggle logic for the compass HUD. Rendering lives in CompassHud.
    internal static class CompassFeature
    {
        private const string Category = "Compass";

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyboardShortcut> ToggleKey;
        public static ConfigEntry<float> Width;
        public static ConfigEntry<float> Height;

        // Best-guess labels for the 5 player-placeable pin icons (Icon0-Icon4) -
        // their actual meaning is set on art assets, not in code, so if a label
        // turns out wrong once you see it in-game, just reorder this array.
        private static readonly (Minimap.PinType Type, string Key, string Description)[] MarkerDefs =
        {
            (Minimap.PinType.Death, "ShowDeathMarkers", "Show death markers on the compass."),
            (Minimap.PinType.Boss, "ShowBossMarkers", "Show boss markers on the compass."),
            (Minimap.PinType.Player, "ShowPlayers", "Show other players on the compass (requires their position to be shared)."),
            (Minimap.PinType.Icon0, "ShowCampfireMarkers", "Show campfire pins on the compass."),
            (Minimap.PinType.Icon1, "ShowHomeMarkers", "Show home pins on the compass."),
            (Minimap.PinType.Icon2, "ShowHammerMarkers", "Show crafting pins on the compass."),
            (Minimap.PinType.Icon3, "ShowCircleMarkers", "Show circle pins on the compass."),
            (Minimap.PinType.Icon4, "ShowPortalMarkers", "Show portal pins on the compass."),
        };

        private static readonly Dictionary<Minimap.PinType, ConfigEntry<bool>> MarkerToggles = new Dictionary<Minimap.PinType, ConfigEntry<bool>>();

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind(
                Category,
                "Enabled",
                true,
                "Show the compass HUD.");

            ToggleKey = config.Bind(
                Category,
                "ToggleKey",
                new KeyboardShortcut(KeyCode.F8),
                "Key to toggle the compass on/off while in-game.");

            Width = config.Bind(
                Category,
                "Width",
                420f,
                new ConfigDescription("Width of the compass bar, in pixels.", new AcceptableValueRange<float>(200f, 1000f)));

            Height = config.Bind(
                Category,
                "Height",
                36f,
                new ConfigDescription("Height of the compass bar, in pixels.", new AcceptableValueRange<float>(20f, 100f)));

            foreach ((Minimap.PinType type, string key, string description) in MarkerDefs)
            {
                MarkerToggles[type] = config.Bind(Category, key, true, description);
            }
        }

        public static bool IsMarkerVisible(Minimap.PinType type)
        {
            return !MarkerToggles.TryGetValue(type, out ConfigEntry<bool> entry) || entry.Value;
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
                    "Compass: " + (Enabled.Value ? "On" : "Off"));
            }
        }
    }
}
