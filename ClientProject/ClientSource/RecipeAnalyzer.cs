// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0079
#pragma warning disable IDE0130
#pragma warning disable IDE0290

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

        public static ItemAnalysis? GetAnalysis(Prefab? item)
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
                    analysisCache.Remove(lastNode.Value.PrefabId);
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
            => item.FabricationRecipes?.Values.ToList() ?? [];

        public static List<DeconstructItem> GetDeconstructionOutputs(ItemPrefab item)
            => item.DeconstructItems.IsDefaultOrEmpty ? [] : [.. item.DeconstructItems];

        public static List<Tuple<ItemPrefab, FabricationRecipe>> GetUsesAsIngredient(ItemPrefab targetItem)
        {
            if (targetItem == null) return [];

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
            if (targetItem == null) return [];

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
                                usesCache[p.Identifier].Add(new Tuple<ItemPrefab, FabricationRecipe>(prefab, recipe));
                            }
                        }
                    }
                }

                if (!prefab.DeconstructItems.IsDefaultOrEmpty)
                {
                    foreach (var di in prefab.DeconstructItems)
                    {
                        if (!sourcesCache.ContainsKey(di.ItemIdentifier)) sourcesCache[di.ItemIdentifier] = [];
                        sourcesCache[di.ItemIdentifier].Add(new Tuple<ItemPrefab, DeconstructItem>(prefab, di));
                    }
                }
            }
            // MARK: AAAA
#if DEBUG
            //System.Threading.Thread.Sleep(1000);
            LuaCsLogger.LogMessage("[SOS] Dependency graph precomputed (Debug Sleep 3s finished).");
#endif
        }
    }
}