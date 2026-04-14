// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Events;
using FluentResults;

namespace SOS
{
    // Client-specific code
    public partial class Plugin : IAssemblyPlugin, IEventKeyUpdate
    {
        private SOSController? controller;

        public void InitClient()
        {
            controller = new SOSController();

            if (!DebugConsole.commands.Exists(c => c.Names.ToString() == "sos")) // \\//
                DebugConsole.commands.Add(new DebugConsole.Command(
                    name: "sos",
                    help: TextSOS.Get("sos.command.help", "Open/Close SOS.").Value,
                    onExecute: _ => controller?.ToggleUI(),
                    getValidArgs: null,
                    isCheat: false
                )
                {
                    RelayToServer = false,
                    OnClientExecute = _ => controller?.ToggleUI()
                });

            LuaCsSetup.Instance.EventService.Subscribe<IEventKeyUpdate>(this);

            LuaCsLogger.LogMessage(TextSOS.Get("sos.client.init", "[SOS] Client: Initialized. Press 'J' to open.").Value);
        }

        public void OnKeyUpdate(double deltaTime)
        {
            controller?.Update();

#if DEBUG
            DebugSOSWindow.Instance?.Update();
#endif
        }

        public void DisposeClient()
        {
            LuaCsSetup.Instance.EventService.Unsubscribe<IEventKeyUpdate>(this);

            DebugConsole.commands.RemoveAll(c => c.Names.Contains("sos"));

            controller?.SaveSettings();
            controller?.Destroy();
            controller = null;
        }
    }
}