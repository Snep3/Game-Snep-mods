using BepInEx;
using HarmonyLib;
using SnepMods.Features.AreaRepair;
using SnepMods.Features.AutoShield;
using SnepMods.Features.Compass;
using SnepMods.Features.FloatingItems;
using SnepMods.UI;

namespace SnepMods
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class SnepModsPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "snep.valheim.snepmods";
        public const string PluginName = "SnepMods";
        public const string PluginVersion = "1.2.2";

        private Harmony _harmony;

        private void Awake()
        {
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll();

            AutoShieldFeature.Init(Config);
            CompassFeature.Init(Config);
            AreaRepairFeature.Init(Config);
            FloatingItemsFeature.Init(Config);
            ModMenu.Init(Config);
        }

        private void Update()
        {
            AutoShieldFeature.Update();
            CompassFeature.Update();
            AreaRepairFeature.Update();
            FloatingItemsFeature.Update();
            ModMenu.Update();
        }

        private void OnGUI()
        {
            CompassHud.OnGUI();
            ModMenu.OnGUI();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
