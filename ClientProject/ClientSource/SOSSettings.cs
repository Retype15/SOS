// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0079
#pragma warning disable IDE0290

using System.Xml.Linq;
using Barotrauma;
using Microsoft.Xna.Framework;

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
        public bool RawXmlMode { get; set; } = false;

        // UI Persistence
        public Point? WindowSize { get; set; }
        public Point? WindowPosition { get; set; }
        public int? LeftPanelWidth { get; set; }
        public int? RightPanelWidth { get; set; }
        public Dictionary<string, SavedLayout> CustomLayouts { get; set; } = [];
    }

    public class SavedLayout
    {
        public Point WindowSize { get; set; }
        public int LeftPanelWidth { get; set; }
        public int RightPanelWidth { get; set; }
    }

    // MARK: - Settings Manager
    public static class SettingsManager
    {
        private const int CurrentSaveVersion = 1;
        private const string NewConfigPath = "Data/sossettings.xml";
        private const string OldConfigPath = "SOS_Settings.xml";

        public static void Save(SettingsData data)
        {
            try
            {
                string? directory = Path.GetDirectoryName(NewConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var doc = new XDocument(
                    new XElement("SOSSettings",
                        new XAttribute("version", CurrentSaveVersion),

                        new XElement("Favorites",
                            data.Favorites.Select(f => new XElement("Item", new XAttribute("id", f)))
                        ),

                        new XElement("State",
                            new XAttribute("lastItem", data.LastItemId ?? ""),
                            new XAttribute("lastSearch", data.LastSearchQuery ?? ""),
                            new XAttribute("rawXml", data.RawXmlMode)
                        ),

                        new XElement("Tracker",
                            new XAttribute("targetId", data.TrackedItemId ?? ""),
                            new XAttribute("recipeHash", data.TrackedRecipeHash.ToString())
                        ),

                        new XElement("Layout",
                            new XAttribute("winX", data.WindowPosition?.X ?? -1),
                            new XAttribute("winY", data.WindowPosition?.Y ?? -1),
                            new XAttribute("winW", data.WindowSize?.X ?? 0),
                            new XAttribute("winH", data.WindowSize?.Y ?? 0),
                            new XAttribute("leftW", data.LeftPanelWidth ?? 0),
                            new XAttribute("rightW", data.RightPanelWidth ?? 0)
                        ),

                        new XElement("Layouts",
                            data.CustomLayouts.Select(kvp => new XElement("Preset",
                                new XAttribute("name", kvp.Key),
                                new XAttribute("winW", kvp.Value.WindowSize.X),
                                new XAttribute("winH", kvp.Value.WindowSize.Y),
                                new XAttribute("leftW", kvp.Value.LeftPanelWidth),
                                new XAttribute("rightW", kvp.Value.RightPanelWidth)
                            ))
                        )
                    )
                );

                doc.Save(NewConfigPath);

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

            string activePath = NewConfigPath;
            if (!File.Exists(NewConfigPath))
            {
                if (File.Exists(OldConfigPath))
                {
                    activePath = OldConfigPath;
                    LuaCsLogger.LogMessage(TextSOS.Get("sos.config.migrating", "[SOS] Migrating settings from old path...").Value);
                }
                else
                {
                    return data;
                }
            }

            try
            {
                XDocument doc = XDocument.Load(activePath);
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
                        data.RawXmlMode = ImGoodBoolParser(state.Attribute("rawXml")?.Value, false);
                    }

                    var tracker = root.Element("Tracker");
                    if (tracker != null)
                    {
                        data.TrackedItemId = tracker.Attribute("targetId")?.Value ?? "";
                        _ = uint.TryParse(tracker.Attribute("recipeHash")?.Value, out uint hash);
                        data.TrackedRecipeHash = hash;
                    }

                    var layout = root.Element("Layout");
                    if (layout != null)
                    {
                        int winX = ImGoodParser(layout.Attribute("winX")?.Value, -1);
                        int winY = ImGoodParser(layout.Attribute("winY")?.Value, -1);
                        if (winX >= 0 && winY >= 0) data.WindowPosition = new Point(winX, winY);

                        int winW = ImGoodParser(layout.Attribute("winW")?.Value, 0);
                        int winH = ImGoodParser(layout.Attribute("winH")?.Value, 0);
                        if (winW > 0 && winH > 0) data.WindowSize = new Point(winW, winH);

                        int leftW = ImGoodParser(layout.Attribute("leftW")?.Value, 0);
                        if (leftW > 0) data.LeftPanelWidth = leftW;

                        int rightW = ImGoodParser(layout.Attribute("rightW")?.Value, 0);
                        if (rightW > 0) data.RightPanelWidth = rightW;
                    }

                    var layouts = root.Element("Layouts")?.Elements("Preset");
                    if (layouts != null)
                    {
                        foreach (var l in layouts)
                        {
                            string name = l.Attribute("name")?.Value ?? "Unnamed";
                            data.CustomLayouts[name] = new SavedLayout
                            {
                                WindowSize = new Point(ImGoodParser(l.Attribute("winW")?.Value, 0), ImGoodParser(l.Attribute("winH")?.Value, 0)),
                                LeftPanelWidth = ImGoodParser(l.Attribute("leftW")?.Value, 0),
                                RightPanelWidth = ImGoodParser(l.Attribute("rightW")?.Value, 0)
                            };
                        }
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

        private static int ImGoodParser(string? value, int fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return int.TryParse(value, out int result) ? result : fallback;
        }

        private static bool ImGoodBoolParser(string? value, bool fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return bool.TryParse(value, out bool result) ? result : fallback;
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