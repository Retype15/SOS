// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.GUI;

using BGUI = Barotrauma.GUI;

namespace SOS
{
    internal abstract class WindowProfileBase
    {
        private GUIFrame? _configPopup = null;

        protected void Update()
        {
            _configPopup?.AddToGUIUpdateList(order: 2);
        }

        protected GUIButton CreateSettingsButton(RectTransform parent)
        {
            var btn = new GUIButton(new RectTransform(new Point(32, 32), parent, isFixedSize: true), "\u2699", style: "GUIButtonSettings")
            {
                ToolTip = Texts.Get("sos.window.settings", "Settings").Value,
                OnClicked = (_, _) => { OpenSettingsPopup(); return true; }
            };
            return btn;
        }

        protected void OpenSettingsPopup()
        {
            var ctrl = SOSController.Instance;
            var configs = ctrl.CachedConfigs;
            if (configs.Count == 0) return;

            _configPopup = new GUIFrame(new RectTransform(new Point(420, 500), BGUI.Canvas, Anchor.Center), style: "InnerFrame")
            {
                Color = Color.Black * 0.95f,
                CanBeFocused = true
            };

            var header = new GUITextBlock(new RectTransform(new Vector2(1f, 0.08f), _configPopup.RectTransform, Anchor.TopCenter),
                Texts.Get("sos.settings.title", "Settings"), font: GUIStyle.LargeFont, textAlignment: Alignment.Center);

            var list = new GUIListBox(new RectTransform(new Vector2(1f, 0.84f), _configPopup.RectTransform, Anchor.Center), style: null)
            {
                Spacing = 4,
                Padding = new Vector4(10, 10, 10, 10)
            };

            foreach (var config in configs)
            {
                config.DrawSettings(list);
            }

            _ = new GUIButton(new RectTransform(new Vector2(1f, 0.08f), _configPopup.RectTransform, Anchor.BottomCenter), Texts.Get("sos.gen.close", "Close [Esc]").Value, style: "GUIButtonLarge")
            {
                OnClicked = (_, _) => { _configPopup.Parent?.RemoveChild(_configPopup); _configPopup = null; return true; }
            };
        }

        protected static (GUIButton back, GUIButton forward) CreateNavigationButtons(RectTransform parent)
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

        protected static GUITabWidget CreateTabWidget(RectTransform parent)
        {
            var widget = new GUITabWidget(parent);
            foreach (var tab in API.CreateTabs())
                widget.RegisterTab(tab);
            return widget;
        }

        protected static bool DataInitialized => SOSController.Instance.DataInitialized;
        protected static bool CanNavigateBack => SOSController.Instance.HistoryBack.Count > 0;
        protected static bool CanNavigateForward => SOSController.Instance.HistoryForward.Count > 0;

        protected static void SelectTarget(Prefab target) => SOSController.Instance.OnTargetSelected(target);

        protected static void OpenContextMenu(Prefab target)
        {
            if (target == null) return;
            List<ContextMenuOption> options = [];
            if (target is ItemPrefab item && item.FabricationRecipes is { Count: > 0 })
            {
                var tracker = SOSController.Instance.Tracker;
                if (item.FabricationRecipes.Count == 1)
                {
                    var single = PrefabResolver.GetFabricationRecipe(item);
                    options.Add(new ContextMenuOption(tracker.GetStringTrackToHUD(single).Value, isEnabled: true, () => tracker.AddOrRemoveRecipe(single)) { Tooltip = Texts.Get("sos.tracker.track-untrack.tooltip", "Track or Untrack all recipes from this item.") });
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

        protected static string GetRawXMLSafe(Prefab item)
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

        protected static void XmlContextMenu(GUITextViewer viewer)
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

        protected static void CloseWindow() => API.Emit(CommKeys.CloseWindow);
    }
}
