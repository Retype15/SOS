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
        public IConfigService ConfigService { get; set; } = null!;
        public IPluginManagementService PluginManagementService { get; set; } = null!;
        public IConsoleCommandsService ConsoleCommandsService { get; set; } = null!;
#pragma warning restore CS8618

        internal ContentPackage Package = null!;

        internal static Plugin Instance = null!;

        public void Initialize()
        {
            if (!PluginManagementService.TryGetPackageForPlugin<Plugin>(out Package))
            {
                RLogger.LogError("Failed to find package!");
                return;
            }

            InitClient();
        }

        public Plugin()
        {
            Instance = this;
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
            DisposeClient();

            ConfigService = null!;
            PluginManagementService = null!;
            ConsoleCommandsService = null!;
            Package = null!;
            Instance = null!;
            RLogger.LogDebug(TextSOS.Get("sos.shared.unloaded", "[SOS] Mod Unloaded.").Value);
            GC.SuppressFinalize(this);
        }
    }

    internal static class TextSOS
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

    internal static class RLogger
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
