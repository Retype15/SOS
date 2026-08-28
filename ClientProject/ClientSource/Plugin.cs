// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics;
using Barotrauma;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Events;
using SOS.Configs;

namespace SOS
{
    // Client-specific code
    public partial class Plugin : IAssemblyPlugin, IEventKeyUpdate
    {
        public SOSController? controller;

        [Conditional("CLIENT")]
        public void InitClient()
        {
            try
            {
                controller = SOSController.Instance;
                API.RegisterConfig(() => CoreConfig.Instance, CoreConfig.ID, 0);
                API.RegisterConfig(() => WindowProfileConfig.Instance, WindowProfileConfig.ID, 0);

                Configs.ConfigHelper.LoadConfigs();

                ConsoleCommandsService.RegisterCommand(
                        name: "sos",
                        help: CommandHelp(),
                        onExecute: args => controller?.ResolveCommand(args),
                        getValidArgs: () => [["log", .. LogLevelStates.Strings], ["help"]]
                );

                LuaCsSetup.Instance.EventService.Subscribe<IEventKeyUpdate>(this);

                if (File.Exists("Data/sossettings.xml"))
                {
                    controller.HaveOldConfigFile = true;
                }

                Logger.Log(Texts.Get("sos.client.init", "[SOS] Client: Initialized. Press 'J' to open.").Value);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[SOS] InitClient FAILED: {ex}");
            }
        }

        public void OnKeyUpdate(double deltaTime)
        {
            controller?.Update();
        }

        [Conditional("CLIENT")]
        public void DisposeClient()
        {
            LuaCsSetup.Instance.EventService.Unsubscribe<IEventKeyUpdate>(this);

            ConsoleCommandsService.RemoveCommand("sos");

            CoreConfig.Instance.Destroy();
            WindowProfileConfig.Destroy();
            SOSController.Instance.Destroy();
            controller = null!;
            API.Clear();
        }

        internal static string CommandHelp()
        {
            return
                Texts.Get("sos.command.help", "Open/Close SOS.\nSub-command available:").Value +
                string.Join("\n", Texts.GetTranslationsByPrefix("sos.command.help")).Replace("{logCommands}", string.Join(", ", LogLevelStates.Strings));
        }
    }
}