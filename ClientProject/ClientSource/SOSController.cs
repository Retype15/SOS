// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SOS.GUI;

using BGUI = Barotrauma.GUI;

namespace SOS
{
    public sealed class SOSController : IDisposable
    {
        private SOSWindow? mainWindow;

        public bool HaveOldConfigFile = false;

        public bool DataInitialized { get; private set; } = false;

        private HashSet<string>? _favoritedItems;
        public HashSet<string> FavoritedItems
        {
            get
            {
                _favoritedItems ??= ConfigHelper.CsvToHashSet(cfg.FavoritesRaw);
                return _favoritedItems;
            }
        }

        private GUIRecipeTracker? _tracker;
        internal GUIRecipeTracker Tracker => _tracker ??= GUIRecipeTracker.InstantiateWithDefault();

        public ClientConfig cfg = ClientConfig.Instance;

        private Keys ToggleKey => cfg.SOSOpenKey.Key;

        public string LastSearchQuery
        {
            get => cfg.LastSearchQuery ?? "";
            set { cfg.LastSearchQuery = value; }
        }
        public Prefab? CurrentTarget { get; internal set; }

        public Stack<Prefab> HistoryBack { get; } = new Stack<Prefab>();
        public Stack<Prefab> HistoryForward { get; } = new Stack<Prefab>();

        public Point? WindowSize { get; set; }
        public Point? WindowPosition { get; set; }
        public int? LeftPanelWidth { get; set; }
        public int? RightPanelWidth { get; set; }
        public bool RawXmlMode
        {
            get => cfg.RawXmlMode;
            set { cfg.RawXmlMode = value; }
        }
        public float XmlFontScale
        {
            get => cfg.XmlFontScale;
            set { cfg.XmlFontScale = value; }
        }

        public int DummyDeathCount
        {
            get => cfg.DummyDeathCount;
            set { cfg.DummyDeathCount = value; }
        }
        public string? DummyCharacterXML
        {
            get => cfg.DummyCharacterXML;
            set { cfg.DummyCharacterXML = value; }
        }
        public List<string> TabHistory { get; } = [];
        public bool DummySimulated
        {
            get => cfg.DummySimulated;
            set { cfg.DummySimulated = value; }
        }

        private static bool migrationPending = false;
        public static bool MigrationPending { get => migrationPending; set => migrationPending = value; }

        public static bool IsSOSBlocked =>
            GameMain.Instance.LoadingScreenOpen == true ||
            migrationPending ||
            CoroutineManager.IsCoroutineRunning("LevelTransition");

        public void PushTabHistory(string uid)
        {
            TabHistory.Remove(uid);
            TabHistory.Insert(0, uid);
        }

        internal Dictionary<string, SavedLayout> CustomLayouts { get; } = [];

        private static SOSController? _instance;
        public static SOSController Instance => _instance ??= new SOSController();

        private SOSController() { }

        public void AddFavorite(string id) { FavoritedItems.Add(id); }
        public void RemoveFavorite(string id) { FavoritedItems.Remove(id); }

        public void SetSearchFilter(string tag) => mainWindow?.SetSearchFilter(tag);

        public void ToggleUI()
        {
            if (mainWindow != null)
            {
                Dispose();
            }
            else
            {
                if (HaveOldConfigFile)
                {
                    MigrationDialog.Show();
                    HaveOldConfigFile = false;
                    Logger.LogDebug("[SOS] Opening migration window.");
                    return;
                }

                if (Screen.Selected == null || IsSOSBlocked) return;

                API.Initialize(Plugin.Instance.PluginManagementService);
                LoadSettings();

                mainWindow = new SOSWindow();

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

        public void OnTargetSelected(Prefab? item, bool isHistoryNavigation = false)
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
            }
            UpdateWindowDetails(item);
        }

        public void ApplyLayout(Point size, int leftW, int rightW)
        {
            WindowSize = size;
            LeftPanelWidth = leftW;
            RightPanelWidth = rightW;
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
        }

        public void DeleteLayout(string name)
        {
            CustomLayouts.Remove(name);
        }

        public void UpdateWindowDetails(Prefab target)
        {
            if (mainWindow == null) return;

            mainWindow.UpdateDetailsPanel(target);

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
            if (target is ItemPrefab item && item.FabricationRecipes is { Count: > 0 })
            {
                if (item.FabricationRecipes.Count == 1)
                {
                    var single = PrefabResolver.GetFabricationRecipe(item);
                    options.Add(new ContextMenuOption(Tracker.GetStringTrackToHUD(single).Value, isEnabled: true, () => Tracker.AddOrRemoveRecipe(single)) { Tooltip = Texts.Get("sos.tracker.track-untrack.tooltip", "Track or Untrack all recipes from this item.") });
                }
                else
                {
                    var subs = new List<ContextMenuOption>
                    {
                        new(
                            Tracker.ContainsAnyRecipes(item)
                                ? Texts.Get("sos.window.remove_all", "Remove All")
                                : Texts.Get("sos.window.track_all", "Track All"),
                            isEnabled: true,
                            Tracker.ContainsAnyRecipes(item)
                                ? () => Tracker.RemoveRecipes(item)
                                : () => Tracker.AddRecipes(item))
                    };

                    foreach (var (id, recipe) in item.FabricationRecipes)
                    {
                        bool tracked = Tracker.ContainsRecipe(recipe);
                        subs.Add(new ContextMenuOption(
                            $"{GUIRecipeTracker.GetTrackOrUntrack(!tracked)} {recipe.DisplayName}",
                            isEnabled: true, () => Tracker.AddOrRemoveRecipe(recipe))
                        { Tooltip = recipe.GetRequirementsToString() });
                    }

                    options.Add(new ContextMenuOption(
                        Texts.Get("sos.context.track_recipe", "Add to HUD").Value,
                        isEnabled: true, [.. subs]));
                }
            }

            options.Add(new ContextMenuOption(Texts.Get("sos.context.view_recipes", "View Recipes"), isEnabled: true, onSelected: () =>
            {
                OnTargetSelected(target);
            }));

            string targetId = target.Identifier.Value;
            bool isFav = FavoritedItems.Contains(targetId);
            string favText = isFav ? Texts.Get("sos.context.remove_favorite", "Remove from Favorites").Value : Texts.Get("sos.context.add_favorite", "Add to Favorites").Value;

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

            if (target is ItemPrefab)
                options.Add(new ContextMenuOption(Tracker.GetStringTrackToHUD(recipe).Value, isEnabled: true, () => Tracker.AddOrRemoveRecipe(recipe)));

            //options.Add(new ContextMenuOption("Ver más info (WIP)", isEnabled: false));

            _ = GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, Texts.Get("sos.context.recipe_options", "Recipe Options"), null, [.. options]);
        }

        public void OnRecipeSelected(ItemPrefab item, FabricationRecipe recipe)
        {
            Tracker.AddOrRemoveRecipe(recipe);
            OnTargetSelected(item);
        }

        public void Update()
        {
            bool canHandleInputs = BGUI.KeyboardDispatcher.Subscriber == null || BGUI.KeyboardDispatcher.Subscriber is GUIDropDown2;

            if (canHandleInputs)
            {
                if (cfg.SOSOpenKeyHit)
                {
                    if (PlayerInput.IsCtrlDown())
                    {
                        CrossThread.RequestExecutionOnMainThread(() => Tracker.ToggleTracker());
                    }
                    else
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
                }
            }

            if (mainWindow != null)
            {
                if (canHandleInputs)
                {
                    if (PlayerInput.KeyHit(Keys.Escape))
                    {
                        //mainWindow.SetSelected();
                        CrossThread.RequestExecutionOnMainThread(ToggleUI);
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
            if (migrationPending) MigrationDialog.Update();
            if (!IsSOSBlocked && Screen.Selected == GameMain.GameScreen) Tracker.Update();
        }

        public static void SaveSettings()
        {
            foreach (var config in API.CreateConfigs())
                config.Save();
        }

        public static void LoadSettings()
        {
            foreach (var config in API.CreateConfigs())
                config.Load();
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
            if (BGUI.MouseOn != null)
            {
                GUIComponent? curr = BGUI.MouseOn;
                while (curr != null)
                {
                    // Any direct
                    if (curr.UserData is Prefab prefab) return prefab;

                    // Specific
                    if (curr.UserData is Item item) return item.Prefab;
                    if (curr.UserData is Affliction affliction) return affliction.Prefab;

                    // Shop
                    if (curr.UserData is PurchasedItem purchasedItem) return purchasedItem.ItemPrefab;
                    if (curr.UserData is FabricationRecipe recipe) return recipe.TargetItem;

                    // Shop Buttons
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

        public void Dispose()
        {
            SaveSettings();

            GUIAnimSequence.ClearAll();

            _favoritedItems = null;
            mainWindow?.Destroy();
            mainWindow = null;

            GC.SuppressFinalize(this);
        }

        public void Destroy()
        {
            Dispose();
            _instance = null;
        }

        ~SOSController() => Dispose();
    }
}