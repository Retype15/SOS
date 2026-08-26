// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics;
using Barotrauma;
using Barotrauma.LuaCs;
using Microsoft.Xna.Framework;

namespace SOS
{
    public enum LogLevel
    {
        None,
        Critical,
        Error,
        Warning,
        Information,
        Debug,
        Trace,
    }
    internal static class LogLevelStates
    {
        internal static string[] Strings = ["none", "critical", "error", "warning", "info", "debug", "trace"];
    }

    public static class Logger
    {
        internal static ILoggerService? LoggerService = null;
#if DEBUG
        internal static LogLevel ActualLogLevel = LogLevel.Debug;
#else
        internal static LogLevel ActualLogLevel = LogLevel.Information;
#endif
        public static bool ComplainsLogLevel(LogLevel level) => level <= ActualLogLevel;

        [Conditional("DEBUG")]
        public static void LogDebug(string? message, Color? color = null, ILoggerService? logger = null, LogLevel level = LogLevel.Debug) => Log(message, color, logger, level);

        [Conditional("DEBUG")]
        public static void LogDebugError(string? message, ILoggerService? logger = null, LogLevel level = LogLevel.Debug | LogLevel.Error) => LogError(message, logger, level);

        [Conditional("DEBUG")]
        public static void LogDebugWarning(string? message, ILoggerService? logger = null, LogLevel level = LogLevel.Debug | LogLevel.Warning) => LogWarning(message, logger, level);

        [Conditional("RELEASE")]
        public static void LogRelease(string? message, Color? color = null, ILoggerService? logger = null, LogLevel level = LogLevel.Information) => Log(message, color, logger, level);

        [Conditional("RELEASE")]
        public static void LogReleaseError(string? message, ILoggerService? logger = null, LogLevel level = LogLevel.Error) => LogError(message, logger, level);

        [Conditional("RELEASE")]
        public static void LogReleaseWarning(string? message, ILoggerService? logger = null, LogLevel level = LogLevel.Warning) => LogWarning(message, logger, level);

        public static void Log(string? message, Color? color = null, ILoggerService? logger = null, LogLevel level = LogLevel.Information)
        {
            if (level > ActualLogLevel || string.IsNullOrEmpty(message)) return;
            color ??= Color.SkyBlue;
            if (logger != null) logger.Log(message, color);
            else if (LoggerService != null) LoggerService.Log(message, color);
            else LuaCsLogger.Log(message, color);
        }

        public static void LogError(string? message, ILoggerService? logger = null, LogLevel level = LogLevel.Error)
        {
            if (level > ActualLogLevel || string.IsNullOrEmpty(message)) return;
            if (logger != null) logger.LogError(message);
            else if (LoggerService != null) LoggerService.LogError(message);
            else LuaCsLogger.LogError(message);
        }

        public static void LogWarning(string? message, ILoggerService? logger = null, LogLevel level = LogLevel.Warning)
        {
            if (level > ActualLogLevel || string.IsNullOrEmpty(message)) return;
            if (logger != null) logger.LogWarning(message);
            else if (LoggerService != null) LoggerService.LogWarning(message);
            else LuaCsLogger.Log(message, Color.Yellow);
        }
    }
}