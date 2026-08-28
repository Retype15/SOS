// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Reflection;
using Barotrauma.LuaCs;

namespace SOS
{
    //MARK: SortedFactory
    internal sealed class SortedFactory<T> where T : class
    {
        private readonly Dictionary<string, (double Order, bool IsActive, Func<T?> Factory)> _dict = [];
        private readonly Dictionary<string, T> _instances = [];
        private (string Id, double Order, Func<T?> Factory)[] _cache = [];
        private bool _isDirty = false;

        private void Add(string id, double order, bool active, Func<T?> Factory)
        {
            lock (_dict)
            {
                _dict[id] = (order, active, Factory);
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

        public bool Register(object obj, string? id = null, double order = 0, bool active = true)
        {
            bool isSuccess = obj switch
            {
                null => false,
                Type type => RegisterType(id, order, active, type),
                Func<T?> func => RegisterFunc(id, order, active, func),
                Func<object> func => RegisterFunc(id, order, active, func),
                _ => RegisterInstance(id, order, active, obj)
            };

            if (isSuccess) Logger.LogDebug($"[SOS.API] Registered '{id}' of type '{obj?.GetType().FullOrName()}' as '{typeof(T).Name}'.", level: LogLevel.Trace);
            else Logger.LogError($"[SOS.API] Failed to register '{id}' of type '{obj?.GetType().FullOrName()}' as '{typeof(T).Name}'.");

            return isSuccess;
        }

        private bool RegisterType(string? id, double order, bool active, Type type)
        {
            if (type.IsAbstract || type.IsInterface || type.GetConstructor(Type.EmptyTypes) == null)
            {
                Logger.LogError($"[SOS.API] Register failed for type: '{type.FullName}', not be an interface, abstract object, and non-void constructor.");
                return false;
            }

            id ??= type.FullOrName();

            try
            {
                Add(id, order, active, () => Activator.CreateInstance(type)?.Cast<T>());

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[SOS.API] Failed to register type '{type.FullName}' as '{typeof(T).Name}'. Type does not satisfy contract. \nException: {ex.Message}");
                return false;
            }
        }

        private bool RegisterFunc(string? id, double order, bool active, Func<T?> func)
        {
            id ??= func.Method.ReturnType.FullOrName();

            Add(id, order, active, func);
            return true;
        }

        private bool RegisterFunc(string? id, double order, bool active, Func<object> func)
        {
            id ??= func.Method.ReturnType.FullOrName();

            Add(id, order, active, () => func().Cast<T>());
            return true;
        }

        private bool RegisterInstance(string? id, double order, bool active, object obj)
        {
            id ??= obj.GetType().FullOrName();

            try
            {
                Add(id, order, active, () => obj.Cast<T>());
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[SOS.API] Failed to register instance from type '{obj.GetType()}' as '{typeof(T).Name}'. Instance does not satisfy contract. \nException: {ex.Message}");
                return false;
            }
        }

        public bool AutoRegister(IPluginManagementService pluginManagementService) => AutoRegister<T>(pluginManagementService);

        public bool AutoRegister<TAuto>(IPluginManagementService pluginManagementService) where TAuto : T
        {
            var result = pluginManagementService.GetImplementingTypes<TAuto>();

            bool anySuccess = false;

            if (result.IsSuccess)
                foreach (Type t in result.Value)
                {
                    var attr = t.GetCustomAttribute<AutoRegisterAttribute>();
                    if (attr != null)
                        anySuccess |= RegisterType(attr.Id ?? t.FullOrName(), attr.Order, attr.Active, t);
                }


            return anySuccess;
        }

        public bool SetActive(string id, bool active)
        {
            lock (_dict)
            {
                if (!_dict.TryGetValue(id, out var entry)) return false;
                if (entry.IsActive == active) return true;

                _dict[id] = (entry.Order, active, entry.Factory);
                _isDirty = true;

                if (!active && _instances.Remove(id, out var instance))
                {
                    if (instance is IDisposable d) d.Dispose();
                }

                return true;
            }
        }

        public bool IsActive(string id)
        {
            lock (_dict)
                return _dict.TryGetValue(id, out var entry) && entry.IsActive;
        }

        public (string Id, double Order, Func<T?> Factory)[] GetSorted()
        {
            if (_isDirty)
            {
                lock (_dict)
                {
                    _cache = [.. _dict
                        .Where(kvp => kvp.Value.IsActive)
                        .OrderBy(kvp => kvp.Value.Order)
                        .ThenBy(kvp => kvp.Key)
                        .Select(kvp => (kvp.Key, kvp.Value.Order, kvp.Value.Factory))];

                    _isDirty = false;
                }
            }
            return _cache;
        }

        public IEnumerable<(string Id, T Instance)> GetAll(bool keepInstance = true)
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
            Func<T?>? factory;
            lock (_dict)
            {
                if (_instances.TryGetValue(id, out var cached))
                    return cached;

                if (!_dict.TryGetValue(id, out var entry))
                    return null;

                factory = entry.Factory;
            }

            T? instance;

            try
            {
                instance = factory();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[SOS.API] Failed to instantiate factory of Id '{id}'. \nException: {ex.Message}");
                return null;
            }

            lock (_dict)
                if (keepInstance && instance != null)
                    _instances[id] = instance;

            return instance;
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
                    Logger.LogDebugWarning($"Cleaning factory to type: '{typeof(T).FullOrName()}'");
                }
                foreach (var kv in _instances)
                    if (kv.Value is IDisposable i) i.Dispose();

                _instances.Clear();
            }
        }
    }
}