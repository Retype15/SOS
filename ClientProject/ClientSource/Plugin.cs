// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics;
using Barotrauma;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Events;

namespace SOS
{
    // Client-specific code
    public partial class Plugin : IAssemblyPlugin, IEventKeyUpdate
    {
        private SOSController? controller;

        [Conditional("CLIENT")]
        public void InitClient()
        {
            try
            {
                controller = SOSController.Instance;
                controller.LoadSettings();

                if (!DebugConsole.commands.Exists(c => c.Names.Any(n => n.Value == "sos")))
                    DebugConsole.commands.Add(new DebugConsole.Command(
                        name: "sos",
                        help: Texts.Get("sos.command.help", "Open/Close SOS.").Value,
                        onExecute: _ => controller?.ToggleUI(),
                        getValidArgs: null,
                        isCheat: false
                    )
                    {
                        RelayToServer = false,
                        OnClientExecute = _ => controller?.ToggleUI()
                    });

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

            DebugConsole.commands.RemoveAll(c => c.Names.Contains("sos"));

            controller?.SaveSettings();
            controller?.Destroy();
            RecipeAnalyzer.Clear();
            API.Clear();
            controller = null;
        }
    }
}