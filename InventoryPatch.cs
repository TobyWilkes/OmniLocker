
using static HandReticle;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;

namespace OmniLocker
{
    public class InventoryPatch
    {
        public static void RemoveItems(TechType techType, int amount)
        {
            if (!GameModeUtils.RequiresIngredients()) return;

            int i = 0;
            while (i < amount)
            {
                if (!Inventory.main.DestroyItem(techType, false))
                {
                    Plugin.Logger.LogInfo($"[PocketLocker] Unable to remove one '{techType}' from player inventory");
                } else
                {
                    amount--;
                }
            }
        }

        public static void RemovePartsOfRecipeAvailable(TechType recipe)
        {
            Inventory main = Inventory.main;
            ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(recipe);

            foreach (var ingredient in ingredients)
            {
                int inventoryCount = main.GetPickupCount(ingredient.techType);
                int toRemove = Math.Min(ingredient.amount, inventoryCount);

                if (toRemove > 0)
                {
                    Plugin.Logger.LogInfo($"Taking {toRemove} {ingredient.techType} from inventory");
                    InventoryPatch.RemoveItems(ingredient.techType, toRemove);
                }
            }
        }

        public static List<Ingredient> GetRecipeItemsMissing(TechType recipe)
        {
            Inventory main = Inventory.main;
            ReadOnlyCollection<Ingredient> ingredients = TechData.GetIngredients(recipe);
            List<Ingredient> remainder = new();

            foreach (var ingredient in ingredients)
            {
                int inventoryCount = main.GetPickupCount(ingredient.techType);
                int missing = Math.Max(ingredient.amount - inventoryCount, 0);

                if (missing == 0) continue;

                remainder.Add(new Ingredient(ingredient.techType, missing));
            }

            return remainder;
        }
    }
}