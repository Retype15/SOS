// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;

namespace SOS
{
    // MARK: RecipeAnalyzer
    public static class RecipeAnalyzer
    {
        private static readonly Dictionary<Identifier, LinkedListNode<ItemAnalysis>> analysisCache = [];
        private static readonly LinkedList<ItemAnalysis> lruList = new();
        private static readonly Dictionary<Identifier, List<Tuple<ItemPrefab, FabricationRecipe>>> usesCache = [];
        private static readonly Dictionary<Identifier, List<Tuple<ItemPrefab, DeconstructItem>>> sourcesCache = [];

        private const int MaxAnalysisCacheSize = 30;

        public static ItemAnalysis? GetAnalysis(ItemPrefab? item)
        {
            if (item == null) return null;

            if (analysisCache.TryGetValue(item.Identifier, out var node))
            {
                lruList.Remove(node);
                lruList.AddFirst(node);
                return node.Value;
            }

            var analysis = new ItemAnalysis(item);

            if (analysisCache.Count >= MaxAnalysisCacheSize)
            {
                var lastNode = lruList.Last;
                if (lastNode != null)
                {
                    analysisCache.Remove(lastNode.Value.ItemId);
                    lruList.RemoveLast();
                }
            }

            var newNode = new LinkedListNode<ItemAnalysis>(analysis);
            lruList.AddFirst(newNode);
            analysisCache[item.Identifier] = newNode;

            return analysis;
        }

        public static void ClearSessionCache()
        {
            analysisCache.Clear();
            lruList.Clear();
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

        public static List<Tuple<ItemPrefab, DeconstructItem>> GetSourcesFromDeconstruction(ItemPrefab targetItem)
        {
            if (targetItem == null) return new List<Tuple<ItemPrefab, DeconstructItem>>();

            if (sourcesCache.TryGetValue(targetItem.Identifier, out var cachedResult)) return cachedResult;

            var results = new List<Tuple<ItemPrefab, DeconstructItem>>();
            foreach (var prefab in ItemPrefab.Prefabs)
            {
                if (prefab.DeconstructItems.IsDefaultOrEmpty) continue;

                foreach (var di in prefab.DeconstructItems)
                {
                    if (di.ItemIdentifier == targetItem.Identifier)
                    {
                        results.Add(new Tuple<ItemPrefab, DeconstructItem>(prefab, di));
                    }
                }
            }

            sourcesCache[targetItem.Identifier] = results;
            return results;
        }
    }
}