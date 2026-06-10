using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace OmniLocker.patches
{

    [HarmonyPatch(typeof(CrafterLogic))]
    public static class CrafterLogicPatch
    {
        [HarmonyPatch(nameof(CrafterLogic.IsCraftRecipeFulfilled))]
        [HarmonyPrefix]
        public static bool IsCraftRecipeFulfilled_Prefix(TechType techType, ref bool __result)
        {
            if (Inventory.main == null)
            {
                return false;
            }
            if (!GameModeUtils.RequiresIngredients())
            {
                return true;
            }

            var remainder = InventoryPatch.GetRecipeItemsMissing(techType);
            if (remainder.Count == 0)
            {
                Plugin.Logger.LogInfo($"{techType}: Can be made from inventory");
                __result = true;
                return false;
            }

            var plan = LocalStorageSearch.GetPlanForIngredients(remainder);
            __result = (plan != null);

            return false;
        }

        [HarmonyPatch(nameof(CrafterLogic.ConsumeResources))]
        [HarmonyPrefix]
        public static bool ConsumeResources_Prefix(TechType techType, ref bool __result)
        {
            var remainder = InventoryPatch.GetRecipeItemsMissing(techType);
            if (remainder.Count == 0)
            {
                // Use original method if consume can be resolved within inventory.
                __result = true;
                return true;
            }

            var plan = LocalStorageSearch.GetPlanForIngredients(remainder);
            if (plan != null)
            {
                Plugin.Logger.LogInfo($"Building {techType}");

                InventoryPatch.RemovePartsOfRecipeAvailable(techType);

                // Remove the required plan from the surrounding containers.
                foreach (var removal in plan)
                {
                    IList<InventoryItem> items = removal.Container.container.GetItems(removal.Type);
                    List<InventoryItem> itemsSnapshot = items.ToList();

                    int toRemove = removal.Amount;

                    foreach (var item in itemsSnapshot)
                    {
                        if (toRemove <= 0)
                            break;

                        Pickupable pickupable = item.item;

                        if (((IItemsContainer)removal.Container.container).AllowedToRemove(pickupable, false))
                        {
                            toRemove--;
                            removal.Container.container.RemoveItem(pickupable, true);
                        }
                    }
                }
                ErrorMessage.AddMessage("Crafted from surrounding containers");

                __result = true;
                return false;
            }

            Plugin.Logger.LogInfo($"Cannot build {techType}");
            __result = false;
            return true;
        }
    }
}
