// Copyright (c) 2026 Reynier
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    // Client-specific code
    public partial class Plugin : IAssemblyPlugin
    {
        private SOSController? controller;

        public void InitClient()
        {
            controller = new SOSController();

            DebugConsole.commands.Add(new DebugConsole.Command(
                name: "sos",
                help: TextSOS.Get("sos.command.help", "Open/Close SOS.").Value,
                onExecute: _ => CrossThread.RequestExecutionOnMainThread(() => controller?.ToggleUI()),
                getValidArgs: null,
                isCheat: false
            )
            {
                RelayToServer = false,
                OnClientExecute = _ => CrossThread.RequestExecutionOnMainThread(() => controller?.ToggleUI())
            });

            GameMain.LuaCs.Hook.Add("keyupdate", "SOS_UpdateLoop", _ =>
            {
                controller?.Update();
                return null;
            });

            LuaCsLogger.LogMessage(TextSOS.Get("sos.client.init", "[SOS] Client: Initialized. Press 'J' to open.").Value);
        }

        public void DisposeClient()
        {
            controller?.SaveSettings();
            controller?.Destroy();
            controller = null;
        }
    }
}