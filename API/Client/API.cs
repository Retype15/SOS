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

        private static readonly SortedFactory<ISOSStatSection> _sectionFactories = new();
        private static readonly SortedFactory<ISOSTab> _tabFactories = new();
        private static readonly SortedFactory<ISOSConfig> _configFactories = new();
        private static readonly SortedFactory<ISOSPrefab> _prefabFactories = new();
        private static readonly SortedFactory<ISOSWindowProfile> _profileFactories = new();

        private static bool _scanned = false;

        //MARK: SortedFactory
        private sealed class SortedFactory<T> where T : class, IIdentifierOrdenable
        {
            private readonly Dictionary<string, (double Order, Func<T?> Factory)> _dict = [];
            private (string Id, double Order, Func<T?> Factory)[] _cache = [];
            private bool _isDirty = false;

            private void Add(T instance) => Add(instance.Id, instance.Order, () => instance);

            private void Add(string id, double order, Func<T?> Factory)
            {
                lock (_dict)
                {
                    _dict[id] = (order, Factory);
                    _isDirty = true;
                }
            }

            private bool Remove(string key)
            {
                lock (_dict)
                {
                    var isSuccess = _dict.Remove(key);
                    if (isSuccess) _isDirty = true;
                    return isSuccess;
                }
            }

            public bool Register(object obj)
            {
                bool isSuccess = obj switch
                {
                    null => false,
                    Type type => RegisterType(type),
                    Func<T?> func => RegisterFunc(func),
                    Func<object> func => RegisterFunc(func),
                    _ => RegisterInstance(obj)
                };
                return isSuccess;
            }

            private bool RegisterType(Type type)
            {
                if (type.IsAbstract || type.IsInterface || type.GetConstructor(Type.EmptyTypes) == null)
                    return false;

                try
                {
                    var dummy = Activator.CreateInstance(type)?.Cast<T>();

                    if (dummy == null)
                    {
                        Logger.LogWarning($"[SOS.API] Failed to instantiate dummy for type '{type.FullName}' as '{typeof(T).Name}'.");
                        return false;
                    }

                    string id = dummy.Id;
                    double order = dummy.Order;

                    Add(id, order, () => Activator.CreateInstance(type)?.Cast<T>());

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[SOS.API] Failed to register type '{type.FullName}' as '{typeof(T).Name}'. Type does not satisfy contract. \nException: {ex.Message}");
                    return false;
                }
            }

            private bool RegisterFunc(Func<T?> func)
            {
                var dummy = func();
                if (dummy == null)
                {
                    Logger.LogWarning($"[SOS.API] Failed to register method. Returned Type on register has been Null.");
                    return false;
                }
                Add(dummy.Id, dummy.Order, func);
                return true;
            }

            private bool RegisterFunc(Func<object> func)
            {
                try
                {
                    var dummy = func().Cast<T>();
                    Add(dummy.Id, dummy.Order, () => func().Cast<T>());
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[SOS.API] Failed to register method. Returned Type does not satisfy contract. \nException: {ex.Message}");
                    return false;
                }
            }

            private bool RegisterInstance(object obj)
            {
                try
                {
                    var section = obj.Cast<T>();
                    Add(section);
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
                        if (typeof(IAutoRegister).IsAssignableFrom(t) || t.GetCustomAttribute<AutoRegisterAttribute>() != null)
                            anySuccess |= RegisterType(t);

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

            public IEnumerable<T> Create()
            {
                foreach (var (Id, _, factory) in GetSorted())
                {
                    T? instance;
                    try
                    {
                        instance = factory();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"[SOS.API] Failed to instantiate section '{typeof(T).Name}' of Id '{Id}'. \nException: {ex.Message}");
                        continue;
                    }

                    if (instance != null)
                        yield return instance;
                }
            }

            public T? Get(string id)
            {
                lock (_dict)
                    if (_dict.TryGetValue(id, out var entry))
                        return entry.Factory();
                return null;
            }

            public T? First()
            {
                var sorted = GetSorted();
                return sorted.Length > 0 ? sorted[0].Factory() : null;
            }

            public T? GetOrFirst(string? id) => (id != null) ? Get(id) ?? First() : First();
            public T? GetOrFirst() => First();

            public void Clear()
            {
                lock (_dict)
                    _dict.Clear();
                _cache = [];
                _isDirty = false;
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

        public static bool RegisterSection(object obj) => _sectionFactories.Register(obj);

        internal static IEnumerable<ISOSStatSection> CreateSections() => _sectionFactories.Create();

        #endregion

        #region Tabs

        public static bool RegisterTab(object obj) => _tabFactories.Register(obj);

        internal static IEnumerable<ISOSTab> CreateTabs() => _tabFactories.Create();

        #endregion

        #region Configs

        public static bool RegisterConfig(object obj) => _configFactories.Register(obj);

        internal static IEnumerable<ISOSConfig> CreateConfigs() => _configFactories.Create();

        #endregion

        #region Prefab Providers

        public static bool RegisterPrefabProvider(object obj) => _prefabFactories.Register(obj);

        internal static IEnumerable<ISOSPrefab> CreatePrefabProviders() => _prefabFactories.Create();

        #endregion

        #region Window Profiles

        public static bool RegisterWindowProfile(object obj) => _profileFactories.Register(obj);

        internal static IEnumerable<ISOSWindowProfile> CreateWindowProfiles() => _profileFactories.Create();

        internal static ISOSWindowProfile? GetWindowProfile(string? id)
        {
            ISOSWindowProfile? v;
            if (string.IsNullOrEmpty(id)) v = _profileFactories.First();
            else
            {
                v = _profileFactories.Get(id);
                if (v == null)
                {
                    Logger.LogWarning("[SOS] Profile not encountered. Trying to use default profile.");
                    v = _profileFactories.First();
                }
            }
            if (v == null)
            {
                var color = Microsoft.Xna.Framework.Color.LightSkyBlue;
                Logger.LogError($"[SOS] No one profile encountered. Try reinstall 'S.O.S - Standard Operation Schematics' Mod, report that in steam mod page or create an issue on Git project(‖color:{color.R},{color.G},{color.B}‖https://github.com/retype15/SOS‖end‖).");
            }
            return v;
        }
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
            Delegate? d;
            lock (_delegates)
                _delegates.TryGetValue(key, out d);

            if (setState) SetState(key, value, false);

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

            Logger.LogDebug($"EMIT CALLED '{key}' with type: {nameof(T)}", level: LogLevel.Trace);
        }

        public static void Emit(string key)
        {
            Delegate? d;
            lock (_delegates)
                _delegates.TryGetValue(key, out d);

            if (d != null)
                foreach (var handler in d.GetInvocationList())
                {
                    try { if (handler is Action handler1) handler1(); }
                    catch (Exception ex) { Logger.LogError($"[SOS] Observer error in key: '{key}'\nException: '{ex.Message}'"); }
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
