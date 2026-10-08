using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SnepMods.Features.AreaRepair
{
    // After the game repairs whatever piece you're pointing at, also repair any
    // damaged pieces within a radius of it.
    [HarmonyPatch(typeof(Player), "Repair")]
    internal static class AreaRepairPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!AreaRepairFeature.Enabled.Value)
            {
                return;
            }

            Piece center = __instance.GetHoveringPiece();
            if (!center)
            {
                return;
            }

            var repairedTears = new HashSet<WearNTear>();
            Collider[] hits = Physics.OverlapSphere(center.transform.position, AreaRepairFeature.Radius.Value);
            int repairedCount = 0;

            foreach (Collider hit in hits)
            {
                WearNTear wearNTear = hit.GetComponentInParent<WearNTear>();
                if (wearNTear == null || !repairedTears.Add(wearNTear))
                {
                    continue;
                }

                if (!PrivateArea.CheckAccess(wearNTear.transform.position, 0f, flash: false))
                {
                    continue;
                }

                if (wearNTear.Repair())
                {
                    repairedCount++;
                }
            }

            if (repairedCount > 0 && (bool)MessageHud.instance)
            {
                MessageHud.instance.ShowMessage(
                    MessageHud.MessageType.TopLeft,
                    "Area repaired " + repairedCount + " nearby piece(s)");
            }
        }
    }
}
