// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    public class SOSWindow
    {
        private readonly GUIFrame? mainFrame;
        private readonly GUIListBox? itemList;
        private readonly GUIFrame? detailsHeader;
        private readonly GUIListBox? colObtain;
        private readonly GUIListBox? colUsage;
        private readonly GUIListBox? metaPanel;
        private readonly SOSController controller;
        private readonly GUITextBox? searchBox;
        private GUIButton? btnBack;
        private GUIButton? btnForward;

        private List<ItemPrefab> allFilteredItems = new List<ItemPrefab>();
        private int itemsLoaded = 0;
        private const int ChunkSize = 50;
        private bool isUpdating = false;

        public SOSWindow(SOSController controller)
        {
            this.controller = controller;
            var parentComponent = Screen.Selected?.Frame;
            if (parentComponent == null) return;

            mainFrame = new GUIFrame(new RectTransform(new Vector2(0.95f, 0.9f), parentComponent.RectTransform, Anchor.Center), "InnerFrame") { CanBeFocused = true };

            var topBar = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.05f), mainFrame.RectTransform, Anchor.TopCenter), "GUIFrameBottom");
            _ = new GUITextBlock(new RectTransform(Vector2.One, topBar.RectTransform), TextSOS.Get("sos.window.title", "SOS - Recipe Browser"), textAlignment: Alignment.Center, font: GUIStyle.LargeFont);

            var historyLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.12f, 0.8f), topBar.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(10, 0) }, isHorizontal: true) { RelativeSpacing = 0.05f };
            btnBack = new GUIButton(new RectTransform(new Vector2(0.45f, 1f), historyLayout.RectTransform), "", style: "GUIButtonToggleLeft") { OnClicked = (_, _) => { controller.NavigateBack(); return true; } };
            if (btnBack.Children.FirstOrDefault() is GUIImage imgBack) imgBack.SpriteEffects = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;
            btnForward = new GUIButton(new RectTransform(new Vector2(0.45f, 1f), historyLayout.RectTransform), "", style: "GUIButtonToggleRight") { OnClicked = (_, _) => { controller.NavigateForward(); return true; } };

            var topButtons = new GUILayoutGroup(new RectTransform(new Vector2(0.2f, 0.8f), topBar.RectTransform, Anchor.CenterRight) { AbsoluteOffset = new Point(10, 0) }, isHorizontal: true) { Stretch = false, RelativeSpacing = 0.05f, ChildAnchor = Anchor.CenterRight };
            _ = new GUIButton(new RectTransform(new Vector2(0.2f, 1f), topButtons.RectTransform), "", style: "GUICancelButton") { OnClicked = (_, _) => { controller.ToggleUI(); return true; } };
            _ = new GUIButton(new RectTransform(new Vector2(0.65f, 1f), topButtons.RectTransform), TextSOS.Get("sos.window.clear_hud", "Clear HUD"), style: "DeviceButton") { OnClicked = (_, _) => { controller.Tracker.SetTrackedItem(null); return true; } };

            var contentArea = new GUILayoutGroup(new RectTransform(new Vector2(0.98f, 0.94f), mainFrame.RectTransform, Anchor.BottomCenter) { AbsoluteOffset = new Point(0, 5) }) { IsHorizontal = true, Stretch = true, RelativeSpacing = 0.01f };

            var leftPanel = new GUILayoutGroup(new RectTransform(new Vector2(0.22f, 1f), contentArea.RectTransform)) { Stretch = true, RelativeSpacing = 0.01f };
            var searchContainer = new GUIFrame(new RectTransform(new Vector2(1f, 0.04f), leftPanel.RectTransform), style: "InnerFrame");
            searchBox = GUI.CreateTextBoxWithPlaceholder(searchContainer.RectTransform, controller.LastSearchQuery, TextSOS.Get("sos.window.search_placeholder", "Search item..."));
            searchBox.OnTextChanged += (_, text) => { controller.LastSearchQuery = text; UpdateSearch(text); return true; };
            itemList = new GUIListBox(new RectTransform(new Vector2(1f, 0.95f), leftPanel.RectTransform), style: "PowerButtonFrame") { Padding = new Vector4(8, 5, 5, 5) };

            var centerPanel = new GUILayoutGroup(new RectTransform(new Vector2(0.53f, 1f), contentArea.RectTransform)) { Stretch = true, RelativeSpacing = 0.005f };
            detailsHeader = new GUIFrame(new RectTransform(new Vector2(1f, 0.1f), centerPanel.RectTransform), style: "CircuitBoxFrame") { Color = Color.Black * 0.4f };
            var recipeSplit = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.89f), centerPanel.RectTransform), isHorizontal: true) { Stretch = true, RelativeSpacing = 0.01f };

            var leftRecipeCol = new GUILayoutGroup(new RectTransform(new Vector2(0.5f, 1f), recipeSplit.RectTransform)) { Stretch = true };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), leftRecipeCol.RectTransform), TextSOS.Get("sos.window.obtain", "OBTAIN"), font: GUIStyle.SubHeadingFont, textColor: Color.LightGreen, textAlignment: Alignment.Center);
            colObtain = new GUIListBox(new RectTransform(new Vector2(1f, 0.95f), leftRecipeCol.RectTransform), style: "GUIBackgroundBlocker") { Spacing = 5 }; //PowerButtonFrame

            var rightRecipeCol = new GUILayoutGroup(new RectTransform(new Vector2(0.5f, 1f), recipeSplit.RectTransform)) { Stretch = true };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), rightRecipeCol.RectTransform), TextSOS.Get("sos.window.usage", "USAGE"), font: GUIStyle.SubHeadingFont, textColor: Color.Cyan, textAlignment: Alignment.Center);
            colUsage = new GUIListBox(new RectTransform(new Vector2(1f, 0.95f), rightRecipeCol.RectTransform), style: "GUIBackgroundBlocker") { Spacing = 5 };

            var rightPanel = new GUILayoutGroup(new RectTransform(new Vector2(0.25f, 1f), contentArea.RectTransform)) { Stretch = true };
            metaPanel = new GUIListBox(new RectTransform(new Vector2(1f, 1f), rightPanel.RectTransform), style: "CircuitBoxFrame")
            {
                Spacing = 10,
                Padding = new Vector4(12),
                CanBeFocused = false,
                Color = Color.Black * 0.4f
            };

            UpdateSearch(controller.LastSearchQuery);
            UpdateNavigationButtons();
        }

        public void UpdateNavigationButtons()
        {
            if (btnBack != null) btnBack.Enabled = controller.HistoryBack.Count > 0;
            if (btnForward != null) btnForward.Enabled = controller.HistoryForward.Count > 0;
        }

        public void RefreshSearch() => UpdateSearch(searchBox?.Text ?? "");

        public void Destroy()
        {
            if (mainFrame?.RectTransform != null)
            {
                mainFrame.RectTransform.Parent = null;
            }
        }

        private void UpdateSearch(string query)
        {
            if (itemList == null) return;

            allFilteredItems = ItemPrefab.Prefabs.Where(p =>
                string.IsNullOrWhiteSpace(query) ||
                p.Name.Value.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Identifier.Value.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Tags.Any(t => t.Value.Contains(query, StringComparison.OrdinalIgnoreCase))
            )
            .OrderByDescending(p => controller.FavoritedItems.Contains(p.Identifier.Value))
            .ThenBy(p => p.Name.Value)
            .ToList();

            itemsLoaded = 0;
            itemList.Content.ClearChildren();

            itemList.ScrollBar.BarScroll = 0;

            LoadNextChunk();
        }

        private void LoadNextChunk()
        {
            if (itemList == null || itemsLoaded >= allFilteredItems.Count || isUpdating) return;

            isUpdating = true;

            float totalScrollableHeightBefore = itemList.Content.Rect.Height - itemList.Rect.Height;
            float currentScrollPixels = itemList.ScrollBar.BarScroll * totalScrollableHeightBefore;

            int nextBatch = Math.Min(itemsLoaded + ChunkSize, allFilteredItems.Count);
            for (int i = itemsLoaded; i < nextBatch; i++)
            {
                var prefab = allFilteredItems[i];
                bool isFav = controller.FavoritedItems.Contains(prefab.Identifier.Value);

                var btn = new GUIButton(new RectTransform(new Point(itemList.Content.Rect.Width, 35), itemList.Content.RectTransform), style: "ListBoxElement")
                {
                    OnClicked = (_, _) => { controller.OnItemSelected(prefab); return true; },
                    OnSecondaryClicked = (_, _) => { controller.OpenContextMenu(prefab); return true; }
                };

                string prefix = isFav ? "* " : "";
                CardBuilder.DrawCompactItemRow(btn, prefab, 1, false, extraText: prefix, color: isFav ? Color.Gold : Color.White);
            }
            itemsLoaded = nextBatch;

            itemList.RecalculateChildren();
            itemList.UpdateScrollBarSize();

            float totalScrollableHeightAfter = itemList.Content.Rect.Height - itemList.Rect.Height;

            if (totalScrollableHeightAfter > 0)
            {
                // TODO: This not work properly at now...
                float newBarScroll = currentScrollPixels / totalScrollableHeightAfter;
                newBarScroll = MathHelper.Clamp(newBarScroll, 0, 0.80f);
                itemList.ScrollBar.BarScroll = newBarScroll;
            }

            isUpdating = false;
        }

        public void Update()
        {
            if (itemList == null || itemsLoaded >= allFilteredItems.Count || isUpdating) return;

            int total = allFilteredItems.Count;

            int currentIndex = (int)(itemList.ScrollBar.BarScroll * (total - 1));

            int threshold = total - 5;

            if (currentIndex >= threshold)
            {
                LoadNextChunk();
            }
        }

        public void UpdateDetailsPanel(ItemPrefab targetItem, List<FabricationRecipe> craft, List<DeconstructItem> decon, List<Tuple<ItemPrefab, FabricationRecipe>> uses, List<ItemPrefab> sources)
        {
            if (detailsHeader == null || colObtain == null || colUsage == null || metaPanel == null) return;

            detailsHeader.ClearChildren();

            var headerLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.85f, 1f), detailsHeader.RectTransform, Anchor.CenterRight), isHorizontal: true) { AbsoluteSpacing = 15 };

            Sprite? icon = targetItem.InventoryIcon ?? targetItem.Sprite;
            if (icon != null)
            {
                var imgFrame = new GUIFrame(new RectTransform(new Vector2(0.15f, 0.9f), headerLayout.RectTransform, Anchor.CenterLeft), style: null);
                _ = new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), imgFrame.RectTransform, Anchor.Center), icon, scaleToFit: true) { Color = targetItem.InventoryIconColor };
            }
            _ = new GUITextBlock(new RectTransform(new Vector2(0.8f, 1f), headerLayout.RectTransform), targetItem.Name.Value, font: GUIStyle.LargeFont, textColor: Color.White, textAlignment: Alignment.CenterLeft);

            Action<ItemPrefab> onPrimary = (clickedItem) => controller.OnItemSelected(clickedItem);
            Action<ItemPrefab> onSecondary = (clickedItem) => controller.OpenContextMenu(clickedItem);

            colObtain.Content.ClearChildren();
            foreach (var recipe in craft) CardBuilder.DrawCraftCard(colObtain, recipe, targetItem, controller, onPrimary, onSecondary); ;
            foreach (var source in sources) CardBuilder.DrawSourceCard(colObtain, source, onPrimary, onSecondary);

            colUsage.Content.ClearChildren();
            if (decon.Count > 0) CardBuilder.DrawDeconCard(colUsage, targetItem, decon, onPrimary, onSecondary);
            foreach (var use in uses) CardBuilder.DrawUseCard(colUsage, use, onPrimary, onSecondary);

            metaPanel.Content.ClearChildren();

            _ = new GUITextBlock(new RectTransform(new Point(metaPanel.Content.Rect.Width, 25), metaPanel.Content.RectTransform), TextSOS.Get("sos.window.additional_info", "ADDITIONAL INFO"), font: GUIStyle.SubHeadingFont, textColor: Color.Gold, textAlignment: Alignment.Center);

            void AddStat(string label, string? value, Color valColor)
            {
                if (string.IsNullOrEmpty(value)) return;

                var row = new GUILayoutGroup(new RectTransform(new Point(metaPanel.Content.Rect.Width, 25), metaPanel.Content.RectTransform), isHorizontal: true);
                _ = new GUITextBlock(new RectTransform(new Vector2(0.4f, 1f), row.RectTransform), label, font: GUIStyle.SmallFont, textColor: Color.Gray);
                _ = new GUITextBlock(new RectTransform(new Vector2(0.6f, 1f), row.RectTransform), value, font: GUIStyle.SmallFont, textColor: valColor);
            }

            string safeId = targetItem.Identifier.IsEmpty ? "unknown" : targetItem.Identifier.Value;
            AddStat(TextSOS.Get("sos.item.id", "ID:").Value, safeId, Color.White);

            int basePrice = targetItem.DefaultPrice?.Price ?? 0;
            AddStat(TextSOS.Get("sos.item.price", "Price:").Value, $"{basePrice} mk", Color.Yellow);

            AddStat(TextSOS.Get("sos.item.max_stack", "Max Stack:").Value, targetItem.MaxStackSize.ToString(), Color.White);

            if (!targetItem.Description.IsNullOrEmpty())
            {
                _ = new GUITextBlock(new RectTransform(new Point(metaPanel.Content.Rect.Width, 20), metaPanel.Content.RectTransform), TextSOS.Get("sos.item.description", "DESCRIPTION:"), font: GUIStyle.SmallFont, textColor: Color.Gold);

                var descBlock = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), metaPanel.Content.RectTransform), targetItem.Description.Value, font: GUIStyle.SmallFont, textColor: Color.LightGray)
                {
                    Wrap = true,
                    CanBeFocused = false
                };

                int h = Math.Max((int)descBlock.TextSize.Y + 10, 30);
                descBlock.RectTransform.NonScaledSize = new Point(descBlock.Rect.Width, h);
            }

            if (targetItem.Tags != null && targetItem.Tags.Count() > 0)
            {
                _ = new GUITextBlock(new RectTransform(new Point(metaPanel.Content.Rect.Width, 20), metaPanel.Content.RectTransform), TextSOS.Get("sos.item.tags", "TAGS:"), font: GUIStyle.SmallFont, textColor: Color.Gold);

                var tagsContainer = new GUIListBox(new RectTransform(new Point(metaPanel.Content.Rect.Width, 45), metaPanel.Content.RectTransform), isHorizontal: true, style: null)
                {
                    Spacing = 1,
                    ScrollBarVisible = false,
                    CanBeFocused = false,
                    ScrollBar = { BarSize = 10, Color = Color.LightSkyBlue * 0.5f }
                };

                foreach (var tag in targetItem.Tags)
                {
                    if (tag.IsEmpty) continue;

                    var tagBadge = new GUIButton(new RectTransform(new Point(10, 24), tagsContainer.Content.RectTransform), style: "OuterGlow")
                    {
                        Color = Color.LightSkyBlue * 0.2f,
                        OnClicked = (_, _) =>
                        {
                            if (searchBox != null) searchBox.Text = tag.Value;
                            UpdateSearch(tag.Value);
                            return true;
                        }
                    };

                    var tagText = new GUITextBlock(new RectTransform(Vector2.One, tagBadge.RectTransform), tag.Value.ToLower(), font: GUIStyle.SmallFont, textAlignment: Alignment.Center)
                    {
                        Padding = new Vector4(12, 0, 12, 0),
                        TextColor = Color.LightSkyBlue,
                        CanBeFocused = false
                    };
                    tagBadge.RectTransform.NonScaledSize = new Point((int)tagText.TextSize.X + 24, 24);
                }
            }
        }
    }
}
