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

        public bool DataInitialized { get; private set; } = false;

        public HashSet<string> FavoritedItems { get; } = [];

        public TrackerManager Tracker { get; } = new();
        private readonly Keys toggleKey = Keys.J;
        private bool wasKeyDown = false;

        public string LastSearchQuery { get; set; } = "";
        public Prefab? CurrentTarget { get; private set; }

        public Stack<Prefab> HistoryBack { get; } = new Stack<Prefab>();
        public Stack<Prefab> HistoryForward { get; } = new Stack<Prefab>();

        public Point? WindowSize { get; set; }
        public Point? WindowPosition { get; set; }
        public int? LeftPanelWidth { get; set; }
        public int? RightPanelWidth { get; set; }
        public bool RawXmlMode { get; set; } = false;
        public float XmlFontScale { get; set; } = 0.9f;

        public Dictionary<string, SavedLayout> CustomLayouts { get; } = [];

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

                if (!DataInitialized)
                {
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        RecipeAnalyzer.PrecomputeCaches();
                        DataInitialized = true;

                        CrossThread.RequestExecutionOnMainThread(() =>
                        {
                            mainWindow?.OnInitializationComplete();
                        });
                    });
                }
                else if (CurrentTarget != null)
                {
                    UpdateWindowDetails(CurrentTarget);
                }
            }
        }

        public void Destroy()
        {
            mainWindow?.Destroy();
            mainWindow = null;
        }

        public void OnTargetSelected(Prefab item, bool isHistoryNavigation = false)
        {
            if (item == null) return;

            if (!isHistoryNavigation && CurrentTarget != null && CurrentTarget != item)
            {
                HistoryBack.Push(CurrentTarget);
                HistoryForward.Clear();
            }

            if (CurrentTarget != item)
            {
                CurrentTarget = item;
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
                LastItemId = this.CurrentTarget?.Identifier.Value ?? "",
                TrackedItemId = this.Tracker.TrackedItem?.Identifier.Value ?? "",
                TrackedRecipeHash = this.Tracker.TrackedRecipe?.RecipeHash ?? 0,
                WindowSize = this.WindowSize,
                WindowPosition = this.WindowPosition,
                LeftPanelWidth = this.LeftPanelWidth,
                RightPanelWidth = this.RightPanelWidth,
                CustomLayouts = this.CustomLayouts,
                RawXmlMode = this.RawXmlMode,
                XmlFontScale = this.XmlFontScale
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
            RawXmlMode = data.RawXmlMode;
            XmlFontScale = data.XmlFontScale;
            foreach (var kvp in data.CustomLayouts) CustomLayouts[kvp.Key] = kvp.Value;

            if (!WindowSize.HasValue) WindowSize = new Point(1250, 850);
            if (!LeftPanelWidth.HasValue) LeftPanelWidth = 250;
            if (!RightPanelWidth.HasValue) RightPanelWidth = 300;

            if (!string.IsNullOrEmpty(data.LastItemId))
            {
                CurrentTarget = (Prefab?)ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == data.LastItemId)
                             ?? (Prefab?)AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == data.LastItemId);
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

        public void UpdateWindowDetails(Prefab target)
        {
            if (mainWindow == null) return;

            if (target is ItemPrefab item)
            {
                var craftRecipes = RecipeAnalyzer.GetCraftingRecipes(item);
                var deconOutputs = RecipeAnalyzer.GetDeconstructionOutputs(item);
                var usesAsIngredient = RecipeAnalyzer.GetUsesAsIngredient(item);
                var obtainedFrom = RecipeAnalyzer.GetSourcesFromDeconstruction(item);

                mainWindow.UpdateDetailsPanel(item, craftRecipes, deconOutputs, usesAsIngredient, obtainedFrom);
            }
            else
            {
                mainWindow.UpdateDetailsPanel(target, [], [], [], []);
            }

            mainWindow?.UpdateNavigationButtons();
        }

        public void NavigateBack()
        {
            if (HistoryBack.Count > 0)
            {
                if (CurrentTarget != null) HistoryForward.Push(CurrentTarget);
                CurrentTarget = HistoryBack.Pop();
                UpdateWindowDetails(CurrentTarget);
            }
        }

        public void NavigateForward()
        {
            if (HistoryForward.Count > 0)
            {
                if (CurrentTarget != null) HistoryBack.Push(CurrentTarget);
                CurrentTarget = HistoryForward.Pop();
                UpdateWindowDetails(CurrentTarget);
            }
        }

        public void OpenContextMenu(Prefab target)
        {
            if (target == null) return;
            List<ContextMenuOption> options = [];
            if (target is ItemPrefab item) options.Add(new ContextMenuOption(TextSOS.Get("sos.context.track", "Track to HUD"), isEnabled: true, onSelected: () => Tracker.SetTrackedItem(item)));

            options.Add(new ContextMenuOption(TextSOS.Get("sos.context.view_recipes", "View Recipes"), isEnabled: true, onSelected: () =>
            {
                OnTargetSelected(target);
            }));

            string targetId = target.Identifier.Value;
            bool isFav = FavoritedItems.Contains(targetId);
            string favText = isFav ? TextSOS.Get("sos.context.remove_favorite", "Remove from Favorites").Value : TextSOS.Get("sos.context.add_favorite", "Add to Favorites").Value;

            options.Add(new ContextMenuOption(favText, isEnabled: true, onSelected: () =>
            {
                if (isFav) FavoritedItems.Remove(targetId);
                else FavoritedItems.Add(targetId);

                mainWindow?.RefreshSearch();
            }));

            RichString name = target.Name();

            _ = GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, name, null, [.. options]);
        }

        public void OpenRecipeContextMenu(Prefab target, FabricationRecipe recipe)
        {
            if (target == null || recipe == null) return;

            var options = new List<ContextMenuOption>();

            if (target is ItemPrefab item)
                if (Tracker.TrackedRecipe == recipe)
                    options.Add(new ContextMenuOption(TextSOS.Get("sos.context.untrack", "Remove from HUD"), isEnabled: true, onSelected: () =>
                    {
                        Tracker.SetTrackedItem(null);
                    }));
                else
                    options.Add(new ContextMenuOption(TextSOS.Get("sos.context.track_recipe", "Add to HUD"), isEnabled: true, onSelected: () =>
                    {
                        Tracker.SetTrackedItem(item, recipe);
                    }));

            //options.Add(new ContextMenuOption("Ver más info (WIP)", isEnabled: false));

            _ = GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, TextSOS.Get("sos.context.recipe_options", "Recipe Options"), null, [.. options]);
        }

        public void OnRecipeSelected(ItemPrefab item, FabricationRecipe recipe)
        {
            Tracker.SetTrackedItem(item, recipe);
            OnTargetSelected(item);
        }

        public void Update()
        {
            bool canHandleInputs = GUI.KeyboardDispatcher.Subscriber == null || GUI.KeyboardDispatcher.Subscriber is GUIDropDown2;

            if (canHandleInputs)
            {
                var kb = Keyboard.GetState();
                bool isKeyDownNow = kb.IsKeyDown(toggleKey);

                if (isKeyDownNow && !wasKeyDown)
                {
                    Prefab? detected = GetPrefabUnderMouse();

                    CrossThread.RequestExecutionOnMainThread(() =>
                    {

                        if (detected != null)
                        {
                            OnTargetSelected(detected);
                            if (mainWindow == null) ToggleUI();
                        }
                        else
                        {
                            ToggleUI();
                        }
                    });

                }
                wasKeyDown = isKeyDownNow;
            }
            else
            {
                wasKeyDown = Keyboard.GetState().IsKeyDown(toggleKey);
            }

            if (mainWindow != null)
            {
                if (canHandleInputs)
                {
                    if (PlayerInput.KeyHit(Keys.Escape))
                    {
                        //mainWindow.SetSelected();
                        CrossThread.RequestExecutionOnMainThread(() => ToggleUI());
                        return;
                    }
                    else if
                    (
                        (PlayerInput.KeyHit(Keys.Right) && PlayerInput.IsAltDown()) ||
                        (PlayerInput.KeyHit(Keys.Back) && PlayerInput.IsShiftDown()) ||
                        PlayerInput.Mouse5ButtonClicked()
                    ) CrossThread.RequestExecutionOnMainThread(() => NavigateForward());
                    else if
                    (
                        (PlayerInput.KeyHit(Keys.Left) && PlayerInput.IsAltDown()) ||
                        PlayerInput.KeyHit(Keys.Back) ||
                        PlayerInput.Mouse4ButtonClicked()
                    ) CrossThread.RequestExecutionOnMainThread(() => NavigateBack());


                }

                mainWindow.Update();
            }

            Tracker.UpdateHUD();
        }

        private static Prefab? GetPrefabUnderMouse()
        {
            // 1. World
            if (PlayerInput.IsShiftDown() && Character.Controlled?.FocusedItem != null)
            {
                return Character.Controlled.FocusedItem.Prefab;
            }

            // 2. Inv
            if (Inventory.SelectedSlot?.Item != null)
            {
                return Inventory.SelectedSlot.Item.Prefab;
            }

            // 3. other GUIs
            if (GUI.MouseOn != null)
            {
                GUIComponent? curr = GUI.MouseOn;
                while (curr != null)
                {
                    // Any direct
                    if (curr.UserData is Prefab prefab) return prefab;

                    // Specific
                    if (curr.UserData is Item item) return item.Prefab;
                    if (curr.UserData is Affliction affliction) return affliction.Prefab;

                    // Shopp
                    if (curr.UserData is PurchasedItem purchasedItem) return purchasedItem.ItemPrefab;
                    if (curr.UserData is FabricationRecipe recipe) return recipe.TargetItem;

                    // Shop Btns
                    if (curr.UserData as string == "addbutton" || curr.UserData as string == "removebutton")
                    {
                        GUIComponent? p = curr.Parent;
                        while (p != null)
                        {
                            if (p.UserData is PurchasedItem pi)
                            {
                                return pi.ItemPrefab;
                            }
                            p = p.Parent;
                        }
                    }

                    // parent
                    curr = curr.Parent;
                }
            }

            return null;
        }
    }
}