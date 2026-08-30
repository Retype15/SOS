// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Xml.Linq;
using Barotrauma;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using SOS.Configs;
using SOS.GUI;
using SOS.Panels.ItemPanel;
using SOS.Prefabs;
using BGUI = Barotrauma.GUI;

namespace SOS.Profiles.TCWP
{
    [AutoRegister("SOS.Default3Column", -1)]
    internal sealed class ThreeColumnWindowProfile : GUIWindow, ISOSWindowProfile
    {
        public string Id => "SOS.Default3Column";
        public string DisplayName => Texts.Get("sos.profile.3CWP.name", "Minimal (Single Column)").Value;
        public string Description => Texts.Get("sos.profile.3CWP.desc", "Compact single-column view focusing purely on content tabs.").Value;

        private Configs.TCWP.TCWPConfig? config;
        public ISOSConfig ProfileConfig => config ??= new();

        private bool _windowEventsRegistered;

        #region Vars

        // Root
        private GUIResizableFrame? leftPanel;
        private GUIFrame? leftContainer;
        private GUIListBox? itemList;
        private GUITextBox? searchBox;

        // Center panel
        private GUIFrame? centerPanel;
        private GUIFrame? detailsHeader;
        private GUITab<Prefab>? centerTabWidget;

        // Right panel
        private GUIResizableFrame? rightPanel;
        private GUIFrame? rightContainer;
        private GUIListBox? metaPanel;
        private GUITextViewer? xmlContentText;
        private GUITickBox? rawXmlTickBox;

        // Search
        private List<Prefab> allFilteredTargets = [];
        private int itemsLoaded = 0;
        private const int ChunkSize = 50;
        private bool isUpdating = false;
        private Dictionary<Type, string>? prefabHeaders;
        private Type? lastTypeInList;

        private readonly double searchDelay = 0.2;
        private double searchExecutionTime = 0;
        private string? pendingSearchQuery;

        // Layout state
        private const int SidebarHiddenThreshold = 100;
        private const int SidebarCompactThreshold = 240;
        private const int CenterCompactThreshold = 250;
        private const int MinCenterWidth = 200;
        private DisplayMode leftPanelMode = DisplayMode.Normal;
        private DisplayMode centerPanelMode = DisplayMode.Normal;
        private DisplayMode rightPanelMode = DisplayMode.Normal;
        private int lastLeftWForReflow = 0;
        private int lastCenterWForReflow = 0;

        // Misc
        private GUIFrame? layoutMenuFrame;
        private Prefab? _lastTarget;
        private static bool needsShowLogo = true;

        #endregion

        public ThreeColumnWindowProfile() : base(
            new RectTransform(new Vector2(0.95f, 0.9f), BGUI.Canvas, Anchor.TopLeft),
            Texts.Get("sos.window.title", "SOS - Recipe Browser"),
            style: "CircuitBoxFrame",
            color: Color.Black * 0.85f,
            buttons: WindowButtons.All)
        {
            RectTransform.MinSize = new Point(400, 200);
            AllowedDirections = ResizeDirection.All;
        }

        public void Init()
        {
            if (!_windowEventsRegistered)
            {
                API.On(CommKeys.ToggleWindow, OnToggleWindow, EventPriority.UI);
                API.On(CommKeys.OpenWindow, OnOpenWindow, EventPriority.UI);
                API.On<Prefab?>(CommKeys.SelectTarget, OnTargetChangedHandler, EventPriority.UI);
                API.On<string>(CommKeys.SetSearchFilter, OnSetSearchFilter, EventPriority.UI);
                API.On<TPLayout>(CommKeys.ApplyLayout, OnApplyLayout, EventPriority.UI);
                API.On(CommKeys.RefreshSearch, RefreshSearch, EventPriority.UI);

                _windowEventsRegistered = true;
            }

            ProfileConfig.Load();
            ApplySavedSizeAndPosition();
            Mode = (config?.IsMaximized ?? false) ? WindowMode.Fullscreen : WindowMode.Windowed;
            BuildMainUI();

            if (needsShowLogo)
            {
                BuildLoadingUI();
                needsShowLogo = false;
            }
        }

        private void OnToggleWindow()
        {
            if (!Visible)
            {
                Visible = true;
                return;
            }

            ProfileHelper.CloseWindow();
        }

        private void OnOpenWindow()
        {
            if (!Visible)
                Visible = true;
        }

        public TPLayout GetTPLayout()
        {
            return new TPLayout
            {
                WindowSize = NormalSize,
                LeftPanelWidth = leftPanel?.Rect.Width ?? config!.LeftPanelWidth,
                RightPanelWidth = rightPanel?.Rect.Width ?? config!.RightPanelWidth
            };
        }

        public void Update()
        {
            if (!Visible) return;

            UpdateLayout();
            HandleSearchDebounce();

            if (itemList != null && itemsLoaded < allFilteredTargets.Count && !isUpdating)
            {
                int total = allFilteredTargets.Count;
                int currentIndex = (int)(itemList.ScrollBar.BarScroll * (total - 1));
                if (currentIndex >= total - 5) LoadNextChunk();
            }

            if (layoutMenuFrame != null && PlayerInput.PrimaryMouseButtonClicked())
            {
                bool overButton = BGUI.MouseOn is GUIButton;
                if (!layoutMenuFrame.IsParentOf(BGUI.MouseOn) && BGUI.MouseOn != layoutMenuFrame && !overButton)
                {
                    RemoveChild(layoutMenuFrame);
                    layoutMenuFrame = null;
                }
            }
        }

        public void Dispose()
        {
            Logger.LogDebug("EXECUTING PROFILE DISPOSE...", level: LogLevel.Trace);
            API.Off(CommKeys.ToggleWindow, OnToggleWindow, EventPriority.UI);
            API.Off(CommKeys.OpenWindow, OnOpenWindow, EventPriority.UI);
            _windowEventsRegistered = false;

            API.Off<Prefab?>(CommKeys.SelectTarget, OnTargetChangedHandler, EventPriority.UI);
            API.Off<string>(CommKeys.SetSearchFilter, OnSetSearchFilter, EventPriority.UI);
            API.Off<TPLayout>(CommKeys.ApplyLayout, OnApplyLayout, EventPriority.UI);
            API.Off(CommKeys.RefreshSearch, RefreshSearch, EventPriority.UI);

            SaveSettings();

            if (centerTabWidget is IDisposable d) d.Dispose();
            centerTabWidget = null;

            if (RectTransform != null)
                RectTransform.Parent = null;

            itemList = null;
            searchBox = null;
            metaPanel = null;
            config = null;
        }

        private void SaveSettings()
        {
            if (config != null)
            {
                config.WindowSize = NormalSize;
                config.WindowPosition = NormalOffset;
                config.IsMaximized = Mode == WindowMode.Fullscreen;
                if (leftPanel != null) config.LeftPanelWidth = leftPanel.Rect.Width;
                if (rightPanel != null) config.RightPanelWidth = rightPanel.Rect.Width;

                config.Save();
                Logger.LogDebug($"Settings for 3ColumnWindowProfile has saved...", level: LogLevel.Trace);
            }
        }

        private void OnTargetChangedHandler(Prefab? target)
        {
            if (target == null) return;
            _lastTarget = target;
            if (detailsHeader == null || centerTabWidget == null || metaPanel == null) return;

            metaPanel.Content.ClearChildren();
            detailsHeader.ClearChildren();

            var headerLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.85f, 1f), detailsHeader.RectTransform, Anchor.CenterRight), isHorizontal: true) { AbsoluteSpacing = 15 };

            if (target == null) return;

            Sprite? icon = target.Icon();

            if (icon != null)
            {
                var imgFrame = new GUIFrame(new RectTransform(new Vector2(0.15f, 0.9f), headerLayout.RectTransform, Anchor.CenterLeft), style: null)
                {
                    OnDrawToolTip = component => component.ToolTip = CardBuilder.GetDetailedTooltip(target)
                };
                _ = new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), imgFrame.RectTransform, Anchor.Center), icon, scaleToFit: true)
                {
                    Color = target.IconColor(),
                    CanBeFocused = true,
                    OnSecondaryClicked = (_, _) => { ProfileHelper.OpenContextMenu(target); return true; }
                };
            }

            var (headerName, headerColor) = target.SafeName(Color.White);
            _ = new GUITextBlock(
                new RectTransform(new Vector2(0.8f, 1f), headerLayout.RectTransform),
                headerName,
                font: GUIStyle.LargeFont, textColor: headerColor, textAlignment: Alignment.CenterLeft)
            {
                Wrap = false,
                AutoScaleHorizontal = true,
                CanBeFocused = true,
                OnSecondaryClicked = (_, _) => { ProfileHelper.OpenContextMenu(target); return true; }
            };


            ProfileHelper.UpdateTabWidget(centerTabWidget, target);

            bool hasDrawed = false;

            foreach (var section in API.GetAllSections())
            {
                try
                {
                    hasDrawed |= section.Draw(metaPanel, target, ProfileHelper.OnPrimary, ProfileHelper.OnSecondary);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[SOS] Exception in section '{section.GetType().FullOrName()}': {ex.Message}");
                    continue;
                }
            }

            if (xmlContentText != null)
                xmlContentText.Text = ProfileHelper.GetRawXMLSafe(target).FormatToXMLCode();
        }

        private void OnSetSearchFilter(string tag)
        {
            if (searchBox != null) searchBox.Text = tag;
            UpdateSearch(tag);
        }

        private void OnApplyLayout(TPLayout layout) => ForceLayoutTo(layout);

        #region BuildUI

        private void BuildLoadingUI()
        {
            var loadingFrame = new GUIFrame(new RectTransform(Vector2.One, RectTransform, Anchor.Center), style: "InnerFrame")
            {
                Color = Color.Black * 0.5f,
                CanBeFocused = false
            };
            GUIImage? logoImage = null;
            var imgPath = $"{Plugin.Instance.Package.Dir}/Content/SOS_LOGO_TEXT.png";
            if (File.Exists(imgPath) && LuaCsFile.CanReadFromPath(imgPath))
            {
                var sprite = new Sprite(imgPath, Vector2.One);
                logoImage = new GUIImage(new RectTransform(new Vector2(0.8f, 0.6f), loadingFrame.RectTransform, Anchor.Center), sprite: sprite, scaleToFit: true)
                { CanBeFocused = false };
                logoImage.ExFadeIn(duration: 0.5f, targetFactor: 0.8f, alsoChildren: true);
            }

            loadingFrame
                .Wait(0.5f)
                .ExFadeOut(duration: 0.5f, targetFactor: 0.6f, alsoChildren: true)
                .Wait(2.0f)
                .ExFadeOut(duration: 0.5f, alsoChildren: true)
                .WaitFinish()
                .Execute(() =>
                {
                    if (loadingFrame != null)
                    {
                        logoImage?.Parent?.RemoveChild(logoImage);
                        RemoveChild(loadingFrame);
                        loadingFrame = null;
                        logoImage = null;
                    }
                });

            var duration = 1.2f;
            this.ExFadeIn(duration, alsoChildren: false);
            TopBar.ExFadeIn(duration, alsoChildren: true);
            ContentArea.ExFadeIn(duration, alsoChildren: true);
            //leftPanel?.ExFadeIn(duration, alsoChildren: true);
            //rightPanel?.ExFadeIn(duration, alsoChildren: true);
            //centerPanel?.ExFadeIn(duration, alsoChildren: true);
        }

        private void ApplySavedSizeAndPosition()
        {
            if (config == null) return;

            RectTransform.NonScaledSize = config.WindowSize;

            var wp = config.WindowPosition;
            if (wp.X >= 0 && wp.Y >= 0)
                RectTransform.AbsoluteOffset = wp;
            else
            {
                int cx = (GameMain.GraphicsWidth / 2) - (Rect.Width / 2);
                int cy = (GameMain.GraphicsHeight / 2) - (Rect.Height / 2);
                RectTransform.AbsoluteOffset = new Point(cx, cy);
            }

            NormalSize = RectTransform.NonScaledSize;
            NormalOffset = RectTransform.AbsoluteOffset;
        }

        private void BuildMainUI()
        {
            leftPanelMode = DisplayMode.Normal;
            centerPanelMode = DisplayMode.Normal;
            rightPanelMode = DisplayMode.Normal;
            lastLeftWForReflow = 0;
            lastCenterWForReflow = 0;
            isUpdating = false;
            pendingSearchQuery = null;
            searchExecutionTime = 0;
            layoutMenuFrame = null;

            var ctrl = SOSController.Instance;
            var cfg = CoreConfig.Instance;

            ProfileHelper.CreateSettingsButton(ToolBox.RectTransform);
            ProfileHelper.CreateNavigationHistoryButtons(ToolBox.RectTransform);

            var text = Texts.Get("sos.window.manage_hud", "MANAGE HUD");
            _ = new GUIButton(new RectTransform(new Point(text.Length * 12, 32), ControlBox.RectTransform, isFixedSize: true), text, style: "DeviceButton")
            {
                OnClicked = (_, _) =>
                {
                    var options = ctrl.Tracker.GetManageHudContextMenuOptions();
                    GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, Texts.Get("sos.window.remove_recipes", "Remove Recipes").Value, null, [.. options]);
                    return true;
                },
                ToolTip = Texts.Get("sos.window.manage_hud_tooltip", "Manage tracked recipes on the HUD").Value
            };

            _ = new GUIButton(new RectTransform(new Point(32, 32), ControlBox.RectTransform, isFixedSize: true), "o", style: "DeviceButton")
            {
                OnClicked = (_, _) => { ctrl.Tracker.ToggleTracker(); return true; },
                ToolTip = Texts.Get("sos.window.toggle_tracker_tooltip", "Toggle HUD tracker (Ctrl+[key])").Value.Replace("[key]", cfg.SOSOpenKey.Key.ToString())
            };

            SetControlBoxContentWidth();

            OnClose += ProfileHelper.CloseWindow;

            // Left panel
            int initialLeftW = (config != null && config.LeftPanelWidth > 0) ? config.LeftPanelWidth : 250;
            leftPanel = new GUIResizableFrame(
                new RectTransform(new Point(initialLeftW, ContentArea.Rect.Height), ContentArea.RectTransform, Anchor.TopLeft, isFixedSize: true),
                style: "InnerFrame")
            {
                AllowedDirections = ResizeDirection.Right,
                IsFixed = true,
                Color = Color.Black * 0.4f,
                RectTransform = { MinSize = new Point(20, 50), MaxSize = new Point(500, 2000) }
            };

            leftContainer = new GUIFrame(new RectTransform(new Vector2(0.95f, 0.98f), leftPanel.RectTransform, Anchor.Center), style: null);
            var leftLayout = new GUILayoutGroup(new RectTransform(Vector2.One, leftContainer.RectTransform)) { Stretch = true, RelativeSpacing = 0.01f };

            var searchContainer = new GUIFrame(new RectTransform(new Vector2(1f, 0.05f), leftLayout.RectTransform), style: "InnerFrame")
            {
                RectTransform = { MinSize = new Point(0, 35), MaxSize = new Point(int.MaxValue, 35) }
            };

            searchBox = BGUI.CreateTextBoxWithPlaceholder(new RectTransform(Vector2.One, searchContainer.RectTransform), cfg.LastSearchQuery, Texts.Get("sos.window.search_placeholder", "Search item..."));
            searchBox.ToolTip = Texts.Get("sos.window.search_tooltip",
                "Search by Name, ID, Category, Tags, ModName, ItemType, Prefab, etc.\n" +
                "  Advanced Filters:\n" +
                "  Identifiers     |  Examples:\n" +
                "———————————————————\n" +
                "  @Mod             |  @Vanilla @Neurotrauma\n" +
                "  #Category    |  #Medical #Weapon\n" +
                "  $Tag               |  $smallitem $pill\n" +
                "  &Slot              |  &Head &Inner\n" +
                "  !ID                    |  !weldingtool\n" +
                "  %Prefab        |  %Affliction %Item\n" +

                "\nExample: 'Brain @NT #Medical $surgery %Item'");

            searchBox.OnTextChanged += (_, text) =>
            {
                pendingSearchQuery = text;
                searchExecutionTime = Timing.TotalTime + searchDelay;
                return true;
            };

            itemList = new GUIListBox(new RectTransform(new Vector2(1f, 1f), leftLayout.RectTransform), style: null)
            {
                Padding = new Vector4(8, 5, 5, 5),
                Color = Color.Black * 0.2f,
                RectTransform = { MinSize = new Point(0, 50) }
            };

            // Center panel
            centerPanel = new GUIFrame(new RectTransform(new Point(200, ContentArea.Rect.Height), ContentArea.RectTransform, Anchor.TopLeft, isFixedSize: true), style: null)
            {
                RectTransform = { MinSize = new Point(200, 50) }
            };

            var centerLayout = new GUILayoutGroup(new RectTransform(Vector2.One, centerPanel.RectTransform)) { Stretch = true, RelativeSpacing = 0.01f };

            detailsHeader = new GUIFrame(new RectTransform(new Vector2(1f, 0.10f), centerLayout.RectTransform), style: "CircuitBoxFrame")
            {
                Color = Color.Black * 0.4f,
                RectTransform = { MinSize = new Point(0, 65), MaxSize = new Point(int.MaxValue, 65) }
            };

            centerTabWidget = ProfileHelper.CreateTabWidget(new RectTransform(new Vector2(1f, 0.90f), centerLayout.RectTransform), API.GetAllTabs());

            ISOSPrefab[] prefabProviders = [.. API.GetAllPrefabProviders()];
            prefabHeaders = prefabProviders.ToDictionary(p => p.PrefabType, p => p.Header);

            // Right panel
            int initialRightW = (config != null && config.RightPanelWidth > 0) ? config.RightPanelWidth : 300;
            rightPanel = new GUIResizableFrame(
                new RectTransform(new Point(initialRightW, ContentArea.Rect.Height), ContentArea.RectTransform, Anchor.TopRight, isFixedSize: true),
                style: "InnerFrame")
            {
                AllowedDirections = ResizeDirection.Left,
                IsFixed = true,
                Color = Color.Black * 0.4f,
                RectTransform = { MinSize = new Point(20, 50), MaxSize = new Point(1000, 2000) }
            };

            rightContainer = new GUIFrame(new RectTransform(new Vector2(0.95f, 0.98f), rightPanel.RectTransform, Anchor.Center), style: null);
            var rightLayout = new GUILayoutGroup(new RectTransform(Vector2.One, rightContainer.RectTransform)) { Stretch = true };

            var rightHeaderArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.045f), rightLayout.RectTransform), style: null)
            { RectTransform = { MinSize = new Point(0, 32) } };

            rawXmlTickBox = new GUITickBox(new RectTransform(new Vector2(1f, 0.45f), rightHeaderArea.RectTransform, Anchor.CenterLeft), Texts.Get("sos.window.raw_xml", "RAW XML").Value, font: GUIStyle.SmallFont)
            {
                Selected = cfg.RawXmlMode,
                ToolTip = Texts.Get("sos.window.raw_xml_tooltip", "Toggles between metadata view and raw XML view of the item.").Value
            };

            var rightContentArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.955f), rightLayout.RectTransform), style: null);

            metaPanel = new GUIListBox(new RectTransform(Vector2.One, rightContentArea.RectTransform), style: "GUIListBox")
            {
                Spacing = 10,
                Padding = new Vector4(18, 15, 18, 15),
                CanBeFocused = true,
                Color = Color.Black * 0.4f
            };

            xmlContentText = new GUITextViewer(new RectTransform(Vector2.One, rightContentArea.RectTransform), style: "GUITextBlock")
            {
                Visible = cfg.RawXmlMode,
                Font = GUIStyle.SmallFont,
                TextScale = cfg.XmlFontScale,
                OnScaleChanged = (scale) => cfg.XmlFontScale = scale,
                ContentMenu = ProfileHelper.XmlContextMenu
            };

            metaPanel.Visible = !cfg.RawXmlMode;
            if (metaPanel.ContentBackground != null) metaPanel.ContentBackground.Color = Color.Transparent;

            rawXmlTickBox.OnSelected = (tick) =>
            {
                cfg.RawXmlMode = tick.Selected;
                metaPanel.Visible = !tick.Selected;
                if (xmlContentText != null) xmlContentText.Visible = tick.Selected;
                return true;
            };

            UpdateSearch(cfg.LastSearchQuery);
            UpdateLayout();

            OnTargetChangedHandler(API.GetState<Prefab?>(CommKeys.SelectTarget));
        }

        #endregion

        #region Search

        private void HandleSearchDebounce()
        {
            if (pendingSearchQuery != null && Timing.TotalTime >= searchExecutionTime)
            {
                CoreConfig.Instance.LastSearchQuery = pendingSearchQuery;
                UpdateSearch(pendingSearchQuery);
                pendingSearchQuery = null;
            }
        }

        public void RefreshSearch() => UpdateSearch(searchBox?.Text ?? "");

        private void UpdateSearch(string query)
        {
            if (itemList == null) return;
            var filter = new SearchFilter(query);

            allFilteredTargets.Clear();
            lastTypeInList = null;

            var candidates = new List<Prefab>();

            foreach (var provider in API.GetAllPrefabProviders())
            {
                try
                {
                    if (!filter.AllowsType(provider.PrefabType.Name)) continue;
                    candidates.AddRange(provider.GetAll(filter));
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[SOS] Provider '{provider.GetType().FullOrName()}' failed: {ex.Message}");
                }
            }

            var ctrl = SOSController.Instance;
            allFilteredTargets = [.. candidates
                .OrderByDescending(c => PrefabHelper.IsFavorite(c.Identifier.Value))];

            itemsLoaded = 0;
            itemList.Content.ClearChildren();
            itemList.ScrollBar.BarScroll = 0;

            LoadNextChunk();
        }

        private void LoadNextChunk()
        {
            if (itemList == null || itemsLoaded >= allFilteredTargets.Count || isUpdating) return;

            isUpdating = true;
            float totalScrollableHeightBefore = itemList.Content.Rect.Height - itemList.Rect.Height;
            float currentScrollPixels = itemList.ScrollBar.BarScroll * totalScrollableHeightBefore;
            int nextBatch = Math.Min(itemsLoaded + ChunkSize, allFilteredTargets.Count);

            int slotSize = 32;
            int itemsInRow = 0;
            int availableWidth = (int)itemList.Rect.Width - 14;
            int maxItemsPerRow = Math.Max(1, availableWidth / slotSize);

            GUILayoutGroup? currentRow = null;
            var ctrl = SOSController.Instance;

            for (int i = itemsLoaded; i < nextBatch; i++)
            {
                var prefab = allFilteredTargets[i];
                Type currentType = prefab.GetType();

                if (currentType != lastTypeInList)
                {
                    lastTypeInList = currentType;
                    var separatorFrame = new GUIButton(new RectTransform(new Vector2(1f, 0f), itemList.Content.RectTransform) { MinSize = new Point(0, slotSize) }, style: "GUIFrameBottom")
                    {
                        Color = Color.White * 0.1f,
                        CanBeFocused = true,
                        ToolTip = Texts.Get("sos.list.separator_tooltip", "Click to filter the list by this type of object.").Value,
                        OnClicked = (btn, _) => { if (searchBox != null) searchBox.Text = $"%{currentType.Name}"; return true; }
                    };

                    string headerText = GetHeaderForType(currentType);
                    _ = new GUITextBlock(new RectTransform(Vector2.One, separatorFrame.RectTransform),
                        headerText, font: GUIStyle.SmallFont, textColor: Color.MediumPurple, textAlignment: Alignment.Center)
                    { Wrap = true };
                    currentRow = null;
                    itemsInRow = 0;
                }

                bool isFav = PrefabHelper.IsFavorite(prefab.Identifier.Value);

                switch (leftPanelMode)
                {
                    case DisplayMode.Compact:
                        if (currentRow == null || itemsInRow >= maxItemsPerRow)
                        {
                            var rowRect = new RectTransform(new Vector2(1f, 0f), itemList.Content.RectTransform) { MinSize = new Point(0, slotSize) };
                            currentRow = new GUILayoutGroup(rowRect, isHorizontal: true) { AbsoluteSpacing = 2 };
                            itemsInRow = 0;
                        }
                        CardBuilder.DrawMinimalItemRow(currentRow, prefab, 1,
                            onPrimaryClick: ProfileHelper.OnPrimary,
                            onSecondaryClick: ProfileHelper.OnSecondary,
                            badgeColor: isFav ? Color.Gold : null);
                        itemsInRow++;
                        break;

                    case DisplayMode.Normal:
                        var btn = new GUIButton(new RectTransform(new Vector2(1f, 0f), itemList.Content.RectTransform) { MinSize = new Point(0, slotSize) }, style: "ListBoxElement")
                        {
                            OnClicked = (_, _) => { ProfileHelper.OnPrimary(prefab); return true; },
                            OnSecondaryClicked = (_, _) => { ProfileHelper.OnSecondary(prefab); return true; },
                            Selected = false,
                            CanBeFocused = true
                        };
                        CardBuilder.DrawCompactItemRow(btn, prefab, 1, false, color: isFav ? Color.Gold : Color.White);
                        break;
                }
            }

            itemsLoaded = nextBatch;
            itemList.RecalculateChildren();
            itemList.UpdateScrollBarSize();

            float totalScrollableHeightAfter = itemList.Content.Rect.Height - itemList.Rect.Height;
            if (totalScrollableHeightAfter > 0)
            {
                float newBarScroll = currentScrollPixels / totalScrollableHeightAfter;
                newBarScroll = MathHelper.Clamp(newBarScroll, 0, 0.80f);
                itemList.ScrollBar.BarScroll = newBarScroll;
            }

            isUpdating = false;
        }

        private string GetHeaderForType(Type type)
        {
            if (prefabHeaders == null) return type.Name.SpacedPascalCase();

            Type? current = type;
            while (current != null && current != typeof(Prefab))
            {
                if (prefabHeaders.TryGetValue(current, out var header))
                    return header;
                current = current.BaseType;
            }
            return type.Name.SpacedPascalCase();
        }

        #endregion

        #region Layout

        private void UpdateLayout()
        {
            if (leftPanel == null || rightPanel == null || centerPanel == null) return;

            Rectangle areaRect = ContentArea.Rect;
            if (areaRect.Width <= 0) return;

            int spacing = (int)(areaRect.Width * 0.015f);
            int leftW = leftPanel.RectTransform.NonScaledSize.X;
            int rightW = rightPanel.RectTransform.NonScaledSize.X;

            int totalAvailableForSides = areaRect.Width - MinCenterWidth - (spacing * 2);
            if (leftW + rightW > totalAvailableForSides)
            {
                float totalSides = (float)leftW + rightW + 0.001f;
                leftW = (int)(totalAvailableForSides * (leftW / totalSides));
                rightW = totalAvailableForSides - leftW;
            }

            int centerWidth = areaRect.Width - leftW - rightW - (spacing * 2);

            leftPanel.RectTransform.NonScaledSize = new Point(leftW, areaRect.Height);
            centerPanel.RectTransform.AbsoluteOffset = new Point(leftW + spacing, 0);
            centerPanel.RectTransform.NonScaledSize = new Point(centerWidth, areaRect.Height);
            rightPanel.RectTransform.NonScaledSize = new Point(rightW, areaRect.Height);

            var newLeftMode = GetModeForWidth(leftW, SidebarHiddenThreshold, SidebarCompactThreshold);
            var newRightMode = GetModeForWidth(rightW, SidebarHiddenThreshold, SidebarCompactThreshold);
            var newCenterMode = GetModeForWidth(centerWidth, -1, CenterCompactThreshold);

            bool needsLeftRefresh = newLeftMode != leftPanelMode;
            if (leftPanelMode == DisplayMode.Compact && Math.Abs(leftW - lastLeftWForReflow) > 34)
            { needsLeftRefresh = true; lastLeftWForReflow = leftW; }

            bool needsCenterRefresh = newCenterMode != centerPanelMode;
            if (centerPanelMode == DisplayMode.Compact && Math.Abs(centerWidth - lastCenterWForReflow) > 34)
            { needsCenterRefresh = true; lastCenterWForReflow = centerWidth; }

            bool needsRightRefresh = newRightMode != rightPanelMode;

            if (needsLeftRefresh) { leftPanelMode = newLeftMode; lastLeftWForReflow = leftW; if (leftContainer != null) leftContainer.Visible = leftPanelMode != DisplayMode.Hidden; RefreshSearch(); }
            if (needsCenterRefresh) { centerPanelMode = newCenterMode; lastCenterWForReflow = centerWidth; if (_lastTarget != null) OnTargetChangedHandler(_lastTarget); }
            if (needsRightRefresh) { rightPanelMode = newRightMode; if (rightContainer != null) rightContainer.Visible = rightPanelMode != DisplayMode.Hidden; if (_lastTarget != null) OnTargetChangedHandler(_lastTarget); }

            AddToGUIUpdateList(order: 1);
        }

        private static DisplayMode GetModeForWidth(int width, int hiddenThreshold, int compactThreshold)
        {
            if (width < hiddenThreshold) return DisplayMode.Hidden;
            if (width < compactThreshold) return DisplayMode.Compact;
            return DisplayMode.Normal;
        }

        public void ForceLayoutUpdate()
        {
            if (leftPanel == null || rightPanel == null) return;
            if (config == null) return;

            RectTransform.NonScaledSize = config.WindowSize;
            if (config.WindowPosition.X >= 0 && config.WindowPosition.Y >= 0)
                RectTransform.AbsoluteOffset = config.WindowPosition;
            if (config.LeftPanelWidth > 0) leftPanel.RectTransform.NonScaledSize = new Point(config.LeftPanelWidth, leftPanel.Rect.Height);
            if (config.RightPanelWidth > 0) rightPanel.RectTransform.NonScaledSize = new Point(config.RightPanelWidth, rightPanel.Rect.Height);
            UpdateLayout();
        }

        private void ForceLayoutTo(Point? windowSize, int? leftW, int? rightW)
        {
            if (windowSize != null) RectTransform.NonScaledSize = (Point)windowSize;
            if (leftPanel != null && leftW != null && leftW >= 0) leftPanel.RectTransform.NonScaledSize = new Point((int)leftW, leftPanel.Rect.Height);
            if (rightPanel != null && rightW != null && rightW >= 0) rightPanel.RectTransform.NonScaledSize = new Point((int)rightW, rightPanel.Rect.Height);
            UpdateLayout();
        }
        private void ForceLayoutTo(TPLayout layout) => ForceLayoutTo(layout.WindowSize, layout.LeftPanelWidth, layout.RightPanelWidth);

        #endregion

        //MARK: Helpers

        internal struct TPLayout
        {
            public Point WindowSize { get; set; }
            public int LeftPanelWidth { get; set; }
            public int RightPanelWidth { get; set; }
        }

        internal static class WindowConfigHelper
        {
            // ─── XML Serialization Helpers ───

            internal static string LayoutsToXml(IDictionary<string, TPLayout>? layouts)
            {
                if (layouts == null || layouts.Count == 0) return "";

                var doc = new XDocument(
                    new XElement("Layouts",
                        layouts.Select(kvp =>
                            new XElement("Preset",
                                new XAttribute("name", kvp.Key),
                                new XAttribute("winW", kvp.Value.WindowSize.X),
                                new XAttribute("winH", kvp.Value.WindowSize.Y),

                                new XAttribute("leftW", kvp.Value.LeftPanelWidth),
                                new XAttribute("rightW", kvp.Value.RightPanelWidth)
                            )
                        )
                    )
                );

                return doc.ToString(SaveOptions.DisableFormatting);
            }

            internal static Dictionary<string, TPLayout> XmlToLayouts(string? xml)
            {
                var result = new Dictionary<string, TPLayout>();
                if (string.IsNullOrEmpty(xml)) return result;

                try
                {
                    var doc = XDocument.Parse(xml);
                    XElement? root = doc.Root;
                    if (root == null || root.Name != "Layouts") return result;
                    int unnamedCount = 0;
                    foreach (var preset in root.Elements("Preset"))
                    {
                        string name = preset.Attribute("name")?.Value ?? $"Unnamed{unnamedCount++}";

                        if (int.TryParse(preset.Attribute("winW")?.Value, out int winW) &&
                            int.TryParse(preset.Attribute("winH")?.Value, out int winH) &&
                            int.TryParse(preset.Attribute("leftW")?.Value, out int leftW) &&
                            int.TryParse(preset.Attribute("rightW")?.Value, out int rightW))
                        {
                            result[name] = new TPLayout
                            {
                                WindowSize = new Point(winW, winH),
                                LeftPanelWidth = leftW,
                                RightPanelWidth = rightW
                            };
                        }
                    }
                }
                catch { }

                return result;
            }
        }
    }
}
