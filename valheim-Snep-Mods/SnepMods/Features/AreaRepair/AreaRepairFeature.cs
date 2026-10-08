using BepInEx.Configuration;
using UnityEngine;

namespace SnepMods.Features.AreaRepair
{
    // Config + toggle logic for area repair. The actual repair logic lives in AreaRepairPatch.
    internal static class AreaRepairFeature
    {
        private const string Category = "AreaRepair";

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyboardShortcut> ToggleKey;
        public static ConfigEntry<float> Radius;

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind(
                Category,
                "Enabled",
                true,
                "When enabled, repairing a piece with the hammer also repairs damaged pieces around it.");

            ToggleKey = config.Bind(
                Category,
                "ToggleKey",
                new KeyboardShortcut(KeyCode.F9),
                "Key to toggle area repair on/off while in-game.");

            Radius = config.Bind(
                Category,
                "Radius",
                5f,
                new ConfigDescription("Radius (in meters) around the repaired piece to also repair.", new AcceptableValueRange<float>(1f, 20f)));
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
                    "Area Repair: " + (Enabled.Value ? "On" : "Off"));
            }
        }
    }
}
