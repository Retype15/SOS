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

        private List<GUIDesplegableBox> activeDropdowns = new List<GUIDesplegableBox>();

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
                Padding = new Vector4(18, 15, 18, 15),
                CanBeFocused = true,
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
            activeDropdowns.Clear();

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
            activeDropdowns.Clear();
            if (detailsHeader == null || colObtain == null || colUsage == null || metaPanel == null) return;
            metaPanel.Content.ClearChildren();

            detailsHeader.ClearChildren();
            var headerLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.85f, 1f), detailsHeader.RectTransform, Anchor.CenterRight), isHorizontal: true) { AbsoluteSpacing = 15 };
            Sprite? icon = targetItem.InventoryIcon ?? targetItem.Sprite;
            if (icon != null)
            {
                var imgFrame = new GUIFrame(new RectTransform(new Vector2(0.15f, 0.9f), headerLayout.RectTransform, Anchor.CenterLeft), style: null);
                _ = new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), imgFrame.RectTransform, Anchor.Center), icon, scaleToFit: true) { Color = targetItem.InventoryIconColor };
            }
            _ = new GUITextBlock(new RectTransform(new Vector2(0.8f, 1f), headerLayout.RectTransform), targetItem.Name.Value, font: GUIStyle.LargeFont, textColor: Color.White, textAlignment: Alignment.CenterLeft);

            Action<ItemPrefab> onPrimary = (p) => controller.OnItemSelected(p);
            Action<ItemPrefab> onSecondary = (p) => controller.OpenContextMenu(p);
            
            colObtain.Content.ClearChildren();
            foreach (var r in craft) CardBuilder.DrawCraftCard(colObtain, r, targetItem, controller, onPrimary, onSecondary);
            foreach (var s in sources) CardBuilder.DrawSourceCard(colObtain, s, onPrimary, onSecondary);
            
            colUsage.Content.ClearChildren();
            if (decon.Count > 0) CardBuilder.DrawDeconCard(colUsage, targetItem, decon, onPrimary, onSecondary);
            foreach (var u in uses) CardBuilder.DrawUseCard(colUsage, u, onPrimary, onSecondary);


            var onBadgeClick = (string tag) => { if (searchBox != null) searchBox.Text = tag; UpdateSearch(tag); };

            var builder = new SectionBuilder
            (
                metaPanel, 
                onBadgeClick, 
                controller, 
                onPrimary, 
                onSecondary
            ); 

            var analysis = RecipeAnalyzer.GetAnalysis(targetItem);


            foreach (var section in analysis.Sections)
            {
                section.Draw(builder);
            }

            //metaPanel.RecalculateChildren();
            //metaPanel.UpdateScrollBarSize();
            //metaPanel.ScrollBar.BarScroll = 0;
        }
    }

    public class GUIDesplegableBox
    {
        public GUIDesplegableBox(GUIComponent parent, Action<string> onbBadgeClick, string labelText, IEnumerable<string> tags, List<ItemPrefab> items, SOSController controller, Action<ItemPrefab> onPrimary, Action<ItemPrefab> onSecondary)
        {
            //var leftSpacing = 5;

            var row = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0f), parent.RectTransform) { MinSize = new Point(0, 24) }, isHorizontal: true)
            {
                CanBeFocused = false,
                AbsoluteSpacing = 5,
            };

            var label = new GUITextBlock(new RectTransform(new Vector2(0.3f, 1f), row.RectTransform), labelText, font: GUIStyle.SmallFont, textColor: Color.Gray) { CanBeFocused = false };

            var badgeFrame = new GUIFrame(new RectTransform(new Vector2(0.55f, 1f), row.RectTransform), style: null) { CanBeFocused = false };
            GUIBadgeList.Create(badgeFrame.RectTransform, tags, onbBadgeClick);

            var dropDown = new GUIDropDown2(new RectTransform(new Point(36, 24), row.RectTransform), elementCount: Math.Min(items.Count, 8), listBoxWidth: (int)(row.Rect.Width * 0.95f), style: "GUIDropDown", expandToRight: false);

            foreach (var item in items)
            {
                bool isFav = controller.FavoritedItems.Contains(item.Identifier.Value);
                string prefix = isFav ? "* " : "";

                CardBuilder.DrawCompactItemRow(dropDown.ListBox.Content, item, 1, true, prefix, isFav ? Color.Gold : Color.White,
                    onPrimaryClick: (p) => { onPrimary?.Invoke(p); dropDown.Dropped = false; },
                    onSecondaryClick: onSecondary);
            }
        }
    }

    public static class GUIBadgeList
    {
        public static void Create(RectTransform targetRect, IEnumerable<string> items, Action<string> onClick)
        {
            var list = new GUIListBox(targetRect, isHorizontal: true, style: "GUIBackgroundBlocker")
            {
                Spacing = 4,
                //ScrollBarEnabled = true,
                //ScrollBarVisible = false,
                //CanBeFocused = false
            };


            /*if (list.ScrollBar != null)
            {
                list.ScrollBar.Color = Color.White;
                //list.ScrollBar.CanBeFocused = false;
                //list.ScrollBar.RectTransform.MaxSize = new Point(0, 0);
            }*/

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;

                var tagBadge = new GUIButton(new RectTransform(new Vector2(0.1f, 0.9f), list.Content.RectTransform), style: "OuterGlow")
                {
                    Color = Color.LightSkyBlue * 0.15f,
                    OnClicked = (_, _) => { onClick?.Invoke(item); return true; }
                };

                var tagText = new GUITextBlock(new RectTransform(Vector2.One, tagBadge.RectTransform), item.ToLower(), font: GUIStyle.SmallFont, textAlignment: Alignment.Center)
                {
                    Padding = new Vector4(8, 0, 8, 0),
                    TextColor = Color.LightSkyBlue,
                    CanBeFocused = false,
                    Wrap = false
                };

                int calculatedWidth = (int)tagText.TextSize.X + 16;
                tagBadge.RectTransform.MinSize = new Point(calculatedWidth, 0);
                tagBadge.RectTransform.MaxSize = new Point(calculatedWidth, int.MaxValue);
            }
        }
    }
}
