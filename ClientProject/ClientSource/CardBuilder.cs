// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0079
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;


namespace SOS
{
    public static class CardBuilder
    {
        private const int RowHeight = 22;
        private const int HeaderHeight = 20;
        private const int CardPadding = 10;

        private static readonly Dictionary<Identifier, string> machineNameCache = [];

        public static RichString GetDetailedTooltip(ItemPrefab prefab)
        {
            LocalizedString name = prefab.Name;
            LocalizedString description = prefab.Description;
            int price = prefab.DefaultPrice?.Price ?? 0;

            string colorWhite = Color.White.ToStringHex();
            string colorGold = Color.Gold.ToStringHex();
            string toolTip = $"‖color:{colorWhite}‖{name}‖color:end‖";

#if DEBUG
            toolTip += $" ({prefab.Identifier})";
            var list = new List<string>();
#endif

            if (price > 0)
            {
                toolTip += $"\n‖color:{colorGold}‖{TextSOS.Get("sos.item.price", "Price")}{price}mk‖color:end‖";
            }
            if (!description.IsNullOrEmpty())
            {
                toolTip += '\n' + description.Value;
            }

            if (prefab.ContentPackage?.Name != GameMain.VanillaContent?.Name && prefab.ContentPackage != null)
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
                machine = string.Join(", ", recipe.SuitableFabricatorIdentifiers.Select(id => ResolveMachineName(id)));
            }
            else
            {
                machine = TextSOS.Get("sos.recipe.hand", "Hand").Value;
            }

            string headerText = $"{machine}:".ToUpper();

            var header = new GUILayoutGroup(new RectTransform(new Point(layout.Rect.Width, HeaderHeight), layout.RectTransform), isHorizontal: true) { CanBeFocused = false };
            _ = new GUITextBlock(new RectTransform(new Vector2(0.7f, 1f), header.RectTransform), headerText, font: GUIStyle.SmallFont, textColor: isTracked ? Color.Gold : Color.Yellow) { CanBeFocused = false };
            _ = new GUITextBlock(new RectTransform(new Vector2(0.3f, 1f), header.RectTransform), $"{recipe.RequiredTime}s", font: GUIStyle.SmallFont, textAlignment: Alignment.Right) { CanBeFocused = false };

            if (recipe.RequiresRecipe)
            {
                bool hasUnlocked = Character.Controlled != null && Character.Controlled.HasRecipeForItem(targetItem.Identifier);

                Color recipeColor = hasUnlocked ? Color.LightGreen : Color.Salmon;
                string recipeText = hasUnlocked ? TextSOS.Get("sos.recipe.unlocked", "Recipe Unlocked").Value : TextSOS.Get("sos.recipe.locked", "Requires Recipe to Unlock").Value;

                _ = new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, RowHeight), layout.RectTransform), recipeText, font: GUIStyle.SmallFont, textColor: recipeColor) { CanBeFocused = false };
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

                _ = new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, RowHeight), layout.RectTransform),
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
            var groupsByMachine = deconList
                .GroupBy(di => string.Join(",", di.RequiredDeconstructor.Select(id => id.Value).OrderBy(s => s)))
                .ToList();

            foreach (var machineGroup in groupsByMachine)
            {
                float poolTotalWeight = machineGroup.Sum(di => di.Commonness);

                var groupedOutputs = machineGroup
                    .GroupBy(di => new {
                        di.ItemIdentifier,
                        OtherKey = string.Join(",", di.RequiredOtherItem.Select(id => id.Value).OrderBy(x => x))
                    })
                    .Select(g => new
                    {
                        g.Key.ItemIdentifier,
                        MaxAmount = g.Max(di => di.Amount),
                        Machines = g.First().RequiredDeconstructor,
                        RequiredOtherItems = g.First().RequiredOtherItem,
                        ItemTotalWeight = g.Sum(di => di.Commonness)
                    })
                    .ToList();

                int totalRows = groupedOutputs.Count;
                totalRows += groupedOutputs.Sum(go => go.RequiredOtherItems.Length);
                if (item.RandomDeconstructionOutput) totalRows++;

                int spacing = 2;
                int calculatedHeight = HeaderHeight + (totalRows * RowHeight) + (totalRows * spacing) + CardPadding + 6;

                var card = new GUIFrame(new RectTransform(new Point(col.Content.Rect.Width, calculatedHeight), col.Content.RectTransform), "InnerFrame")
                {
                    Color = Color.Black * 0.4f,
                };

                var machineIds = groupedOutputs[0].Machines;
                string machineName = (machineIds == null || machineIds.Length == 0)
                    ? ResolveMachineName("deconstructor".ToIdentifier())
                    : string.Join(", ", machineIds.Select(id => ResolveMachineName(id)));

                var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center))
                {
                    AbsoluteSpacing = spacing,
                    CanBeFocused = false
                };

                var header = new GUILayoutGroup(new RectTransform(new Point(layout.Rect.Width, HeaderHeight), layout.RectTransform), isHorizontal: true) { CanBeFocused = false };
                _ = new GUITextBlock(new RectTransform(new Vector2(0.7f, 1f), header.RectTransform), $"{machineName}:".ToUpper(), font: GUIStyle.SmallFont, textColor: Color.Yellow) { CanBeFocused = false };
                _ = new GUITextBlock(new RectTransform(new Vector2(0.3f, 1f), header.RectTransform), $"{item.DeconstructTime}s", font: GUIStyle.SmallFont, textAlignment: Alignment.Right) { CanBeFocused = false };

                if (item.RandomDeconstructionOutput)
                {
                    string randomText = TextSOS.Get("sos.recipe.random_outputs", "Gives [amount] at random:")
                                        .Replace("[amount]", item.RandomDeconstructionOutputAmount.ToString()).Value;
                    _ = new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, RowHeight), layout.RectTransform),
                        randomText, font: GUIStyle.SmallFont, textColor: Color.Orange)
                    { CanBeFocused = false };
                }

                foreach (var output in groupedOutputs)
                {
                    string extras = "";
                    Color itemColor = Color.White;

                    if (item.RandomDeconstructionOutput)
                    {
                        if (poolTotalWeight > 0)
                        {
                            float chance = (output.ItemTotalWeight / poolTotalWeight) * 100f;
                            extras = $" ({chance:0.#}%)";
                            itemColor = Color.Orange;
                        }
                    }
                    else
                    {
                        float chance = Math.Min(output.ItemTotalWeight, 1f);
                        if (chance < 1f)
                        {
                            extras = $" ({chance * 100:0.#}%)";
                            itemColor = Color.Orange;
                        }
                    }

                    var prefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier == output.ItemIdentifier);

                    DrawCompactItemRow(layout, prefab, output.MaxAmount, true, extras, itemColor, onPrimary, onSecondary);

                    foreach (var extraId in output.RequiredOtherItems)
                    {
                        var extraPrefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier == extraId);
                        if (extraPrefab != null)
                        {
                            DrawCompactItemRow(layout, extraPrefab, 1, true, " + ", Color.Cyan, onPrimary, onSecondary);
                        }
                    }
                }
            }
        }

        public static void DrawUseCard(GUIListBox col, GroupedUsage usage, Action<ItemPrefab> onPrimary, Action<ItemPrefab> onSecondary)
        {
            int calculatedHeight = HeaderHeight + RowHeight + CardPadding;
            var card = new GUIFrame(new RectTransform(new Point(col.Content.Rect.Width, calculatedHeight), col.Content.RectTransform), "InnerFrame")
            {
                Color = Color.Black * 0.4f,
            };
            var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2 };

            string machine;
            if (usage.MachineIds != null && usage.MachineIds.Count > 0)
            {
                machine = string.Join(", ", usage.MachineIds.Select(id => ResolveMachineName(id)));
            }
            else
            {
                machine = TextSOS.Get("sos.recipe.hand", "Hand").Value;
            }

            _ = new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, HeaderHeight), layout.RectTransform),
                $"{machine}:".ToUpper(), font: GUIStyle.SmallFont, textColor: Color.Yellow);

            string requirementInfo = usage.AmountRequired > 1 ? $" (pide x{usage.AmountRequired})" : "";

            DrawCompactItemRow(layout, usage.TargetItem, usage.AmountCreated, true, requirementInfo, null, onPrimary, onSecondary);
        }

        public static void DrawSourceCard(GUIListBox col, GroupedSource group, Action<ItemPrefab> onPrimary, Action<ItemPrefab> onSecondary)
        {
            int extraRows = group.RequiredOtherItems?.Count ?? 0;
            int calculatedHeight = HeaderHeight + RowHeight + (extraRows * RowHeight) + CardPadding;
            var card = new GUIFrame(new RectTransform(new Point(col.Content.Rect.Width, calculatedHeight), col.Content.RectTransform), "InnerFrame")
            {
                Color = Color.Black * 0.4f,
            };
            var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.95f), card.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2 };

            string machineName = (group.MachineIds == null || group.MachineIds.Length == 0)
                ? ResolveMachineName("deconstructor".ToIdentifier())
                : string.Join(", ", group.MachineIds.Select(id => ResolveMachineName(id)));

            string headerText = (machineName + ':').ToUpper();

            _ = new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, HeaderHeight), layout.RectTransform),
                headerText, font: GUIStyle.SmallFont, textColor: Color.LightGreen);

            DrawCompactItemRow(layout, group.SourceItem, 1, true, "", null, onPrimary, onSecondary);

            foreach (var otherId in group.RequiredOtherItems ?? [])
            {
                var otherPrefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier == otherId);
                if (otherPrefab != null)
                {
                    string prefix = " + ";
                    DrawCompactItemRow(layout, otherPrefab, 1, true, prefix, Color.Cyan, onPrimary, onSecondary);
                }
            }
            // TODO: That's no convince me, maybe change disposition?
            /*if (group.IsRandom && group.TotalCommonness > 0)
            {
                _ = new GUITextBlock(new RectTransform(new Point(layout.Rect.Width, RowHeight / 2), layout.RectTransform),
                    TextSOS.Get("sos.recipe.chance", "Chance based output").Value, font: GUIStyle.SmallFont, textColor: Color.Gray * 0.8f, textAlignment: Alignment.Right);
            }*/
        }

        public static void DrawCompactItemRow(GUIComponent parent, ItemPrefab? prefab, float amount, bool isCardInside, string extraText = "", Color? color = null, Action<ItemPrefab>? onPrimaryClick = null, Action<ItemPrefab>? onSecondaryClick = null)
        {
            var rowRect = new RectTransform(new Point(parent.Rect.Width, RowHeight), parent.RectTransform);

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

            string amtStr = (amount > 1) ? $" x{amount}" : "";
            _ = new GUITextBlock(
                new RectTransform(new Vector2(0.8f, 1f), contentLayout.RectTransform),
                $"{nameStr}{amtStr}{extraText}",
                font: GUIStyle.SmallFont,
                textColor: aColor,
                textAlignment: Alignment.CenterLeft
            )
            {
                CanBeFocused = false
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
