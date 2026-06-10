using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using OmniLocker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OmniLocker
{
    [BepInPlugin("com.toby.myfirstplugin", "My First Plugin", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("OmniLocker loaded");

            var harmony = new Harmony("com.toby.OmniLocker");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.IsCraftRecipeFulfilled))]
    public static class CrafterLogicPatch
    {
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
    }

    [HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.ConsumeResources))]
    public static class ConsumeResourcesPatch
    {
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


    [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.WriteIngredients))]
    public static class WriteIngredientsPatch
    {
        [HarmonyPrefix]
        public static bool WriteIngredients_Prefix(IList<Ingredient> ingredients, List<TooltipIcon> icons)
        {
            if (ingredients == null)
            {
                return false;
            }

            Inventory main = Inventory.main;
            StringBuilder stringBuilder = new StringBuilder();

            foreach (var ingredient in ingredients)
            {
                stringBuilder.Length = 0;

                TechType techType = ingredient.techType;
                int requiredAmount = ingredient.amount;
                int inventoryCount = main.GetPickupCount(techType);
                int missingAmount = Math.Max(requiredAmount - inventoryCount, 0);

                bool meetsInventoryRequirement =
                    missingAmount == 0 || !GameModeUtils.RequiresIngredients();

                List<ItemRemovalCandidate> plan = null;

                if (!meetsInventoryRequirement)
                {
                    plan = LocalStorageSearch.GetPlanForIngredients(
                        new List<Ingredient>
                        {
                        new Ingredient(techType, missingAmount)
                        }
                    );
                }

                bool canPullFromLocal = plan != null && plan.Count > 0;

                Sprite sprite = SpriteManager.Get(techType);

                string color = "#94DE00FF";

                if (!meetsInventoryRequirement && !canPullFromLocal)
                {
                    color = "#DF4026FF";
                }

                stringBuilder.Append($"<color={color}>");

                string name = Language.main.GetOrFallback(
                    TooltipFactory.techTypeIngredientStrings.Get(techType),
                    techType
                );

                stringBuilder.Append(name);

                if (requiredAmount > 1)
                {
                    stringBuilder.Append(" x");
                    stringBuilder.Append(requiredAmount);
                }

                if ((inventoryCount > 0 && inventoryCount < requiredAmount) || canPullFromLocal)
                {
                    stringBuilder.Append(" (");
                    stringBuilder.Append(inventoryCount);

                    if (canPullFromLocal)
                    {
                        stringBuilder.Append($"<color=#deaa00FF> + {missingAmount}</color>");
                    }

                    stringBuilder.Append(")");
                }

                stringBuilder.Append("</color>");

                icons.Add(new TooltipIcon(sprite, stringBuilder.ToString()));
            }

            return false;
        }
    }
}