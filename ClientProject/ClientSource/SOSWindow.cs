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
    public enum DisplayMode
    {
        Normal,
        Compact,
        Hidden
    }

    public class SOSWindow
    {
        private readonly GUIResizableFrame? mainFrame;
        private readonly GUIListBox? itemList;
        private readonly GUIFrame? detailsHeader;
        private readonly GUIFrame? recipeAreaFrame;
        private readonly GUIListBox? colObtain;
        private readonly GUIListBox? colUsage;
        private readonly GUIListBox? metaPanel;
        private readonly SOSController controller;
        private readonly GUITextBox? searchBox;
        private readonly GUIButton? btnBack;
        private readonly GUIButton? btnForward;

        private readonly GUIFrame? contentArea;
        private readonly GUIResizableFrame? leftPanel;
        private readonly GUIFrame? leftContainer;
        private readonly GUIFrame? centerPanelContainer;
        private readonly GUIResizableFrame? rightPanel;
        private readonly GUIFrame? rightContainer;
        private GUIFrame? layoutMenuFrame;

        private List<ItemPrefab> allFilteredItems = [];
        private int itemsLoaded = 0;
        private const int ChunkSize = 50;
        private bool isUpdating = false;

        private readonly List<GUIDesplegableBox> activeDropdowns = [];

        private DisplayMode leftPanelMode = DisplayMode.Normal;
        private DisplayMode centerPanelMode = DisplayMode.Normal;
        private DisplayMode rightPanelMode = DisplayMode.Normal;
        private int lastLeftWForReflow = 0;

        private const int HeaderHeight = 48;
        private const int BottomMargin = 10;
        private const int SidebarHiddenThreshold = 70;
        private const int SidebarCompactThreshold = 240;
        private const int CenterCompactThreshold = 250;
        private const int MinCenterWidth = 200;
        private int lastCenterWForReflow = 0;

        private readonly GUITextViewer? xmlContentText;
        private readonly GUITickBox? rawXmlTickBox;

        private ItemPrefab? currentItem;
        private List<FabricationRecipe>? currentCraft;
        private List<DeconstructItem>? currentDecon;
        private List<Tuple<ItemPrefab, FabricationRecipe>>? currentUses;
        private List<Tuple<ItemPrefab, DeconstructItem>>? currentSources;

        private static readonly Dictionary<Identifier, string> itemSlotCache = [];

        public SOSWindow(SOSController controller)
        {
            this.controller = controller;
            var parentComponent = Screen.Selected?.Frame;
            if (parentComponent == null) return;

            mainFrame = new GUIResizableFrame(new RectTransform(new Vector2(0.95f, 0.9f), parentComponent.RectTransform, Anchor.TopLeft), style: "CircuitBoxFrame")
            {
                CanBeFocused = true,
                Selected = true,
                Color = Color.Black * 0.85f,
                AllowedDirections = ResizeDirection.All
            };
            mainFrame.RectTransform.MinSize = new Point(400, 200);

            Point initialSize = controller.WindowSize ?? new Point(1000, 700);

            mainFrame.RectTransform.NonScaledSize = initialSize;

            if (controller.WindowPosition.HasValue)
            {
                mainFrame.RectTransform.AbsoluteOffset = controller.WindowPosition.Value;
            }
            else
            {
                int centerX = (GameMain.GraphicsWidth / 2) - (initialSize.X / 2);
                int centerY = (GameMain.GraphicsHeight / 2) - (initialSize.Y / 2);

                mainFrame.RectTransform.AbsoluteOffset = new Point(centerX, centerY);
            }

            var topBar = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.0f), mainFrame.RectTransform, Anchor.TopCenter), "GUIFrameBottom");
            topBar.RectTransform.MinSize = new Point(0, HeaderHeight);
            topBar.RectTransform.MaxSize = new Point(int.MaxValue, HeaderHeight);

            _ = new GUITextBlock(new RectTransform(Vector2.One, topBar.RectTransform),
                TextSOS.Get("sos.window.title", "SOS - Recipe Browser"),
                textAlignment: Alignment.Center, font: GUIStyle.LargeFont);

            var leftTools = new GUILayoutGroup(new RectTransform(new Vector2(0.35f, 0.8f), topBar.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(10, 0) }, isHorizontal: true)
            {
                AbsoluteSpacing = 5,
                Stretch = false
            };

            var btnLayouts = new GUIButton(new RectTransform(new Point(32, 32), leftTools.RectTransform), "", style: "GUIButtonSettings")
            {
                ToolTip = TextSOS.Get("sos.window.settings", "Settings (WIP)"),
                OnClicked = (btn, _) => { ToggleLayoutMenu(btn); return true; }
            };
            btnBack = new GUIButton(new RectTransform(new Point(32, 32), leftTools.RectTransform), "", style: "GUIButtonToggleLeft")
            {
                OnClicked = (_, _) => { controller.NavigateBack(); return true; }
            };
            if (btnBack.Children.FirstOrDefault() is GUIImage imgB) imgB.SpriteEffects = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;

            btnForward = new GUIButton(new RectTransform(new Point(32, 32), leftTools.RectTransform), "", style: "GUIButtonToggleRight")
            {
                OnClicked = (_, _) => { controller.NavigateForward(); return true; }
            };

            var topButtons = new GUILayoutGroup(new RectTransform(new Vector2(0.2f, 0.8f), topBar.RectTransform, Anchor.CenterRight) { AbsoluteOffset = new Point(10, 0) }, isHorizontal: true) { Stretch = false, RelativeSpacing = 0.05f, ChildAnchor = Anchor.CenterRight };
            _ = new GUIButton(new RectTransform(new Vector2(0.2f, 1f), topButtons.RectTransform), "", style: "GUICancelButton")
            {
                OnClicked = (_, _) => { controller.ToggleUI(); return true; },
                ToolTip = TextSOS.Get("sos.gen.close", "Close [Esc]")
            };
            _ = new GUIButton(new RectTransform(new Vector2(0.65f, 1f), topButtons.RectTransform), TextSOS.Get("sos.window.clear_hud", "Clear HUD"), style: "DeviceButton")
            {
                OnClicked = (_, _) => { controller.Tracker.SetTrackedItem(null); return true; },
                ToolTip = TextSOS.Get("sos.window.clear_hud_tooltip", "Clears the active HUD tracker")
            };

            contentArea = new GUIFrame(new RectTransform(new Vector2(0.98f, 0.0f), mainFrame.RectTransform, Anchor.BottomCenter), style: null);

            leftPanel = new GUIResizableFrame(new RectTransform(new Vector2(0.20f, 1f), contentArea.RectTransform, Anchor.TopLeft), style: "InnerFrame")
            {
                AllowedDirections = ResizeDirection.Right,
                IsFixed = true,
                Color = Color.Black * 0.4f
            };
            leftPanel.RectTransform.MinSize = new Point(20, 50);
            leftPanel.RectTransform.MaxSize = new Point(500, 2000);

            if (controller.LeftPanelWidth.HasValue)
            {
                leftPanel.RectTransform.NonScaledSize = new Point(controller.LeftPanelWidth.Value, 0);
            }

            leftContainer = new GUIFrame(new RectTransform(new Vector2(0.95f, 0.98f), leftPanel.RectTransform, Anchor.Center), style: null);
            var leftLayout = new GUILayoutGroup(new RectTransform(Vector2.One, leftContainer.RectTransform)) { Stretch = true, RelativeSpacing = 0.01f };

            var searchContainer = new GUIFrame(new RectTransform(new Vector2(1f, 0.05f), leftLayout.RectTransform), style: "InnerFrame");

            searchContainer.RectTransform.MinSize = new Point(0, 35);
            searchContainer.RectTransform.MaxSize = new Point(int.MaxValue, 35);

            searchBox = GUI.CreateTextBoxWithPlaceholder(new RectTransform(Vector2.One, searchContainer.RectTransform), controller.LastSearchQuery, TextSOS.Get("sos.window.search_placeholder", "Search item..."));
            searchBox.ToolTip = TextSOS.Get("sos.window.search_tooltip", "Search by name, ID or tags");
            searchBox.OnTextChanged += (_, text) => { controller.LastSearchQuery = text; UpdateSearch(text); return true; };

            itemList = new GUIListBox(new RectTransform(new Vector2(1f, 1f), leftLayout.RectTransform), style: "GUIListBox")
            {
                Padding = new Vector4(8, 5, 5, 5),
                Color = Color.Black * 0.2f
            };
            itemList.RectTransform.MinSize = new Point(0, 50);

            centerPanelContainer = new GUIFrame(new RectTransform(new Vector2(0.52f, 1f), contentArea.RectTransform, Anchor.TopLeft), style: null);
            var centerLayout = new GUILayoutGroup(new RectTransform(Vector2.One, centerPanelContainer.RectTransform))
            {
                Stretch = true,
                RelativeSpacing = 0.01f
            };
            centerPanelContainer.RectTransform.MinSize = new Point(200, 50);

            detailsHeader = new GUIFrame(new RectTransform(new Vector2(1f, 0.15f), centerLayout.RectTransform), style: "CircuitBoxFrame")
            {
                Color = Color.Black * 0.4f
            };
            detailsHeader.RectTransform.MinSize = new Point(0, 95);
            detailsHeader.RectTransform.MaxSize = new Point(int.MaxValue, 95);

            recipeAreaFrame = new GUIFrame(new RectTransform(new Vector2(1f, 0.84f), centerLayout.RectTransform), style: null);
            var recipeSplit = new GUILayoutGroup(new RectTransform(Vector2.One, recipeAreaFrame.RectTransform), isHorizontal: true)
            {
                Stretch = true,
                RelativeSpacing = 0.02f
            };

            var obtainContainer = new GUILayoutGroup(new RectTransform(new Vector2(0.49f, 1f), recipeSplit.RectTransform)) { Stretch = true };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), obtainContainer.RectTransform), TextSOS.Get("sos.window.obtain", "OBTAIN"), font: GUIStyle.SubHeadingFont, textColor: Color.LightGreen, textAlignment: Alignment.Center);
            colObtain = new GUIListBox(new RectTransform(new Vector2(1f, 0.95f), obtainContainer.RectTransform), style: "GUIListBox")
            {
                Spacing = 5,
                Color = Color.Black * 0.3f
            };

            var usageContainer = new GUILayoutGroup(new RectTransform(new Vector2(0.49f, 1f), recipeSplit.RectTransform)) { Stretch = true };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), usageContainer.RectTransform), TextSOS.Get("sos.window.usage", "USAGE"), font: GUIStyle.SubHeadingFont, textColor: Color.Cyan, textAlignment: Alignment.Center);
            colUsage = new GUIListBox(new RectTransform(new Vector2(1f, 0.95f), usageContainer.RectTransform), style: "GUIListBox")
            {
                Spacing = 5,
                Color = Color.Black * 0.3f
            };

            rightPanel = new GUIResizableFrame(new RectTransform(new Vector2(0.24f, 1f), contentArea.RectTransform, Anchor.TopRight), style: "InnerFrame")
            {
                AllowedDirections = ResizeDirection.Left,
                IsFixed = true,
                Color = Color.Black * 0.4f
            };
            rightPanel.RectTransform.MinSize = new Point(20, 50);
            rightPanel.RectTransform.MaxSize = new Point(1000, 2000);

            if (controller.RightPanelWidth.HasValue)
            {
                rightPanel.RectTransform.NonScaledSize = new Point(controller.RightPanelWidth.Value, 0);
            }

            rightContainer = new GUIFrame(new RectTransform(new Vector2(0.95f, 0.98f), rightPanel.RectTransform, Anchor.Center), style: null);
            var rightLayout = new GUILayoutGroup(new RectTransform(Vector2.One, rightContainer.RectTransform)) { Stretch = true };

            var rightHeaderArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.045f), rightLayout.RectTransform), style: null);
            rightHeaderArea.RectTransform.MinSize = new Point(0, 32);
            rawXmlTickBox = new GUITickBox(new RectTransform(new Vector2(1f, 0.45f), rightHeaderArea.RectTransform, Anchor.CenterLeft), TextSOS.Get("sos.window.raw_xml", "RAW XML"), font: GUIStyle.SmallFont, style: "SwitchHorizontal")
            {
                Selected = controller.RawXmlMode
            };

            var rightContentArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.955f), rightLayout.RectTransform), style: null);

            metaPanel = new GUIListBox(new RectTransform(Vector2.One, rightContentArea.RectTransform), style: "GUIListBox")
            {
                Spacing = 10,
                Padding = new Vector4(18, 15, 18, 15),
                CanBeFocused = true,
                Color = Color.Black * 0.4f,
            };

            xmlContentText = new GUITextViewer(new RectTransform(Vector2.One, rightContentArea.RectTransform), style: "GUITextBlock")
            {
                Visible = controller.RawXmlMode
            };
            metaPanel.Visible = !controller.RawXmlMode;

            if (metaPanel.ContentBackground != null) metaPanel.ContentBackground.Color = Color.Transparent;

            rawXmlTickBox.OnSelected = (tick) =>
            {
                controller.RawXmlMode = tick.Selected;
                controller.MarkDirty();
                metaPanel.Visible = !tick.Selected;
                if (xmlContentText != null)
                {
                    xmlContentText.Visible = tick.Selected;
                }
                return true;
            };

            UpdateSearch(controller.LastSearchQuery);
            UpdateNavigationButtons();

            UpdateLayout();
            mainFrame.ForceLayoutRecalculation();
        }

        public void SetSelected()
        {
            if (mainFrame == null) return;
            mainFrame.Selected = true;
        }

        private static string GetItemSlotsCached(ItemPrefab prefab)
        {
            if (itemSlotCache.TryGetValue(prefab.Identifier, out var cached)) return cached;

            if (prefab.ConfigElement == null) return itemSlotCache[prefab.Identifier] = "";

            var slots = new List<string>();
            foreach (var element in prefab.ConfigElement.Descendants())
            {
                string n = element.Name.ToString().ToLowerInvariant();
                if (n == "wearable" || n == "holdable")
                {
                    string s = element.GetAttributeString("slots", "");
                    if (!string.IsNullOrEmpty(s)) slots.Add(s.Replace("+", " "));
                }
            }

            return itemSlotCache[prefab.Identifier] = string.Join(" ", slots).ToLowerInvariant();
        }

        public void UpdateNavigationButtons()
        {
            if (btnBack != null)
            {
                btnBack.Enabled = controller.HistoryBack.Count > 0;
                if (btnBack.Enabled)
                {
                    var prevItem = controller.HistoryBack.Peek();

                    var (navBackName, _) = SafeItemName.Get(prevItem, Color.White);
                    // TODO: Use colored Text.
                    btnBack.ToolTip = $"{TextSOS.Get("sos.window.back", "Back")}: {navBackName}";
                }
            }

            if (btnForward != null)
            {
                btnForward.Enabled = controller.HistoryForward.Count > 0;
                if (btnForward.Enabled)
                {
                    var nextItem = controller.HistoryForward.Peek();

                    var (navForwardName, _) = SafeItemName.Get(nextItem, Color.White);

                    // TODO: Use colored Text.
                    btnForward.ToolTip = $"{TextSOS.Get("sos.window.forward", "Forward")}: {navForwardName}";
                }
            }
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
            string lowerQuery = query.ToLowerInvariant();

            allFilteredItems = [.. ItemPrefab.Prefabs.Where(p =>
                string.IsNullOrWhiteSpace(query) ||
                p.Name.Value.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Identifier.Value.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Tags.Any(t => t.Value.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                GetItemSlotsCached(p).Contains(lowerQuery)
            )
            .OrderByDescending(p => controller.FavoritedItems.Contains(p.Identifier.Value))
            .ThenBy(p => p.Name.Value)];

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

            if (leftPanelMode == DisplayMode.Compact)
            {
                int slotSize = 34;
                int itemsInRow = 0;
                int availableWidth = (int)itemList.Rect.Width - 14;
                int maxItemsPerRow = Math.Max(1, availableWidth / slotSize);

                var rowRect = new RectTransform(new Vector2(1f, 0f), itemList.Content.RectTransform) { MinSize = new Point(0, slotSize) };
                var currentRow = new GUILayoutGroup(rowRect, isHorizontal: true) { AbsoluteSpacing = 2 };

                for (int i = itemsLoaded; i < nextBatch; i++)
                {
                    var prefab = allFilteredItems[i];
                    bool isFav = controller.FavoritedItems.Contains(prefab.Identifier.Value);

                    if (itemsInRow >= maxItemsPerRow)
                    {
                        rowRect = new RectTransform(new Vector2(1f, 0f), itemList.Content.RectTransform) { MinSize = new Point(0, slotSize) };
                        currentRow = new GUILayoutGroup(rowRect, isHorizontal: true) { AbsoluteSpacing = 2 };
                        itemsInRow = 0;
                    }

                    CardBuilder.DrawMinimalItemRow(currentRow, prefab, 1,
                        onPrimaryClick: p => controller.OnItemSelected(p),
                        onSecondaryClick: p => controller.OpenContextMenu(p),
                        badgeColor: isFav ? Color.Gold : (Color?)null);

                    itemsInRow++;
                }
            }
            else
            {
                for (int i = itemsLoaded; i < nextBatch; i++)
                {
                    var prefab = allFilteredItems[i];
                    bool isFav = controller.FavoritedItems.Contains(prefab.Identifier.Value);

                    var btn = new GUIButton(new RectTransform(new Vector2(1f, 0f), itemList.Content.RectTransform) { MinSize = new Point(0, 35) }, style: "ListBoxElement")
                    {
                        OnClicked = (_, _) => { controller.OnItemSelected(prefab); return true; },
                        OnSecondaryClicked = (_, _) => { controller.OpenContextMenu(prefab); return true; }
                    };

                    string prefix = isFav ? "* " : "";
                    CardBuilder.DrawCompactItemRow(btn, prefab, 1, false, extraText: prefix, color: isFav ? Color.Gold : Color.White);
                }
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
            if (mainFrame == null) return;

            mainFrame.AddToGUIUpdateList();

            layoutMenuFrame?.AddToGUIUpdateList();

            UpdateLayout();

            if (itemList == null || itemsLoaded >= allFilteredItems.Count || isUpdating) return;

            int total = allFilteredItems.Count;
            int currentIndex = (int)(itemList.ScrollBar.BarScroll * (total - 1));
            if (currentIndex >= total - 5) LoadNextChunk();

            if (layoutMenuFrame != null && PlayerInput.PrimaryMouseButtonClicked())
            {
                bool overButton = GUI.MouseOn is GUIButton;

                if (!layoutMenuFrame.IsParentOf(GUI.MouseOn) && GUI.MouseOn != layoutMenuFrame && !overButton)
                {
                    mainFrame.RemoveChild(layoutMenuFrame);
                    layoutMenuFrame = null;
                }
            }
        }

        private void UpdateLayout()
        {
            if (mainFrame == null || contentArea == null || leftPanel == null || rightPanel == null || centerPanelContainer == null) return;

            int availableHeight = mainFrame.Rect.Height - HeaderHeight - BottomMargin;
            contentArea.RectTransform.NonScaledSize = new Point(contentArea.Rect.Width, availableHeight);

            Rectangle areaRect = contentArea.Rect;
            if (areaRect.Width <= 0) return;

            int spacing = (int)(areaRect.Width * 0.015f);
            int leftW = leftPanel.Rect.Width;
            int rightW = rightPanel.Rect.Width;

            int totalAvailableForSides = areaRect.Width - MinCenterWidth - (spacing * 2);
            if (leftW + rightW > totalAvailableForSides)
            {
                float totalSides = (float)leftW + rightW + 0.001f;
                float leftRatio = leftW / totalSides;
                leftW = (int)(totalAvailableForSides * leftRatio);
                rightW = totalAvailableForSides - leftW;
            }

            centerPanelContainer.RectTransform.AbsoluteOffset = new Point(leftW + spacing, 0);
            int centerWidth = Math.Max(0, areaRect.Width - leftW - rightW - (spacing * 2));

            leftPanel.RectTransform.NonScaledSize = new Point(leftW, areaRect.Height);
            centerPanelContainer.RectTransform.NonScaledSize = new Point(centerWidth, areaRect.Height);
            rightPanel.RectTransform.NonScaledSize = new Point(rightW, areaRect.Height);

            var newLeftMode = GetModeForWidth(leftW, SidebarHiddenThreshold, SidebarCompactThreshold);
            var newRightMode = GetModeForWidth(rightW, SidebarHiddenThreshold, SidebarCompactThreshold);
            var newCenterMode = GetModeForWidth(centerWidth, -1, CenterCompactThreshold);

            bool needsLeftRefresh = newLeftMode != leftPanelMode;

            if (leftPanelMode == DisplayMode.Compact && Math.Abs(leftW - lastLeftWForReflow) > 34)
            {
                needsLeftRefresh = true;
                lastLeftWForReflow = leftW;
            }

            bool needsCenterRefresh = newCenterMode != centerPanelMode;

            if (centerPanelMode == DisplayMode.Compact && Math.Abs(centerWidth - lastCenterWForReflow) > 34)
            {
                needsCenterRefresh = true;
                lastCenterWForReflow = centerWidth;
            }

            bool needsRightRefresh = newRightMode != rightPanelMode;

            if (needsLeftRefresh)
            {
                leftPanelMode = newLeftMode;
                lastLeftWForReflow = leftW;
                if (leftContainer != null) leftContainer.Visible = leftPanelMode != DisplayMode.Hidden;
                RefreshSearch();
            }

            if (needsCenterRefresh)
            {
                centerPanelMode = newCenterMode;
                lastCenterWForReflow = centerWidth;

                if (currentItem != null && currentCraft != null && currentDecon != null && currentUses != null && currentSources != null)
                {
                    UpdateDetailsPanel(currentItem, currentCraft, currentDecon, currentUses, currentSources);
                }
            }

            if (needsRightRefresh)
            {
                rightPanelMode = newRightMode;
                if (rightContainer != null) rightContainer.Visible = rightPanelMode != DisplayMode.Hidden;
                if (currentItem != null && currentCraft != null && currentDecon != null && currentUses != null && currentSources != null)
                {
                    UpdateDetailsPanel(currentItem, currentCraft, currentDecon, currentUses, currentSources);
                }
            }

            // aaaa
            if (mainFrame != null && mainFrame.RectTransform.NonScaledSize != (controller.WindowSize ?? Point.Zero))
            {
                controller.WindowSize = mainFrame.RectTransform.NonScaledSize;
                controller.MarkDirty();
            }
            if (mainFrame != null && mainFrame.RectTransform.AbsoluteOffset != (controller.WindowPosition ?? new Point(-999)))
            {
                controller.WindowPosition = mainFrame.RectTransform.AbsoluteOffset;
                controller.MarkDirty();
            }
            if (leftPanel != null && leftPanel.Rect.Width != (controller.LeftPanelWidth ?? 0))
            {
                controller.LeftPanelWidth = leftPanel.Rect.Width;
                controller.MarkDirty();
            }
            if (rightPanel != null && rightPanel.Rect.Width != (controller.RightPanelWidth ?? 0))
            {
                controller.RightPanelWidth = rightPanel.Rect.Width;
                controller.MarkDirty();
            }

            if (centerPanelContainer != null && recipeAreaFrame != null && detailsHeader != null)
            {
                int headerHeight = 95;

                detailsHeader.RectTransform.AbsoluteOffset = Point.Zero;
                detailsHeader.RectTransform.NonScaledSize = new Point(centerWidth, headerHeight);

                int recipeHeight = Math.Max(0, centerPanelContainer.Rect.Height - headerHeight);
                recipeAreaFrame.RectTransform.AbsoluteOffset = new Point(0, headerHeight);
                recipeAreaFrame.RectTransform.NonScaledSize = new Point(centerWidth, recipeHeight);
            }
        }

        private static DisplayMode GetModeForWidth(int width, int hiddenThreshold, int compactThreshold)
        {
            if (width < hiddenThreshold) return DisplayMode.Hidden;
            if (width < compactThreshold) return DisplayMode.Compact;
            return DisplayMode.Normal;
        }

        public void UpdateDetailsPanel(ItemPrefab targetItem, List<FabricationRecipe> craft, List<DeconstructItem> decon, List<Tuple<ItemPrefab, FabricationRecipe>> uses, List<Tuple<ItemPrefab, DeconstructItem>> sources)
        {
            currentItem = targetItem;
            currentCraft = craft;
            currentDecon = decon;
            currentUses = uses;
            currentSources = sources;

            activeDropdowns.Clear();
            if (detailsHeader == null || colObtain == null || colUsage == null || metaPanel == null) return;
            metaPanel.Content.ClearChildren();

            detailsHeader.ClearChildren();
            var headerLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.85f, 1f), detailsHeader.RectTransform, Anchor.CenterRight), isHorizontal: true) { AbsoluteSpacing = 15 };
            Sprite? icon = targetItem.InventoryIcon ?? targetItem.Sprite;
            if (icon != null)
            {
                var imgFrame = new GUIFrame(new RectTransform(new Vector2(0.15f, 0.9f), headerLayout.RectTransform, Anchor.CenterLeft), style: null)
                {
                    ToolTip = CardBuilder.GetDetailedTooltip(targetItem)
                };
                _ = new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), imgFrame.RectTransform, Anchor.Center), icon, scaleToFit: true) { Color = targetItem.InventoryIconColor, CanBeFocused = false };
            }

            var (headerName, headerColor) = SafeItemName.Get(targetItem, Color.White);

            _ = new GUITextBlock
            (
                new RectTransform(new Vector2(0.8f, 1f), headerLayout.RectTransform),
                headerName,
                font: GUIStyle.LargeFont,
                textColor: headerColor,
                textAlignment: Alignment.CenterLeft
            )
            {
                Wrap = true,
                AutoScaleHorizontal = true,
                CanBeFocused = false
            };

            void onPrimary(ItemPrefab p) => controller.OnItemSelected(p);
            void onSecondary(ItemPrefab p) => controller.OpenContextMenu(p);

            var groupedSources = sources
                .GroupBy(s => new
                {
                    SourceId = s.Item1.Identifier,
                    MachineKey = string.Join(",", s.Item2.RequiredDeconstructor.Select(id => id.Value).OrderBy(x => x)),
                    OtherItemsKey = string.Join(",", s.Item2.RequiredOtherItem.Select(id => id.Value).OrderBy(x => x))
                })
                .Select(group => new GroupedSource
                {
                    SourceItem = group.First().Item1,
                    MachineIds = group.First().Item2.RequiredDeconstructor,
                    RequiredOtherItems = [.. group.First().Item2.RequiredOtherItem],
                    TotalCommonness = group.Sum(g => g.Item2.Commonness),
                    IsRandom = group.First().Item1.RandomDeconstructionOutput
                })
                .ToList();

            colObtain.Content.ClearChildren();
            foreach (var r in craft) CardBuilder.DrawCraftCard(colObtain, r, targetItem, controller, onPrimary, onSecondary, centerPanelMode);
            foreach (var src in groupedSources) CardBuilder.DrawSourceCard(colObtain, src, onPrimary, onSecondary, centerPanelMode);

            var currentItemId = targetItem.Identifier;
            var groupedUses = uses
                .GroupBy(u => new
                {
                    ResultId = u.Item1.Identifier,
                    ReqAmount = u.Item2.RequiredItems.FirstOrDefault(ri =>
                        ri.ItemPrefabs.Any(p => p.Identifier == currentItemId))?.Amount ?? 1
                })
                .Select(group => new GroupedUsage
                {
                    TargetItem = group.First().Item1,
                    MachineIds = [.. group.SelectMany(g => g.Item2.SuitableFabricatorIdentifiers).Distinct()],
                    AmountCreated = group.First().Item2.Amount,
                    AmountRequired = group.Key.ReqAmount
                })
                .OrderBy(gu => gu.TargetItem?.Name.Value)
                .ToList();

            colUsage.Content.ClearChildren();
            if (decon.Count > 0) CardBuilder.DrawDeconCard(colUsage, targetItem, decon, onPrimary, onSecondary, centerPanelMode);
            foreach (var usage in groupedUses) CardBuilder.DrawUseCard(colUsage, usage, onPrimary, onSecondary, centerPanelMode);


            void onBadgeClick(string tag) { if (searchBox != null) searchBox.Text = tag; UpdateSearch(tag); }

            var builder = new SectionBuilder
            (
                metaPanel,
onBadgeClick,
                controller,
onPrimary,
onSecondary
            );

            var analysis = RecipeAnalyzer.GetAnalysis(targetItem);

            if (analysis == null || analysis.Sections == null) return;

            foreach (var section in analysis.Sections)
            {
                section.Draw(builder);
            }

            if (xmlContentText != null && targetItem.ConfigElement != null)
            {
                try
                {
                    string rawXml = targetItem.ConfigElement.ToString() ?? "<!-- Empty XML -->";

                    if (rawXml == "Barotrauma.ContentXElement")
                    {
                        var prop = targetItem.ConfigElement.GetType().GetProperty("Element")
                                ?? targetItem.ConfigElement.GetType().GetProperty("XElement");
                        var field = targetItem.ConfigElement.GetType().GetField("Element")
                                ?? targetItem.ConfigElement.GetType().GetField("XElement");

                        object? inner = prop?.GetValue(targetItem.ConfigElement)
                                        ?? field?.GetValue(targetItem.ConfigElement);

                        if (inner != null)
                        {
                            rawXml = inner.ToString() ?? rawXml;
                        }
                    }

                    xmlContentText.Text = XMLHighlighter.Format(rawXml);
                }
                catch
                {
                    xmlContentText.Text = XMLHighlighter.Format("<!-- Error parsing XML data -->");
                }
            }
            else if (xmlContentText != null)
            {
                xmlContentText.Text = XMLHighlighter.Format("<!-- No XML data found for this item -->");
            }
        }

        public Point GetCurrentSize() => mainFrame?.Rect.Size ?? new Point(1000, 700);
        public int GetLeftWidth() => leftPanel?.Rect.Width ?? 250;
        public int GetRightWidth() => rightPanel?.Rect.Width ?? 250;

        public void ForceLayoutUpdate()
        {
            if (mainFrame == null || leftPanel == null || rightPanel == null) return;

            if (controller.WindowSize.HasValue) mainFrame.RectTransform.NonScaledSize = controller.WindowSize.Value;
            if (controller.WindowPosition.HasValue) mainFrame.RectTransform.AbsoluteOffset = controller.WindowPosition.Value;
            if (controller.LeftPanelWidth.HasValue) leftPanel.RectTransform.NonScaledSize = new Point(controller.LeftPanelWidth.Value, leftPanel.Rect.Height);
            if (controller.RightPanelWidth.HasValue) rightPanel.RectTransform.NonScaledSize = new Point(controller.RightPanelWidth.Value, rightPanel.Rect.Height);

            UpdateLayout();
        }

        private void ToggleLayoutMenu(GUIComponent anchor)
        {
            if (mainFrame == null) return;
            if (layoutMenuFrame != null)
            {
                mainFrame?.RemoveChild(layoutMenuFrame);
                layoutMenuFrame = null;
                return;
            }

            layoutMenuFrame = new GUIFrame(new RectTransform(new Point(280, 380), mainFrame.RectTransform), style: "InnerFrame")
            {
                IgnoreLayoutGroups = true,
                Color = Color.Black * 0.98f,
                CanBeFocused = true
            };

            int localX = anchor.Rect.X - mainFrame.Rect.X;
            int localY = anchor.Rect.Bottom - mainFrame.Rect.Y + 2;
            layoutMenuFrame.RectTransform.AbsoluteOffset = new Point(localX, localY);

            var list = new GUIListBox(new RectTransform(new Point(200, 350), layoutMenuFrame.RectTransform, Anchor.TopCenter) { AbsoluteOffset = new Point(0, 10) }, style: null)
            {
                Spacing = 4
            };

            AddGroupHeader(list, "STANDARD");
            AddPresetRow(list, "Minimal", () => controller.ApplyLayout(new Point(500, 600), 0, 0), false, anchor);
            AddPresetRow(list, "Medium-List", () => controller.ApplyLayout(new Point(850, 650), 220, 0), false, anchor);
            AddPresetRow(list, "Medium-Desc", () => controller.ApplyLayout(new Point(850, 650), 0, 250), false, anchor);
            AddPresetRow(list, "Full View", () => controller.ApplyLayout(new Point(1450, 850), 250, 300), false, anchor);

            if (controller.CustomLayouts.Count > 0)
            {
                AddGroupHeader(list, "My PRESETS");
                foreach (var key in controller.CustomLayouts.Keys.ToList())
                {
                    var saved = controller.CustomLayouts[key];
                    AddPresetRow(list, key, () => controller.ApplyLayout(saved.WindowSize, saved.LeftPanelWidth, saved.RightPanelWidth), true, anchor);
                }
            }

            _ = new GUIButton(new RectTransform(new Vector2(0.9f, 0.1f), layoutMenuFrame.RectTransform, Anchor.BottomCenter) { AbsoluteOffset = new Point(0, 10) }, "+ SAVE ACTUAL", style: "DeviceButton")
            {
                OnClicked = (_, _) =>
                {
                    string newName = $"Layout {controller.CustomLayouts.Count + 1}";
                    controller.CustomLayouts[newName] = new SavedLayout
                    {
                        WindowSize = mainFrame.Rect.Size,
                        LeftPanelWidth = leftPanel?.Rect.Width ?? 0,
                        RightPanelWidth = rightPanel?.Rect.Width ?? 0
                    };
                    controller.MarkDirty();
                    ToggleLayoutMenu(anchor);
                    ToggleLayoutMenu(anchor);
                    return true;
                }
            };
        }

        private static void AddGroupHeader(GUIListBox list, string text)
        {
            var header = new GUIFrame(new RectTransform(new Point(list.Content.Rect.Width, 20), list.Content.RectTransform), style: "GUIFrameBottom")
            {
                Color = Color.Gray * 0.4f,
                CanBeFocused = false
            };
            _ = new GUITextBlock(new RectTransform(Vector2.One, header.RectTransform), text, font: GUIStyle.SmallFont, textAlignment: Alignment.Center)
            {
                CanBeFocused = false
            };
        }

        private void AddPresetRow(GUIListBox list, string name, Action onApply, bool canDelete, GUIComponent anchor)
        {
            var row = new GUILayoutGroup(new RectTransform(new Point(list.Content.Rect.Width, 30), list.Content.RectTransform), isHorizontal: true) { AbsoluteSpacing = 2 };

            var btn = new GUIButton(new RectTransform(new Vector2(canDelete ? 0.82f : 1f, 1f), row.RectTransform), name, style: "ListBoxElement")
            {
                OnClicked = (_, _) => { onApply(); ToggleLayoutMenu(anchor); return true; }
            };

            if (canDelete)
            {
                _ = new GUIButton(new RectTransform(new Vector2(0.18f, 1f), row.RectTransform), "", style: "CategoryButton.All")
                {
                    OnClicked = (_, _) =>
                    {
                        controller.CustomLayouts.Remove(name);
                        controller.MarkDirty();
                        ToggleLayoutMenu(anchor);
                        ToggleLayoutMenu(anchor);
                        return true;
                    }
                };
            }
        }
    }

    public class GroupedSource
    {
        public ItemPrefab? SourceItem;
        public Identifier[]? MachineIds;
        public List<Identifier>? RequiredOtherItems;
        public float TotalCommonness;
        public bool IsRandom;
    }

    public class GroupedUsage
    {
        public ItemPrefab? TargetItem;
        public List<Identifier>? MachineIds;
        public float AmountCreated;
        public float AmountRequired;
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
                string prefix = isFav ? " *" : "";

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
                    OnClicked = (_, _) => { onClick?.Invoke(item); return true; },
                    //ToolTip = TextSOS.Get("sos.window.tag_tooltip", "Search items with tag: [tag]").Replace("[tag]", item)
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
