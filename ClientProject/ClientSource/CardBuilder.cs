// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using Microsoft.Xna.Framework;


namespace SOS
{
    public static class CardBuilder
    {
        private const int RowHeight = 22;
        private const int HeaderHeight = 20;
        private const int CardPadding = 10;

        public static void DrawHeader(GUIFrame parent, ItemPrefab item, SOSController controller)
        {
            var layout = new GUILayoutGroup(new RectTransform(Vector2.One, parent.RectTransform), isHorizontal: true) { AbsoluteSpacing = 10 };
            Sprite? icon = item.InventoryIcon ?? item.Sprite;
            if (icon != null)
            {
                var imgFrame = new GUIFrame(new RectTransform(new Vector2(0.1f, 0.9f), layout.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(10, 0) }, style: "InnerFrame");
                _ = new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), imgFrame.RectTransform, Anchor.Center), icon, scaleToFit: true) { Color = item.InventoryIconColor };
            }
            var textLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.8f, 1f), layout.RectTransform)) { RelativeSpacing = 0.02f };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.6f), textLayout.RectTransform), item.Name.Value, font: GUIStyle.LargeFont, textColor: Color.Cyan);
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.4f), textLayout.RectTransform), TextSOS.Get("sos.item.header_info", "ID: [id] | Price: [price] mk").Replace("[id]", item.Identifier.Value).Replace("[price]", (item.DefaultPrice?.Price ?? 0).ToString()), font: GUIStyle.SmallFont, textColor: Color.Gray);
        }

        public static void BuildColumn<T>(GUIListBox container, string title, List<T> items, Action<GUIListBox, T> drawCard)
        {
            var colFrame = new GUIFrame(new RectTransform(new Point(280, container.Content.Rect.Height), container.Content.RectTransform), style: null);
            _ = new GUITextBlock(new RectTransform(new Point(colFrame.Rect.Width, 25), colFrame.RectTransform), title, font: GUIStyle.SubHeadingFont, textColor: Color.Gold, textAlignment: Alignment.Center);

            var list = new GUIListBox(new RectTransform(new Point(colFrame.Rect.Width, colFrame.Rect.Height - 30), colFrame.RectTransform, Anchor.BottomCenter)) { Spacing = 5 };
            foreach (var item in items) drawCard(list, item);
        }

        public static void DrawCraftCard(GUIListBox col, FabricationRecipe recipe, ItemPrefab targetItem, SOSController controller, Action<ItemPrefab> onPrimary, Action<ItemPrefab> onSecondary)
        {
            int extraRows = recipe.RequiredSkills.Length + (recipe.RequiresRecipe ? 1 : 0);
            int calculatedHeight = HeaderHeight + ((recipe.RequiredItems.Length + extraRows) * RowHeight) + CardPadding;

            bool isTracked = controller.Tracker.TrackedRecipe == recipe;

            var card = new GUIButton(new RectTransform(new Point(col.Content.Rect.Width, calculatedHeight), col.Content.RectTransform), style: "InnerFrame")
            {
                Color = Color.Black * 0.4f,
                OutlineColor = isTracked ? Color.Gold : Color.Transparent,
                OnClicked = (_, _) => { controller.OnRecipeSelected(targetItem, recipe); return true; },
                OnSecondaryClicked = (_, _) => { controller.OpenRecipeContextMenu(targetItem, recipe); return true; }
            };

            var layout = new GUILayoutGroup(new RectTransform(new Point(card.Rect.Width - 10, card.Rect.Height - 10), card.RectTransform, Anchor.Center))
            {
                AbsoluteSpacing = 2,
                IsHorizontal = false,
                CanBeFocused = false
            };

            string machine;
            if (recipe.SuitableFabricatorIdentifiers.Length > 0)
            {
                machine = string.Join(", ", recipe.SuitableFabricatorIdentifiers.Select(id =>
                    TextManager.Get("EntityName." + id).Fallback(id.Value).Value));
            }
            else { machine = TextSOS.Get("sos.recipe.hand", "Hand").Value; }

            var header = new GUILayoutGroup(new RectTransform(new Point(layout.Rect.Width, HeaderHeight), layout.RectTransform), isHorizontal: true) { CanBeFocused = false };
            new GUITextBlock(new RectTransform(new Vector2(0.7f, 1f), header.RectTransform), machine.ToUpper(), font: GUIStyle.SmallFont, textColor: isTracked ? Color.Gold : Color.Yellow) { CanBeFocused = false };
            new GUITextBlock(new RectTransform(new Vector2(0.3f, 1f), header.RectTransform), $"{recipe.RequiredTime}s", font: GUIStyle.SmallFont, textAlignment: Alignment.Right) { CanBeFocused = false };

            if (recipe.RequiresRecipe)
            {
                bool hasUnlocked = Character.Controlled != null && Character.Controlled.HasRecipeForItem(targetItem.Identifier);

                Color recipeColor = hasUnlocked ? Color.LightGreen : Color.Salmon;
                string recipeText = hasUnlocked ? TextSOS.Get("sos.recipe.unlocked", "Recipe Unlocked").Value : TextSOS.Get("sos.recipe.locked", "Requires Recipe to Unlock").Value;

                new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, RowHeight), layout.RectTransform), recipeText, font: GUIStyle.SmallFont, textColor: recipeColor) { CanBeFocused = false };
            }

            foreach (var skill in recipe.RequiredSkills)
            {
                int playerLevel = 0;
                if (Character.Controlled != null)
                {
                    playerLevel = (int)Character.Controlled.GetSkillLevel(skill.Identifier);
                }

                Color skillColor;

                if (playerLevel >= skill.Level)
                {
                    skillColor = Color.LightGreen;
                }
                else if (playerLevel >= skill.Level - 10)
                {
                    skillColor = Color.Yellow;
                }
                else
                {
                    skillColor = Color.Salmon;
                }

                string localizedSkillName = TextManager.Get("SkillName." + skill.Identifier).Fallback(skill.Identifier.Value).Value;

                new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, RowHeight), layout.RectTransform),
                    $"{localizedSkillName}: {playerLevel}/{skill.Level}",
                    font: GUIStyle.SmallFont, textColor: skillColor)
                { CanBeFocused = false };
            }

            foreach (var req in recipe.RequiredItems)
            {
                DrawCompactItemRow(layout, req.FirstMatchingPrefab, req.Amount, true, "", null, onPrimary, onSecondary);
            }
        }

        public static void DrawDeconCard(GUIListBox col, ItemPrefab item, List<DeconstructItem> deconList, Action<ItemPrefab> onPrimary, Action<ItemPrefab> onSecondary)
        {
            var groupedOutputs = deconList
                .GroupBy(di => new { di.ItemIdentifier, di.Commonness, di.MinCondition })
                .Select(g => new
                {
                    ItemIdentifier = g.Key.ItemIdentifier,
                    Commonness = g.Key.Commonness,
                    MinCondition = g.Key.MinCondition,
                    TotalAmount = g.Sum(di => di.Amount)
                })
                .ToList();

            int extraRows = item.RandomDeconstructionOutput ? 1 : 0;
            int calculatedHeight = HeaderHeight + ((groupedOutputs.Count + extraRows) * RowHeight) + CardPadding;

            var card = new GUIFrame(new RectTransform(new Point(col.Content.Rect.Width, calculatedHeight), col.Content.RectTransform), "InnerFrame")
            {
                Color = Color.Black * 0.4f,
            };
            var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2 };

            var header = new GUILayoutGroup(new RectTransform(new Point(layout.Rect.Width, HeaderHeight), layout.RectTransform), isHorizontal: true);
            _ = new GUITextBlock(new RectTransform(new Vector2(0.7f, 1f), header.RectTransform), TextSOS.Get("sos.recipe.deconstructor", "DECONSTRUCTOR"), font: GUIStyle.SmallFont, textColor: Color.Yellow);
            _ = new GUITextBlock(new RectTransform(new Vector2(0.3f, 1f), header.RectTransform), $"{item.DeconstructTime}s", font: GUIStyle.SmallFont, textAlignment: Alignment.Right);

            if (item.RandomDeconstructionOutput)
                _ = new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, RowHeight), layout.RectTransform), TextSOS.Get("sos.recipe.random_outputs", "Gives [amount] at random:").Replace("[amount]", item.RandomDeconstructionOutputAmount.ToString()), font: GUIStyle.SmallFont, textColor: Color.Orange);

            foreach (var output in groupedOutputs)
            {
                var prefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier == output.ItemIdentifier);
                string extras = output.Commonness < 1f ? $" ({output.Commonness * 100}%)" : "";

                DrawCompactItemRow(layout, prefab, output.TotalAmount, true, extras, output.Commonness < 1f ? Color.Orange : Color.White, onPrimary, onSecondary);
            }
        }

        public static void DrawUseCard(GUIListBox col, Tuple<ItemPrefab, FabricationRecipe> data, Action<ItemPrefab> onPrimary, Action<ItemPrefab> onSecondary)
        {
            int calculatedHeight = HeaderHeight + RowHeight + CardPadding;
            var card = new GUIFrame(new RectTransform(new Point(col.Content.Rect.Width, calculatedHeight), col.Content.RectTransform), "InnerFrame")
            {
                Color = Color.Black * 0.4f,
            };
            var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2 };

            var recipe = data.Item2;
            string machine;
            if (recipe.SuitableFabricatorIdentifiers.Length > 0)
            {
                machine = string.Join(", ", recipe.SuitableFabricatorIdentifiers.Select(id =>
                    TextManager.Get("EntityName." + id).Fallback(id.Value).Value));
            }
            else { machine = TextSOS.Get("sos.recipe.hand", "Hand").Value; }

            new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, HeaderHeight), layout.RectTransform), machine.ToUpper(), font: GUIStyle.SmallFont, textColor: Color.Yellow);

            DrawCompactItemRow(layout, data.Item1, data.Item2.Amount, true, "", null, onPrimary, onSecondary);
        }

        public static void DrawSourceCard(GUIListBox col, ItemPrefab sourceItem, Action<ItemPrefab> onPrimary, Action<ItemPrefab> onSecondary)
        {
            int calculatedHeight = HeaderHeight + RowHeight + CardPadding;
            var card = new GUIFrame(new RectTransform(new Point(col.Content.Rect.Width, calculatedHeight), col.Content.RectTransform), "InnerFrame")
            {
                Color = Color.Black * 0.4f,
            };
            var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2 };

            new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, HeaderHeight), layout.RectTransform), TextSOS.Get("sos.recipe.deconstructing", "DECONSTRUCTING:"), font: GUIStyle.SmallFont, textColor: Color.LightGreen);
            DrawCompactItemRow(layout, sourceItem, 1, true, "", null, onPrimary, onSecondary);
        }

        public static void DrawCompactItemRow(GUIComponent parent, ItemPrefab? prefab, float amount, bool isCardInside, string extraText = "", Color? color = null, Action<ItemPrefab>? onPrimaryClick = null, Action<ItemPrefab>? onSecondaryClick = null)
        {
            var rowRect = new RectTransform(new Point(parent.Rect.Width, RowHeight), parent.RectTransform);

            GUIComponent container;

            if (prefab != null && (onPrimaryClick != null || onSecondaryClick != null))
            {
                var btn = new GUIButton(rowRect, style: "ListBoxElement")
                {
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
                container = new GUILayoutGroup(rowRect, isHorizontal: true) { AbsoluteSpacing = 5, CanBeFocused = false };
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

            string name = prefab?.Name.Value ?? TextSOS.Get("sos.gen.unknown", "???").Value;
            //string amtStr = amount > 1 || isCardInside ? $" x{amount}" : "";
            string amtStr = (amount > 1) ? $" x{amount}" : "";
            _ = new GUITextBlock(new RectTransform(new Vector2(0.8f, 1f), contentLayout.RectTransform), $"{name}{amtStr}{extraText}", font: GUIStyle.SmallFont, textColor: color ?? Color.White, textAlignment: Alignment.CenterLeft) { CanBeFocused = false };
        }
    }
}
