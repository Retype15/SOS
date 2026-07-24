// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    internal static class Logger
    {
        [Conditional("DEBUG")]
        public static void LogDebug(string message, Color? color = null) => LuaCsLogger.Log(message, color ?? Color.SkyBlue);

        [Conditional("DEBUG")]
        public static void LogDebugError(string message) => LuaCsLogger.LogError(message);

        [Conditional("DEBUG")]
        public static void LogDebugWarning(string message, Color? color = null) => LuaCsLogger.Log(message, color ?? Color.Yellow);

        [Conditional("RELEASE")]
        public static void LogRelease(string message, Color? color = null) => LuaCsLogger.Log(message, color ?? Color.SkyBlue);

        [Conditional("RELEASE")]
        public static void LogReleaseError(string message) => LuaCsLogger.LogError(message);

        [Conditional("RELEASE")]
        public static void LogReleaseWarning(string message, Color? color = null) => LuaCsLogger.Log(message, color ?? Color.Yellow);

        public static void Log(string message, Color? color = null) => LuaCsLogger.Log(message, color ?? Color.SkyBlue);

        public static void LogError(string message) => LuaCsLogger.LogError(message);

        public static void LogWarning(string message, Color? color = null) => LuaCsLogger.Log(message, color ?? Color.Yellow);
    }
}