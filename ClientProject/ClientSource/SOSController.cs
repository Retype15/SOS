// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SOS
{
    public sealed class SOSController
    {
        private SOSWindow? mainWindow;

        public bool HaveOldConfigFile = false;

        public bool DataInitialized { get; private set; } = false;

        private HashSet<string>? _favoritedItems;
        public HashSet<string> FavoritedItems
        {
            get
            {
                _favoritedItems ??= ClientConfig.CsvToHashSet(cfg.FavoritesRaw ?? "");
                return _favoritedItems;
            }
        }

        public GUIRecipeTracker Tracker { get; } = GUIRecipeTracker.InstantiateWithDefault();

        public ClientConfig cfg = ClientConfig.Instance;

        private Keys ToggleKey => cfg.SOSOpenKey.Key;
        private bool wasKeyDown = false;

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

        public Dictionary<string, SavedLayout> CustomLayouts { get; } = [];

        private static SOSController? _instance;
        public static SOSController Instance => _instance ??= new SOSController();

        private SOSController() { }

        public void AddFavorite(string id) { FavoritedItems.Add(id); }
        public void RemoveFavorite(string id) { FavoritedItems.Remove(id); }

        public void ToggleUI()
        {
            if (mainWindow != null)
            {
                SaveSettings();
                this.Destroy();
            }
            else
            {
                if (HaveOldConfigFile)
                {
                    MigrationDialog.Show();
                    HaveOldConfigFile = false;
                    RLogger.LogDebug("Abriendo la ventana de migracion");
                    return;
                }

                if (Screen.Selected == null || IsSOSBlocked) return;

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
            GUIAnimSequence.ClearAll();

            _favoritedItems = null;
            mainWindow?.Destroy();
            mainWindow = null;
            ClientConfig.Destroy();
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
            }
            UpdateWindowDetails(item);
        }

        public void SaveSettings()
        {
            if (ClinicalSimulatorManager.Patient != null)
            {
                this.DummyDeathCount = ClinicalSimulatorManager.DeathCount;
                this.DummyCharacterXML = ClinicalSimulatorManager.ExportSaveData()?.ToString();
                this.DummySimulated = !ClinicalSimulatorManager.HasStarted;
            }

            cfg.LastItemId = CurrentTarget?.Identifier.Value ?? "";
            cfg.WindowSizeX = WindowSize?.X ?? -1;
            cfg.WindowSizeY = WindowSize?.Y ?? -1;
            cfg.WindowPositionX = WindowPosition?.X ?? -1;
            cfg.WindowPositionY = WindowPosition?.Y ?? -1;
            cfg.LeftPanelWidth = LeftPanelWidth ?? 0;
            cfg.RightPanelWidth = RightPanelWidth ?? 0;
            cfg.FavoritesRaw = FavoritedItems.ToCsv();
            cfg.TabHistoryRaw = TabHistory.ToCsv();
            cfg.CustomLayoutsRaw = ClientConfig.LayoutsToXml(CustomLayouts);
            cfg.TrackedRecipesRaw = Tracker.ToCsv();
            cfg.TrackerVisible = Tracker.Visible;

            cfg.SaveAll();
        }

        public void LoadSettings()
        {
            if (cfg == null) return;

            // Simple fields (auto-persisted, read from cfg)
            LastSearchQuery = cfg.LastSearchQuery;
            RawXmlMode = cfg.RawXmlMode;
            XmlFontScale = cfg.XmlFontScale;
            DummyDeathCount = cfg.DummyDeathCount;
            DummyCharacterXML = cfg.DummyCharacterXML;
            DummySimulated = cfg.DummySimulated;

            // Window geometry (batch)
            int wx = cfg.WindowSizeX;
            int wy = cfg.WindowSizeY;
            WindowSize = (wx >= 0 && wy >= 0) ? new Point(wx, wy) : null;

            int px = cfg.WindowPositionX;
            int py = cfg.WindowPositionY;
            WindowPosition = (px >= 0 && py >= 0) ? new Point(px, py) : null;

            LeftPanelWidth = cfg.LeftPanelWidth > 0 ? cfg.LeftPanelWidth : null;
            RightPanelWidth = cfg.RightPanelWidth > 0 ? cfg.RightPanelWidth : null;

            // Complex fields (deserialize from store)
            FavoritedItems.Clear();
            foreach (var fav in ClientConfig.CsvToHashSet(cfg.FavoritesRaw))
                FavoritedItems.Add(fav);
            TabHistory.Clear();
            TabHistory.AddRange(ClientConfig.CsvToList(cfg.TabHistoryRaw));

            CustomLayouts.Clear();
            var loaded = ClientConfig.XmlToLayouts(cfg.CustomLayoutsRaw);
            foreach (var kvp in loaded) CustomLayouts[kvp.Key] = kvp.Value;

            // Defaults
            if (!WindowSize.HasValue) WindowSize = new Point(1250, 850);
            if (!LeftPanelWidth.HasValue) LeftPanelWidth = 250;
            if (!RightPanelWidth.HasValue) RightPanelWidth = 300;

            // Restore last selection
            string lastId = cfg.LastItemId;
            if (!string.IsNullOrEmpty(lastId))
            {
                CurrentTarget = (Prefab?)ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == lastId)
                             ?? (Prefab?)AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == lastId);
            }

            // Restore tracker
            Tracker.FromCsv(cfg.TrackedRecipesRaw);
            Tracker.Visible = cfg.TrackerVisible;
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
            if (target is ItemPrefab item) options.Add(new ContextMenuOption(Tracker.GetStringTrackToHUD(item).Value, isEnabled: true, onSelected: () => Tracker.AddOrRemoveRecipe(item)));

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
                options.Add(new ContextMenuOption(Tracker.GetStringTrackToHUD(item).Value, isEnabled: true, onSelected: () => Tracker.AddOrRemoveRecipe(item)));

            //options.Add(new ContextMenuOption("Ver más info (WIP)", isEnabled: false));

            _ = GUIContextMenu.CreateContextMenu(PlayerInput.MousePosition, TextSOS.Get("sos.context.recipe_options", "Recipe Options"), null, [.. options]);
        }

        public void OnRecipeSelected(ItemPrefab item, FabricationRecipe recipe)
        {
            Tracker.AddOrRemoveRecipe(recipe);
            OnTargetSelected(item);
        }

        public void Update()
        {
            bool canHandleInputs = GUI.KeyboardDispatcher.Subscriber == null || GUI.KeyboardDispatcher.Subscriber is GUIDropDown2;

            if (canHandleInputs)
            {
                //var kb = Keyboard.GetState();
                //bool isKeyDownNow = kb.IsKeyDown(ToggleKey);
                bool isKeyDownNow = cfg.SOSOpenKeyDown;

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
                wasKeyDown = cfg.SOSOpenKeyDown;
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