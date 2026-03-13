// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;

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
            LuaCsLogger.LogMessage(TextSOS.Get("sos.shared.loaded", "[SOS] Loaded Successfully.").Value);
#if DEBUG
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
            LuaCsLogger.LogMessage(TextSOS.Get("sos.shared.unloaded", "[SOS] Mod Unloaded.").Value);
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
}
