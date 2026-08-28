// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Runtime.InteropServices;

namespace SOS
{
    internal sealed class EventBus
    {
        private readonly Dictionary<string, EventChannel> _channels = [];
        private readonly Dictionary<string, object?> _state = [];

        private sealed class EventChannel
        {
            private const int DEPTH = 4;
            private readonly SortedDictionary<double, List<Delegate>> buckets = [];

            private static double Normalize(double order) => Math.Round(order, DEPTH, MidpointRounding.AwayFromZero);

            public SortedDictionary<double, List<Delegate>>.KeyCollection Keys => buckets.Keys;

            public void Add(Delegate handler, double order)
            {
                double key = Normalize(order);
                lock (buckets)
                {
                    if (!buckets.TryGetValue(key, out var list))
                        buckets[key] = list = [];

                    list.Add(handler);
                }
            }

            public bool Remove(Delegate handler, double? order = null)
            {
                bool removed = false;

                if (order.HasValue)
                {
                    double key = Normalize(order.Value);
                    lock (buckets)
                        if (buckets.TryGetValue(key, out var list))
                        {
                            removed = list.Remove(handler);
                            if (list.Count == 0) buckets.Remove(key);
                        }
                }
                else
                {
                    List<double> emptybuckets = [];

                    lock (buckets)
                    {
                        foreach (var (prio, list) in buckets)
                        {
                            if (list.Remove(handler))
                            {
                                removed = true;
                                if (list.Count == 0)
                                    emptybuckets.Add(prio);
                            }
                        }

                        foreach (var prio in emptybuckets)
                            buckets.Remove(prio);
                    }
                }

                return removed;
            }

            private Delegate[]? GetValueCopy(double order)
            {
                lock (buckets)
                {
                    if (buckets.TryGetValue(order, out var list))
                        return [.. list];
                }
                return null;
            }

            public bool Call()
            {
                double[]? channels;
                lock (buckets) channels = [.. Keys];
                var result = false;

                foreach (var order in channels)
                    result |= Call(order);

                return result;
            }

            public bool Call(double order)
            {
                order = Normalize(order);
                var handlers = GetValueCopy(order);
                if (handlers == null || handlers.Length == 0) return false;
                var result = false;
                foreach (var handler in handlers)
                {
                    try
                    {
                        if (handler is Action h)
                        {
                            h();
                            result = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[SOS.API] Observer error in handler '{handler.Method.Name}' at order {order}: {ex.Message}");
                    }
                }
                return result;
            }

            public bool Call<T>(T value)
            {
                double[]? channels;
                lock (buckets) channels = [.. Keys];
                var result = false;

                foreach (var order in channels)
                    result |= Call<T>(order, value);

                return result;
            }

            public bool Call<T>(double order, T value)
            {
                order = Normalize(order);
                var handlers = GetValueCopy(order);
                if (handlers == null || handlers.Length == 0) return false;
                var result = false;
                foreach (var handler in handlers)
                {
                    try
                    {
                        switch (handler)
                        {
                            case Action<T> h1: h1(value); result = true; break;
                            case Action h2: h2(); result = true; break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"[SOS.API] Observer error in handler '{handler.Method.Name}' at order {order}: {ex.Message}");
                    }
                }

                return result;
            }
        }

        public void On<T>(string key, Action<T> handler, double order = 0)
        {
            lock (_channels)
            {
                if (!_channels.TryGetValue(key, out var channel))
                    _channels[key] = channel = new();

                channel.Add(handler, order);
            }

            Logger.LogDebug($"ON '{key}' (T:{typeof(T).FullOrName()}) registered at order {order}.", level: LogLevel.Trace);
        }

        public void On(string key, Action handler, double order = 0)
        {
            lock (_channels)
            {
                if (!_channels.TryGetValue(key, out var channel))
                    _channels[key] = channel = new();

                channel.Add(handler, order);
            }

            Logger.LogDebug($"ON '{key}' registered at order {order}.", level: LogLevel.Trace);
        }

        public void Off<T>(string key, Action<T> handler, double? order = null, bool removeState = false)
        {
            lock (_channels)
            {
                if (_channels.TryGetValue(key, out var channel))
                    channel.Remove(handler, order);
            }

            if (removeState) RemoveState(key);

            Logger.LogDebug($"OFF '{key}' (T:{typeof(T).FullOrName()}) removed {(order.HasValue ? $"at order {order}" : "on all")}.", level: LogLevel.Trace);
        }

        public void Off(string key, Action handler, double? order = null, bool removeState = false)
        {
            lock (_channels)
            {
                if (_channels.TryGetValue(key, out var channel))
                    channel.Remove(handler, order);
            }
            if (removeState) RemoveState(key);

            Logger.LogDebug($"OFF '{key}' removed {(order.HasValue ? $"at order {order}" : "on all")}.", level: LogLevel.Trace);
        }

        public bool Emit<T>(string key, T value, bool setState = true)
        {
            var result = _channels.GetValueLocked(key)?.Call<T>(value) ?? false;

            if (setState) SetState<T>(key, value, false);

            Logger.LogDebug($"EMIT '{key}' called with value '{value}' (T:{typeof(T).FullOrName()})", level: LogLevel.Trace);

            return result;
        }

        public bool Emit(string key)
        {
            var result = _channels.GetValueLocked(key)?.Call() ?? false;

            Logger.LogDebug($"EMIT'{key}' called.", level: LogLevel.Trace);

            return result;
        }

        public void SetState<T>(string key, T value, bool emit = false)
        {
            lock (_state)
                _state[key] = value;

            if (emit) Emit(key, value, false);

            Logger.LogDebug($"SET_STATE CALLED '{key}' with type: {typeof(T).FullOrName()}.", level: LogLevel.Trace);
        }

        public void SetState<T>(string key, Func<T> method, bool emit = false)
        {
            lock (_state)
                _state[key] = method;

            if (emit) Emit(key, method(), false);

            Logger.LogDebug($"SET_STATE CALLED '{key}' with a delegate function: 'Func<{typeof(T).FullOrName()}>'.", level: LogLevel.Trace);
        }

        public T? GetState<T>(string key)
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
                    Logger.LogDebug($"GET_STATE CALLED '{key}' with type: {typeof(T).FullOrName()}, returned {result}.", level: LogLevel.Trace);
                    return result;
                }
                Logger.LogDebug($"GET_STATE CALLED '{key}' with type: {typeof(T).FullOrName()}, saved type is {value?.GetType().Name}, returned default.", level: LogLevel.Trace);
            }

            return default;
        }

        public bool RemoveState(string key)
        {
            lock (_state)
                return _state.Remove(key);
        }
    }
}