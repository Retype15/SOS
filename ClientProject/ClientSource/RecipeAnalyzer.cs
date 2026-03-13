// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SOS
{
    // MARK: RecipeAnalyzer
    public static class RecipeAnalyzer
    {
        private static readonly Dictionary<Identifier, List<Tuple<ItemPrefab, FabricationRecipe>>> usesCache = new Dictionary<Identifier, List<Tuple<ItemPrefab, FabricationRecipe>>>();
        private static readonly Dictionary<Identifier, List<ItemPrefab>> sourcesCache = new Dictionary<Identifier, List<ItemPrefab>>();

        private const int MaxAnalysisCacheSize = 30;
        private static readonly Dictionary<Identifier, ItemAnalysis> analysisCache = new Dictionary<Identifier, ItemAnalysis>();
        private static readonly Queue<Identifier> analysisCacheOrder = new Queue<Identifier>();

        public static ItemAnalysis GetAnalysis(ItemPrefab item)
        {
            if (item == null) return null;

            if (analysisCache.TryGetValue(item.Identifier, out var cachedAnalysis))
            {
                UpdateCachePriority(item.Identifier);
                return cachedAnalysis;
            }

            var analysis = new ItemAnalysis(item);

            if (analysisCache.Count >= MaxAnalysisCacheSize)
            {
                Identifier oldest = analysisCacheOrder.Dequeue();
                analysisCache.Remove(oldest);
            }

            analysisCache[item.Identifier] = analysis;
            analysisCacheOrder.Enqueue(item.Identifier);
            return analysis;
        }

        private static void UpdateCachePriority(Identifier id)
        {
            var list = analysisCacheOrder.ToList();
            if (list.Remove(id))
            {
                analysisCacheOrder.Clear();
                foreach (var item in list) analysisCacheOrder.Enqueue(item);
                analysisCacheOrder.Enqueue(id);
            }
        }

        public static void ClearSessionCache()
        {
            analysisCache.Clear();
            analysisCacheOrder.Clear();
            usesCache.Clear();
            sourcesCache.Clear();
        }

        // MARK: - consults

        public static List<FabricationRecipe> GetCraftingRecipes(ItemPrefab item)
            => item.FabricationRecipes?.Values.ToList() ?? new List<FabricationRecipe>();

        public static List<DeconstructItem> GetDeconstructionOutputs(ItemPrefab item)
            => item.DeconstructItems.IsDefaultOrEmpty ? new List<DeconstructItem>() : item.DeconstructItems.ToList();

        public static List<Tuple<ItemPrefab, FabricationRecipe>> GetUsesAsIngredient(ItemPrefab targetItem)
        {
            if (targetItem == null) return new List<Tuple<ItemPrefab, FabricationRecipe>>();

            if (usesCache.TryGetValue(targetItem.Identifier, out var cachedResult)) return cachedResult;

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

            usesCache[targetItem.Identifier] = results;
            return results;
        }

        public static List<ItemPrefab> GetSourcesFromDeconstruction(ItemPrefab targetItem)
        {
            if (targetItem == null) return new List<ItemPrefab>();

            if (sourcesCache.TryGetValue(targetItem.Identifier, out var cachedResult)) return cachedResult;

            var results = new List<ItemPrefab>();
            foreach (var prefab in ItemPrefab.Prefabs)
            {
                if (!prefab.DeconstructItems.IsDefaultOrEmpty && prefab.DeconstructItems.Any(di => di.ItemIdentifier == targetItem.Identifier))
                {
                    results.Add(prefab);
                }
            }

            sourcesCache[targetItem.Identifier] = results;
            return results;
        }
    }
}