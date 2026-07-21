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

namespace SOS
{
    public static class API
    {
        private static readonly SortedList<(int Order, string Id), Func<ISOSStatSection?>> _sectionFactories = [];
        private static readonly HashSet<string> _registeredIds = [];

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

        public static bool RegisterSection(object obj) => obj switch
        {
            null => false,
            Type type => RegisterType(type),
            _ => RegisterInstance(obj)
        };

        public static IEnumerable<ISOSStatSection> CreateSections()
        {
            foreach (var (key, factory) in _sectionFactories)
            {
                ISOSStatSection? instance;
                try
                {
                    instance = factory();
                }
                catch (Exception ex)
                {
                    LogWarning($"[SOS.API] Failed to instantiate stat section of Id '{key.Id}'. Exception: {ex.Message}");
                    continue;
                }

                if (instance == null) continue;

                //LogWarning($"[SOS.API] Exception thrown during Analyze of Id '{key.Id}'. Exception: {ex.Message}");

                yield return instance;
            }
        }

        private static bool RegisterType(Type type)
        {
            if (!type.IsAbstract && !type.IsInterface &&
                type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                    .All(c => c.GetParameters().Length > 0))
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
                int order = dummy.Order;

                if (!_registeredIds.Add(id))
                {
                    LogWarning($"[SOS.API] Section ID '{id}' is already registered.");
                    return false;
                }

                _sectionFactories.Add((order, id), () => Activator.CreateInstance(type)?.Cast<ISOSStatSection>());
                LogDebug($"Registered type: {type.FullName} [Order: {order}]", Color.Green);
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"[SOS.API] Failed to register type '{type.FullName}'. Type does not satisfy contract. Exception: {ex.Message}");
                return false;
            }
        }

        private static bool RegisterInstance(object obj)
        {
            try
            {
                var section = obj.Cast<ISOSStatSection>();
                string id = section.Id;
                int order = section.Order;

                if (!_registeredIds.Add(id))
                {
                    LogWarning($"[SOS.API] Section ID '{id}' is already registered.");
                    return false;
                }

                _sectionFactories.Add((order, id), () => section);
                LogDebug($"Registered instance: {id} [Order: {order}]", Color.Green);
                return true;
            }
            catch (Exception ex)
            {
                LogWarning($"[SOS.API] Failed to register instance from type '{obj.GetType()}'. Instance does not satisfy contract. Exception: {ex.Message}");
                return false;
            }
        }

        #endregion

        public static void Clear()
        {
            _sectionFactories.Clear();
            _registeredIds.Clear();
            _scanned = false;
        }

        internal static void Log(string message, Color? color = null)
            => LuaCsLogger.Log(message, color ?? Color.DeepSkyBlue);

        internal static void LogWarning(string message) => Log(message, Color.Yellow);

        [Conditional("DEBUG")]
        internal static void LogDebug(string message, Color? color = null) => Log(message, color);

    }
}
