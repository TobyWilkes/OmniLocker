using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace OmniLocker.patches
{

    [HarmonyPatch(typeof(TooltipFactory), nameof(TooltipFactory.WriteIngredients))]
    public static class TooltipFactoryPatch
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