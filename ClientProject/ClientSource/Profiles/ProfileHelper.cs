// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.Configs;
using SOS.GUI;
using SOS.Panels.ItemPanel;

using BGUI = Barotrauma.GUI;

namespace SOS.Profiles
{
    public static class ProfileHelper
    {
        private const float MinColumnWidth = 450f;

        internal static GUIResizableFrame? _settingsWindow;
        private static GUIComponent? _contentContainer;
        private static List<GUIListBox>? _contentLists;
        private static int _currentColumnCount = 0;

        public static bool IsSettingsOpen => _settingsWindow != null || _contentContainer != null || _contentLists != null;

        public static GUIButton CreateSettingsButton(RectTransform parent, GUIComponent? configHost = null)
        {
            var btn = new GUIButton(new RectTransform(new Point(32, 32), parent, isFixedSize: true), "\u2699", style: "GUIButtonSettings")
            {
                ToolTip = Texts.Get("sos.window.settings", "Settings").Value,
                OnClicked = (_, _) =>
                {
                    if (IsSettingsOpen) CloseSettings();
                    else OpenSettings(configHost);
                    return true;
                }
            };
            return btn;
        }

        public static void OpenSettings(GUIComponent? host = null)
        {
            Logger.LogDebug("ProfileHelper.OpenSettings: start", level: LogLevel.Trace);

            if (IsSettingsOpen)
            {
                CloseSettings();
            }

            if (host != null)
            {
                _contentContainer = host;
                _contentLists = null;
            }
            else
            {
                var cfg = WindowProfileConfig.Instance;
                Point initialSize = cfg.SettingsWindowSize.X > 0 && cfg.SettingsWindowSize.Y > 0
                    ? cfg.SettingsWindowSize
                    : new Point(550, 600);

                _settingsWindow = new GUIResizableFrame(new RectTransform(initialSize, BGUI.Canvas, Anchor.Center), style: "InnerFrame")
                {
                    Color = Color.Black * 0.95f,
                    CanBeFocused = true,
                    AllowedDirections = ResizeDirection.All,
                    ClampToParentBounds = true,
                    RectTransform = { MinSize = new Point(450, 400) }
                };

                if (cfg.SettingsWindowPosition.X >= 0 && cfg.SettingsWindowPosition.Y >= 0)
                    _settingsWindow.RectTransform.AbsoluteOffset = cfg.SettingsWindowPosition;

                var topBar = new GUIFrame(new RectTransform(new Vector2(1f, 0.08f), _settingsWindow.RectTransform, Anchor.TopCenter), style: null)
                {
                    RectTransform = { MinSize = new Point(0, 36), MaxSize = new Point(int.MaxValue, 36) }
                };

                _ = new GUITextBlock(new RectTransform(new Vector2(0.8f, 1f), topBar.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(10, 0) },
                    Texts.Get("sos.window.settings", "Settings"), font: GUIStyle.LargeFont, textAlignment: Alignment.CenterLeft);

                _ = new GUIButton(new RectTransform(new Point(28, 28), topBar.RectTransform, Anchor.CenterRight) { AbsoluteOffset = new Point(-6, 0), IsFixedSize = true }, "", style: "GUICancelButton")
                {
                    ToolTip = Texts.Get("sos.gen.close", "Close [Esc]").Value,
                    OnClicked = (_, _) => { CloseSettings(); return true; }
                };

                _contentContainer = new GUIFrame(new RectTransform(new Vector2(1f, 0.92f), _settingsWindow.RectTransform, Anchor.BottomCenter), style: null);

                _settingsWindow.RectTransform.SizeChanged += OnSettingsWindowResized;
            }

            RefreshSettings();
            Logger.LogDebug("ProfileHelper.OpenSettings: end", level: LogLevel.Trace);
        }

        private static void OnSettingsWindowResized()
        {
            if (_settingsWindow == null || _contentContainer == null) return;

            var cfg = WindowProfileConfig.Instance;
            cfg.SettingsWindowSize = _settingsWindow.RectTransform.NonScaledSize;
            cfg.SettingsWindowPosition = _settingsWindow.RectTransform.AbsoluteOffset;

            var configs = SOSController.Instance.CachedConfigs;
            if (configs.Count == 0) configs = [.. API.CreateConfigs()];

            int targetColumns = CalculateColumnCount(_contentContainer.Rect.Width, configs.Count, MinColumnWidth);
            if (targetColumns != _currentColumnCount)
            {
                RefreshSettings();
            }
        }

        public static void CloseSettings()
        {
            if (!IsSettingsOpen) return;
            Logger.LogDebug("ProfileHelper.CloseSettings: closing settings", level: LogLevel.Trace);

            if (_settingsWindow != null)
            {
                var cfg = WindowProfileConfig.Instance;
                cfg.SettingsWindowSize = _settingsWindow.RectTransform.NonScaledSize;
                cfg.SettingsWindowPosition = _settingsWindow.RectTransform.AbsoluteOffset;

                _settingsWindow.RemoveFromGUIUpdateList();
                _settingsWindow.Parent?.RemoveChild(_settingsWindow);
                _settingsWindow = null;
            }

            _contentContainer = null;
            _contentLists = null;
            _currentColumnCount = 0;

            SOSController.Instance.SaveSettings();
        }

        public static void RefreshSettings()
        {
            Logger.LogDebug("ProfileHelper.RefreshSettings: start in-place refresh", level: LogLevel.Trace);

            var configs = SOSController.Instance.CachedConfigs;
            if (configs.Count == 0) configs = [.. API.CreateConfigs()];

            if (_contentContainer != null)
            {
                _contentContainer.ClearChildren();
                DrawSettings(_contentContainer, configs, MinColumnWidth);
            }
            else if (_contentLists != null)
            {
                foreach (var list in _contentLists)
                {
                    list.Content.ClearChildren();
                }
                DrawSettings(_contentLists, configs);
            }

            Logger.LogDebug("ProfileHelper.RefreshSettings: end in-place refresh", level: LogLevel.Trace);
        }

        public static void DrawSettings(GUIComponent container, IReadOnlyList<ISOSConfig> configs, float minColumnWidth = MinColumnWidth)
        {
            if (container == null || configs == null || configs.Count == 0) return;

            int colCount = CalculateColumnCount(container.Rect.Width, configs.Count, minColumnWidth);
            _currentColumnCount = colCount;

            var layoutGroup = new GUILayoutGroup(new RectTransform(Vector2.One, container.RectTransform), isHorizontal: true)
            {
                Stretch = true,
                RelativeSpacing = 0.01f,
                CanBeFocused = false
            };

            float relWidth = 1.0f / colCount;
            var createdLists = new List<GUIListBox>(colCount);

            for (int i = 0; i < colCount; i++)
            {
                var list = new GUIListBox(new RectTransform(new Vector2(relWidth, 1f), layoutGroup.RectTransform), style: null)
                {
                    Spacing = 4,
                    Padding = new Vector4(8, 8, 8, 8),
                    CanBeFocused = true
                };
                createdLists.Add(list);
            }

            DrawSettings(createdLists, configs);

            var activeLists = createdLists.Where(l => l.Content.Children.Any()).ToList();

            if (activeLists.Count < createdLists.Count)
            {
                foreach (var emptyList in createdLists.Where(l => !l.Content.Children.Any()))
                {
                    emptyList.RemoveFromGUIUpdateList();
                    layoutGroup.RemoveChild(emptyList);
                }

                if (activeLists.Count > 0)
                {
                    float adjustedWidth = 1.0f / activeLists.Count;
                    foreach (var list in activeLists)
                    {
                        list.RectTransform.RelativeSize = new Vector2(adjustedWidth, 1f);
                    }
                }
                layoutGroup.Recalculate();
            }
        }

        public static void DrawSettings(GUIListBox targetList, IReadOnlyList<ISOSConfig> configs)
        {
            DrawSettings([targetList], configs);
        }

        public static void DrawSettings(IEnumerable<GUIListBox> targetLists, IReadOnlyList<ISOSConfig> configs)
        {
            var lists = targetLists as IList<GUIListBox> ?? [.. targetLists];
            if (lists.Count == 0 || configs.Count == 0) return;

            int listCount = lists.Count;
            int totalConfigs = configs.Count;
            int chunkSize = (int)Math.Ceiling((double)totalConfigs / listCount);

            for (int listIndex = 0; listIndex < listCount; listIndex++)
            {
                int startIndex = listIndex * chunkSize;
                int count = Math.Min(chunkSize, totalConfigs - startIndex);

                if (count <= 0) break;

                var targetList = lists[listIndex];
                for (int i = 0; i < count; i++)
                {
                    var config = configs[startIndex + i];
                    config.DrawSettings(targetList);
                }
            }
        }

        private static int CalculateColumnCount(float availableWidth, int configCount, float minColumnWidth = 450f)
        {
            if (availableWidth <= 0 || configCount <= 0) return 1;
            int maxColumnsByWidth = (int)Math.Floor(availableWidth / minColumnWidth);
            return Math.Max(1, Math.Min(configCount, maxColumnsByWidth));
        }

        public static (GUIButton back, GUIButton forward) CreateNavigationButtons(RectTransform parent)
        {
            var back = new GUIButton(new RectTransform(new Point(32, 32), parent, isFixedSize: true), "", style: "GUIButtonToggleLeft")
            {
                ToolTip = Texts.Get("sos.window.back", "Back").Value,
                OnClicked = (_, _) => { API.Emit(CommKeys.NavigateBack); return true; }
            };
            if (back.Children.FirstOrDefault() is GUIImage imgB) imgB.SpriteEffects = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;

            var forward = new GUIButton(new RectTransform(new Point(32, 32), parent, isFixedSize: true), "", style: "GUIButtonToggleRight")
            {
                ToolTip = Texts.Get("sos.window.forward", "Forward").Value,
                OnClicked = (_, _) => { API.Emit(CommKeys.NavigateForward); return true; }
            };

            return (back, forward);
        }

        public static GUITabWidget CreateTabWidget(RectTransform parent)
        {
            var widget = new GUITabWidget(parent);
            foreach (var tab in API.CreateTabs())
                widget.RegisterTab(tab);
            return widget;
        }

        public static bool DataInitialized => RecipeAnalyzer.DataInitialized;
        public static bool CanNavigateBack => SOSController.Instance.HistoryBack.Count > 0;
        public static bool CanNavigateForward => SOSController.Instance.HistoryForward.Count > 0;

        public static void SelectTarget(Prefab target) => SOSController.Instance.OnTargetSelected(target);

        public static void OpenContextMenu(Prefab target)
        {
            if (target == null) return;
            List<ContextMenuOption> options = [];
            if (target is ItemPrefab item && item.FabricationRecipes is { Count: > 0 })
            {
                var tracker = SOSController.Instance.Tracker;
                if (item.FabricationRecipes.Count == 1)
                {
                    var single = PrefabResolver.GetFabricationRecipe(item);
                    options.Add(new ContextMenuOption(tracker.GetStringTrackToHUD(single).Value, isEnabled: true, () => tracker.AddOrRemoveRecipe(single)) { Tooltip = Texts.Get("sos.tracker.track-untrack.tooltip", "Track or untrack all recipes from this item.") });
                }
                else
                {
                    var subs = new List<ContextMenuOption>
                    {
                        new(
                            tracker.ContainsAnyRecipes(item)
                                ? Texts.Get("sos.window.remove_all", "Remove All")
                                : Texts.Get("sos.window.track_all", "Track All"),
                            isEnabled: true,
                            tracker.ContainsAnyRecipes(item)
                                ? () => tracker.RemoveRecipes(item)
                                : () => tracker.AddRecipes(item))
                    };

                    foreach (var (id, recipe) in item.FabricationRecipes)
                    {
                        bool tracked = tracker.ContainsRecipe(recipe);
                        subs.Add(new ContextMenuOption(
                            $"{GUIRecipeTracker.GetTrackOrUntrack(!tracked)} {recipe.DisplayName}",
                            isEnabled: true, () => tracker.AddOrRemoveRecipe(recipe))
                        { Tooltip = recipe.GetRequirementsToString() });
                    }

                    options.Add(new ContextMenuOption(
                        Texts.Get("sos.context.track_recipe", "Add to HUD").Value,
                        isEnabled: true, [.. subs]));
                }
            }

            options.Add(new ContextMenuOption(Texts.Get("sos.context.view_recipes", "View Recipes"), isEnabled: true, onSelected: () =>
            {
                SelectTarget(target);
            }));
            var favoritedItems = SOSController.Instance.FavoritedItems;
            string targetId = target.Identifier.Value;
            bool isFav = favoritedItems.Contains(targetId);
            string favText = isFav ? Texts.Get("sos.context.remove_favorite", "Remove from Favorites").Value : Texts.Get("sos.context.add_favorite", "Add to Favorites").Value;

            options.Add(new ContextMenuOption(favText, isEnabled: true, onSelected: () =>
            {
                if (isFav) favoritedItems.Remove(targetId);
                else favoritedItems.Add(targetId);

                API.Emit(CommKeys.RefreshSearch);
            }));

            RichString name = target.Name();

            _ = GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, name, null, [.. options]);
        }

        #region XML

        public static string GetRawXMLSafe(Prefab item)
        {
            var configElement = item.ConfigElement();
            if (configElement == null) return "<!-- No XML data found for this item -->";
            try
            {
                string rawXml = configElement.ToString() ?? "<!-- Empty XML -->";
                if (rawXml == "Barotrauma.ContentXElement")
                {
                    var type = configElement.GetType();
                    var prop = type.GetProperty("Element") ?? type.GetProperty("XElement");
                    var field = type.GetField("Element") ?? type.GetField("XElement");
                    object? inner = prop?.GetValue(configElement) ?? field?.GetValue(configElement);
                    if (inner != null) rawXml = inner.ToString() ?? rawXml;
                }
                return rawXml;
            }
            catch { return "<!-- Error parsing XML data -->"; }
        }

        public static void XmlContextMenu(GUITextViewer viewer)
        {
            var options = new List<ContextMenuOption>
            {
                new(Texts.Get("sos.xml.reset_zoom", "Reset Zoom").Value, isEnabled: true, onSelected: () =>
                {
                    viewer.TextScale = 0.8f;
                    viewer.scrollBarsNeedsRecalculation = true;
                    viewer.OnScaleChanged?.Invoke(viewer.TextScale);
                }),
                new(Texts.Get("sos.xml.copy", "Copy XML").Value, isEnabled: true, onSelected: () => Clipboard.SetText(viewer.Text.ToString()))
            };
            GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, "XML Actions", null, [.. options]);
        }

        #endregion

        public static void CloseWindow() => API.Emit(CommKeys.CloseWindow);
    }
}
