// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;

namespace SOS
{
    // MARK: RecipeAnalyzer
    internal static class RecipeAnalyzer
    {
        private static readonly Dictionary<Identifier, List<(ItemPrefab Item, FabricationRecipe Recipe)>> usesCache = [];
        private static readonly Dictionary<Identifier, List<(ItemPrefab Item, DeconstructItem DeconstructItem)>> sourcesCache = [];

        public static void Clear()
        {
            usesCache.Clear();
            sourcesCache.Clear();
        }

        // MARK: - consults

        public static List<FabricationRecipe> GetCraftingRecipes(ItemPrefab item)
            => item.FabricationRecipes?.Values.ToList() ?? [];

        public static List<DeconstructItem> GetDeconstructionOutputs(ItemPrefab item)
            => item.DeconstructItems.IsDefaultOrEmpty ? [] : [.. item.DeconstructItems];

        public static List<(ItemPrefab Item, FabricationRecipe Recipe)> GetUsesAsIngredient(ItemPrefab targetItem)
        {
            if (targetItem == null) return [];

            if (usesCache.TryGetValue(targetItem.Identifier, out var cachedResult)) return cachedResult;

            var results = new List<(ItemPrefab Item, FabricationRecipe Recipe)>();
            foreach (var prefab in ItemPrefab.Prefabs)
            {
                if (prefab.FabricationRecipes == null) continue;
                foreach (var recipe in prefab.FabricationRecipes.Values)
                {
                    if (recipe.RequiredItems.Length > 0 && recipe.RequiredItems.Any(req => req.ItemPrefabs != null && req.ItemPrefabs.Any(p => p != null && p.Identifier == targetItem.Identifier)))
                    {
                        results.Add((prefab, recipe));
                    }
                }
            }

            usesCache[targetItem.Identifier] = results;
            return results;
        }

        public static List<(ItemPrefab Item, DeconstructItem DeconstructItem)> GetSourcesFromDeconstruction(ItemPrefab targetItem)
        {
            if (targetItem == null) return [];

            if (sourcesCache.TryGetValue(targetItem.Identifier, out var cachedResult)) return cachedResult;

            var results = new List<(ItemPrefab Item, DeconstructItem DeconstructItem)>();
            foreach (var prefab in ItemPrefab.Prefabs)
            {
                if (prefab.DeconstructItems.IsDefaultOrEmpty) continue;

                foreach (var di in prefab.DeconstructItems)
                {
                    if (di.ItemIdentifier == targetItem.Identifier)
                    {
                        results.Add((prefab, di));
                    }
                }
            }

            sourcesCache[targetItem.Identifier] = results;
            return results;
        }

        public static void PrecomputeCaches()
        {
            usesCache.Clear();
            sourcesCache.Clear();

            var allPrefabs = ItemPrefab.Prefabs;

            foreach (var prefab in allPrefabs)
            {
                if (prefab.FabricationRecipes != null)
                {
                    foreach (var recipe in prefab.FabricationRecipes.Values)
                    {
                        foreach (var req in recipe.RequiredItems)
                        {
                            foreach (var p in req.ItemPrefabs)
                            {
                                if (p == null) continue;
                                if (!usesCache.ContainsKey(p.Identifier)) usesCache[p.Identifier] = [];
                                usesCache[p.Identifier].Add((prefab, recipe));
                            }
                        }
                    }
                }

                if (!prefab.DeconstructItems.IsDefaultOrEmpty)
                {
                    foreach (var di in prefab.DeconstructItems)
                    {
                        if (!sourcesCache.ContainsKey(di.ItemIdentifier)) sourcesCache[di.ItemIdentifier] = [];
                        sourcesCache[di.ItemIdentifier].Add((prefab, di));
                    }
                }
            }
            // MARK: AAAA

            Logger.LogDebug("[SOS] Dependency graph precomputed.");
        }
    }
}