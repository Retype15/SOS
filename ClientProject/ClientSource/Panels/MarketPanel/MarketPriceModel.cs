// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.Panels.MarketPanel
{
    internal enum PurchaseBlock
    {
        None,
        NotSold,
        Difficulty,
        Faction,
        Reputation
    }


    internal sealed class StoreQuote
    {
        public required Identifier StoreId { get; init; }
        public required string Title { get; init; }
        public Sprite? Icon { get; init; }
        public Color IconColor { get; init; } = Color.White;

        public required int BasePrice { get; init; }
        public int BuyMin { get; init; }
        public int BuyMax { get; init; }
        public int SellMin { get; init; }
        public int SellMax { get; init; }

        public PurchaseBlock Block { get; init; }
        public int RequiredDifficulty { get; init; }
        public Identifier RequiredFaction { get; init; }
        public float RequiredReputation { get; init; }

        public int Stock { get; init; } = -1;
        public int Balance { get; init; } = -1;

        public bool IsLive { get; init; }
        public bool IsDailySpecial { get; init; }
        public bool IsRequestedGood { get; init; }

        public IReadOnlyList<string> OutpostTypes { get; init; } = [];
    }

    internal sealed class FactionQuote
    {
        public required string Name { get; init; }
        public required float Reputation { get; init; }
        public required int BasePrice { get; init; }
        public int BuyPrice { get; init; }
        public int SellPrice { get; init; }
        public bool CanBuy { get; init; }
        public bool CanSell { get; init; }
        public Sprite? Icon { get; init; }
        public Color IconColor { get; init; } = Color.White;
    }

    internal static class MarketPriceModel
    {
        public static int? ResolveBasePrice(ItemPrefab item) => StoreResolver.ResolveBasePriceInfo(item)?.Price;

        public static IReadOnlyList<StoreQuote> GetLiveQuotes(ItemPrefab item, Location? location)
        {
            var quotes = new List<StoreQuote>();
            if (location?.Stores == null) { return quotes; }

            var campaign = GameMain.GameSession?.Campaign as CampaignMode;
            var activeStores = StoreResolver.GetStoresWithLiveMerchant();
            bool canSell = item.CanBeSold;

            foreach (var (storeId, store) in location.Stores)
            {
                if (!activeStores.Contains(storeId)) { continue; }
                if (item.GetPriceInfo(store) is not PriceInfo priceInfo) { continue; }

                PurchaseBlock block = GetPurchaseBlock(location, store, priceInfo, campaign);
                int buyPrice = block == PurchaseBlock.None ? store.GetAdjustedItemBuyPrice(item, priceInfo) : 0;
                int sellPrice = canSell ? store.GetAdjustedItemSellPrice(item, priceInfo) : 0;
                var (icon, iconColor) = StoreResolver.ResolveMerchantVisuals(storeId);

                quotes.Add(new StoreQuote
                {
                    StoreId = storeId,
                    Title = StoreResolver.ResolveStoreName(storeId),
                    Icon = icon,
                    IconColor = iconColor,
                    BasePrice = priceInfo.Price,
                    BuyMin = buyPrice,
                    BuyMax = buyPrice,
                    SellMin = sellPrice,
                    SellMax = sellPrice,
                    Block = block,
                    RequiredDifficulty = priceInfo.MinLevelDifficulty,
                    RequiredFaction = priceInfo.RequiredFaction,
                    RequiredReputation = GetRequiredReputation(priceInfo, store.GetMerchantOrLocationFactionIdentifier()),
                    Stock = store.Stock.FirstOrDefault(entry => entry.ItemPrefabIdentifier == item.Identifier)?.Quantity ?? 0,
                    Balance = store.Balance,
                    IsLive = true,
                    IsDailySpecial = store.DailySpecials.Contains(item),
                    IsRequestedGood = store.RequestedGoods.Contains(item)
                });
            }

            quotes.Sort(static (a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            return quotes;
        }

        public static IReadOnlyList<StoreQuote> GetOutpostTypeQuotes(ItemPrefab item, IReadOnlySet<Identifier> excludedStoreIds)
        {
            var quotes = new List<StoreQuote>();
            float shopMultiplier = (GameMain.GameSession?.Campaign as CampaignMode)?.Settings?.ShopPriceMultiplier ?? 1f;

            foreach (var storeId in StoreResolver.GetStoreCatalog(item))
            {
                if (excludedStoreIds.Contains(storeId)) { continue; }
                if (StoreResolver.GetStorePriceInfo(item, storeId) is not { Price: > 0 } priceInfo) { continue; }

                IReadOnlyList<LocationType> outpostTypes = StoreResolver.GetOutpostTypes(storeId);
                if (outpostTypes.Count == 0) { continue; }

                bool canBuy = priceInfo.CanBeBought;
                bool canSell = item.CanBeSold;
                int buyMin = int.MaxValue;
                int buyMax = 0;
                int sellMin = int.MaxValue;
                int sellMax = 0;
                foreach (var outpostType in outpostTypes)
                {
                    if (canBuy)
                    {
                        int buyPrice = EstimateBuyPrice(outpostType, priceInfo, shopMultiplier);
                        buyMin = Math.Min(buyMin, buyPrice);
                        buyMax = Math.Max(buyMax, buyPrice);
                    }

                    if (canSell)
                    {
                        int sellPrice = EstimateSellPrice(outpostType, priceInfo);
                        sellMin = Math.Min(sellMin, sellPrice);
                        sellMax = Math.Max(sellMax, sellPrice);
                    }
                }

                var (icon, iconColor) = StoreResolver.ResolveMerchantVisuals(storeId);
                quotes.Add(new StoreQuote
                {
                    StoreId = storeId,
                    Title = StoreResolver.ResolveStoreName(storeId),
                    Icon = icon,
                    IconColor = iconColor,
                    BasePrice = priceInfo.Price,
                    BuyMin = canBuy ? buyMin : 0,
                    BuyMax = canBuy ? buyMax : 0,
                    SellMin = canSell ? sellMin : 0,
                    SellMax = canSell ? sellMax : 0,
                    Block = canBuy ? PurchaseBlock.None : PurchaseBlock.NotSold,
                    RequiredDifficulty = priceInfo.MinLevelDifficulty,
                    RequiredFaction = priceInfo.RequiredFaction,
                    RequiredReputation = priceInfo.MinReputation.Values.DefaultIfEmpty(0f).Max(),
                    OutpostTypes = [.. outpostTypes
                        .Select(static type => type.Name.Value)
                        .Distinct()
                        .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)]
                });
            }

            quotes.Sort(static (a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
            return quotes;
        }

        public static IReadOnlyList<FactionQuote> GetFactionQuotes(ItemPrefab item, Location? location)
        {
            var quotes = new List<FactionQuote>();
            if (GameMain.GameSession?.Campaign is not CampaignMode campaign) { return quotes; }
            if (campaign.Factions is not { Count: > 0 } factions) { return quotes; }
            if (StoreResolver.ResolveBasePriceInfo(item) is not { } priceInfo) { return quotes; }

            LocationType? locationType = location?.Type;
            float buyModifier = locationType?.StoreBuyPriceModifier ?? 1f;
            float sellModifier = locationType?.StoreSellPriceModifier ?? 0.3f;
            float maxReputationModifier = locationType?.StoreMaxReputationModifier ?? 0.1f;
            float minReputationModifier = locationType?.StoreMinReputationModifier ?? 1f;
            float shopMultiplier = campaign.Settings?.ShopPriceMultiplier ?? 1f;
            bool canBuy = priceInfo.CanBeBought;
            bool canSell = item.CanBeSold;

            foreach (var faction in factions)
            {
                Reputation reputation = faction.Reputation;
                float buyReputationModifier = GetReputationModifier(reputation, maxReputationModifier, minReputationModifier, buying: true);
                float sellReputationModifier = GetReputationModifier(reputation, maxReputationModifier, minReputationModifier, buying: false);

                quotes.Add(new FactionQuote
                {
                    Name = faction.Prefab.Name.Value,
                    Reputation = reputation.Value,
                    BasePrice = priceInfo.Price,
                    CanBuy = canBuy,
                    CanSell = canSell,
                    BuyPrice = canBuy ? Math.Max((int)(buyModifier * priceInfo.Price * priceInfo.BuyingPriceMultiplier * buyReputationModifier * shopMultiplier), 1) : 0,
                    SellPrice = canSell ? Math.Max((int)(sellModifier * priceInfo.Price * sellReputationModifier), 1) : 0,
                    Icon = faction.Prefab.Icon,
                    IconColor = faction.Prefab.IconColor
                });
            }

            quotes.Sort(static (a, b) => b.Reputation.CompareTo(a.Reputation));
            return quotes;
        }

        private static PurchaseBlock GetPurchaseBlock(Location location, Location.StoreInfo store, PriceInfo priceInfo, CampaignMode? campaign)
        {
            if (!priceInfo.CanBeBought) { return PurchaseBlock.NotSold; }

            float difficulty = location.LevelData?.Difficulty ?? 0f;
            if (difficulty < priceInfo.MinLevelDifficulty) { return PurchaseBlock.Difficulty; }

            Identifier? faction = location.Faction?.Prefab.Identifier;
            Identifier? secondaryFaction = location.SecondaryFaction?.Prefab.Identifier;
            if (!priceInfo.RequiredFaction.IsEmpty &&
                faction != priceInfo.RequiredFaction &&
                secondaryFaction != priceInfo.RequiredFaction)
            {
                return PurchaseBlock.Faction;
            }

            Identifier merchantFaction = store.GetMerchantOrLocationFactionIdentifier();
            return ReputationRequirementsMet(priceInfo, merchantFaction, campaign) ? PurchaseBlock.None : PurchaseBlock.Reputation;
        }

        private static bool ReputationRequirementsMet(PriceInfo priceInfo, Identifier faction, CampaignMode? campaign)
        {
            if (priceInfo.MinReputation.Count == 0) { return true; }

            return campaign != null &&
                   priceInfo.MinReputation.TryGetValue(faction, out float required) &&
                   MathF.Round(campaign.GetReputation(faction)) >= required;
        }

        private static float GetRequiredReputation(PriceInfo priceInfo, Identifier faction)
            => priceInfo.MinReputation.TryGetValue(faction, out float required) ? required : -1f;

        private static float GetReputationModifier(Reputation reputation, float maxModifier, float minModifier, bool buying)
        {
            if (reputation.Value > 0f && reputation.MaxReputation > 0)
            {
                float t = reputation.Value / reputation.MaxReputation;
                return MathHelper.Lerp(1f, buying ? 1f - maxModifier : 1f + maxModifier, t);
            }

            if (reputation.Value < 0f && reputation.MinReputation < 0)
            {
                float t = reputation.Value / reputation.MinReputation;
                return MathHelper.Lerp(1f, buying ? 1f + minModifier : 1f - minModifier, t);
            }

            return 1f;
        }

        private static int EstimateBuyPrice(LocationType outpostType, PriceInfo priceInfo, float shopMultiplier)
            => Math.Max((int)(outpostType.StoreBuyPriceModifier * priceInfo.Price * priceInfo.BuyingPriceMultiplier * shopMultiplier), 1);

        private static int EstimateSellPrice(LocationType outpostType, PriceInfo priceInfo)
            => Math.Max((int)(outpostType.StoreSellPriceModifier * priceInfo.Price), 1);
    }
}