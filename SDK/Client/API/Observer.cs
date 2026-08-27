// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Runtime.InteropServices;

namespace SOS
{
    public static partial class API
    {
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

            Logger.LogDebug($"SET_STATE CALLED '{key}' with type: {typeof(T).FullOrName()}.", level: LogLevel.Trace);
        }

        public static void SetState<T>(string key, Func<T> method, bool emit = false)
        {
            lock (_state)
                _state[key] = method;

            if (emit) Emit(key, method(), false);

            Logger.LogDebug($"SET_STATE CALLED '{key}' with a delegate function: 'Func<{typeof(T).FullOrName()}>'.", level: LogLevel.Trace);
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
                    Logger.LogDebug($"GET_STATE CALLED '{key}' with type: {typeof(T).FullOrName()}, returned {result}.", level: LogLevel.Trace);
                    return result;
                }
                Logger.LogDebug($"GET_STATE CALLED '{key}' with type: {typeof(T).FullOrName()}, saved type is {value?.GetType().Name}, returned default.", level: LogLevel.Trace);
            }

            return default;
        }

        public static bool RemoveState(string key)
        {
            lock (_state)
                return _state.Remove(key);
        }
    }
}