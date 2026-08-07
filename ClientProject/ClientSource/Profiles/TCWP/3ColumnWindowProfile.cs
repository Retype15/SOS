// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Xml.Linq;
using Barotrauma;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using SOS.GUI;
using SOS.Panels.AfflictionPanel;
using SOS.Panels.ItemPanel;
using BGUI = Barotrauma.GUI;

namespace SOS.Profiles.TCWP
{
    [AutoRegister]
    internal sealed class ThreeColumnWindowProfile : ISOSWindowProfile
    {
        public string Id => "SOS.Profile.Default3Column";
        public double Order => 0;

        private Configs.TCWP.TCWPConfig? _config;
        public ISOSConfig? ProfileConfig => _config ??= new();

        private enum ProfileState { Loading, Ready }
        private ProfileState _state = ProfileState.Loading;

        #region Vars

        // Root
        private GUIResizableFrame? mainFrame;
        private GUIFrame? topBar;
        private GUIFrame? contentArea;
        private GUIImage? logoImage;
        private GUITextBlock? loadingText;

        // Left panel
        private GUIResizableFrame? leftPanel;
        private GUIFrame? leftContainer;
        private GUIListBox? itemList;
        private GUITextBox? searchBox;
        private GUIButton? btnBack;
        private GUIButton? btnForward;
        private GUIButton? btnSettings;

        // Center panel
        private GUIFrame? centerPanel;
        private GUIFrame? detailsHeader;
        private GUIComponent? centerTabWidget;

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
        private ISOSPrefab[]? prefabProviders;
        private Dictionary<Type, string>? prefabHeaders;
        private Type? lastTypeInList;

        private readonly double searchDelay = 0.2;
        private double searchExecutionTime = 0;
        private string? pendingSearchQuery;

        // Layout state
        private const int HeaderHeight = 42;
        private const int BottomMargin = 10;
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
        private Prefab? _currentItem;

        #endregion

        public void Open()
        {
            API.On<Prefab?>(CommKeys.SelectTarget, OnTargetChangedHandler);
            API.On<string>(CommKeys.SetSearchFilter, OnSetSearchFilter);
            API.On<TPLayout>(CommKeys.ApplyLayout, OnApplyLayout);
            API.On("RefreshSearch", RefreshSearch);

            if (!ProfileHelper.DataInitialized)
            {
                RecipeAnalyzer.Initialize();
                BuildLoadingUI();
            }
            else
                BuildMainUIAndShow();
        }

        public void Update()
        {
            if (_state == ProfileState.Loading && ProfileHelper.DataInitialized)
            {
                TransitionToMainUI();
            }

            if (mainFrame == null) return;

            if (_state == ProfileState.Ready)
            {
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
                        mainFrame.RemoveChild(layoutMenuFrame);
                        layoutMenuFrame = null;
                    }
                }
            }
        }

        public void Close()
        {
            API.Off<Prefab?>(CommKeys.SelectTarget, OnTargetChangedHandler);
            API.Off<string>(CommKeys.SetSearchFilter, OnSetSearchFilter);
            API.Off<TPLayout>(CommKeys.ApplyLayout, OnApplyLayout);
            API.Off(CommKeys.RefreshSearch, RefreshSearch);

            ClinicalSimulatorManager.Destroy();

            if (centerTabWidget is IDisposable d) d.Dispose();
            centerTabWidget = null;

            if (mainFrame?.RectTransform != null)
                mainFrame.RectTransform.Parent = null;

            mainFrame = null;
            itemList = null;
            searchBox = null;
            metaPanel = null;
            _state = ProfileState.Loading;
        }

        public void Destroy()
        {
            Close();
            _config = null;
        }

        private void OnTargetChangedHandler(Prefab? target)
        {
            if (target == null) return;
            _currentItem = target;
            if (mainFrame == null || detailsHeader == null || centerTabWidget == null || metaPanel == null) return;

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
                _ = new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), imgFrame.RectTransform, Anchor.Center), icon, scaleToFit: true) { Color = target.IconColor(), CanBeFocused = false };
            }

            var (headerName, headerColor) = target.SafeName(Color.White);
            _ = new GUITextBlock(
                new RectTransform(new Vector2(0.8f, 1f), headerLayout.RectTransform),
                headerName,
                font: GUIStyle.LargeFont, textColor: headerColor, textAlignment: Alignment.CenterLeft)
            { Wrap = false, AutoScaleHorizontal = true, CanBeFocused = false };


            if (centerTabWidget is GUITabWidget tw)
                tw.UpdateTabs(target, OnPrimary, OnSecondary);

            foreach (var section in API.CreateSections())
            {
                try
                {
                    if (section.Analyze(target))
                        section.Draw(metaPanel, OnPrimary, OnSecondary);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[SOS] Exception in section '{section.Id}': {ex.Message}");
                    continue;
                }
                finally { if (section is IDisposable d) d.Dispose(); }
            }

            if (xmlContentText != null)
                xmlContentText.Text = ProfileHelper.GetRawXMLSafe(target).FormatToXMLCode();

            UpdateNavigationButtonStates();
        }

        private void OnSetSearchFilter(string tag)
        {
            if (searchBox != null) searchBox.Text = tag;
            UpdateSearch(tag);
        }

        private void OnApplyLayout(TPLayout layout) => ForceLayoutTo(layout);

        private void OnPrimary(Prefab p) => ProfileHelper.SelectTarget(p);
        private void OnSecondary(Prefab p) => ProfileHelper.OpenContextMenu(p);

        #region BuildUI

        private void BuildLoadingUI()
        {
            if (mainFrame != null) return;

            mainFrame = new GUIResizableFrame(new RectTransform(new Vector2(0.95f, 0.9f), BGUI.Canvas, Anchor.TopLeft), style: "CircuitBoxFrame")
            {
                CanBeFocused = true,
                Selected = true,
                Color = Color.Black * 0.85f,
                AllowedDirections = ResizeDirection.All,
                RectTransform = { MinSize = new Point(400, 200) }
            };

            ApplySavedSizeAndPosition();

            var loadingFrame = new GUIFrame(new RectTransform(Vector2.One, mainFrame.RectTransform, Anchor.Center), style: "InnerFrame") { Color = Color.Black * 0.5f };

            var imgPath = $"{Plugin.Instance.Package.Dir}/Content/SOS_LOGO_TEXT.png";
            if (File.Exists(imgPath) && LuaCsFile.CanReadFromPath(imgPath))
            {
                var sprite = new Sprite(imgPath, Vector2.One);
                logoImage = new GUIImage(new RectTransform(new Vector2(0.8f, 0.6f), loadingFrame.RectTransform, Anchor.Center), sprite: sprite, scaleToFit: true);
                logoImage.ExFadeIn(duration: 0.5f, targetFactor: 0.8f, alsoChildren: true);
            }

            loadingText = new GUITextBlock(new RectTransform(new Vector2(0.9f, 0.2f), loadingFrame.RectTransform, Anchor.BottomCenter)
            { AbsoluteOffset = new Point(0, -30) },
            Texts.Get("sos.window.loading", "Loading dependencies..."),
            font: GUIStyle.LargeFont, textAlignment: Alignment.Center, wrap: true);

            loadingText.Wait(0.5f).ExFadeIn(duration: 0.5f);
            _state = ProfileState.Loading;
        }

        private void TransitionToMainUI()
        {
            if (mainFrame == null) return;

            loadingText?.Wait(0.5f)
                .Execute(() => loadingText?.SetRichText(Texts.Get("sos.window.loading.complete", "Loading complete!").SetColor(Color.LightGreen)))
                .WaitFinish()
                .ExBlink(duration: 4.0f, minAlpha: 0.0f, maxAlpha: 0.6f, interval: 1.0f, alsoChildren: true).WaitFinish()
                .ExFadeOut(0.5f)
                .Execute(() => loadingText = null);

            var logo = logoImage;
            logoImage = null;

            mainFrame.Children.FirstOrDefault()?
                .Wait(0.5f)
                .ExFadeOut(duration: 0.5f, targetFactor: 0.6f, alsoChildren: true)
                .Wait(4.0f)
                .ExFadeOut(duration: 1.0f, alsoChildren: true)
                .WaitFinish()
                .Execute(() =>
                {
                    logo?.Parent?.RemoveChild(logo);
                });

            BuildMainUI();
            _state = ProfileState.Ready;

            topBar?.ExFadeIn(duration: 1.0f, alsoChildren: false);
            contentArea?.SetAlpha(0.0f);
            contentArea?.ExFadeIn(duration: 1.0f, targetFactor: 1.0f, alsoChildren: true);
        }

        private void ApplySavedSizeAndPosition()
        {
            if (mainFrame == null) return;
            var cfg = _config;
            if (cfg == null) return;

            mainFrame.RectTransform.NonScaledSize = cfg.WindowSize;

            var wp = cfg.WindowPosition;
            if (wp.X >= 0 && wp.Y >= 0)
                mainFrame.RectTransform.AbsoluteOffset = wp;
            else
            {
                int cx = (GameMain.GraphicsWidth / 2) - (mainFrame.Rect.Width / 2);
                int cy = (GameMain.GraphicsHeight / 2) - (mainFrame.Rect.Height / 2);
                mainFrame.RectTransform.AbsoluteOffset = new Point(cx, cy);
            }
        }

        private void BuildMainUIAndShow()
        {
            mainFrame = new GUIResizableFrame(new RectTransform(new Vector2(0.95f, 0.9f), BGUI.Canvas, Anchor.TopLeft), style: "CircuitBoxFrame")
            {
                CanBeFocused = true,
                Selected = true,
                Color = Color.Black * 0.85f,
                AllowedDirections = ResizeDirection.All,
                RectTransform = { MinSize = new Point(400, 200) }
            };

            ApplySavedSizeAndPosition();
            BuildMainUI();
            _state = ProfileState.Ready;
        }

        private void BuildMainUI()
        {
            if (mainFrame == null) return;

            leftPanelMode = DisplayMode.Normal;
            centerPanelMode = DisplayMode.Normal;
            rightPanelMode = DisplayMode.Normal;
            lastLeftWForReflow = 0;
            lastCenterWForReflow = 0;
            isUpdating = false;
            pendingSearchQuery = null;
            searchExecutionTime = 0;
            layoutMenuFrame = null;

            topBar = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.0f), mainFrame.RectTransform, Anchor.TopCenter), "GUIFrameBottom");
            topBar.RectTransform.MinSize = new Point(0, HeaderHeight);
            topBar.RectTransform.MaxSize = new Point(int.MaxValue, HeaderHeight);

            _ = new GUITextBlock(new RectTransform(Vector2.One, topBar.RectTransform),
                Texts.Get("sos.window.title", "SOS - Recipe Browser"),
                textAlignment: Alignment.Center, font: GUIStyle.LargeFont);

            var leftTools = new GUILayoutGroup(new RectTransform(new Vector2(0.32f, 0.8f), topBar.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(10, 0) }, isHorizontal: true)
            {
                AbsoluteSpacing = 5,
                Stretch = false
            };

            btnSettings = ProfileHelper.CreateSettingsButton(leftTools.RectTransform);
            (btnBack, btnForward) = ProfileHelper.CreateNavigationButtons(leftTools.RectTransform);

            var topButtons = new GUILayoutGroup(new RectTransform(new Vector2(0.2f, 0.8f), topBar.RectTransform, Anchor.CenterRight) { AbsoluteOffset = new Point(10, 0) }, isHorizontal: true)
            { Stretch = false, RelativeSpacing = 0.05f, ChildAnchor = Anchor.CenterRight };

            _ = new GUIButton(new RectTransform(new Point(32, 32), topButtons.RectTransform, isFixedSize: true), "", style: "GUICancelButton")
            {
                OnClicked = (_, _) => { ProfileHelper.CloseWindow(); return true; },
                ToolTip = Texts.Get("sos.gen.close", "Close [Esc]").Value
            };

            var manageGroup = new GUILayoutGroup(new RectTransform(new Vector2(0.65f, 1f), topButtons.RectTransform), isHorizontal: true)
            { RelativeSpacing = 0f, Stretch = false, ChildAnchor = Anchor.CenterRight };

            var ctrl = SOSController.Instance;
            var text = Texts.Get("sos.window.manage_hud", "MANAGE HUD");
            _ = new GUIButton(new RectTransform(new Point(text.Length * 12, 32), manageGroup.RectTransform, isFixedSize: true), text, style: "DeviceButton")
            {
                OnClicked = (_, _) =>
                {
                    var options = ctrl.Tracker.GetManageHudContextMenuOptions();
                    GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, Texts.Get("sos.window.remove_recipes", "Remove Recipes").Value, null, [.. options]);
                    return true;
                },
                ToolTip = Texts.Get("sos.window.manage_hud_tooltip", "Manage tracked recipes on the HUD").Value
            };

            _ = new GUIButton(new RectTransform(new Point(32, 32), manageGroup.RectTransform, isFixedSize: true), "o", style: "DeviceButton")
            {
                OnClicked = (_, _) => { ctrl.Tracker.ToggleTracker(); return true; },
                ToolTip = Texts.Get("sos.window.toggle_tracker_tooltip", "Toggle HUD tracker (Ctrl+[key])").Value.Replace("[key]", ctrl.cfg.SOSOpenKey.Key.ToString())
            };

            contentArea = new GUIFrame(new RectTransform(new Vector2(0.98f, 0.0f), mainFrame.RectTransform, Anchor.TopCenter)
            { AbsoluteOffset = new Point(0, HeaderHeight) }, style: null);

            // Left panel
            leftPanel = new GUIResizableFrame(new RectTransform(new Vector2(0.20f, 1f), contentArea.RectTransform, Anchor.TopLeft), style: "InnerFrame")
            {
                AllowedDirections = ResizeDirection.Right,
                IsFixed = true,
                Color = Color.Black * 0.4f,
                RectTransform = { MinSize = new Point(20, 50), MaxSize = new Point(500, 2000) }
            };

            if (_config != null && _config.LeftPanelWidth > 0)
                leftPanel.RectTransform.NonScaledSize = new Point(_config.LeftPanelWidth, 0);

            leftContainer = new GUIFrame(new RectTransform(new Vector2(0.95f, 0.98f), leftPanel.RectTransform, Anchor.Center), style: null);
            var leftLayout = new GUILayoutGroup(new RectTransform(Vector2.One, leftContainer.RectTransform)) { Stretch = true, RelativeSpacing = 0.01f };

            var searchContainer = new GUIFrame(new RectTransform(new Vector2(1f, 0.05f), leftLayout.RectTransform), style: "InnerFrame")
            {
                RectTransform = { MinSize = new Point(0, 35), MaxSize = new Point(int.MaxValue, 35) }
            };

            searchBox = BGUI.CreateTextBoxWithPlaceholder(new RectTransform(Vector2.One, searchContainer.RectTransform), ctrl.LastSearchQuery, Texts.Get("sos.window.search_placeholder", "Search item..."));
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
            centerPanel = new GUIFrame(new RectTransform(new Vector2(0.52f, 1f), contentArea.RectTransform, Anchor.TopLeft), style: null)
            { RectTransform = { MinSize = new Point(200, 50) } };

            var centerLayout = new GUILayoutGroup(new RectTransform(Vector2.One, centerPanel.RectTransform)) { Stretch = true, RelativeSpacing = 0.01f };

            detailsHeader = new GUIFrame(new RectTransform(new Vector2(1f, 0.10f), centerLayout.RectTransform), style: "CircuitBoxFrame")
            {
                Color = Color.Black * 0.4f,
                RectTransform = { MinSize = new Point(0, 65), MaxSize = new Point(int.MaxValue, 65) }
            };

            centerTabWidget = ProfileHelper.CreateTabWidget(new RectTransform(new Vector2(1f, 0.90f), centerLayout.RectTransform));

            prefabProviders = [.. API.CreatePrefabProviders()];
            prefabHeaders = prefabProviders.ToDictionary(p => p.PrefabType, p => p.Header);

            // Right panel
            rightPanel = new GUIResizableFrame(new RectTransform(new Vector2(0.24f, 1f), contentArea.RectTransform, Anchor.TopRight), style: "InnerFrame")
            {
                AllowedDirections = ResizeDirection.Left,
                IsFixed = true,
                Color = Color.Black * 0.4f,
                RectTransform = { MinSize = new Point(20, 50), MaxSize = new Point(1000, 2000) }
            };

            if (_config != null && _config.RightPanelWidth > 0)
                rightPanel.RectTransform.NonScaledSize = new Point(_config.RightPanelWidth, 0);

            rightContainer = new GUIFrame(new RectTransform(new Vector2(0.95f, 0.98f), rightPanel.RectTransform, Anchor.Center), style: null);
            var rightLayout = new GUILayoutGroup(new RectTransform(Vector2.One, rightContainer.RectTransform)) { Stretch = true };

            var rightHeaderArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.045f), rightLayout.RectTransform), style: null)
            { RectTransform = { MinSize = new Point(0, 32) } };

            rawXmlTickBox = new GUITickBox(new RectTransform(new Vector2(1f, 0.45f), rightHeaderArea.RectTransform, Anchor.CenterLeft), Texts.Get("sos.window.raw_xml", "RAW XML").Value, font: GUIStyle.SmallFont)
            {
                Selected = ctrl.RawXmlMode,
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
                Visible = ctrl.RawXmlMode,
                Font = GUIStyle.SmallFont,
                TextScale = ctrl.XmlFontScale,
                OnScaleChanged = (scale) => ctrl.cfg.XmlFontScale = scale,
                ContentMenu = ProfileHelper.XmlContextMenu
            };

            metaPanel.Visible = !ctrl.RawXmlMode;
            if (metaPanel.ContentBackground != null) metaPanel.ContentBackground.Color = Color.Transparent;

            rawXmlTickBox.OnSelected = (tick) =>
            {
                ctrl.cfg.RawXmlMode = tick.Selected;
                metaPanel.Visible = !tick.Selected;
                if (xmlContentText != null) xmlContentText.Visible = tick.Selected;
                return true;
            };

            UpdateSearch(ctrl.LastSearchQuery);
            UpdateLayout();
            mainFrame.ForceLayoutRecalculation();

            OnTargetChangedHandler(API.GetState<Prefab?>(CommKeys.SelectTarget));
        }

        #endregion

        #region Search

        private void HandleSearchDebounce()
        {
            if (pendingSearchQuery != null && Timing.TotalTime >= searchExecutionTime)
            {
                var ctrl = SOSController.Instance;
                ctrl.cfg.LastSearchQuery = pendingSearchQuery;
                UpdateSearch(pendingSearchQuery);
                pendingSearchQuery = null;
            }
        }

        public void RefreshSearch() => UpdateSearch(searchBox?.Text ?? "");

        private void UpdateSearch(string query)
        {
            if (itemList == null || prefabProviders == null) return;
            var filter = new SearchFilter(query);

            allFilteredTargets.Clear();
            lastTypeInList = null;

            var candidates = new List<Prefab>();

            foreach (var provider in prefabProviders.OrderBy(p => p.Order))
            {
                try
                {
                    if (!filter.AllowsType(provider.PrefabType.Name)) continue;
                    candidates.AddRange(provider.GetAll(filter));
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[SOS] Provider '{provider.Id}' failed: {ex.Message}");
                }
            }

            var ctrl = SOSController.Instance;
            allFilteredTargets = [.. candidates
                .OrderByDescending(c => ctrl.FavoritedItems.Contains(c.Identifier.Value))];

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

                bool isFav = ctrl.FavoritedItems.Contains(prefab.Identifier.Value);

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
                            onPrimaryClick: p => OnPrimary(p),
                            onSecondaryClick: p => OnSecondary(p),
                            badgeColor: isFav ? Color.Gold : null);
                        itemsInRow++;
                        break;

                    case DisplayMode.Normal:
                        var btn = new GUIButton(new RectTransform(new Vector2(1f, 0f), itemList.Content.RectTransform) { MinSize = new Point(0, slotSize) }, style: "ListBoxElement")
                        {
                            OnClicked = (_, _) => { OnPrimary(prefab); return true; },
                            OnSecondaryClicked = (_, _) => { OnSecondary(prefab); return true; },
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

        //MARK: Navigation

        private void UpdateNavigationButtonStates()
        {
            if (btnBack != null)
            {
                btnBack.Enabled = ProfileHelper.CanNavigateBack;
                if (btnBack.Enabled && SOSController.Instance.HistoryBack.Count > 0)
                {
                    var prevItem = SOSController.Instance.HistoryBack.Peek();
                    var (navBackName, _) = prevItem.SafeName(Color.White);
                    btnBack.OnDrawToolTip = component => component.ToolTip = RichString.Rich($"{Texts.Get("sos.window.back", "Back")}: {navBackName.SetColor(Color.BlueViolet)}\n{Texts.Get("sos.window.back.shortcuts", "Shortcuts:\n- Alt + Left Arrow\n- Backspace\n- Mouse 4")}");
                }
            }

            if (btnForward != null)
            {
                btnForward.Enabled = ProfileHelper.CanNavigateForward;
                if (btnForward.Enabled && SOSController.Instance.HistoryForward.Count > 0)
                {
                    var nextItem = SOSController.Instance.HistoryForward.Peek();
                    var (navForwardName, _) = nextItem.SafeName(Color.White);
                    btnForward.OnDrawToolTip = component => component.ToolTip = RichString.Rich($"{Texts.Get("sos.window.forward", "Forward")}: {navForwardName.SetColor(Color.BlueViolet)}\n{Texts.Get("sos.window.forward.shortcuts", "Shortcuts:\n- Alt + Right Arrow\n- Shift + Backspace\n- Mouse 5")}");
                }
            }
        }

        #region Layout

        private void UpdateLayout()
        {
            if (mainFrame == null || contentArea == null || leftPanel == null || rightPanel == null || centerPanel == null) return;

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
            if (needsCenterRefresh) { centerPanelMode = newCenterMode; lastCenterWForReflow = centerWidth; if (_currentItem != null) OnTargetChangedHandler(_currentItem); }
            if (needsRightRefresh) { rightPanelMode = newRightMode; if (rightContainer != null) rightContainer.Visible = rightPanelMode != DisplayMode.Hidden; if (_currentItem != null) OnTargetChangedHandler(_currentItem); }

            //TODO: Ver si optimizamos esto para no guardar cada frame.
            if (_config != null && mainFrame.RectTransform.NonScaledSize != Point.Zero)
            {
                _config.WindowSize = mainFrame.RectTransform.NonScaledSize;
                _config.WindowPosition = mainFrame.RectTransform.AbsoluteOffset;
                _config.LeftPanelWidth = leftPanel.Rect.Width;
                _config.RightPanelWidth = rightPanel.Rect.Width;
            }

            mainFrame.AddToGUIUpdateList(order: 1);
        }

        private static DisplayMode GetModeForWidth(int width, int hiddenThreshold, int compactThreshold)
        {
            if (width < hiddenThreshold) return DisplayMode.Hidden;
            if (width < compactThreshold) return DisplayMode.Compact;
            return DisplayMode.Normal;
        }

        public void ForceLayoutUpdate()
        {
            if (mainFrame == null || leftPanel == null || rightPanel == null) return;
            if (_config == null) return;

            mainFrame.RectTransform.NonScaledSize = _config.WindowSize;
            if (_config.WindowPosition.X >= 0 && _config.WindowPosition.Y >= 0)
                mainFrame.RectTransform.AbsoluteOffset = _config.WindowPosition;
            if (_config.LeftPanelWidth > 0) leftPanel.RectTransform.NonScaledSize = new Point(_config.LeftPanelWidth, leftPanel.Rect.Height);
            if (_config.RightPanelWidth > 0) rightPanel.RectTransform.NonScaledSize = new Point(_config.RightPanelWidth, rightPanel.Rect.Height);
            UpdateLayout();
        }

        private void ForceLayoutTo(Point? windowSize, int? leftW, int? rightW)
        {
            if (mainFrame == null) return;
            if (windowSize != null) mainFrame.RectTransform.NonScaledSize = (Point)windowSize;
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

            internal static string LayoutsToXml(Dictionary<string, TPLayout>? layouts)
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
