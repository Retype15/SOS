// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics;
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

                if (File.Exists("Data/sossettings.xml"))
                {
                    controller.HaveOldConfigFile = true;
                }

                RLogger.Log(TextSOS.Get("sos.client.init", "[SOS] Client: Initialized. Press 'J' to open.").Value);
            }
            catch (Exception ex)
            {
                RLogger.LogError($"[SOS] InitClient FAILED: {ex}");
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

    // TODO: Must to Change site...
    public class PrefabAdapter
    {
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