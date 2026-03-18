// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0079
#pragma warning disable IDE0290

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Barotrauma;

namespace SOS
{
    // MARK: - Settings Data
    public class SettingsData
    {
        public HashSet<string> Favorites { get; set; } = [];
        public string LastSearchQuery { get; set; } = "";
        public string LastItemId { get; set; } = "";
        public string TrackedItemId { get; set; } = "";
        public uint TrackedRecipeHash { get; set; } = 0;
    }

    // MARK: - Settings Manager
    public static class SettingsManager
    {
        private const int CurrentSaveVersion = 1;
        private const string ConfigPath = "SOS_Settings.xml";

        public static void Save(SettingsData data)
        {
            try
            {
                var doc = new XDocument(
                    new XElement("SOSSettings",
                        new XAttribute("version", CurrentSaveVersion),

                        new XElement("Favorites",
                            data.Favorites.Select(f => new XElement("Item", new XAttribute("id", f)))
                        ),

                        new XElement("State",
                            new XAttribute("lastItem", data.LastItemId ?? ""),
                            new XAttribute("lastSearch", data.LastSearchQuery ?? "")
                        ),

                        new XElement("Tracker",
                            new XAttribute("targetId", data.TrackedItemId ?? ""),
                            new XAttribute("recipeHash", data.TrackedRecipeHash.ToString())
                        )
                    )
                );

                doc.Save(ConfigPath);
#if DEBUG
                LuaCsLogger.LogMessage(TextSOS.Get("sos.config.saved", "[SOS] Settings saved (v[version]).").Replace("[version]", CurrentSaveVersion.ToString()).Value);
#endif
            }
            catch (Exception e)
            {
                LuaCsLogger.LogError(TextSOS.Get("sos.config.save_error", "[SOS] Failed to save settings: [error]").Replace("[error]", e.Message).Value);
            }
        }

        public static SettingsData Load()
        {
            var data = new SettingsData();
            if (!File.Exists(ConfigPath)) return data;

            try
            {
                XDocument doc = XDocument.Load(ConfigPath);
                XElement? root = doc.Element("SOSSettings");
                if (root == null) return data;

                int fileVersion = int.Parse(root.Attribute("version")?.Value ?? "0");

                if (fileVersion >= 1)
                {
                    var favs = root.Element("Favorites")?.Elements("Item");
                    if (favs != null)
                    {
                        foreach (var f in favs) data.Favorites.Add(f.Attribute("id")?.Value ?? "");
                    }

                    var state = root.Element("State");
                    if (state != null)
                    {
                        data.LastSearchQuery = state.Attribute("lastSearch")?.Value ?? "";
                        data.LastItemId = state.Attribute("lastItem")?.Value ?? "";
                    }

                    var tracker = root.Element("Tracker");
                    if (tracker != null)
                    {
                        data.TrackedItemId = tracker.Attribute("targetId")?.Value ?? "";
                        _ = uint.TryParse(tracker.Attribute("recipeHash")?.Value, out uint hash);
                        data.TrackedRecipeHash = hash;
                    }
                }
#if DEBUG
                LuaCsLogger.LogMessage(TextSOS.Get("sos.config.loaded", "[SOS] Settings v[version] loaded successfully.").Replace("[version]", fileVersion.ToString()).Value);
#endif
            }
            catch (Exception e)
            {
                LuaCsLogger.LogError(TextSOS.Get("sos.config.load_error", "[SOS] Error reading settings file: [error]").Replace("[error]", e.Message).Value);
            }

            return data;
        }
    }

    // MARK: - Settings Menu
    public class SettingsMenu
    {
        private GUIFrame? settingsFrame;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Quitar el parámetro no utilizado", Justification = "<pendiente>")]
        public static void OpenMenu(GUIComponent parent)
        {
            // TODO
        }

        public void CloseMenu()
        {
            settingsFrame?.Parent.RemoveChild(settingsFrame);
            settingsFrame = null;
        }
    }
}