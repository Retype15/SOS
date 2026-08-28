// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.Configs;
using SOS.GUI;

using BGUI = Barotrauma.GUI;

namespace SOS.Profiles
{
    public static class ProfileHelper
    {
        private const float MinColumnWidth = 450f;

        internal static GUIWindow? _settingsWindow;
        private static GUIComponent? _contentContainer;
        private static List<GUIListBox>? _contentLists;
        private static int _currentColumnCount = 0;

        public static bool IsSettingsOpen => _settingsWindow != null || _contentContainer != null || _contentLists != null;

        internal static readonly List<string> TabHistory = [];

        public static void PushTabHistory(string uid)
        {
            TabHistory.Remove(uid);
            TabHistory.Insert(0, uid);
        }

        public static IReadOnlyList<string> GetTabHistory() => TabHistory;

        public static void ClearTabHistory() => TabHistory.Clear();

        private static readonly List<Prefab> _history = [];
        private static int _historyIndex = -1;

        public static bool CanNavigateBack => _historyIndex > 0;
        public static bool CanNavigateForward => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

        public static Prefab? PeekBack() => CanNavigateBack ? _history[_historyIndex - 1] : null;
        public static Prefab? PeekForward() => CanNavigateForward ? _history[_historyIndex + 1] : null;

        private static bool _isNavigating = false;
        private static bool _subscribed = false;

        private const double HistoryOrder = EventPriority.State + 0.5;

        public static void Subscribe()
        {
            if (_subscribed) return;
            API.On<Prefab?>(CommKeys.SelectTarget, HistoryPush, HistoryOrder);
            API.On(CommKeys.NavigateBack, HistoryBack, HistoryOrder);
            API.On(CommKeys.NavigateForward, HistoryForward, HistoryOrder);
            _subscribed = true;
        }

        public static void Unsubscribe()
        {
            if (!_subscribed) return;
            API.Off<Prefab?>(CommKeys.SelectTarget, HistoryPush, HistoryOrder);
            API.Off(CommKeys.NavigateBack, HistoryBack, HistoryOrder);
            API.Off(CommKeys.NavigateForward, HistoryForward, HistoryOrder);
            _subscribed = false;
        }

        public static void Update()
        {
            _settingsWindow?.AddToGUIUpdateList(order: 1);
        }

        public static void OnTargetSelected(Prefab? item)
        {
            if (item == null) return;
            var cur = API.GetState<Prefab?>(CommKeys.SelectTarget);
            if (cur == item) return;
            API.Emit(CommKeys.SelectTarget, item);
        }
        public static void NavigateBack() => API.Emit(CommKeys.NavigateBack);
        public static void NavigateForward() => API.Emit(CommKeys.NavigateForward);

        internal static void HistoryPush(Prefab? prefab)
        {
            if (prefab == null || _isNavigating) return;

            _history.Remove(prefab);

            if (CanNavigateForward)
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);

            _history.Add(prefab);
            _historyIndex++;
        }

        private static void HistoryBack()
        {
            if (!CanNavigateBack) return;
            _historyIndex--;
            var prev = _history[_historyIndex];
            _isNavigating = true;
            API.Emit(CommKeys.SelectTarget, prev);
            _isNavigating = false;
        }

        private static void HistoryForward()
        {
            if (!CanNavigateForward) return;
            _historyIndex++;
            var next = _history[_historyIndex];
            _isNavigating = true;
            API.Emit(CommKeys.SelectTarget, next);
            _isNavigating = false;
        }

        public static void SelectTarget(Prefab target)
        {
            var cur = API.GetState<Prefab?>(CommKeys.SelectTarget);
            if (cur != target)
                API.Emit(CommKeys.SelectTarget, target);
        }

        public static Point SettingsWindowSize { get; set; } = new(550, 600);
        public static Point SettingsWindowPosition { get; set; } = new(-1, -1);

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
            try
            {
                if (IsSettingsOpen) CloseSettings();

                if (host != null)
                {
                    _contentContainer = host;
                    _contentLists = null;
                }
                else
                {
                    Point initialSize = SettingsWindowSize.X > 0 && SettingsWindowSize.Y > 0
                        ? SettingsWindowSize
                        : new Point(550, 600);

                    _settingsWindow = new GUIWindow(
                        new RectTransform(initialSize, BGUI.Canvas, Anchor.Center) { MinSize = new Point(450, 400) },
                        Texts.Get("sos.window.settings", "Settings"),
                        style: "InnerFrame",
                        color: Color.Black * 0.95f,
                        buttons: WindowButtons.Close)
                    {
                        ClampToParentBounds = true,
                        AllowedDirections = ResizeDirection.All
                    };

                    if (SettingsWindowPosition.X >= 0 && SettingsWindowPosition.Y >= 0)
                        _settingsWindow.RectTransform.AbsoluteOffset = SettingsWindowPosition;

                    _settingsWindow.OnClose += CloseSettings;
                    _settingsWindow.RectTransform.SizeChanged += OnSettingsWindowResized;

                    var resetAllBtn = new GUIButton(
                        new RectTransform(new Point(120, 32), _settingsWindow.ControlBox.RectTransform, isFixedSize: true), // TODO: Añadir medida automática por cantidad de caracteres.
                        Texts.Get("sos.config.reset_all", "RESET ALL"),
                        style: "DeviceButton")
                    {
                        Color = Color.IndianRed * 0.9f,
                        ToolTip = Texts.Get("sos.config.reset_all_tooltip", "Resets all S.O.S. configurations to their default values.").Value,
                        OnClicked = (_, _) =>
                        {
                            ConfigHelper.ResetConfigs();
                            ProfileHelper.RefreshSettings();
                            return true;
                        }
                    };
                    _settingsWindow.SetControlBoxContentWidth();
                    _contentContainer = _settingsWindow.ContentArea;
                }
                RefreshSettings();
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] ProfileHelper.OpenSettings failed\n{ex}", level: LogLevel.Error);
            }
            Logger.LogDebug("ProfileHelper.OpenSettings: end", level: LogLevel.Trace);
        }

        private static void OnSettingsWindowResized()
        {
            if (_settingsWindow == null || _contentContainer == null) return;
            try
            {
                SettingsWindowSize = _settingsWindow.NormalSize;
                SettingsWindowPosition = _settingsWindow.NormalOffset;

                //TODO: Revisar si vale la pena un GetCountConfigs() más eficiente...
                var configs = API.GetAllConfigs();
                int targetColumns = CalculateColumnCount(_contentContainer.Rect.Width, configs.Count(), MinColumnWidth);
                if (targetColumns != _currentColumnCount)
                    RefreshSettings();
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] ProfileHelper.OnSettingsWindowResized failed\n{ex}", level: LogLevel.Error);
            }
        }

        public static void CloseSettings()
        {
            if (!IsSettingsOpen) return;
            Logger.LogDebug("ProfileHelper.CloseSettings: closing settings", level: LogLevel.Trace);
            try
            {
                if (_settingsWindow != null)
                {
                    SettingsWindowSize = _settingsWindow.NormalSize;
                    SettingsWindowPosition = _settingsWindow.NormalOffset;

                    _settingsWindow.OnClose -= CloseSettings;
                    _settingsWindow.RectTransform.SizeChanged -= OnSettingsWindowResized;
                    _settingsWindow.RemoveFromGUIUpdateList();
                    _settingsWindow.Parent?.RemoveChild(_settingsWindow);
                    _settingsWindow = null;
                }
                _contentContainer = null;
                _contentLists = null;
                _currentColumnCount = 0;
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] ProfileHelper.CloseSettings failed\n{ex}", level: LogLevel.Error);
            }
            ConfigHelper.SaveConfigs();
        }

        public static void RefreshSettings()
        {
            Logger.LogDebug("ProfileHelper.RefreshSettings: start in-place refresh", level: LogLevel.Trace);
            try
            {
                if (_contentContainer != null)
                {
                    _contentContainer.ClearChildren();
                    DrawSettings(_contentContainer, API.GetAllConfigs(), MinColumnWidth);
                }
                else if (_contentLists != null)
                {
                    foreach (var list in _contentLists)
                        list.Content.ClearChildren();
                    DrawSettings(_contentLists, API.GetAllConfigs());
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] ProfileHelper.RefreshSettings failed\n{ex}", level: LogLevel.Error);
            }
            Logger.LogDebug("ProfileHelper.RefreshSettings: end in-place refresh", level: LogLevel.Trace);
        }

        public static void DrawSettings(GUIComponent container, IEnumerable<ISOSConfig> configs, float minColumnWidth = MinColumnWidth)
        {
            if (container == null || !configs.Any()) return;
            try
            {
                int colCount = CalculateColumnCount(container.Rect.Width, configs.Count(), minColumnWidth);
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
                _contentLists = createdLists;
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
                            list.RectTransform.RelativeSize = new Vector2(adjustedWidth, 1f);
                    }
                    layoutGroup.Recalculate();
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] ProfileHelper.DrawSettings(container) failed\n{ex}", level: LogLevel.Error);
            }
        }

        public static void DrawSettings(GUIListBox targetList, IReadOnlyList<ISOSConfig> configs) => DrawSettings([targetList], configs);

        public static void DrawSettings(IReadOnlyList<GUIListBox> targetLists, IEnumerable<ISOSConfig> configs)
        {
            var configList = configs.ToList();
            if (targetLists.Count == 0 || configList.Count == 0) return;
            try
            {
                int listCount = targetLists.Count;
                int totalConfigs = configList.Count;
                int chunkSize = (int)Math.Ceiling((double)totalConfigs / listCount);
                for (int listIndex = 0; listIndex < listCount; listIndex++)
                {
                    int startIndex = listIndex * chunkSize;
                    int count = Math.Min(chunkSize, totalConfigs - startIndex);
                    if (count <= 0) break;
                    var targetList = targetLists[listIndex];
                    for (int i = 0; i < count; i++)
                    {
                        var config = configList[startIndex + i];
                        config.DrawSettings(targetList);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] ProfileHelper.DrawSettings(lists) failed\n{ex}", level: LogLevel.Error);
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

        public static GUITabWidget CreateTabWidget(RectTransform parent, IEnumerable<ITab> tabs)
        {
            var widget = new GUITabWidget(parent);
            foreach (var tab in tabs)
                widget.RegisterTab(tab);
            widget.OnTabSelected = tab => PushTabHistory(tab.Id);
            return widget;
        }

        public static void UpdateTabWidget(GUITabWidget widget, Prefab target, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            widget.UpdateTabs(target, onPrimary, onSecondary);
            foreach (var id in TabHistory)
                if (widget.TrySelectTab(id)) break;
        }

        public static void OpenContextMenu(Prefab target, Vector2? position = null)
        {
            if (target == null) return;
            var options = API.GetAllPrefabProviders()
                .Where(p => p.PrefabType.IsAssignableFrom(target.GetType()))
                .SelectMany(p => p.BuildContextOptions(target))
                .ToList();
            if (options.Count == 0) return;
            RichString name = target.Name();
            _ = GUIContextMenu.CreateContextMenu(position ?? PlayerInput.MousePosition, name, null, [.. options]);
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

        public static void ToggleWindow() => API.Emit(CommKeys.ToggleWindow);
        public static void OpenWindow() => API.Emit(CommKeys.OpenWindow);
        public static void CloseWindow() => API.Emit(CommKeys.CloseWindow);
    }
}
