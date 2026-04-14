// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0079
#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.LuaCs;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Runtime.CompilerServices;
[assembly: IgnoresAccessChecksTo("Barotrauma")]
[assembly: IgnoresAccessChecksTo("DedicatedServer")]
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]

namespace SOS
{
    public partial class Plugin : IAssemblyPlugin
    {
        public void Initialize()
        {
#if CLIENT
            InitClient();
#endif
        }

        public void OnLoadCompleted()
        {
            TextManager.VerifyLanguageAvailable();
#if DEBUG
            LuaCsLogger.LogMessage(TextSOS.Get("sos.shared.loaded", "[SOS] Loaded Successfully.").Value);
            LuaCsLogger.LogMessage(TextSOS.Get("sos.shared.debugmode", "[SOS] Debug Mode is enabled.").Value);
#endif
        }

        public void PreInitPatching() { }

        public void Dispose()
        {
#if CLIENT
            RecipeAnalyzer.ClearSessionCache();
            DisposeClient();
#endif
#if DEBUG
            LuaCsLogger.LogMessage(TextSOS.Get("sos.shared.unloaded", "[SOS] Mod Unloaded.").Value);
#endif
            GC.SuppressFinalize(this);
        }
    }

    public static class TextSOS
    {
        public static LocalizedString Get(string key, string fallback = "")
        {
            var text = TextManager.Get(key);

            if (!string.IsNullOrEmpty(fallback))
            {
#if DEBUG
                return text.Fallback("[NT]" + fallback); // NT=NOT-TRANSLATED
#else
                return text.Fallback(fallback);
#endif
            }
            return text;
        }
    }

    public static class RLogger
    {
        [Conditional("DEBUG")]
        public static void LogDebug(string message, Color? color = null) => LuaCsLogger.LogMessage(message, color);

        [Conditional("DEBUG")]
        public static void LogDebugError(string message) => LuaCsLogger.LogError(message);

        [Conditional("RELEASE")]
        public static void LogRelease(string message, Color? color = null) => LuaCsLogger.LogMessage(message, color);

        [Conditional("RELEASE")]
        public static void LogReleaseError(string message) => LuaCsLogger.LogError(message);

        public static void Log(string message, Color? color = null) => LuaCsLogger.LogMessage(message, color);

        public static void LogError(string message) => LuaCsLogger.LogError(message);

        public static void LogWarning(string message, Color? color = null) => LuaCsLogger.LogMessage(message, color ?? Color.Yellow);
    }
}
