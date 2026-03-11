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
            LuaCsLogger.LogMessage(TextSOS.Get("sos.shared.loaded", "[SOS] Loaded Successfully.").Value);
        }

        public void PreInitPatching() { }

        public void Dispose()
        {
#if CLIENT
            DisposeClient();
#endif
            LuaCsLogger.LogMessage(TextSOS.Get("sos.shared.unloaded", "[SOS] Mod Unloaded.").Value);
            GC.SuppressFinalize(this);
        }
    }

    public static class TextSOS
    {
        public static LocalizedString Get(string key, string fallback)
        {
            return TextManager.Get(key).Fallback(fallback);
        }
    }
}
