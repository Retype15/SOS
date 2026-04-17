// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Events;
using Microsoft.Xna.Framework;

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

    // TODO: Must to Change site...
    public class PrefabAdapter
    {
        public static LocalizedString Name(Prefab prefab)
        {
            return prefab switch
            {
                ItemPrefab item => item.Name,
                AfflictionPrefab affliction => affliction.Name,
                _ => TextSOS.Get("sos.gen.unknown", "???")
            };
        }
        public static Sprite? Icon(Prefab prefab)
        {
            return prefab switch
            {
                ItemPrefab item => item.InventoryIcon ?? item.Sprite,
                AfflictionPrefab affliction => affliction.Icon,
                _ => null
            };
        }
        public static Color IconColor(Prefab prefab)
        {
            return prefab switch
            {
                ItemPrefab item => item.InventoryIconColor,
                AfflictionPrefab affliction => affliction.IconColors?.First() ?? Color.White,
                _ => Color.White
            };
        }
        public static PriceInfo? DefaultPrice(Prefab prefab)
        {
            return prefab switch
            {
                ItemPrefab item => item.DefaultPrice,
                _ => null
            };
        }

        public static ContentXElement? ConfigElement(Prefab prefab)
        {
            return prefab switch
            {
                ItemPrefab item => item.ConfigElement,
                AfflictionPrefab affliction => affliction.configElement,
                _ => null
            };
        }
    }
}