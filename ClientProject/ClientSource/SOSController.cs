// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using Microsoft.Xna.Framework.Input;
using System.Xml.Linq;


namespace SOS
{
    public class SOSController
    {
        private SOSWindow? mainWindow;

        public HashSet<string> FavoritedItems { get; } = new HashSet<string>();

        public TrackerManager Tracker { get; } = new TrackerManager();
        private readonly Keys toggleKey = Keys.J;
        private bool wasKeyDown = false;

        public string LastSearchQuery { get; set; } = "";
        public ItemPrefab? CurrentItem { get; private set; }

        public Stack<ItemPrefab> HistoryBack { get; } = new Stack<ItemPrefab>();
        public Stack<ItemPrefab> HistoryForward { get; } = new Stack<ItemPrefab>();

        private bool isDirty = false;
        private const int CurrentSaveVersion = 1;
        private readonly string configPath = "SOS_Settings.xml";

        public SOSController()
        {
            LoadSettings();
        }

        private void MarkDirty() => isDirty = true;

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
            if (mainWindow != null)
            {
                mainWindow.Destroy();
                mainWindow = null;
            }
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

            try
            {
                XDocument doc = new XDocument(
                    new XElement("SOSSettings",
                        new XAttribute("version", CurrentSaveVersion),

                        new XElement("Favorites",
                            FavoritedItems.Select(f => new XElement("Item", new XAttribute("id", f)))
                        ),

                        new XElement("State",
                            new XAttribute("lastItem", CurrentItem?.Identifier.Value ?? ""),
                            new XAttribute("lastSearch", LastSearchQuery ?? "")
                        ),

                        new XElement("Tracker",
                            new XAttribute("targetId", Tracker.TrackedItem?.Identifier.Value ?? ""),
                            new XAttribute("recipeHash", Tracker.TrackedRecipe?.RecipeHash.ToString() ?? "0")
                        )
                    )
                );

                doc.Save(configPath);
                isDirty = false;
                LuaCsLogger.LogMessage(TextSOS.Get("sos.config.saved", "[SOS] Settings saved (v[version]).").Replace("[version]", CurrentSaveVersion.ToString()).Value);
            }
            catch (Exception e)
            {
                LuaCsLogger.LogError(TextSOS.Get("sos.config.save_error", "[SOS] Failed to save settings: [error]").Replace("[error]", e.Message).Value);
            }
        }

        public void LoadSettings()
        {
            if (!File.Exists(configPath)) return;

            try
            {
                XDocument doc = XDocument.Load(configPath);
                XElement? root = doc.Element("SOSSettings");
                if (root == null) return;

                int fileVersion = int.Parse(root.Attribute("version")?.Value ?? "0");

                if (fileVersion >= 1)
                {
                    var favs = root.Element("Favorites")?.Elements("Item");
                    if (favs != null)
                    {
                        foreach (var f in favs) FavoritedItems.Add(f.Attribute("id")?.Value ?? "");
                    }

                    var state = root.Element("State");
                    if (state != null)
                    {
                        LastSearchQuery = state.Attribute("lastSearch")?.Value ?? "";
                        string lastItemId = state.Attribute("lastItem")?.Value ?? "";

                        if (!string.IsNullOrEmpty(lastItemId))
                        {
                            CurrentItem = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == lastItemId);
                        }
                    }

                    var tracker = root.Element("Tracker");
                    if (tracker != null)
                    {
                        string targetId = tracker.Attribute("targetId")?.Value ?? "";
                        uint recipeHash = uint.Parse(tracker.Attribute("recipeHash")?.Value ?? "0");

                        var targetPrefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == targetId);
                        if (targetPrefab != null)
                        {
                            var specificRecipe = targetPrefab.FabricationRecipes?.Values
                                .FirstOrDefault(r => r.RecipeHash == recipeHash);

                            Tracker.SetTrackedItem(targetPrefab, specificRecipe);
                        }
                    }
                }

                LuaCsLogger.LogMessage(TextSOS.Get("sos.config.loaded", "[SOS] Settings v[version] loaded successfully.").Replace("[version]", fileVersion.ToString()).Value);
            }
            catch (Exception e)
            {
                LuaCsLogger.LogError(TextSOS.Get("sos.config.load_error", "[SOS] Error reading settings file: [error]").Replace("[error]", e.Message).Value);
            }
        }

        private void UpdateWindowDetails(ItemPrefab item)
        {
            if (mainWindow == null) return;

            var craftRecipes = RecipeAnalyzer.GetCraftingRecipes(item);
            var deconOutputs = RecipeAnalyzer.GetDeconstructionOutputs(item);
            var usesAsIngredient = RecipeAnalyzer.GetUsesAsIngredient(item);
            var obtainedFrom = RecipeAnalyzer.GetSourcesFromDeconstruction(item);

            mainWindow.UpdateDetailsPanel(item, craftRecipes, deconOutputs, usesAsIngredient, obtainedFrom);

            mainWindow.UpdateNavigationButtons();
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
            var options = new List<ContextMenuOption>();

            options.Add(new ContextMenuOption(TextSOS.Get("sos.context.track", "Track to HUD"), isEnabled: true, onSelected: () =>
            {
                Tracker.SetTrackedItem(item);
            }));

            options.Add(new ContextMenuOption(TextSOS.Get("sos.context.view_recipes", "View Recipes"), isEnabled: true, onSelected: () =>
            {
                OnItemSelected(item);
            }));

            string targetId = item.Identifier.Value;
            bool isFav = FavoritedItems.Contains(targetId);
            string favText = isFav ? TextSOS.Get("sos.context.remove_favorite", "Remove from Favorites").Value : TextSOS.Get("sos.context.add_favorite", "Add to Favorites").Value;

            options.Add(new ContextMenuOption(favText, isEnabled: true, onSelected: () =>
            {
                if (isFav) FavoritedItems.Remove(targetId);
                else FavoritedItems.Add(targetId);

                mainWindow?.RefreshSearch();
            }));

            GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, item.Name, null, options.ToArray());
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

            GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, TextSOS.Get("sos.context.recipe_options", "Recipe Options"), null, options.ToArray());
        }

        public void OnRecipeSelected(ItemPrefab item, FabricationRecipe recipe)
        {
            Tracker.SetTrackedItem(item, recipe);
            OnItemSelected(item);
        }

        public void Update()
        {
            if (GUI.KeyboardDispatcher.Subscriber != null) return;

            var kb = Keyboard.GetState();
            bool isKeyDownNow = kb.IsKeyDown(toggleKey);

            if (isKeyDownNow && !wasKeyDown)
            {
                CrossThread.RequestExecutionOnMainThread(() => ToggleUI());
            }
            wasKeyDown = isKeyDownNow;

            if (mainWindow != null)
            {
                if (kb.IsKeyDown(Keys.Escape))
                {
                    CrossThread.RequestExecutionOnMainThread(() => ToggleUI());
                }

                if (PlayerInput.KeyHit(Keys.Back) || PlayerInput.Mouse4ButtonClicked())
                {
                    CrossThread.RequestExecutionOnMainThread(() => NavigateBack());
                }
                mainWindow.Update();
            }

            Tracker.UpdateHUD();
        }
    }
}