// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma.LuaCs;

namespace SOS
{
    public static partial class API
    {
        private static EventBus eventBus = new();

        // Factories  
        private static readonly SortedFactory<ISOSStatSection> _sectionFactories = new();
        private static readonly SortedFactory<ISOSTab> _tabFactories = new();
        private static readonly SortedFactory<ISOSConfig> _configFactories = new();
        private static readonly SortedFactory<ISOSPrefab> _prefabFactories = new();
        private static readonly SortedFactory<ISOSWindowProfile> _profileFactories = new();

        private static bool _scanned = false;

        #region Info Sections

        public static bool RegisterSection(object obj, string? id = null, double order = 0.0)
            => _sectionFactories.Register(obj, id, order);

        public static T? GetSection<T>(string id, bool keepInstance = true)
            => GetSection(id, keepInstance) is T t ? t : default;

        public static ISOSStatSection? GetSection(string id, bool keepInstance = true)
            => _sectionFactories.Get(id, keepInstance);

        public static IEnumerable<ISOSStatSection> GetAllSections(bool keepInstance = true)
            => _sectionFactories.GetAll(keepInstance).Select(t => t.Instance);

        public static bool RemoveSection(string id, bool onlyInstance = false)
            => _sectionFactories.Remove(id, onlyInstance);

        #endregion

        #region Tabs

        public static bool RegisterTab(object obj, string? id = null, double order = 0.0)
            => _tabFactories.Register(obj, id, order);

        public static T? GetTab<T>(string id, bool keepInstance = true)
            => GetTab(id, keepInstance) is T t ? t : default;

        public static ISOSTab? GetTab(string id, bool keepInstance = true)
            => _tabFactories.Get(id, keepInstance);

        public static IEnumerable<ISOSTab> GetAllTabs(bool keepInstance = true)
            => _tabFactories.GetAll(keepInstance).Select(t => t.Instance);

        public static bool RemoveTab(string id, bool onlyInstance = false)
            => _tabFactories.Remove(id, onlyInstance);

        #endregion

        #region Configs

        public static bool RegisterConfig(object obj, string? id = null, double order = 0.0)
            => _configFactories.Register(obj, id, order);

        public static T? GetConfig<T>(string id, bool keepInstance = true)
            => GetConfig(id, keepInstance) is T t ? t : default;

        public static ISOSConfig? GetConfig(string id, bool keepInstance = true)
            => _configFactories.Get(id, keepInstance);

        public static IEnumerable<ISOSConfig> GetAllConfigs(bool keepInstance = true)
            => _configFactories.GetAll(keepInstance).Select(t => t.Instance);

        public static bool RemoveConfig(string id, bool onlyInstance = false)
            => _configFactories.Remove(id, onlyInstance);

        #endregion

        #region Prefab Providers

        public static bool RegisterPrefabProvider(object obj, string? id = null, double order = 0.0)
            => _prefabFactories.Register(obj, id, order);

        public static T? GetPrefabProvider<T>(string id, bool refresh = true)
            => GetPrefabProvider(id, refresh) is T t ? t : default;

        public static ISOSPrefab? GetPrefabProvider(string id, bool keepInstance = true)
            => _prefabFactories.Get(id, keepInstance);

        public static IEnumerable<ISOSPrefab> GetAllPrefabProviders(bool keepInstance = true)
            => _prefabFactories.GetAll(keepInstance).Select(t => t.Instance);

        public static bool RemovePrefabProvider(string id, bool onlyInstance = false)
            => _prefabFactories.Remove(id, onlyInstance);

        #endregion

        #region Window Profiles

        public static bool RegisterWindowProfile(object obj, string? id = null, double order = 0.0)
            => _profileFactories.Register(obj, id, order);


        public static T? GetWindowProfile<T>(string id, bool keepInstance = false)
            => GetWindowProfile(id, keepInstance) is T t ? t : default;

        public static ISOSWindowProfile? GetWindowProfile(string? id, bool keepInstance = false)
        {
            ISOSWindowProfile? v;
            if (string.IsNullOrEmpty(id)) v = _profileFactories.First(keepInstance);
            else
            {
                Logger.LogDebug($"GetWindowProfile >> id: '{id}'", level: LogLevel.Trace);
                v = _profileFactories.Get(id, keepInstance);
                if (v == null)
                {
                    Logger.LogWarning("[SOS] Profile not encountered. Trying to use default profile.");
                    v = _profileFactories.First();
                }
                Logger.LogDebug($"GetWindowProfile >> Name: '{v?.DisplayName ?? "null"}'", level: LogLevel.Trace);
            }
            if (v == null)
            {
                var color = Microsoft.Xna.Framework.Color.LightSkyBlue;
                Logger.LogDebugError($"[SOS] No one profile encountered.\n => Profile list: {string.Join(',', GetAllWindowProfiles().ToList().Select(p => p.DisplayName))}\n => Profile _dict => {string.Join(',', _profileFactories.GetSorted().Select(f => $"[{f.Id}, {f.Order}]"))}");
                Logger.LogReleaseError($"[SOS] No one profile encountered. Try reinstall 'S.O.S - Standard Operation Schematics' Mod, report that in steam mod page or create an issue on Git project(‖color:{color.R},{color.G},{color.B}‖https://github.com/retype15/SOS‖end‖).");
            }
            return v;
        }

        public static IEnumerable<ISOSWindowProfile> GetAllWindowProfiles(bool keepInstance = false)
            => _profileFactories.GetAll(keepInstance).Select(t => t.Instance);

        public static bool RemoveWindowProfile(string id, bool onlyInstance = false)
            => _profileFactories.Remove(id, onlyInstance);

        #endregion

        #region EventBus Facade


        public static void On(string key, Action handler, double order = 0) => eventBus.On(key, handler, order);

        public static void On<T>(string key, Action<T> handler, double order = 0) => eventBus.On<T>(key, handler, order);

        public static void Off(string key, Action handler, double? order = null, bool removeState = false) => eventBus.Off(key, handler, order, removeState);

        public static void Off<T>(string key, Action<T> handler, double? order = null, bool removeState = false) => eventBus.Off<T>(key, handler, order, removeState);

        public static bool Emit(string key, double? order = null) => eventBus.Emit(key, order);

        public static bool Emit<T>(string key, T value, double? order = null, bool setState = true) => eventBus.Emit<T>(key, value, order, setState);

        public static bool EmitRange(string key, double min = double.MinValue, double max = double.MaxValue) => eventBus.Emit(key, min, max);

        public static bool EmitRange<T>(string key, T value, double min = double.MinValue, double max = double.MaxValue, bool setState = true) => eventBus.Emit<T>(key, value, min, max, setState);

        public static void SetState<T>(string key, T value, bool emit = false) => eventBus.SetState<T>(key, value, emit);

        public static void SetState<T>(string key, Func<T> method, bool emit = false) => eventBus.SetState<T>(key, method, emit);

        public static T? GetState<T>(string key) => eventBus.GetState<T>(key);

        public static bool RemoveState(string key) => eventBus.RemoveState(key);

        #endregion

        #region Internal helpers

        internal static void Initialize(IPluginManagementService pluginManagementService)
        {
            if (_scanned) return;

            _sectionFactories.AutoRegister(pluginManagementService);
            _tabFactories.AutoRegister(pluginManagementService);
            _configFactories.AutoRegister(pluginManagementService);
            _prefabFactories.AutoRegister(pluginManagementService);
            _profileFactories.AutoRegister(pluginManagementService);

            _scanned = true;
        }

        internal static void ClearTemporaryInstances()
        {
            _sectionFactories.Clear(true);
            _tabFactories.Clear(true);
            _prefabFactories.Clear(true);
            _profileFactories.Clear(true);
        }

        internal static void Clear()
        {
            _sectionFactories.Clear();
            _tabFactories.Clear();
            _configFactories.Clear();
            _prefabFactories.Clear();
            _profileFactories.Clear();
            _scanned = false;
            eventBus = new();
        }

        #endregion
    }
}
