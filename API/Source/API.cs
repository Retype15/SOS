// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Barotrauma;
using Barotrauma.LuaCs;
using Microsoft.Xna.Framework;

[assembly: IgnoresAccessChecksTo("Barotrauma")]
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]
[assembly: InternalsVisibleTo("SOS")]

namespace SOS
{
    public static class API
    {

        private static readonly SortedFactory<ISOSStatSection> _sectionFactories = new();
        private static readonly SortedFactory<ISOSCenterTab> _tabFactories = new();

        private static bool _scanned = false;

        private sealed class SortedFactory<T> where T : class, IIdentifierOrdenable
        {
            private readonly Dictionary<string, (double Order, Func<T?> Factory)> _dict = [];
            private (string Id, double Order, Func<T?> Factory)[] _cache = [];
            private bool _isDirty = false;

            public void Add(string id, double order, Func<T?> Factory)
            {
                _dict[id] = (order, Factory);
                _isDirty = true;
            }

            public void Add(T instance)
            {
                _dict[instance.Id] = (instance.Order, () => instance);
                _isDirty = true;
            }

            public bool Remove(string key)
            {
                var isSuccess = _dict.Remove(key);
                if (isSuccess) _isDirty = true;
                return isSuccess;
            }

            public bool Register(object obj)
            {
                bool isSuccess = obj switch
                {
                    null => false,
                    Type type => RegisterType(type),
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
                        LogWarning($"[SOS.API] Failed to instantiate dummy for type '{type.FullName}' as '{typeof(T).Name}'.");
                        return false;
                    }

                    string id = dummy.Id;
                    double order = dummy.Order;

                    Add(id, order, () => Activator.CreateInstance(type)?.Cast<T>());

                    //LogDebug($"Registered type: {type.FullName} [Order: {order}]", Color.Green);
                    return true;
                }
                catch (Exception ex)
                {
                    LogWarning($"[SOS.API] Failed to register type '{type.FullName}' as '{typeof(T).Name}'. Type does not satisfy contract. \nException: {ex.Message}");
                    return false;
                }
            }

            private bool RegisterInstance(object obj)
            {
                try
                {
                    var section = obj.Cast<T>();
                    Add(section);
                    //LogDebug($"Registered instance: {section.Id} [Order: {section.Order}]", Color.Green);
                    return true;
                }
                catch (Exception ex)
                {
                    LogWarning($"[SOS.API] Failed to register instance from type '{obj.GetType()}' as '{typeof(T).Name}'. Instance does not satisfy contract. \nException: {ex.Message}");
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
                        LogWarning($"[SOS.API] Failed to instantiate section '{typeof(T).Name}' of Id '{Id}'. \nException: {ex.Message}");
                        continue;
                    }

                    if (instance != null)
                        yield return instance;
                }
            }

            public void Clear()
            {
                _dict.Clear();
                _cache = [];
                _isDirty = false;
            }
        }

        internal static void Initialize(IPluginManagementService pluginManagementService)
        {
            if (_scanned) return;

            LogDebug("INICIANDO DESDE SOS!!!", Color.Gold);
            _sectionFactories.AutoRegister(pluginManagementService);
            _tabFactories.AutoRegister(pluginManagementService);

            _scanned = true;
        }

        #region Lateral Sections

        public static bool RegisterSection(object obj) => _sectionFactories.Register(obj);

        internal static IEnumerable<ISOSStatSection> CreateSections() => _sectionFactories.Create();

        #endregion

        #region Tabs

        public static bool RegisterTab(object obj) => _tabFactories.Register(obj);

        internal static IEnumerable<ISOSCenterTab> CreateTabs() => _tabFactories.Create();

        #endregion

        internal static void Clear()
        {
            _sectionFactories.Clear();
            _tabFactories.Clear();
            _scanned = false;
        }

        internal static void Log(string message, Color? color = null)
            => LuaCsLogger.Log(message, color ?? Color.DeepSkyBlue);

        internal static void LogWarning(string message) => Log(message, Color.Yellow);

        [Conditional("DEBUG")]
        internal static void LogDebug(string message, Color? color = null) => Log(message, color);

    }
}
