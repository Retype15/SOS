// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;

namespace SOS
{
    public static class RecipeAnalyzer
    {
        public static List<FabricationRecipe> GetCraftingRecipes(ItemPrefab item)
            => item.FabricationRecipes?.Values.ToList() ?? new List<FabricationRecipe>();

        public static List<DeconstructItem> GetDeconstructionOutputs(ItemPrefab item)
            => item.DeconstructItems.IsDefaultOrEmpty ? new List<DeconstructItem>() : item.DeconstructItems.ToList();

        public static List<Tuple<ItemPrefab, FabricationRecipe>> GetUsesAsIngredient(ItemPrefab targetItem)
        {
            var results = new List<Tuple<ItemPrefab, FabricationRecipe>>();
            foreach (var prefab in ItemPrefab.Prefabs)
            {
                if (prefab.FabricationRecipes == null) continue;
                foreach (var recipe in prefab.FabricationRecipes.Values)
                {
                    if (recipe.RequiredItems.Length > 0 && recipe.RequiredItems.Any(req => req.ItemPrefabs != null && req.ItemPrefabs.Any(p => p != null && p.Identifier == targetItem.Identifier)))
                    {
                        results.Add(new Tuple<ItemPrefab, FabricationRecipe>(prefab, recipe));
                    }
                }
            }
            return results;
        }

        public static List<ItemPrefab> GetSourcesFromDeconstruction(ItemPrefab targetItem)
        {
            var results = new List<ItemPrefab>();
            foreach (var prefab in ItemPrefab.Prefabs)
            {
                if (!prefab.DeconstructItems.IsDefaultOrEmpty && prefab.DeconstructItems.Any(di => di.ItemIdentifier == targetItem.Identifier))
                {
                    results.Add(prefab);
                }
            }
            return results;
        }
    }

}
