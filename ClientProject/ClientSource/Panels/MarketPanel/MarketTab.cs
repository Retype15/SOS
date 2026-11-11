// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.Configs;
using SOS.GUI;

namespace SOS.Panels.MarketPanel
{
    /// <summary>
    /// Market inspection tab: shows what the current outpost pays for the item, how the rest of the
    /// store types compare, and how each campaign faction would treat the player.
    /// </summary>
    [AutoRegister("SOS.Tab.Market", 1)]
    public sealed class MarketTab : ISOSTab, IDisposable
    {
        private const int MinCardWidth = 260;
        private const int MaxColumns = 3;
        private const double PollInterval = 0.5;

        private sealed class MarketSection
        {
            public required GUIFrame Header { get; init; }
            public required GUITextBlock Title { get; init; }
            public required GUIAccordion Accordion { get; init; }
        }

        private MarketSection? _outpostSection;
        private MarketSection? _storesSection;
        private MarketSection? _factionsSection;

        private GUIFrame? _container;
        private GUITextBlock? _basePriceText;
        private GUITextBlock? _availabilityText;
        private GUIListBox? _cardsListBox;

        private Prefab? _currentPrefab;
        private ItemPrefab? CurrentItem => _currentPrefab as ItemPrefab;

        private double _nextPollTime;
        private Location? _lastLocation;
        private int _columnCount;
        private int _purchasedUnits;

        public bool CanHandle(Prefab prefab)
            => prefab is ItemPrefab item && (item.DefaultPrice != null || item.StorePrices is { Count: > 0 });

        public void Init(GUIFrame container, GUIButton tabButton)
        {
            _container = container;
            BuildSkeleton();
        }

        public void Update(Prefab target)
        {
            _currentPrefab = target;
            Refresh();
        }

        public void Dispose()
        {
            _container = null;
            _basePriceText = null;
            _availabilityText = null;
            _cardsListBox = null;
            _currentPrefab = null;
            _lastLocation = null;
            _outpostSection = null;
            _storesSection = null;
            _factionsSection = null;
        }

        private void BuildSkeleton()
        {
            if (_container == null) { return; }
            _container.ClearChildren();

            var summaryHeader = new GUIFrame(new RectTransform(new Vector2(1f, 0.08f), _container.RectTransform, Anchor.TopCenter), style: "InnerFrame")
            {
                Color = Color.Black * 0.45f,
                CanBeFocused = false
            };

            var summaryLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.96f, 0.9f), summaryHeader.RectTransform, Anchor.Center), isHorizontal: true)
            {
                Stretch = true,
                CanBeFocused = false
            };

            _basePriceText = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1f), summaryLayout.RectTransform), "",
                font: GUIStyle.SubHeadingFont, textColor: Color.Gold)
            {
                CanBeFocused = false,
                AutoScaleHorizontal = true
            };

            _availabilityText = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1f), summaryLayout.RectTransform), "",
                font: GUIStyle.SmallFont, textAlignment: Alignment.CenterRight)
            {
                CanBeFocused = false,
                AutoScaleHorizontal = true
            };

            _cardsListBox = new GUIListBox(new RectTransform(new Vector2(1f, 0.92f), _container.RectTransform, Anchor.BottomCenter), style: null)
            {
                Spacing = 6,
                Padding = new Vector4(6, 6, 6, 6)
            };

            _outpostSection = CreateSection(Texts.Get("sos.market.section_outpost", "CURRENT OUTPOST").Value, Color.LightGreen,
                MarketConfig.Instance.OutpostCollapsed, collapsed => MarketConfig.Instance.OutpostCollapsed = collapsed);
            _storesSection = CreateSection(Texts.Get("sos.market.section_stores", "OTHER STORE TYPES").Value, Color.Gold,
                MarketConfig.Instance.StoresCollapsed, collapsed => MarketConfig.Instance.StoresCollapsed = collapsed);
            _factionsSection = CreateSection(Texts.Get("sos.market.section_factions", "CAMPAIGN FACTION RATES").Value, Color.CornflowerBlue,
                MarketConfig.Instance.FactionsCollapsed, collapsed => MarketConfig.Instance.FactionsCollapsed = collapsed);

            _ = new GUICustomComponent(new RectTransform(Point.Zero, _container.RectTransform), onUpdate: OnTick);
        }

        private MarketSection CreateSection(string title, Color color, bool collapsed, Action<bool> onToggle)
        {
            var header = new GUIFrame(
                new RectTransform(new Vector2(1f, 0f), _cardsListBox!.Content.RectTransform)
                {
                    MinSize = new Point(0, GUIAccordion.HeaderHeight),
                    MaxSize = new Point(int.MaxValue, GUIAccordion.HeaderHeight)
                },
                style: null)
            {
                CanBeFocused = false
            };

            var titleBlock = new GUITextBlock(
                new RectTransform(new Vector2(0.85f, 1f), header.RectTransform, Anchor.CenterLeft),
                title, font: GUIStyle.SubHeadingFont, textColor: color)
            {
                CanBeFocused = false,
                Padding = new Vector4(10, 0, 0, 0)
            };

            _ = new GUIFrame(
                new RectTransform(new Vector2(1f, 0.07f), header.RectTransform, Anchor.BottomCenter),
                style: "HorizontalLine", color: color * 0.45f)
            {
                CanBeFocused = false
            };

            var accordion = new GUIAccordion(
                header,
                new RectTransform(new Vector2(1f, 0f), _cardsListBox.Content.RectTransform),
                collapsed: collapsed,
                onToggle: onToggle);

            return new MarketSection { Header = header, Title = titleBlock, Accordion = accordion };
        }

        private void OnTick(float deltaTime, GUIComponent component)
        {
            if (Timing.TotalTime < _nextPollTime) { return; }
            _nextPollTime = Timing.TotalTime + PollInterval;

            Location? location = GameMain.GameSession?.Map?.CurrentLocation;
            if (location != _lastLocation ||
                CountPurchasedUnits(location) != _purchasedUnits ||
                GetColumnCount() != _columnCount)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_container == null || _cardsListBox == null) { return; }
            if (CurrentItem is not ItemPrefab item) { return; }
            if (_outpostSection == null || _storesSection == null || _factionsSection == null) { return; }

            Location? location = GameMain.GameSession?.Map?.CurrentLocation;
            _lastLocation = location;
            _columnCount = GetColumnCount();
            _purchasedUnits = CountPurchasedUnits(location);

            _outpostSection.Title.Text = BuildOutpostTitle(location);

            IReadOnlyList<StoreQuote> liveQuotes = MarketPriceModel.GetLiveQuotes(item, location);
            var liveStoreIds = liveQuotes.Select(static quote => quote.StoreId).ToHashSet();
            IReadOnlyList<StoreQuote> storeTypeQuotes = MarketPriceModel.GetOutpostTypeQuotes(item, liveStoreIds);
            IReadOnlyList<FactionQuote> factionQuotes = MarketPriceModel.GetFactionQuotes(item, location);

            bool hasOutpost = PopulateStoreSection(_outpostSection, liveQuotes);
            bool hasStores = PopulateStoreSection(_storesSection, storeTypeQuotes);
            bool hasFactions = PopulateFactionSection(_factionsSection, factionQuotes);

            ApplySectionVisibility(_outpostSection, hasOutpost);
            ApplySectionVisibility(_storesSection, hasStores);
            ApplySectionVisibility(_factionsSection, hasFactions);

            UpdateSummary(item);
        }

        private bool PopulateStoreSection(MarketSection section, IReadOnlyList<StoreQuote> quotes)
        {
            var content = section.Accordion.Content;
            content.ClearChildren();

            if (quotes.Count == 0) { return false; }

            for (int index = 0; index < quotes.Count; index += _columnCount)
            {
                var row = CreateGridRow(content.RectTransform);
                for (int column = 0; column < _columnCount; column++)
                {
                    int quoteIndex = index + column;
                    if (quoteIndex < quotes.Count)
                    {
                        MarketCardFactory.CreateStoreCard(row.RectTransform, quotes[quoteIndex]);
                    }
                    else
                    {
                        AddGridFiller(row);
                    }
                }
            }

            return true;
        }

        private bool PopulateFactionSection(MarketSection section, IReadOnlyList<FactionQuote> quotes)
        {
            var content = section.Accordion.Content;
            content.ClearChildren();

            if (quotes.Count == 0) { return false; }

            for (int index = 0; index < quotes.Count; index += _columnCount)
            {
                var row = CreateGridRow(content.RectTransform);
                for (int column = 0; column < _columnCount; column++)
                {
                    int quoteIndex = index + column;
                    if (quoteIndex < quotes.Count)
                    {
                        MarketCardFactory.CreateFactionCard(row.RectTransform, quotes[quoteIndex]);
                    }
                    else
                    {
                        AddGridFiller(row);
                    }
                }
            }

            return true;
        }

        private static GUILayoutGroup CreateGridRow(RectTransform parent)
            => new(new RectTransform(new Vector2(1f, 0f), parent)
            {
                MinSize = new Point(0, MarketCardFactory.CardHeight),
                MaxSize = new Point(int.MaxValue, MarketCardFactory.CardHeight)
            }, isHorizontal: true)
            {
                Stretch = true,
                AbsoluteSpacing = 6,
                CanBeFocused = false
            };

        private static void AddGridFiller(GUILayoutGroup row)
            => _ = new GUIFrame(new RectTransform(Vector2.One, row.RectTransform), style: null) { CanBeFocused = false };

        private void UpdateSummary(ItemPrefab item)
        {
            if (_basePriceText == null || _availabilityText == null) { return; }

            _basePriceText.Text = MarketPriceModel.ResolveBasePrice(item) is { } basePrice
                ? $"{Texts.Get("sos.item.base_price", "Base Price:").Value} {TextManager.FormatCurrency(basePrice).Value}"
                : Texts.Get("sos.market.no_base_price", "No base price").Value;

            (string key, string fallback, Color color) state = (item.CanBeBought, item.CanBeSold) switch
            {
                (true, true) => ("sos.market.state_buy_sell", "Purchasable and sellable", Color.LightGreen),
                (true, false) => ("sos.market.state_buy_only", "Purchasable (not sellable)", Color.Yellow),
                (false, true) => ("sos.market.state_sell_only", "Sell only", Color.Salmon),
                _ => ("sos.market.state_not_traded", "Not traded", Color.Gray)
            };

            _availabilityText.Text = Texts.Get(state.key, state.fallback).Value;
            _availabilityText.TextColor = state.color;
        }

        private static void ApplySectionVisibility(MarketSection section, bool hasContent)
        {
            SetVisible(section.Header, hasContent);
            SetVisible(section.Accordion, hasContent && !section.Accordion.IsCollapsed);
        }

        private static void SetVisible(GUIComponent component, bool visible)
        {
            if (component.Visible == visible) { return; }

            component.Visible = visible;
            if (visible) { component.AddToGUIUpdateList(); }
        }

        private int GetColumnCount()
        {
            int width = _cardsListBox?.Rect.Width ?? 0;
            if (width <= 0) { width = MinCardWidth * 2; }
            return Math.Clamp(width / MinCardWidth, 1, MaxColumns);
        }

        private int CountPurchasedUnits(Location? location)
        {
            if (location?.Stores == null || CurrentItem is not ItemPrefab item) { return 0; }
            if ((GameMain.GameSession?.Campaign as CampaignMode)?.CargoManager is not CargoManager cargo) { return 0; }

            int purchased = 0;
            foreach (var (_, store) in location.Stores)
                purchased += cargo.GetPurchasedItemCount(store, item);

            return purchased;
        }

        private static string BuildOutpostTitle(Location? location)
        {
            string title = Texts.Get("sos.market.section_outpost", "CURRENT OUTPOST").Value;
            string name = location?.DisplayName.Value ?? "";
            return string.IsNullOrWhiteSpace(name) ? title : $"{title} - {name.ToUpperInvariant()}";
        }
    }
}