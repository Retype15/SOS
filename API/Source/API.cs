// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Barotrauma;
using Barotrauma.LuaCs;
using Microsoft.Xna.Framework;

[assembly: IgnoresAccessChecksTo("Barotrauma")]
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]

namespace SOS
{
    public static class API
    {

        private static readonly SortedFactory<ISOSStatSection> _sectionFactories = new();

        private sealed class SortedFactory<T> where T : IIdentifierOrdenable
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

            public void Clear()
            {
                _dict.Clear();
                _cache = [];
                _isDirty = false;
            }

        }

        private static bool _scanned = false;

        public static void Initialize(IPluginManagementService pluginManagementService)
        {
            if (_scanned) return;

            //LogDebug("INICIANDO DESDE SOS!!!", Color.Gold);

            var result = pluginManagementService.GetImplementingTypes<ISOSStatSection>();

            if (result.IsSuccess)
            {
                foreach (Type t in result.Value)
                    RegisterType(t);
            }
            _scanned = true;
        }

        #region Lateral Sections

        public static bool RegisterSection(object obj)
        {
            bool isSuccess = obj switch
            {
                null => false,
                Type type => RegisterType(type),
                _ => RegisterInstance(obj)
            };
            return isSuccess;
        }
        public static IEnumerable<ISOSStatSection> CreateSections()
        {
            foreach (var (key, _, factory) in _sectionFactories.GetSorted())
            {
                ISOSStatSection? instance;
                try
                {
                    instance = factory();
                }
                catch (Exception ex)
                {
                    LogWarning($"[SOS.API] Failed to instantiate stat section of Id '{key}'. \nException: {ex.Message}");
                    continue;
                }

                if (instance == null) continue;

                //LogWarning($"[SOS.API] Exception thrown during Analyze of Id '{key.Id}'. Exception: {ex.Message}");

                yield return instance;
            }
        }

        private static bool RegisterType(Type type)
        {
            if (type.IsAbstract || type.IsInterface || type.GetConstructor(Type.EmptyTypes) == null)
                return false;

            try
            {
                var dummy = Activator.CreateInstance(type)?.Cast<ISOSStatSection>();

                if (dummy == null)
                {
                    LogWarning($"[SOS.API] Failed to instantiate dummy for type '{type.FullName}'.");
                    return false;
                }

                string id = dummy.Id;
                double order = dummy.Order;

                _sectionFactories.Add(id, order, () => Activator.CreateInstance(type)?.Cast<ISOSStatSection>());

                LogDebug($"Registered type: {type.FullName} [Order: {order}]", Color.Green);
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"[SOS.API] Failed to register type '{type.FullName}'. Type does not satisfy contract. \nException: {ex.Message}");
                return false;
            }
        }

        private static bool RegisterInstance(object obj)
        {
            try
            {
                var section = obj.Cast<ISOSStatSection>();
                string id = section.Id;
                double order = section.Order;

                _sectionFactories.Add(section);
                LogDebug($"Registered instance: {id} [Order: {order}]", Color.Green);
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"[SOS.API] Failed to register instance from type '{obj.GetType()}'. Instance does not satisfy contract. \nException: {ex.Message}");
                return false;
            }
        }

        #endregion

        public static void Clear()
        {
            _sectionFactories.Clear();
            _scanned = false;
        }

        internal static void Log(string message, Color? color = null)
            => LuaCsLogger.Log(message, color ?? Color.DeepSkyBlue);

        internal static void LogWarning(string message) => Log(message, Color.Yellow);

        [Conditional("DEBUG")]
        internal static void LogDebug(string message, Color? color = null) => Log(message, color);

    }
}
