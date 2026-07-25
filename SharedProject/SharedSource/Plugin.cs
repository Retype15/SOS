// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Runtime.CompilerServices;
using Barotrauma;
using Barotrauma.LuaCs;

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
        public ILoggerService LoggerService { get; set; } = null!;
        public IConsoleCommandsService ConsoleCommandsService { get; set; } = null!;
#pragma warning restore CS8618

        internal ContentPackage Package = null!;

        internal static Plugin Instance = null!;

        public void Initialize()
        {
            Logger.LoggerService = LoggerService;

            if (!PluginManagementService.TryGetPackageForPlugin<Plugin>(out Package))
            {
                Logger.LogError("Failed to find package!");
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
            Logger.Log(Texts.Get("sos.shared.loaded", "[SOS] Loaded Successfully.").Value);
            Logger.LogDebug(Texts.Get("sos.shared.debugmode", "[SOS] Debug Mode is enabled.").Value);
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
            Logger.LogDebug(Texts.Get("sos.shared.unloaded", "[SOS] Mod Unloaded.").Value);
            Logger.LoggerService = null;
            GC.SuppressFinalize(this);
        }
    }
}
