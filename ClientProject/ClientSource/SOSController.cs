// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework.Input;
using SOS.Configs;
using SOS.GUI;

using BGUI = Barotrauma.GUI;

namespace SOS
{
    public sealed class SOSController : IDisposable
    {
        private static SOSController? _instance;
        public static SOSController Instance => _instance ??= new();

        internal ISOSWindowProfile? ActiveProfile = null;

        private bool _isOpened;
        public bool IsOpened => _isOpened;

        public bool HaveOldConfigFile = false;

        public bool DataInitialized { get; private set; } = false;

        internal List<ISOSConfig> CachedConfigs { get; set; } = [];

        private GUIRecipeTracker? _tracker;
        internal GUIRecipeTracker Tracker => _tracker ??= GUIRecipeTracker.InstantiateWithDefault();

        public Stack<Prefab> HistoryBack { get; } = new Stack<Prefab>();
        public Stack<Prefab> HistoryForward { get; } = new Stack<Prefab>();

        public List<string> TabHistory { get; } = [];

        private static bool migrationPending = false;
        public static bool MigrationPending { get => migrationPending; set => migrationPending = value; }

        public static bool IsSOSBlocked =>
            GameMain.Instance.LoadingScreenOpen == true ||
            migrationPending ||
            CoroutineManager.IsCoroutineRunning("LevelTransition");

        //MARK: Config delegates
        public ClientConfig cfg = ClientConfig.Instance;

        public HashSet<string> FavoritedItems => cfg.FavoritedItems;

        private Keys ToggleKey => cfg.SOSOpenKey.Key;

        public string LastSearchQuery => cfg.LastSearchQuery;

        public Prefab? CurrentTarget
        {
            get => cfg.CurrentTarget;
            internal set => cfg.CurrentTarget = value;
        }

        public bool RawXmlMode => cfg.RawXmlMode;

        public float XmlFontScale => cfg.XmlFontScale;

        private WindowProfileConfig _windowProfileConfig = WindowProfileConfig.Instance;

        private SOSController() { }

        public void PushTabHistory(string uid)
        {
            TabHistory.Remove(uid);
            TabHistory.Insert(0, uid);
        }

        private void Subscribe()
        {
            API.On(CommKeys.NavigateBack, NavigateBack);
            API.On(CommKeys.NavigateForward, NavigateForward);
            API.On(CommKeys.CloseWindow, ToggleUI);
            API.On<string>(CommKeys.ChangeProfile, ChangeProfile);
        }

        private void Unsubscribe()
        {
            API.Off(CommKeys.NavigateBack, NavigateBack);
            API.Off(CommKeys.NavigateForward, NavigateForward);
            API.Off(CommKeys.CloseWindow, ToggleUI);
            API.Off<string>(CommKeys.ChangeProfile, ChangeProfile);
        }

        public void ChangeProfile(string profileId)
        {
            ActiveProfile?.Destroy();
            ActiveProfile = API.GetWindowProfile(profileId);

            if (ActiveProfile != null)
            {
                _windowProfileConfig.ActiveProfileId = profileId;
                ActiveProfile.Open();
            }
        }

        public void OnTargetSelected(Prefab? item)
        {
            if (item == null) return;

            if (CurrentTarget != item)
            {
                if (CurrentTarget != null)
                {
                    HistoryBack.Push(CurrentTarget);
                    HistoryForward.Clear();
                }
                CurrentTarget = item;
                API.Emit(CommKeys.SelectTarget, item);
            }
        }

        public void NavigateBack()
        {
            if (HistoryBack.Count > 0)
            {
                if (CurrentTarget != null) HistoryForward.Push(CurrentTarget);
                CurrentTarget = HistoryBack.Pop();
                API.Emit(CommKeys.SelectTarget, CurrentTarget);
            }
        }

        public void NavigateForward()
        {
            if (HistoryForward.Count > 0)
            {
                if (CurrentTarget != null) HistoryBack.Push(CurrentTarget);
                CurrentTarget = HistoryForward.Pop();
                API.Emit(CommKeys.SelectTarget, CurrentTarget);
            }
        }

        public void ResolveCommand(string[] args)
        {
            if (args.Length == 0) { ToggleUI(); return; }
            if (args.Length == 1) return;
            if (args.Length == 2) switch (args[0].ToLowerInvariant())
                {
                    case "log": Logger.ActualLogLevel = (LogLevel)LogLevelStates.Strings.IndexOf(args[1].ToLowerInvariant()); return;
                }

            Logger.LogWarning($"Command 'sos {string.Join(' ', args)}' not recognized.");
            return;
        }

        public void ToggleUI()
        {
            if (_isOpened)
            {
                SaveSettings();
                Unsubscribe();
                ActiveProfile?.Close();
                CachedConfigs.Clear();
                _isOpened = false;
                return;
            }

            if (HaveOldConfigFile)
            {
                MigrationDialog.Show();
                HaveOldConfigFile = false;
                Logger.LogDebug("[SOS] Opening migration window.");
                return;
            }

            if (Screen.Selected == null || IsSOSBlocked) return;

            API.Initialize(Plugin.Instance.PluginManagementService);
            CachedConfigs = [.. API.CreateConfigs()];
            ActiveProfile ??= API.GetWindowProfile(WindowProfileConfig.Instance.ActiveProfileId);

            LoadSettings();

            Subscribe();

            ActiveProfile?.Open();
            _isOpened = true;

            if (!DataInitialized)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    RecipeAnalyzer.PrecomputeCaches();
                    DataInitialized = true;
                });
            }
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
                                if (!_isOpened) ToggleUI();
                            }
                            else
                            {
                                ToggleUI();
                            }
                        });
                    }
                }
            }

            if (_isOpened)
            {
                if (canHandleInputs)
                {
                    if (PlayerInput.KeyHit(Keys.Escape))
                    {
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

                ActiveProfile?.Update();
            }
            if (migrationPending) MigrationDialog.Update();
            if (!IsSOSBlocked && Screen.Selected == GameMain.GameScreen) Tracker.Update();
        }

        public void SaveSettings()
        {
            foreach (var config in CachedConfigs)
                config.Save();
        }

        public void LoadSettings()
        {
            foreach (var config in CachedConfigs)
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

            Unsubscribe();

            ClientConfig.Destroy();
            cfg = null!;
            ClientConfig.Destroy();
            _windowProfileConfig = null!;

            SOS.Prefabs.Item.ItemPrefabProvider.Destroy();
            ActiveProfile?.Close();
            ActiveProfile?.Destroy();
            ActiveProfile = null;
            _isOpened = false;

            GC.SuppressFinalize(this);
        }

        public void Destroy()
        {
            Dispose();
            RecipeAnalyzer.Clear();
            _instance = null;
        }

        ~SOSController() => Dispose();
    }
}