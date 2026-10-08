using HarmonyLib;

namespace SnepMods.Features.FloatingItems
{
    // Vanilla's own Floating component (used on boats, message-in-a-bottle, etc.) already
    // handles buoyancy against WaterVolume, gated by ZNetView.IsOwner() - it only ever
    // acts on items the local client currently has physics ownership of. Most dropped
    // items don't ship with a Floating component, so we add one ourselves right after
    // ItemDrop finishes its own Awake (Rigidbody/Collider/ZNetView already exist by then).
    //
    // On a shared server, ownership would otherwise only reach us via vanilla's passive
    // ~2s proximity sweep (ZDOMan.ReleaseNearbyZDOS) - which can just as easily hand it
    // to a nearer player without this mod, letting it sink for good before we ever get a
    // turn. RequestOwn() uses the same RPC vanilla uses for pickup to actively claim
    // ownership right away instead of waiting on that sweep.
    [HarmonyPatch(typeof(ItemDrop), "Awake")]
    internal static class FloatingItemsPatch
    {
        private static void Postfix(ItemDrop __instance)
        {
            if (!FloatingItemsFeature.Enabled.Value)
            {
                return;
            }

            // Items are also instantiated outside the networked world (e.g. the main-menu
            // character preview loading the player's inventory) with no ZDO behind their
            // ZNetView. Vanilla's own Awake skips those the same way; RequestOwn() would
            // otherwise NRE inside ZNetView.InvokeRPC.
            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid() || ZRoutedRpc.instance == null)
            {
                return;
            }

            if (__instance.GetComponent<Floating>() == null)
            {
                Floating floating = __instance.gameObject.AddComponent<Floating>();
                floating.m_force = FloatingItemsFeature.Force.Value;
            }

            __instance.RequestOwn();
        }
    }
}
