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
[assembly: IgnoresAccessChecksTo("DedicatedServer")]
[assembly: IgnoresAccessChecksTo("BarotraumaCore")]

namespace SOS
{
    public partial class Plugin : IAssemblyPlugin
    {

#pragma warning disable CS8618
        public IConfigService ConfigService { get; set; }
        public IPluginManagementService PluginManagementService { get; set; }
        public IConsoleCommandsService ConsoleCommandsService { get; set; }
#pragma warning restore CS8618

        public ContentPackage _package = null!;

        public void Initialize()
        {
            if (!PluginManagementService.TryGetPackageForPlugin<Plugin>(out _package))
            {
                RLogger.LogError("Failed to find package!");
                return;
            }
#if CLIENT
            InitClient();
#endif
        }

        public void OnLoadCompleted()
        {
            //TextManager.VerifyLanguageAvailable();
            RLogger.Log(TextSOS.Get("sos.shared.loaded", "[SOS] Loaded Successfully.").Value);
            RLogger.LogDebug(TextSOS.Get("sos.shared.debugmode", "[SOS] Debug Mode is enabled.").Value);
        }

        public void PreInitPatching() { }

        public void Dispose()
        {
#if CLIENT
            RecipeAnalyzer.ClearSessionCache();
            DisposeClient();
#endif
            RLogger.LogDebug(TextSOS.Get("sos.shared.unloaded", "[SOS] Mod Unloaded.").Value);
            GC.SuppressFinalize(this);
        }
    }

    public static class TextSOS
    {
        private static readonly Dictionary<string, Dictionary<Identifier, string>> prefixCache = [];
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

        public static Dictionary<Identifier, string> GetTranslationsByPrefix(string prefix)
        {
            if (prefixCache.TryGetValue(prefix, out var cached)) return cached;

            var allTranslations = TextManager.GetAllTagTextPairs();
            var filtered = allTranslations
                .Where(kvp => kvp.Key.StartsWith(prefix))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            prefixCache[prefix] = filtered;
            return filtered;
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
