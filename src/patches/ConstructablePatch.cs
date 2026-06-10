using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace OmniLocker.patches
{
    [HarmonyPatch(typeof(Constructable), nameof(Constructable.Construct))]
    public static class ConstructablePatch
    {
        public static void PostConstructUpdate(Constructable __instance)
        {
            __instance.UpdateMaterial();
            if (__instance.constructedAmount >= 1f)
            {
                __instance.SetState(true, true);
            }
        }

        [HarmonyPrefix]
        public static bool Construct_Prefix(Constructable __instance, ref bool __result)
        {
            if (__instance._constructed)
            {
                __result = false;
                return false;
            }

            // Get previous and resource used at current moment.
            int count = __instance.resourceMap.Count;
            int resourceID = __instance.GetResourceID();
            __instance.constructedAmount += Time.deltaTime / ((float)count * Constructable.GetConstructInterval());
            __instance.constructedAmount = Mathf.Clamp01(__instance.constructedAmount);
            int resourceID2 = __instance.GetResourceID();

            // If we're onto a new step, try to consume it.
            if (resourceID2 != resourceID)
            {
                TechType destroyTechType = __instance.resourceMap[resourceID2 - 1];

                if (Inventory.main.DestroyItem(destroyTechType, false) || !GameModeUtils.RequiresIngredients())
                {
                    ConstructablePatch.PostConstructUpdate(__instance);
                    __result = true;
                    return false;
                }

                var localResources = LocalStorageSearch.GetLocal(destroyTechType);
                foreach (var candidate in localResources)
                {
                    IList<InventoryItem> items = candidate.Container.container.GetItems(destroyTechType);
                    foreach (var item in items)
                    {
                        if (((IItemsContainer)candidate.Container.container).AllowedToRemove(item.item, false))
                        {
                            candidate.Container.container.RemoveItem(item.item, true);

                            ConstructablePatch.PostConstructUpdate(__instance);
                            __result = true;
                            return false;
                        }
                    }
                }


                // On failure, reset progress to the start of the current step.
                __instance.constructedAmount = (float)resourceID / (float)count;
                __result = false;
                return false;
            }

            ConstructablePatch.PostConstructUpdate(__instance);
            __result = true;
            return false;
        }
    }
}
