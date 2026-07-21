// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Reflection;
using MoonSharp.Interpreter;

namespace SOS
{
    public class DuckProxy<T> : DispatchProxy where T : class
    {
        private readonly Dictionary<MethodInfo, Func<object?[], object?>> _handlerMap = [];

        private DuckProxy() { }

        public static T Create(object target)
        {
            var instance = Create<T, DuckProxy<T>>();

            ((DuckProxy<T>)(object)instance).Initialize(target);

            return instance;
        }

        private void Initialize(object target)
        {
            switch (target)
            {
                case null:
                    throw new ArgumentNullException(nameof(target));

                case Table luaTable:
                    ConfigureLuaTable(luaTable);
                    break;

                default:
                    ConfigureClrObject(target);
                    break;
            }
        }

        private static IEnumerable<MethodInfo> GetAllInterfaceMethods(Type type)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                yield return method;
            foreach (var parent in type.GetInterfaces())
                foreach (var method in GetAllInterfaceMethods(parent))
                    yield return method;
        }

        private void ConfigureClrObject(object target)
        {
            var targetType = target.GetType();
            var interfaceType = typeof(T);
            var errors = new List<string>();

            foreach (var interfaceMethod in GetAllInterfaceMethods(interfaceType))
            {
                var parameterTypes = interfaceMethod.GetParameters()
                    .Select(p => p.ParameterType)
                    .ToArray();

                var targetMethod = targetType.GetMethod(interfaceMethod.Name, parameterTypes);

                if (targetMethod == null)
                {
                    errors.Add($"Missing method or property: '{interfaceMethod.Name}'");
                    continue;
                }

                if (targetMethod.ReturnType != interfaceMethod.ReturnType)
                {
                    errors.Add($"Return type mismatch on '{interfaceMethod.Name}'. Expected: {interfaceMethod.ReturnType.Name}, Found: {targetMethod.ReturnType.Name}");
                    continue;
                }

                _handlerMap[interfaceMethod] = args =>
                {
                    try
                    {
                        return targetMethod.Invoke(target, args);
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.InnerException ?? ex;
                    }
                };
            }

            if (errors.Count > 0)
            {
                throw new InvalidCastException(
                    $"Type '{targetType.FullName}' does not satisfy contract of '{interfaceType.FullName}':\n" +
                    string.Join("\n", errors));
            }
        }

        private void ConfigureLuaTable(Table table)
        {
            var interfaceType = typeof(T);
            var errors = new List<string>();

            foreach (var interfaceMethod in GetAllInterfaceMethods(interfaceType))
            {
                var methodName = interfaceMethod.Name;
                var returnType = interfaceMethod.ReturnType;

                if (methodName.StartsWith("get_"))
                {
                    var propName = methodName[4..];


                    if (table.Get(propName).IsNil())
                    {
                        errors.Add($"Missing property '{propName}' in Lua table.");
                        continue;
                    }

                    _handlerMap[interfaceMethod] = _ => table.Get(propName).ToObject(returnType);
                }
                else if (methodName.StartsWith("set_"))
                {
                    var propName = methodName[4..];

                    _handlerMap[interfaceMethod] = args =>
                    {
                        table.Set(propName, DynValue.FromObject(table.OwnerScript, args.FirstOrDefault()));
                        return null;
                    };
                }
                else
                {
                    var luaFunc = table.Get(methodName);

                    if (luaFunc.Type != DataType.Function && luaFunc.Type != DataType.ClrFunction)
                    {
                        errors.Add($"Missing function '{methodName}' in Lua table.");
                        continue;
                    }

                    _handlerMap[interfaceMethod] = args =>
                    {
                        var result = table.OwnerScript.Call(luaFunc, args);
                        return returnType == typeof(void) ? null : result.ToObject(returnType);
                    };
                }
            }

            if (errors.Count > 0)
            {
                throw new InvalidCastException(
                    $"Lua table does not satisfy contract of '{interfaceType.FullName}':\n" +
                    string.Join("\n", errors));
            }
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod != null && _handlerMap.TryGetValue(targetMethod, out var handler))
                return handler(args ?? []);

            throw new NotImplementedException($"Operation '{targetMethod?.Name}' is not supported.");
        }
    }

    public static class DuckExtensions
    {
        public static T Cast<T>(this object target) where T : class
        {
            ArgumentNullException.ThrowIfNull(target);

            if (target is T native)
                return native;

            T proxy = DuckProxy<T>.Create(target);
            return proxy;
        }

        public static bool TryCast<T>(this object target, out T a) where T : class
        {
            try
            {
                a = target.Cast<T>();
                return true;
            }
            catch
            {
                a = null!;
                return false;
            }
        }
    }
}