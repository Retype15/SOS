// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Barotrauma.LuaCs;

[assembly: IgnoresAccessChecksTo("Barotrauma")]
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]
[assembly: InternalsVisibleTo("SOS")]

namespace SOS
{
    public static class API
    {

        // Factories  
        private static readonly SortedFactory<ISOSStatSection> _sectionFactories = new();
        private static readonly SortedFactory<ISOSTab> _tabFactories = new();
        private static readonly SortedFactory<ISOSConfig> _configFactories = new();
        private static readonly SortedFactory<ISOSPrefab> _prefabFactories = new();
        private static readonly SortedFactory<ISOSWindowProfile> _profileFactories = new();

        private static bool _scanned = false;

        //MARK: SortedFactory
        private sealed class SortedFactory<T> where T : class
        {
            private readonly Dictionary<string, (double Order, Func<T?> Factory)> _dict = [];
            private readonly Dictionary<string, T> _instances = [];
            private (string Id, double Order, Func<T?> Factory)[] _cache = [];
            private bool _isDirty = false;

            private void Add(string id, double order, Func<T?> Factory)
            {
                lock (_dict)
                {
                    _dict[id] = (order, Factory);
                    _instances.Remove(id);
                    _isDirty = true;
                }
            }

            public bool Remove(string key, bool onlyInstance = false)
            {
                lock (_dict)
                {
                    var isSuccess = !onlyInstance && _dict.Remove(key);
                    _instances.Remove(key);
                    _isDirty |= isSuccess;
                    return isSuccess;
                }
            }

            public bool Register(object obj, string? id, double order)
            {
                bool isSuccess = obj switch
                {
                    null => false,
                    Type type => RegisterType(type, id, order),
                    Func<T?> func => RegisterFunc(func, id, order),
                    Func<object> func => RegisterFunc(func, id, order),
                    _ => RegisterInstance(obj, id, order)
                };

                if (isSuccess) Logger.LogDebug($"[SOS.API] Registered '{id}' of type '{obj?.GetType().FullOrName()}' as '{typeof(T).Name}'.", level: LogLevel.Trace);
                else Logger.LogError($"[SOS.API] Failed to register '{id}' of type '{obj?.GetType().FullOrName()}' as '{typeof(T).Name}'.");

                return isSuccess;
            }

            private bool RegisterType(Type type, string? id, double order)
            {
                if (type.IsAbstract || type.IsInterface || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Logger.LogError($"[SOS.API] Register failed for type: '{type.FullName}', not be an interface, abstract object, and non-void constructor.");
                    return false;
                }

                id ??= type.FullOrName();

                try
                {
                    Add(id, order, () => Activator.CreateInstance(type)?.Cast<T>());

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[SOS.API] Failed to register type '{type.FullName}' as '{typeof(T).Name}'. Type does not satisfy contract. \nException: {ex.Message}");
                    return false;
                }
            }

            private bool RegisterFunc(Func<T?> func, string? id, double order)
            {
                id ??= func.Method.ReturnType.FullOrName();

                Add(id, order, func);
                return true;
            }

            private bool RegisterFunc(Func<object> func, string? id, double order)
            {
                id ??= func.Method.ReturnType.FullOrName();

                Add(id, order, () => func().Cast<T>());
                return true;
            }

            private bool RegisterInstance(object obj, string? id, double order)
            {
                id ??= obj.GetType().FullOrName();

                try
                {
                    Add(id, order, () => obj.Cast<T>());
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[SOS.API] Failed to register instance from type '{obj.GetType()}' as '{typeof(T).Name}'. Instance does not satisfy contract. \nException: {ex.Message}");
                    return false;
                }
            }

            public bool AutoRegister<TAuto>(IPluginManagementService pluginManagementService) where TAuto : T
            {
                var result = pluginManagementService.GetImplementingTypes<TAuto>();

                bool anySuccess = false;

                if (result.IsSuccess)
                    foreach (Type t in result.Value)
                    {
                        var attr = t.GetCustomAttribute<AutoRegisterAttribute>();
                        if (attr != null)
                            anySuccess |= RegisterType(t, attr.Id ?? t.FullOrName(), attr.Order);
                    }


                return anySuccess;
            }

            public bool AutoRegister(IPluginManagementService pluginManagementService) => AutoRegister<T>(pluginManagementService);

            public (string Id, double Order, Func<T?> Factory)[] GetSorted()
            {
                if (_isDirty)
                {
                    lock (_dict)
                        _cache = [.. _dict
                        .OrderBy(kvp => kvp.Value.Order)
                        .ThenBy(kvp => kvp.Key)
                        .Select(kvp => (kvp.Key, kvp.Value.Order, kvp.Value.Factory))];

                    _isDirty = false;
                }
                return _cache;
            }

            public IEnumerable<(string Id, T Instance)> Create(bool keepInstance = true)
            {
                foreach (var (Id, _, factory) in GetSorted())
                {
                    T? instance = null;
                    lock (_dict)
                        _instances.TryGetValue(Id, out instance);

                    try
                    {
                        instance ??= factory();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"[SOS.API] Failed to instantiate section '{typeof(T).Name}' of Id '{Id}'. \nException: {ex.Message}");
                        continue;
                    }

                    if (instance != null)
                    {
                        if (keepInstance)
                            lock (_dict)
                                _instances[Id] = instance;
                        yield return (Id, instance);
                    }
                }
            }

            public T? Get(string id, bool keepInstance = true)
            {
                lock (_dict)
                {
                    if (_instances.TryGetValue(id, out var cached))
                        return cached;

                    if (_dict.TryGetValue(id, out var entry))
                    {
                        var instance = entry.Factory();
                        if (keepInstance && instance != null)
                            _instances[id] = instance;

                        return instance;
                    }
                }
                return null;
            }

            public T? First(bool keepInstance = true)
            {
                var sorted = GetSorted();
                return sorted.Length > 0 ? Get(sorted[0].Id, keepInstance) : null;
            }

            public T? GetOrFirst(string? id, bool keepInstance = true)
                => (id != null && Get(id, keepInstance) is { } instance) ? instance : First(keepInstance);

            public void Clear(bool onlyInstances = false)
            {
                lock (_dict)
                {
                    if (!onlyInstances)
                    {
                        _dict.Clear();
                        _cache = [];
                        _isDirty = false;
                        Logger.LogDebugWarning($"Limpiado factory para tipo: '{nameof(T)}'");
                    }
                    foreach (var kv in _instances)
                        if (kv.Value is IDisposable i) i.Dispose();

                    _instances.Clear();
                }
            }
        }

        #region Window Facade

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

        #region Lateral Sections

        public static bool RegisterSection(object obj, string? id = null, double order = 0.0)
            => _sectionFactories.Register(obj, id, order);

        public static T? GetSection<T>(string id, bool keepInstance = true)
            => GetSection(id, keepInstance) is T t ? t : default;

        public static ISOSStatSection? GetSection(string id, bool keepInstance = true)
            => _sectionFactories.Get(id, keepInstance);

        public static IEnumerable<ISOSStatSection> GetAllSections(bool keepInstance = true)
            => _sectionFactories.Create(keepInstance).Select(t => t.Instance);

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
            => _tabFactories.Create(keepInstance).Select(t => t.Instance);

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
            => _configFactories.Create(keepInstance).Select(t => t.Instance);

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
            => _prefabFactories.Create(keepInstance).Select(t => t.Instance);

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
                Logger.LogError($"[SOS] No one profile encountered. Try reinstall 'S.O.S - Standard Operation Schematics' Mod, report that in steam mod page or create an issue on Git project(‖color:{color.R},{color.G},{color.B}‖https://github.com/retype15/SOS‖end‖).");
            }
            return v;
        }

        public static IEnumerable<ISOSWindowProfile> GetAllWindowProfiles(bool keepInstance = false) => _profileFactories.Create(keepInstance).Select(t => t.Instance);

        public static bool RemoveWindowProfile(string id, bool onlyInstance = false)
                => _profileFactories.Remove(id, onlyInstance);
        #endregion

        #endregion

        #region Shared States.

        private static readonly Dictionary<string, Delegate> _delegates = [];
        private static readonly Dictionary<string, object?> _state = [];

        public static void On<T>(string key, Action<T> handler)
        {
            lock (_delegates)
            {
                if (_delegates.TryGetValue(key, out var existing))
                    _delegates[key] = Delegate.Combine(existing, handler);
                else
                    _delegates[key] = handler;
            }

            Logger.LogDebug($"ON CALLED '{key}' with type: {nameof(T)}", level: LogLevel.Trace);
        }

        public static void On(string key, Action handler)
        {
            lock (_delegates)
            {
                if (_delegates.TryGetValue(key, out var existing))
                    _delegates[key] = Delegate.Combine(existing, handler);
                else
                    _delegates[key] = handler;
            }

            Logger.LogDebug($"ON CALLED '{key}'.", level: LogLevel.Trace);
        }

        public static void Off<T>(string key, Action<T> handler, bool removeState = false)
        {
            lock (_delegates)
            {
                if (_delegates.TryGetValue(key, out var existing))
                {
                    var removed = Delegate.Remove(existing, handler);
                    if (removed != null)
                        _delegates[key] = removed;
                    else
                        _delegates.Remove(key);
                }
            }

            if (removeState) RemoveState(key);

            Logger.LogDebug($"OFF CALLED '{key}' with type: {nameof(T)}", level: LogLevel.Trace);
        }

        public static void Off(string key, Action handler, bool removeState = false)
        {
            lock (_delegates)
            {
                if (_delegates.TryGetValue(key, out var existing))
                {
                    var removed = Delegate.Remove(existing, handler);
                    if (removed != null)
                        _delegates[key] = removed;
                    else
                        _delegates.Remove(key);
                }
            }

            if (removeState) RemoveState(key);

            Logger.LogDebug($"OFF CALLED '{key}'.", level: LogLevel.Trace);
        }

        public static void Emit<T>(string key, T value, bool setState = true)
        {
            lock (_delegates)
            {
                _delegates.TryGetValue(key, out var d);

                if (d != null)
                    foreach (var handler in d.GetInvocationList())
                    {
                        try
                        {
                            switch (handler)
                            {
                                case Action<T> h1: h1(value); break;
                                case Action h2: h2(); break;
                            }
                        }
                        catch (Exception ex) { Logger.LogError($"[SOS] Observer error in key:'{key}'  method:'{handler.Method.Name}' Exception: {ex.Message}"); }
                    }
            }
            if (setState) SetState(key, value, false);

            Logger.LogDebug($"EMIT CALLED '{key}' with type: {nameof(T)}", level: LogLevel.Trace);
        }

        public static void Emit(string key)
        {
            lock (_delegates)
            {
                _delegates.TryGetValue(key, out var d);

                if (d != null)
                    foreach (var handler in d.GetInvocationList())
                    {
                        try { if (handler is Action handler1) handler1(); }
                        catch (Exception ex) { Logger.LogError($"[SOS] Observer error in key: '{key}'\nException: '{ex.Message}'"); }
                    }
            }
            Logger.LogDebug($"EMIT CALLED '{key}'.", level: LogLevel.Trace);
        }

        public static void SetState<T>(string key, T value, bool emit = false)
        {
            lock (_state)
                _state[key] = value;

            if (emit) Emit(key, value, false);

            Logger.LogDebug($"SET_STATE CALLED '{key}' with type: {nameof(T)}.", level: LogLevel.Trace);
        }

        public static void SetState<T>(string key, Func<T> method, bool emit = false)
        {
            lock (_state)
                _state[key] = method;

            if (emit) Emit(key, method(), false);

            Logger.LogDebug($"SET_STATE CALLED '{key}' with a delegate function: 'Func<{nameof(T)}>'.", level: LogLevel.Trace);
        }

        public static T? GetState<T>(string key)
        {
            lock (_state)
            {
                if (_state.TryGetValue(key, out var value))
                {
                    var result = value switch
                    {
                        T t => t,
                        Func<T> ft => ft(),
                        _ => throw new SafeArrayTypeMismatchException($"GetState called with diferent type signature: T:'{typeof(T)}' is not {value?.GetType()}"),
                    };
                    Logger.LogDebug($"GET_STATE CALLED '{key}' with type: {nameof(T)}, returned {result}.", level: LogLevel.Trace);
                    return result;
                }
                Logger.LogDebug($"GET_STATE CALLED '{key}' with type: {nameof(T)}, saved type is {value?.GetType().Name}, returned default.", level: LogLevel.Trace);
            }

            return default;
        }

        public static bool RemoveState(string key)
        {
            lock (_state)
                return _state.Remove(key);
        }

        #endregion

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
            lock (_delegates) _delegates.Clear();
            lock (_state) _state.Clear();
        }
    }

    public static class CommKeys
    {
        /// <summary>
        /// Cambia el Prefab objetivo.
        /// </summary>
        /// <param name="Type">Prefab?</param>
        public static string SelectTarget => "SelectTarget";
        public static string ChangeProfile => "ChangeProfile";
        public static string ApplyLayout => "ApplyLayout";
        public static string NavigateBack => "NavigateBack";
        public static string NavigateForward => "NavigateForward";
        public static string RefreshSearch => "RefreshSearch";
        public static string ToggleWindow => "ToggleWindow";
        public static string OpenWindow => "OpenWindow";
        public static string CloseWindow => "CloseWindow";
        public static string SetSearchFilter => "SetSearchFilter";
    }
}
