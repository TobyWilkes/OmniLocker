
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static HandReticle;

namespace OmniLocker
{
    public class LocalStorageSearch
    {
        private static float lastUpdated = -1f;

        private static List<StorageContainer> storages = new();
        public static void RefreshLocalStorage()
        {
            float now = Time.time;
            if (now - lastUpdated <= 1) return;

            StorageContainer[] candidates = UnityEngine.Object.FindObjectsOfType<StorageContainer>();
            storages.Clear();

            foreach (var storage in candidates)
            {
                float distance = UnityEngine.Vector3.Distance(
                    Player.main.transform.position,
                    storage.transform.position
                );

                if (distance <= 25f)
                {
                    storages.Add(storage);
                }
            }

            Plugin.Logger.LogInfo(
                $"Found {storages.Count} nearby containers"
            );

            lastUpdated = now;
        }

        public static int GetRemovableAmountFromContainer(StorageContainer container, TechType type)
        {
            // Not sure what the AllowedToRemove flag is for, but respect it to be safe.
            IList<InventoryItem> items = container.container.GetItems(type);
            int removable = 0;
            foreach (var item in items)
            {
                Pickupable pickupable = item.item;

                if (((IItemsContainer)container.container).AllowedToRemove(pickupable, false))
                {
                    removable++;
                }
            }
            return removable;
        }

        public static List<ItemRemovalCandidate> GetLocal(TechType type)
        {
            List<ItemRemovalCandidate> output = new();

            foreach (var storage in storages)
            {
                if (storage.container.GetCount(type) == 0) continue;

                // Not sure what the AllowedToRemove flag is for, but respect it to be safe.
                IList<InventoryItem> items = storage.container.GetItems(type);
                int removable = LocalStorageSearch.GetRemovableAmountFromContainer(storage, type);
                if (removable > 0)
                {
                    output.Add(new ItemRemovalCandidate
                    {
                        Amount = removable,
                        Container = storage,
                        Type = type
                     });
                }
            }
            return output;
        }

        public static bool ExistsInLocalStorage(Ingredient ingredient)
        {
            return LocalStorageSearch.GetPlanForIngredients(
                 new List<Ingredient> { ingredient }
             ) != null;
        }

        public static List<ItemRemovalCandidate> GetPlanForIngredients(List<Ingredient> ingredients)
        {
            LocalStorageSearch.RefreshLocalStorage();

            List<ItemRemovalCandidate> containers = new();

            foreach (var ingredient in ingredients)
            {
                var local = LocalStorageSearch.GetLocal(ingredient.techType);

                // todo: replace with transform
                int remainder = ingredient.amount;
                foreach (var candidate in local)
                {

                    int toTake = Math.Min(candidate.Amount, remainder);
                    if (toTake == 0) continue;

                    containers.Add(new ItemRemovalCandidate
                    {
                        Amount = toTake,
                        Container = candidate.Container,
                        Type = ingredient.techType
                    });

                    remainder -= toTake;
                }

                if (remainder > 0)
                {
                    return null;
                }
            }

            return containers;
        }
    }

    public class ItemRemovalCandidate
    {
        public StorageContainer Container { get; set; }
        public TechType Type { get; set; }
        public int Amount { get; set; }
    }
}