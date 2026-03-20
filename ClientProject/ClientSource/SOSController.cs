// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0079
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SOS
{
    public class SOSController
    {
        private SOSWindow? mainWindow;

        public HashSet<string> FavoritedItems { get; } = [];

        public TrackerManager Tracker { get; } = new();
        private readonly Keys toggleKey = Keys.J;
        private bool wasKeyDown = false;

        public string LastSearchQuery { get; set; } = "";
        public ItemPrefab? CurrentItem { get; private set; }

        public Stack<ItemPrefab> HistoryBack { get; } = new Stack<ItemPrefab>();
        public Stack<ItemPrefab> HistoryForward { get; } = new Stack<ItemPrefab>();

        public Point? WindowSize { get; set; }
        public Point? WindowPosition { get; set; }
        public int? LeftPanelWidth { get; set; }
        public int? RightPanelWidth { get; set; }

        public Dictionary<string, SavedLayout> CustomLayouts { get; } = new();

        private bool isDirty = false;

        public SOSController()
        {
            LoadSettings();
        }

        public void MarkDirty() => isDirty = true;

        public void SetTrackedItem(ItemPrefab? item, FabricationRecipe? recipe = null)
        {
            Tracker.SetTrackedItem(item, recipe);
            MarkDirty();
        }

        public void AddFavorite(string id) { if (FavoritedItems.Add(id)) MarkDirty(); }
        public void RemoveFavorite(string id) { if (FavoritedItems.Remove(id)) MarkDirty(); }

        public void ToggleUI()
        {
            if (mainWindow != null)
            {
                SaveSettings();
                this.Destroy();
            }
            else
            {
                if (Screen.Selected == null) return;

                mainWindow = new SOSWindow(this);

                if (CurrentItem != null)
                {
                    UpdateWindowDetails(CurrentItem);
                }
            }
        }

        public void Destroy()
        {
            mainWindow?.Destroy();
            mainWindow = null;
        }

        public void OnItemSelected(ItemPrefab item, bool isHistoryNavigation = false)
        {
            if (item == null) return;

            if (!isHistoryNavigation && CurrentItem != null && CurrentItem != item)
            {
                HistoryBack.Push(CurrentItem);
                HistoryForward.Clear();
            }

            if (CurrentItem != item)
            {
                CurrentItem = item;
                MarkDirty();
            }
            UpdateWindowDetails(item);
        }

        public void SaveSettings()
        {
            if (!isDirty) return;

            var data = new SettingsData
            {
                Favorites = this.FavoritedItems,
                LastSearchQuery = this.LastSearchQuery,
                LastItemId = this.CurrentItem?.Identifier.Value ?? "",
                TrackedItemId = this.Tracker.TrackedItem?.Identifier.Value ?? "",
                TrackedRecipeHash = this.Tracker.TrackedRecipe?.RecipeHash ?? 0,
                WindowSize = this.WindowSize,
                WindowPosition = this.WindowPosition,
                LeftPanelWidth = this.LeftPanelWidth,
                RightPanelWidth = this.RightPanelWidth,
                CustomLayouts = this.CustomLayouts
            };

            SettingsManager.Save(data);
            isDirty = false;
        }

        public void LoadSettings()
        {
            var data = SettingsManager.Load();

            foreach (var fav in data.Favorites) FavoritedItems.Add(fav);

            LastSearchQuery = data.LastSearchQuery;
            WindowSize = data.WindowSize;
            WindowPosition = data.WindowPosition;
            LeftPanelWidth = data.LeftPanelWidth;
            RightPanelWidth = data.RightPanelWidth;
            foreach (var kvp in data.CustomLayouts) CustomLayouts[kvp.Key] = kvp.Value;

            if (!WindowSize.HasValue) WindowSize = new Point(1250, 850);
            if (!LeftPanelWidth.HasValue) LeftPanelWidth = 250;
            if (!RightPanelWidth.HasValue) RightPanelWidth = 300;

            if (!string.IsNullOrEmpty(data.LastItemId))
            {
                CurrentItem = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == data.LastItemId);
            }

            if (!string.IsNullOrEmpty(data.TrackedItemId))
            {
                var targetPrefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == data.TrackedItemId);
                if (targetPrefab != null)
                {
                    var specificRecipe = targetPrefab.FabricationRecipes?.Values
                        .FirstOrDefault(r => r.RecipeHash == data.TrackedRecipeHash);

                    Tracker.SetTrackedItem(targetPrefab, specificRecipe);
                }
            }

        }

        public void ApplyLayout(Point size, int leftW, int rightW)
        {
            WindowSize = size;
            LeftPanelWidth = leftW;
            RightPanelWidth = rightW;
            MarkDirty();
            mainWindow?.ForceLayoutUpdate();
        }

        public void SaveCurrentLayout(string name)
        {
            if (mainWindow == null) return;
            
            CustomLayouts[name] = new SavedLayout
            {
                WindowSize = mainWindow.GetCurrentSize(),
                LeftPanelWidth = mainWindow.GetLeftWidth(),
                RightPanelWidth = mainWindow.GetRightWidth()
            };
            MarkDirty();
        }

        public void DeleteLayout(string name)
        {
            if (CustomLayouts.Remove(name)) MarkDirty();
        }

        private void UpdateWindowDetails(ItemPrefab item)
        {
            if (mainWindow == null) return;

            var craftRecipes = RecipeAnalyzer.GetCraftingRecipes(item);
            var deconOutputs = RecipeAnalyzer.GetDeconstructionOutputs(item);
            var usesAsIngredient = RecipeAnalyzer.GetUsesAsIngredient(item);
            var obtainedFrom = RecipeAnalyzer.GetSourcesFromDeconstruction(item);

            mainWindow?.UpdateDetailsPanel(item, craftRecipes, deconOutputs, usesAsIngredient, obtainedFrom);

            mainWindow?.UpdateNavigationButtons();
        }

        public void NavigateBack()
        {
            if (HistoryBack.Count > 0)
            {
                if (CurrentItem != null) HistoryForward.Push(CurrentItem);
                CurrentItem = HistoryBack.Pop();
                UpdateWindowDetails(CurrentItem);
            }
        }

        public void NavigateForward()
        {
            if (HistoryForward.Count > 0)
            {
                if (CurrentItem != null) HistoryBack.Push(CurrentItem);
                CurrentItem = HistoryForward.Pop();
                UpdateWindowDetails(CurrentItem);
            }
        }

        public void OpenContextMenu(ItemPrefab item)
        {
            if (item == null) return;
            List<ContextMenuOption> options =
            [
                new ContextMenuOption(TextSOS.Get("sos.context.track", "Track to HUD"), isEnabled: true, onSelected: () =>
                {
                    Tracker.SetTrackedItem(item);
                }),
                new ContextMenuOption(TextSOS.Get("sos.context.view_recipes", "View Recipes"), isEnabled: true, onSelected: () =>
                {
                    OnItemSelected(item);
                }),
            ];

            string targetId = item.Identifier.Value;
            bool isFav = FavoritedItems.Contains(targetId);
            string favText = isFav ? TextSOS.Get("sos.context.remove_favorite", "Remove from Favorites").Value : TextSOS.Get("sos.context.add_favorite", "Add to Favorites").Value;

            options.Add(new ContextMenuOption(favText, isEnabled: true, onSelected: () =>
            {
                if (isFav) FavoritedItems.Remove(targetId);
                else FavoritedItems.Add(targetId);

                mainWindow?.RefreshSearch();
            }));

            _ = GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, item.Name, null, [.. options]);
        }

        public void OpenRecipeContextMenu(ItemPrefab item, FabricationRecipe recipe)
        {
            if (item == null || recipe == null) return;

            var options = new List<ContextMenuOption>();

            bool isCurrentlyTracked = Tracker.TrackedRecipe == recipe;

            if (isCurrentlyTracked)
            {
                options.Add(new ContextMenuOption(TextSOS.Get("sos.context.untrack", "Remove from HUD"), isEnabled: true, onSelected: () =>
                {
                    Tracker.SetTrackedItem(null);
                }));
            }
            else
            {
                options.Add(new ContextMenuOption(TextSOS.Get("sos.context.track_recipe", "Add to HUD"), isEnabled: true, onSelected: () =>
                {
                    Tracker.SetTrackedItem(item, recipe);
                }));
            }

            //options.Add(new ContextMenuOption("Ver más info (WIP)", isEnabled: false));

            _ = GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, TextSOS.Get("sos.context.recipe_options", "Recipe Options"), null, [.. options]);
        }

        public void OnRecipeSelected(ItemPrefab item, FabricationRecipe recipe)
        {
            Tracker.SetTrackedItem(item, recipe);
            OnItemSelected(item);
        }

        public void Update()
        {
            if (GUI.KeyboardDispatcher.Subscriber != null && GUI.KeyboardDispatcher.Subscriber is not GUIDropDown2) return;

            var kb = Keyboard.GetState();
            bool isKeyDownNow = kb.IsKeyDown(toggleKey);

            if (isKeyDownNow && !wasKeyDown)
            {
                CrossThread.RequestExecutionOnMainThread(() => ToggleUI());
            }
            wasKeyDown = isKeyDownNow;

            if (mainWindow != null)
            {
                if (PlayerInput.KeyHit(Keys.Escape))
                {
                    mainWindow.SetSelected();
                    //PlayerInput.KeyDown(Keys.Escape);
                    CrossThread.RequestExecutionOnMainThread(() => ToggleUI());
                    return;
                }

                if (PlayerInput.KeyHit(Keys.Back) || PlayerInput.Mouse4ButtonClicked())
                {
                    CrossThread.RequestExecutionOnMainThread(() => NavigateBack());
                }

                mainWindow?.Update();
            }

            Tracker.UpdateHUD();
        }
    }
}