// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Text;
using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.Extensions;

namespace SOS.Panels.MarketPanel
{
    internal static class MarketCardFactory
    {
        public const int CardHeight = 64;

        private const int IconSize = 30;
        private const int SeparatorWidth = 2;

        /// <summary>
        /// Creates the card for a live or estimated store quote.
        /// </summary>
        public static GUIFrame CreateStoreCard(RectTransform parent, StoreQuote quote)
        {
            var (card, info) = CreateShell(parent, quote.Icon, quote.IconColor, quote.IsLive ? Color.LimeGreen * 0.75f : Color.Transparent);

            var rowTop = CreateRow(info.RectTransform);
            bool hasTag = quote.IsDailySpecial || quote.IsRequestedGood;
            _ = CreateText(rowTop.RectTransform, quote.Title, Color.Gold, hasTag ? 0.72f : 1f);

            if (hasTag)
            {
                (string tag, Color tagColor) = quote.IsDailySpecial
                    ? (Texts.Get("sos.market.tag_special", "SPECIAL").Value, Color.Cyan)
                    : (Texts.Get("sos.market.tag_demand", "DEMAND").Value, Color.Orange);
                _ = CreateText(rowTop.RectTransform, tag, tagColor, 0.28f, Alignment.Right);
            }

            var rowBottom = CreateRow(info.RectTransform);
            _ = CreateText(rowBottom.RectTransform, FormatSellPrice(quote.SellMin, quote.SellMax), Color.Yellow, 0.38f);
            _ = CreateText(rowBottom.RectTransform, FormatBuyPrice(quote), GetBuyColor(quote), 0.42f);
            if (quote.IsLive)
            {
                _ = CreateText(rowBottom.RectTransform, $"x{quote.Stock}", Color.DeepSkyBlue, 0.20f, Alignment.Right);
            }

            card.OnDrawToolTip = component => component.ToolTip = BuildStoreTooltip(quote).Rich();
            return card;
        }

        /// <summary>
        /// Creates the card for a faction rate quote.
        /// </summary>
        public static GUIFrame CreateFactionCard(RectTransform parent, FactionQuote quote)
        {
            var (card, info) = CreateShell(parent, quote.Icon, quote.IconColor, Color.Transparent);

            var rowTop = CreateRow(info.RectTransform);
            _ = CreateText(rowTop.RectTransform, quote.Name, Color.White, 0.68f);

            string sign = quote.Reputation > 0f ? "+" : "";
            Color reputationColor = quote.Reputation > 0f ? Color.LightGreen : quote.Reputation < 0f ? Color.Salmon : Color.Gray;
            _ = CreateText(rowTop.RectTransform, $"{sign}{quote.Reputation:0} Rep", reputationColor, 0.32f, Alignment.Right);

            var rowBottom = CreateRow(info.RectTransform);
            _ = CreateText(rowBottom.RectTransform, FormatFactionSellPrice(quote), Color.Yellow, 0.50f);
            _ = CreateText(rowBottom.RectTransform, FormatFactionBuyPrice(quote), Color.LightGray, 0.50f);

            card.OnDrawToolTip = component => component.ToolTip = BuildFactionTooltip(quote).Rich();
            return card;
        }

        private static (GUIFrame Card, GUILayoutGroup Info) CreateShell(RectTransform parent, Sprite? icon, Color iconColor, Color outlineColor)
        {
            var card = new GUIFrame(new RectTransform(Vector2.One, parent), style: "InnerFrame")
            {
                Color = Color.Black * 0.45f,
                OutlineColor = outlineColor,
                CanBeFocused = true
            };

            var mainLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.96f, 0.9f), card.RectTransform, Anchor.Center), isHorizontal: true)
            {
                Stretch = true,
                AbsoluteSpacing = 6,
                CanBeFocused = false
            };

            var iconBox = new GUIFrame(
                new RectTransform(new Point(IconSize, IconSize), mainLayout.RectTransform, Anchor.Center) { IsFixedSize = true },
                style: null)
            {
                CanBeFocused = false
            };

            if (icon != null)
            {
                _ = new GUIImage(new RectTransform(Vector2.One, iconBox.RectTransform), icon, scaleToFit: true)
                {
                    Color = iconColor,
                    CanBeFocused = false
                };
            }

            _ = new GUIFrame(
                new RectTransform(new Point(SeparatorWidth, IconSize + 6), mainLayout.RectTransform, Anchor.Center) { IsFixedSize = true },
                style: null, color: Color.Gray * 0.35f)
            {
                CanBeFocused = false
            };

            var info = new GUILayoutGroup(new RectTransform(Vector2.One, mainLayout.RectTransform))
            {
                Stretch = true,
                CanBeFocused = false
            };

            return (card, info);
        }

        private static GUILayoutGroup CreateRow(RectTransform parent)
            => new(new RectTransform(new Vector2(1f, 0.5f), parent), isHorizontal: true) { CanBeFocused = false };

        private static GUITextBlock CreateText(RectTransform parent, string text, Color color, float width, Alignment alignment = Alignment.CenterLeft)
        {
            Anchor anchor = alignment == Alignment.Right ? Anchor.CenterRight : Anchor.CenterLeft;
            return new GUITextBlock(new RectTransform(new Vector2(width, 1f), parent, anchor), text,
                font: GUIStyle.SmallFont, textColor: color, textAlignment: alignment)
            {
                CanBeFocused = false,
                AutoScaleHorizontal = true,
                Padding = alignment == Alignment.Right ? new Vector4(0, 0, 2, 0) : Vector4.Zero
            };
        }

        private static Color GetBuyColor(StoreQuote quote) => quote.Block switch
        {
            PurchaseBlock.None => quote.IsDailySpecial ? Color.LightGreen : Color.LightGray,
            PurchaseBlock.NotSold => Color.Gray * 0.7f,
            _ => Color.Salmon
        };

        private static string FormatBuyPrice(StoreQuote quote) => quote.Block switch
        {
            PurchaseBlock.NotSold => $"B: {NotAvailable}",
            PurchaseBlock.Difficulty => $"B: {string.Format(Texts.Get("sos.market.locked_difficulty", "Locked ({0}% diff)").Value, quote.RequiredDifficulty)}",
            PurchaseBlock.Faction => $"B: {Texts.Get("sos.market.locked_faction", "Locked (faction)").Value}",
            PurchaseBlock.Reputation when quote.RequiredReputation < 0f => $"B: {Texts.Get("sos.market.locked_no_trade", "Locked (not traded)").Value}",
            PurchaseBlock.Reputation => $"B: {string.Format(Texts.Get("sos.market.locked_reputation", "Locked ({0:0} rep)").Value, quote.RequiredReputation)}",
            _ => $"B: {FormatPriceRange(quote.BuyMin, quote.BuyMax)}"
        };

        private static string FormatSellPrice(int min, int max)
            => min > 0 ? $"S: {FormatPriceRange(min, max)}" : $"S: {NotAvailable}";

        private static string FormatFactionSellPrice(FactionQuote quote)
            => quote.CanSell ? $"S: {FormatPrice(quote.SellPrice)}" : $"S: {NotAvailable}";

        private static string FormatFactionBuyPrice(FactionQuote quote)
            => quote.CanBuy ? $"B: {FormatPrice(quote.BuyPrice)}" : $"B: {NotAvailable}";

        private static string NotAvailable => Texts.Get("sos.market.na", "N/A").Value;

        private static string FormatPriceRange(int min, int max)
            => min == max ? FormatPrice(min) : $"{TextManager.FormatCurrency(min, includeCurrencySymbol: false).Value}-{FormatPrice(max)}";

        private static string FormatPrice(int price) => TextManager.FormatCurrency(price).Value;

        private static string BuildStoreTooltip(StoreQuote quote)
        {
            var sb = new StringBuilder();
            sb.AppendLine(quote.Title);
            sb.AppendLine($"{Texts.Get("sos.item.base_price", "Base Price:").Value} {FormatPrice(quote.BasePrice)}");

            sb.AppendLine(quote.SellMin > 0
                ? $"- {Texts.Get("sos.market.sell", "Sell Price").Value}: {FormatPriceRange(quote.SellMin, quote.SellMax)}"
                : $"- {Texts.Get("sos.market.sell", "Sell Price").Value}: {Texts.Get("sos.market.not_sold", "Not accepted").Value}");

            sb.AppendLine(quote.Block switch
            {
                PurchaseBlock.NotSold => $"- {Texts.Get("sos.market.buy", "Buy Price").Value}: {Texts.Get("sos.market.not_sold", "Not accepted").Value}",
                PurchaseBlock.None => $"- {Texts.Get("sos.market.buy", "Buy Price").Value}: {FormatPriceRange(quote.BuyMin, quote.BuyMax)}",
                _ => $"- {Texts.Get("sos.market.buy", "Buy Price").Value}: {Texts.Get("sos.market.locked", "Locked").Value}"
            });

            if (quote.IsLive)
            {
                sb.AppendLine();
                sb.AppendLine($"{Texts.Get("sos.market.stock", "Stock:").Value} {quote.Stock}");
                sb.AppendLine($"{Texts.Get("sos.market.funds", "Store funds:").Value} {FormatPrice(quote.Balance)}");

                if (quote.IsDailySpecial) { sb.AppendLine(Texts.Get("sos.market.daily_special", "Daily special: cheaper while it lasts.").Value); }
                if (quote.IsRequestedGood) { sb.AppendLine(Texts.Get("sos.market.requested_good", "Requested good: the store pays more for it.").Value); }

                switch (quote.Block)
                {
                    case PurchaseBlock.Difficulty:
                        sb.AppendLine(string.Format(Texts.Get("sos.market.req_difficulty", "Requires biome difficulty of at least {0}%.").Value, quote.RequiredDifficulty));
                        break;
                    case PurchaseBlock.Faction:
                        sb.AppendLine(string.Format(Texts.Get("sos.market.req_faction", "Only sold at outposts of {0}.").Value, quote.RequiredFaction));
                        break;
                    case PurchaseBlock.Reputation:
                        sb.AppendLine(quote.RequiredReputation < 0f
                            ? Texts.Get("sos.market.no_trade_faction", "This store does not trade with your faction.").Value
                            : string.Format(Texts.Get("sos.market.req_reputation", "Requires a reputation of at least {0:0}.").Value, quote.RequiredReputation));
                        break;
                }
            }
            else
            {
                if (quote.OutpostTypes.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(Texts.Get("sos.market.found_in_outposts", "Can be found in outposts:").Value);
                    sb.AppendLine($"- {string.Join(", ", quote.OutpostTypes)}");
                }

                if (quote.RequiredDifficulty > 0)
                {
                    sb.AppendLine(string.Format(Texts.Get("sos.market.estimated_difficulty", "Estimated: requires biome difficulty of at least {0}%.").Value, quote.RequiredDifficulty));
                }
                if (!quote.RequiredFaction.IsEmpty)
                {
                    sb.AppendLine(string.Format(Texts.Get("sos.market.estimated_faction", "Estimated: only sold at outposts of {0}.").Value, quote.RequiredFaction));
                }
                if (quote.RequiredReputation > 0f)
                {
                    sb.AppendLine(string.Format(Texts.Get("sos.market.estimated_reputation", "Estimated: requires a reputation of at least {0}.").Value, quote.RequiredReputation));
                }
            }

            return sb.ToString();
        }

        private static string BuildFactionTooltip(FactionQuote quote)
        {
            var sb = new StringBuilder();
            sb.AppendLine(quote.Name);
            sb.AppendLine($"{Texts.Get("sos.market.reputation", "Reputation:").Value} {quote.Reputation:0}");
            sb.AppendLine($"{Texts.Get("sos.item.base_price", "Base Price:").Value} {FormatPrice(quote.BasePrice)}");
            sb.AppendLine($"- {Texts.Get("sos.market.sell", "Sell Price").Value}: {(quote.CanSell ? FormatPrice(quote.SellPrice) : NotAvailable)}");
            sb.AppendLine($"- {Texts.Get("sos.market.buy", "Buy Price").Value}: {(quote.CanBuy ? FormatPrice(quote.BuyPrice) : NotAvailable)}");
            sb.AppendLine();
            sb.Append(Texts.Get("sos.market.faction_tooltip", "Estimated at an outpost of the current location type, varying only the reputation.").Value);
            return sb.ToString();
        }
    }
}