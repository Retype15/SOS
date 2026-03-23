// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0079
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace SOS
{
    public static class CardBuilder
    {
        private const int RowHeight = 22;
        private const int HeaderHeight = 20;
        private const int CardPadding = 2;

        private static readonly Dictionary<Identifier, string> machineNameCache = [];

        public static RichString GetDetailedTooltip(ItemPrefab prefab)
        {
            if (prefab == null) return RichString.Rich("");

            string toolTip = $"‖color:White‖{prefab.Name.Value}‖color:end‖";

#if DEBUG
            toolTip += $" ‖color:gui.orange‖({prefab.Identifier})‖color:end‖";
#endif

            int price = prefab.DefaultPrice?.Price ?? 0;
            if (price > 0)
            {
                toolTip += $"\n‖color:{Color.Gold.ToStringHex()}‖{TextSOS.Get("sos.item.price", "Price")}{price}mk‖color:end‖";
            }

            if (!prefab.Description.IsNullOrEmpty())
            {
                toolTip += "\n" + prefab.Description.Value;
            }
            if (prefab.ContentPackage != null && prefab.ContentPackage.Name != "Vanilla")
            {
                string modColor = XMLExtensions.ToStringHex(Color.MediumPurple);
                toolTip += $"\n‖color:{modColor}‖{prefab.ContentPackage.Name}‖color:end‖";
            }

            return RichString.Rich(toolTip);
        }

        public static void DrawHeader(GUIFrame parent, ItemPrefab item)
        {
            var layout = new GUILayoutGroup(new RectTransform(Vector2.One, parent.RectTransform), isHorizontal: true) { AbsoluteSpacing = 10 };
            Sprite? icon = item.InventoryIcon ?? item.Sprite;
            if (icon != null)
            {
                var imgFrame = new GUIFrame(new RectTransform(new Vector2(0.1f, 0.9f), layout.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(10, 0) }, style: "InnerFrame")
                {
                    ToolTip = GetDetailedTooltip(item)
                };
                _ = new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), imgFrame.RectTransform, Anchor.Center), icon, scaleToFit: true) { Color = item.InventoryIconColor, CanBeFocused = false };
            }
            var textLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.8f, 1f), layout.RectTransform)) { RelativeSpacing = 0.02f };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.6f), textLayout.RectTransform), item.Name.Value, font: GUIStyle.LargeFont, textColor: Color.Cyan);
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.4f), textLayout.RectTransform), TextSOS.Get("sos.item.header_info", "ID: [id] | Price: [price] mk").Replace("[id]", item.Identifier.Value).Replace("[price]", (item.DefaultPrice?.Price ?? 0).ToString()), font: GUIStyle.SmallFont, textColor: Color.Gray);
        }

        public static void BuildColumn<T>(GUIListBox container, string title, List<T> items, Action<GUIListBox, T, DisplayMode> drawCard, DisplayMode mode = DisplayMode.Normal)
        {
            var colFrame = new GUIFrame(new RectTransform(new Vector2(0f, 1f), container.Content.RectTransform) { MinSize = new Point(280, 0) }, style: null);
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), colFrame.RectTransform) { MinSize = new Point(0, 25) }, title, font: GUIStyle.SubHeadingFont, textColor: Color.Gold, textAlignment: Alignment.Center);

            var list = new GUIListBox(new RectTransform(new Vector2(1f, 1f), colFrame.RectTransform, Anchor.BottomCenter) { AbsoluteOffset = new Point(0, 30) }) { Spacing = 5 };
            foreach (var item in items) drawCard(list, item, mode);
        }

        public class UIMachineGroup
        {
            public string MachineName = "";
            public List<IRecipeCard> Cards = [];
            public bool IsVendingMachine = false;
            public string PriceString = "";

            public void AddCard(IRecipeCard card) => Cards.Add(card);

            public void Draw(GUIListBox list)
            {
                new MachineHeaderCard(MachineName, IsVendingMachine, PriceString).Draw(list);
                foreach (var card in Cards) card.Draw(list);
            }
        }

        public interface IRecipeCard
        {
            void Draw(GUIListBox list);
        }

        public class MachineHeaderCard : IRecipeCard
        {
            public string Title;
            public bool IsVendingMachine;
            public string Price;

            public MachineHeaderCard(string title, bool isVending = false, string price = "")
            {
                Title = title; IsVendingMachine = isVending; Price = price;
            }

            public void Draw(GUIListBox list)
            {
                var frame = new GUIFrame(new RectTransform(new Vector2(1f, 0f), list.Content.RectTransform) { MinSize = new Point(0, HeaderHeight) }, style: null);
                string text = IsVendingMachine
                    ? TextSOS.Get("sos.recipe.buyable", "Buyable at [Title] a [Price] mk").Replace("[Title]", Title).Replace("[Price]", Price).Value
                    : Title.ToUpper() + ":";
                _ = new GUITextBlock(new RectTransform(Vector2.One, frame.RectTransform), text, font: GUIStyle.SmallFont, textColor: Color.Yellow, textAlignment: Alignment.CenterLeft);
            }
        }

        public class CraftRecipeCard : IRecipeCard
        {
            public FabricationRecipe Recipe;
            public ItemPrefab TargetItem;
            public SOSController Controller;
            public Action<ItemPrefab> OnPrimary;
            public Action<ItemPrefab> OnSecondary;

            public CraftRecipeCard(FabricationRecipe recipe, ItemPrefab target, SOSController controller, Action<ItemPrefab> onP, Action<ItemPrefab> onS)
            {
                Recipe = recipe; TargetItem = target; Controller = controller; OnPrimary = onP; OnSecondary = onS;
            }

            public void Draw(GUIListBox list)
            {
                bool isTracked = Controller.Tracker.TrackedRecipe == Recipe;
                bool showName = !Recipe.RequiresRecipe && Recipe.RequiredSkills.Length == 0;
                int rows = (showName ? 1 : 0) + (Recipe.RequiresRecipe ? 1 : 0) + Recipe.RequiredSkills.Length + Recipe.RequiredItems.Length;
                int height = (rows * RowHeight) + (rows * 2) + CardPadding;

                var card = new GUIButton(new RectTransform(new Vector2(1f, 0f), list.Content.RectTransform) { MinSize = new Point(0, height) }, style: "InnerFrame")
                {
                    Color = Color.Black * 0.4f,
                    OutlineColor = isTracked ? Color.Gold : Color.Transparent,
                    OnClicked = (_, _) => { OnPrimary?.Invoke(TargetItem); return true; },
                    OnSecondaryClicked = (_, _) => { Controller.OpenRecipeContextMenu(TargetItem, Recipe); return true; }
                };

                var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2, CanBeFocused = false };

                bool timeDrawn = false;
                void DrawRowWithTime(string text, Color color)
                {
                    var row = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0f), layout.RectTransform) { MinSize = new Point(0, RowHeight) }, isHorizontal: true) { CanBeFocused = false };
                    _ = new GUITextBlock(new RectTransform(new Vector2(0.7f, 1f), row.RectTransform), text, font: GUIStyle.SmallFont, textColor: color) { CanBeFocused = false };
                    if (!timeDrawn)
                    {
                        _ = new GUITextBlock(new RectTransform(new Vector2(0.3f, 1f), row.RectTransform), $"{Recipe.RequiredTime}s", font: GUIStyle.SmallFont, textAlignment: Alignment.Right, textColor: Color.Gray)
                        {
                            CanBeFocused = false,
                            Padding = new Vector4(0, 0, 25, 0)
                        };
                        timeDrawn = true;
                    }
                }

                if (Recipe.RequiresRecipe)
                {
                    bool hasUnlocked = Character.Controlled != null && Character.Controlled.HasRecipeForItem(TargetItem.Identifier);
                    string txt = hasUnlocked ? TextSOS.Get("sos.recipe.unlocked", "Recipe Unlocked").Value : TextSOS.Get("sos.recipe.locked", "Requires Recipe to Unlock").Value;
                    DrawRowWithTime(txt, hasUnlocked ? Color.LightGreen : Color.Salmon);
                }

                foreach (var skill in Recipe.RequiredSkills)
                {
                    int lvl = Character.Controlled != null ? (int)Character.Controlled.GetSkillLevel(skill.Identifier) : 0;
                    Color c = lvl >= skill.Level ? Color.LightGreen : (lvl >= skill.Level - 10 ? Color.Yellow : Color.Salmon);
                    string txt = $"{TextManager.Get("SkillName." + skill.Identifier).Fallback(skill.Identifier.Value).Value}: {lvl}/{skill.Level}";
                    DrawRowWithTime(txt, c);
                }

                if (showName)
                {
                    DrawRowWithTime(TargetItem.Name.Value, isTracked ? Color.Gold : Color.LightGray);
                }

                foreach (var req in Recipe.RequiredItems) DrawCompactItemRow(layout, req.FirstMatchingPrefab, req.Amount, true, "", null, OnPrimary, OnSecondary);
            }
        }

        public class SourceRecipeCard : IRecipeCard
        {
            public GroupedSource Source;
            public Action<ItemPrefab> OnPrimary;
            public Action<ItemPrefab> OnSecondary;

            public SourceRecipeCard(GroupedSource source, Action<ItemPrefab> onP, Action<ItemPrefab> onS) { Source = source; OnPrimary = onP; OnSecondary = onS; }

            public void Draw(GUIListBox list)
            {
                int rows = 1 + (Source.RequiredOtherItems?.Count ?? 0);
                int height = (rows * RowHeight) + (rows * 2) + CardPadding;

                var card = new GUIButton(new RectTransform(new Vector2(1f, 0f), list.Content.RectTransform) { MinSize = new Point(0, height) }, style: "InnerFrame")
                {
                    Color = Color.Black * 0.4f,
                    OnClicked = (_, _) => { if (Source.SourceItem != null) OnPrimary?.Invoke(Source.SourceItem); return true; },
                    OnSecondaryClicked = (_, _) => { if (Source.SourceItem != null) OnSecondary?.Invoke(Source.SourceItem); return true; }
                };

                var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2, CanBeFocused = false };

                string timeText = Source.SourceItem != null ? $" ({Source.SourceItem.DeconstructTime}s)" : "";
                DrawCompactItemRow(layout, Source.SourceItem, Source.Amount, true, timeText, Color.LightGreen, null, null);

                foreach (var otherId in Source.RequiredOtherItems ?? [])
                {
                    var other = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier == otherId);
                    if (other != null) DrawCompactItemRow(layout, other, 1, true, " + ", Color.Cyan, null, null);
                }
            }
        }

        public class DeconOutputCard : IRecipeCard
        {
            public ItemPrefab Item;
            public List<DeconstructItem> Outputs;
            public Action<ItemPrefab> OnPrimary;
            public Action<ItemPrefab> OnSecondary;

            public DeconOutputCard(ItemPrefab item, List<DeconstructItem> outputs, Action<ItemPrefab> onP, Action<ItemPrefab> onS) { Item = item; Outputs = outputs; OnPrimary = onP; OnSecondary = onS; }

            public void Draw(GUIListBox list)
            {
                var groupedOutputs = Outputs.GroupBy(di => di.ItemIdentifier).Select(g => new { ID = g.Key, Max = g.Max(di => di.Amount), Weight = g.Sum(di => di.Commonness) }).ToList();
                bool isRandom = Item.RandomDeconstructionOutput;
                int rows = (isRandom ? 1 : 0) + groupedOutputs.Count;
                int height = (rows * RowHeight) + (rows * 2) + CardPadding;

                var card = new GUIFrame(new RectTransform(new Vector2(1f, 0f), list.Content.RectTransform) { MinSize = new Point(0, height) }, "InnerFrame") { Color = Color.Black * 0.4f };
                var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2, CanBeFocused = false };

                if (isRandom)
                {
                    _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), layout.RectTransform) { MinSize = new Point(0, RowHeight) },
                        TextSOS.Get("sos.recipe.random_outputs", "Gives [amount] at random:").Replace("[amount]", Item.RandomDeconstructionOutputAmount.ToString()).Value,
                        font: GUIStyle.SmallFont, textColor: Color.Orange)
                    { CanBeFocused = false };
                }

                float totalWeight = Outputs.Sum(di => di.Commonness);
                foreach (var outpt in groupedOutputs)
                {
                    string extras = "";
                    Color c = Color.White;
                    if (Item.RandomDeconstructionOutput && totalWeight > 0) { extras = $" ({((outpt.Weight / totalWeight) * 100f):0.#}%)"; c = Color.Orange; }
                    else if (!Item.RandomDeconstructionOutput && outpt.Weight < 1f) { extras = $" ({outpt.Weight * 100:0.#}%)"; c = Color.Orange; }

                    var prefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier == outpt.ID);
                    DrawCompactItemRow(layout, prefab, outpt.Max, true, extras, c, OnPrimary, OnSecondary);
                }
            }
        }

        public class SingleDeconOutputCard : IRecipeCard
        {
            public ItemPrefab SourceItem;
            public Identifier OutputID;
            public int Amount;
            public float Weight;
            public Action<ItemPrefab> OnPrimary;
            public Action<ItemPrefab> OnSecondary;

            public SingleDeconOutputCard(ItemPrefab source, Identifier outputID, int amount, float weight, Action<ItemPrefab> onP, Action<ItemPrefab> onS)
            {
                SourceItem = source; OutputID = outputID; Amount = amount; Weight = weight; OnPrimary = onP; OnSecondary = onS;
            }

            public void Draw(GUIListBox list)
            {
                var card = new GUIButton(new RectTransform(new Vector2(1f, 0f), list.Content.RectTransform) { MinSize = new Point(0, RowHeight + CardPadding) }, style: "InnerFrame")
                {
                    Color = Color.Black * 0.4f,
                    OnClicked = (_, _) => { var p = ItemPrefab.Prefabs.FirstOrDefault(pref => pref.Identifier == OutputID); if (p != null) OnPrimary?.Invoke(p); return true; },
                    OnSecondaryClicked = (_, _) => { var p = ItemPrefab.Prefabs.FirstOrDefault(pref => pref.Identifier == OutputID); if (p != null) OnSecondary?.Invoke(p); return true; }
                };

                var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2, CanBeFocused = false };
                string chance = Weight < 1f ? $" ({Weight * 100:0.#}%)" : "";
                var prefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier == OutputID);
                DrawCompactItemRow(layout, prefab, Amount, true, chance, Weight < 1f ? Color.Orange : Color.White, null, null);
            }
        }

        public class UsageRecipeCard : IRecipeCard
        {
            public GroupedUsage Usage;
            public Action<ItemPrefab> OnPrimary;
            public Action<ItemPrefab> OnSecondary;

            public UsageRecipeCard(GroupedUsage usage, Action<ItemPrefab> onP, Action<ItemPrefab> onS) { Usage = usage; OnPrimary = onP; OnSecondary = onS; }

            public void Draw(GUIListBox list)
            {
                int rows = 1;
                int height = (rows * RowHeight) + (rows * 2) + CardPadding;

                var card = new GUIButton(new RectTransform(new Vector2(1f, 0f), list.Content.RectTransform) { MinSize = new Point(0, height) }, style: "InnerFrame")
                {
                    Color = Color.Black * 0.4f,
                    OnClicked = (_, _) => { if (Usage.TargetItem != null) OnPrimary?.Invoke(Usage.TargetItem); return true; },
                    OnSecondaryClicked = (_, _) => { if (Usage.TargetItem != null) OnSecondary?.Invoke(Usage.TargetItem); return true; }
                };

                var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { CanBeFocused = false };

                string req = Usage.AmountRequired > 1 ? $" ({TextSOS.Get("sos.recipe.requires", "Requires")} x{Usage.AmountRequired})" : "";
                DrawCompactItemRow(layout, Usage.TargetItem, Usage.AmountCreated, true, req, null, null, null);
            }
        }

        public static void DrawMinimalItemRow(GUIComponent parent, ItemPrefab? prefab, float amount, Action<ItemPrefab>? onPrimaryClick = null, Action<ItemPrefab>? onSecondaryClick = null, Color? badgeColor = null)
        {
            var btnRect = new RectTransform(new Point(30, 30), parent.RectTransform);

            var btn = new GUIButton(btnRect, style: "GUIButton")
            {
                ToolTip = prefab != null ? GetDetailedTooltip(prefab) : null,
                Color = Color.Black * 0.4f
            };

            if (prefab != null && (onPrimaryClick != null || onSecondaryClick != null))
            {
                btn.OnClicked = (_, _) => { onPrimaryClick?.Invoke(prefab); return true; };
                btn.OnSecondaryClicked = (_, _) => { onSecondaryClick?.Invoke(prefab); return true; };
            }

            Sprite? icon = prefab?.InventoryIcon ?? prefab?.Sprite;
            if (icon != null)
            {
                _ = new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), btn.RectTransform, Anchor.Center), icon, scaleToFit: true)
                {
                    Color = prefab?.InventoryIconColor ?? Color.White,
                    CanBeFocused = false
                };
            }

            if (amount > 1)
            {
                _ = new GUITextBlock(new RectTransform(Vector2.One, btn.RectTransform), $"x{amount}", font: GUIStyle.SmallFont, textColor: Color.LightGreen, textAlignment: Alignment.BottomRight)
                {
                    CanBeFocused = false,
                    Padding = Vector4.Zero
                };
            }

            if (badgeColor.HasValue)
            {
                btn.OutlineColor = badgeColor.Value;
            }
        }

        public static void DrawCompactItemRow(GUIComponent parent, ItemPrefab? prefab, float amount, bool isCardInside, string extraText = "", Color? color = null, Action<ItemPrefab>? onPrimaryClick = null, Action<ItemPrefab>? onSecondaryClick = null)
        {
            var rowRect = new RectTransform(new Vector2(1f, 0f), parent.RectTransform) { MinSize = new Point(0, RowHeight) };

            GUIComponent container;

            if (prefab != null && (onPrimaryClick != null || onSecondaryClick != null))
            {
                var btn = new GUIButton(rowRect, style: "ListBoxElement")
                {
                    ToolTip = GetDetailedTooltip(prefab),
                    OnClicked = (_, _) =>
                    {
                        onPrimaryClick?.Invoke(prefab);
                        return true;
                    },
                    OnSecondaryClicked = (_, _) =>
                    {
                        onSecondaryClick?.Invoke(prefab);
                        return true;
                    }
                };
                container = btn;
            }
            else
            {
                container = new GUILayoutGroup(rowRect, isHorizontal: true)
                {
                    AbsoluteSpacing = 5,
                    CanBeFocused = true,
                    ToolTip = prefab != null ? GetDetailedTooltip(prefab) : null
                };
            }

            var contentLayout = new GUILayoutGroup(new RectTransform(Vector2.One, container.RectTransform), isHorizontal: true)
            {
                AbsoluteSpacing = 5,
                CanBeFocused = false
            };

            Sprite? icon = prefab?.InventoryIcon ?? prefab?.Sprite;
            if (icon != null)
            {
                var imgFrame = new GUIFrame(new RectTransform(new Point(20, 20), contentLayout.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(isCardInside ? 0 : 5, 0) }, style: null) { CanBeFocused = false };
                _ = new GUIImage(new RectTransform(Vector2.One, imgFrame.RectTransform), icon, scaleToFit: true) { Color = prefab?.InventoryIconColor ?? Color.White, CanBeFocused = false };
            }

            var (nameStr, aColor) = SafeItemName.Get(prefab, color ?? Color.White);

            var nameBlock = new GUITextBlock(new RectTransform(new Vector2(0.6f, 1f), contentLayout.RectTransform), nameStr, font: GUIStyle.SmallFont, textColor: aColor, textAlignment: Alignment.CenterLeft) { CanBeFocused = false };

            string amtStr = (amount > 1 || amount < 1) ? $" x{amount}" : "";
            var rightBlock = new GUITextBlock(new RectTransform(new Vector2(0.4f, 1f), contentLayout.RectTransform), $"{extraText}{amtStr}", font: GUIStyle.SmallFont, textColor: color ?? Color.Gray, textAlignment: Alignment.CenterRight)
            {
                CanBeFocused = false,
                Padding = new Vector4(0, 0, 25, 0)
            };
        }

        public static string ResolveMachineName(Identifier id)
        {
            if (id.IsEmpty) return "";
            if (machineNameCache.TryGetValue(id, out var cached)) return cached;

            var localized = TextManager.Get("EntityName." + id);
            if (localized.Loaded && !localized.Value.Contains("EntityName."))
            {
                return machineNameCache[id] = localized.Value;
            }

            var matchingPrefab = ItemPrefab.Prefabs.FirstOrDefault(p =>
                p.Identifier == id || p.Tags.Contains(id));

            if (matchingPrefab != null)
            {
                return machineNameCache[id] = matchingPrefab.Name.Value;
            }

            string fallback = id.Value.Replace("_", " ").Replace(".", " ");
            return machineNameCache[id] = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fallback);
        }
    }

    public static class SafeItemName
    {
        public static (string Name, Color TextColor) Get(ItemPrefab? prefab, Color defaultColor)
        {
            if (prefab == null)
                return (TextSOS.Get("sos.gen.unknown", "???").Value, defaultColor);

            if (prefab.Name.IsNullOrEmpty())
            {
                return ($"[{prefab.Identifier}]", Color.Red);
            }

            return (prefab.Name.Value, defaultColor);
        }
    }
}
