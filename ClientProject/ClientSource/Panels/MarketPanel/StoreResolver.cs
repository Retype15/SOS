// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Globalization;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.Panels.MarketPanel
{
    internal static class StoreResolver
    {
        private static readonly Dictionary<Identifier, Identifier> MerchantJobs = new()
        {
            ["merchantarmory".ToIdentifier()] = "securityofficer".ToIdentifier(),
            ["merchantmedical".ToIdentifier()] = "medicaldoctor".ToIdentifier(),
            ["merchantengineering".ToIdentifier()] = "engineer".ToIdentifier(),
            ["merchantmining".ToIdentifier()] = "mechanic".ToIdentifier(),
            ["merchantfuel".ToIdentifier()] = "engineer".ToIdentifier(),
            ["merchantoutpost".ToIdentifier()] = "assistant".ToIdentifier(),
            ["merchantgeneral".ToIdentifier()] = "assistant".ToIdentifier(),
            ["merchantclown".ToIdentifier()] = "assistant".ToIdentifier(),
            ["merchantsubmarine".ToIdentifier()] = "captain".ToIdentifier(),
            ["merchantresearch".ToIdentifier()] = "medicaldoctor".ToIdentifier()
        };

        /// <summary>
        /// Returns every store identifier that can exist in Europa, sorted by display name
        /// </summary>
        public static IReadOnlyList<Identifier> GetStoreCatalog(ItemPrefab item)
        {
            var catalog = new HashSet<Identifier>();

            foreach (var outpost in OutpostGenerationParams.OutpostParams)
            {
                catalog.UnionWith(outpost.GetStoreIdentifiers());
            }

            if (item.StorePrices != null)
            {
                catalog.UnionWith(item.StorePrices.Keys);
            }

            catalog.Remove(Identifier.Empty);

            return [.. catalog
                .Select(id => (Id: id, Name: ResolveStoreName(id)))
                .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .Select(entry => entry.Id)];
        }

        /// <summary>
        /// REturns the outpost types where the given store can appear.
        /// </summary>
        public static IReadOnlyList<LocationType> GetOutpostTypes(Identifier storeId)
        {
            var types = new List<LocationType>();

            foreach (var outpost in OutpostGenerationParams.OutpostParams)
            {
                if (!outpost.GetStoreIdentifiers().Contains(storeId)) { continue; }

                foreach (var typeId in outpost.AllowedLocationTypes)
                {
                    if (LocationType.Prefabs.TryGet(typeId, out var type) && !types.Contains(type))
                    {
                        types.Add(type);
                    }
                }
            }

            if (types.Count == 0)
            {
                types.AddRange(LocationType.Prefabs.Where(static type => type.HasOutpost));
            }

            return types;
        }

        /// <summary>
        /// Resolves the localized vanilla name of a store.
        /// </summary>
        public static string ResolveStoreName(Identifier storeId)
        {
            if (storeId.IsEmpty) { return TextManager.Get("store").Value; }

            var localized = TextManager.Get($"storename.{storeId}");
            return !localized.IsNullOrEmpty() ? localized.Value : Beautify(storeId);
        }

        /// <summary>
        /// Resolves the icon and tint used to represent a store
        /// </summary>
        public static (Sprite? Sprite, Color Color) ResolveMerchantVisuals(Identifier storeId)
        {
            if (MerchantJobs.TryGetValue(storeId, out var jobId) &&
                JobPrefab.Prefabs.TryGet(jobId, out var job) &&
                job.Icon is { } jobIcon)
            {
                return (jobIcon, job.UIColor);
            }

            Sprite? fallbackSprite = GUIStyle.GetComponentStyle("StoreTradingIcon")?.GetDefaultSprite();
            return (fallbackSprite, Color.Gold);
        }

        /// <summary>
        /// Returns the price info the given store uses for the item otherwise the default.
        /// </summary>
        public static PriceInfo? GetStorePriceInfo(ItemPrefab item, Identifier storeId)
        {
            if (!storeId.IsEmpty && item.StorePrices != null && item.StorePrices.TryGetValue(storeId, out var storePrice))
            {
                return storePrice;
            }

            return item.DefaultPrice;
        }

        /// <summary>
        /// Returns the most representative price info of the item.
        /// </summary>
        public static PriceInfo? ResolveBasePriceInfo(ItemPrefab item)
        {
            if (item.DefaultPrice is { Price: > 0 } defaultPrice) { return defaultPrice; }
            if (item.StorePrices == null) { return null; }

            PriceInfo? cheapest = null;
            foreach (var priceInfo in item.StorePrices.Values)
            {
                if (priceInfo is not { Price: > 0 }) { continue; }
                if (cheapest == null || priceInfo.Price < cheapest.Price) { cheapest = priceInfo; }
            }

            return cheapest;
        }

        private static string Beautify(Identifier storeId)
        {
            string raw = storeId.Value;
            if (raw.StartsWith("merchant", StringComparison.OrdinalIgnoreCase) && raw.Length > "merchant".Length)
            {
                raw = raw["merchant".Length..];
            }

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(raw.Replace('_', ' '));
        }
    }
}