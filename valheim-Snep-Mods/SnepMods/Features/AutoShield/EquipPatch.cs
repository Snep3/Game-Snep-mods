using System.Collections.Generic;
using HarmonyLib;

namespace SnepMods.Features.AutoShield
{
    // Auto re-equips the player's last shield when they switch to a one-handed weapon,
    // mirroring the game's own "keep shield when swapping weapons" case for Torch.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    internal static class EquipPatch
    {
        private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result)
        {
            if (!__result || !AutoShieldFeature.Enabled.Value)
            {
                return;
            }

            if (!(__instance is Player player) || player != Player.m_localPlayer)
            {
                return;
            }

            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                AutoShieldFeature.LastShieldName = item.m_shared.m_name;
                return;
            }

            if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.OneHandedWeapon)
            {
                return;
            }

            if (player.LeftItem != null || string.IsNullOrEmpty(AutoShieldFeature.LastShieldName))
            {
                return;
            }

            List<ItemDrop.ItemData> items = player.GetInventory().GetAllItems();
            foreach (ItemDrop.ItemData candidate in items)
            {
                if (candidate.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield
                    && candidate.m_shared.m_name == AutoShieldFeature.LastShieldName)
                {
                    player.EquipItem(candidate);
                    break;
                }
            }
        }
    }
}
