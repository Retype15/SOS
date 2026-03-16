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
                onExecute: _ => controller?.ToggleUI(),
                getValidArgs: null,
                isCheat: false
            )
            {
                RelayToServer = false,
                OnClientExecute = _ => controller?.ToggleUI()
            });

#if DEBUG
            DebugConsole.commands.Add(new DebugConsole.Command(
                name: "debugsos",
                help: "Abre la ventana de pruebas de UI Escalable.",
                onExecute: _ => 
                {
                    DebugSOSWindow.Instance?.Destroy();
                    InitDebugSOSWindow();
                },
                getValidArgs: null,
                isCheat: false
            )
            {
                RelayToServer = false,
                OnClientExecute = _ => InitDebugSOSWindow()
            });
#endif

            GameMain.LuaCs.Hook.Add("keyupdate", "SOS_UpdateLoop", _ =>
            {
                controller?.Update();
#if DEBUG
                DebugSOSWindow.Instance?.Update();
#endif
                return null;
            });

            LuaCsLogger.LogMessage(TextSOS.Get("sos.client.init", "[SOS] Client: Initialized. Press 'J' to open.").Value);
        }

        public static void InitDebugSOSWindow()
        {
            _ = new DebugSOSWindow();
        }

        public void DisposeClient()
        {
            controller?.SaveSettings();
            controller?.Destroy();
            controller = null;
        }
    }
}