// Copyright (c) 2026 Retype15
// AI GENERATED FILE. Don't AFFECT the Licence

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Xml.Linq;
using Barotrauma;
using Microsoft.Xna.Framework;
using BGUI = Barotrauma.GUI;

namespace SOS
{
    internal static class MigrationDialog
    {
        private const string LegacyConfigPath = "Data/sossettings.xml";

        private static GUIFrame? overlay;
        private static GUIFrame? dialog;

        public static void Update() => overlay?.AddToGUIUpdateList(order: 1);

        public static void Show()
        {
            if (overlay != null) return;

            SOSController.MigrationPending = true;

            // ─── Full-screen semi-transparent overlay ───

            overlay = new GUIFrame(new RectTransform(Vector2.One, BGUI.Canvas), style: null)
            {
                Color = Color.Black * 0.5f
            };

            // ─── Dialog frame ───

            dialog = new GUIFrame(new RectTransform(new Vector2(0.48f, 0.30f), overlay.RectTransform, Anchor.Center), style: "InnerFrame");

            // ─── Title ───

            var titleArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.30f), dialog.RectTransform), style: null);
            _ = new GUITextBlock(new RectTransform(Vector2.One, titleArea.RectTransform, Anchor.Center),
                Texts.Get("sos.migration.title", "SOS — Migración de Configuración"),
                font: GUIStyle.LargeFont, textAlignment: Alignment.Center);

            // ─── Description ───

            var descArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.5f), dialog.RectTransform), style: null);
            _ = new GUITextBlock(new RectTransform(Vector2.One, descArea.RectTransform, Anchor.Center),
                Texts.Get("sos.migration.description",
                    "Se encontró una configuración anterior de SOS.\n¿Deseas importar tus datos al nuevo sistema?"),
                font: GUIStyle.SmallFont, textAlignment: Alignment.Center);

            // ─── Buttons ───

            var btnArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.35f), dialog.RectTransform, Anchor.BottomCenter), style: null);
            var btnLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.8f, 0.8f), btnArea.RectTransform, Anchor.Center), isHorizontal: true)
            {
                RelativeSpacing = 0.1f,
                Stretch = true
            };

            // Import button
            var importBtn = new GUIButton(new RectTransform(new Vector2(0.3f, 1f), btnLayout.RectTransform),
                Texts.Get("sos.migration.import", "Import"))
            {
                ToolTip = Texts.Get("sos.migration.import_tooltip",
                    "Imports your old settings and renames \"Data/sossettings.xml\" to \"Data/sossettings_old.xml\".")
            };
            importBtn.OnClicked += ImportAction;

            // Discard button
            var discardBtn = new GUIButton(new RectTransform(new Vector2(0.3f, 1f), btnLayout.RectTransform),
                Texts.Get("sos.migration.discard", "Discard"))
            {
                ToolTip = Texts.Get("sos.migration.discard_tooltip",
                    "Discards the old settings and renames \"Data/sossettings.xml\" to \"Data/sossettings_old.xml\".")
            };
            discardBtn.OnClicked += DiscardAction;

            // Ignore button
            var ignoreBtn = new GUIButton(new RectTransform(new Vector2(0.3f, 1f), btnLayout.RectTransform),
                Texts.Get("sos.migration.ignore", "Ignore"))
            {
                ToolTip = Texts.Get("sos.migration.ignore_tooltip",
                    "Closes without changes. The old file remains untouched; you will be prompted again on next launch.")
            };
            ignoreBtn.OnClicked += IgnoreAction;

            Logger.LogDebug("Showing MigrationDialog", level: LogLevel.Trace);
        }

        private static bool ImportAction(GUIButton button, object userdata)
        {
            var controller = SOSController.Instance;

            try
            {
                if (!File.Exists(LegacyConfigPath))
                {
                    RenameOldFile();
                    Close();
                    return true;
                }

                XDocument doc = XDocument.Load(LegacyConfigPath);
                XElement? root = doc.Element("SOSSettings");
                if (root == null)
                {
                    RenameOldFile();
                    Close();
                    return true;
                }

                int fileVersion = int.Parse(root.Attribute("version")?.Value ?? "0");
                if (fileVersion < 1) { RenameOldFile(); Close(); return true; }

                // Favorites
                var favs = root.Element("Favorites")?.Elements("Item");
                if (favs != null)
                    foreach (var f in favs)
                        controller.FavoritedItems.Add(f.Attribute("id")?.Value ?? "");

                // State
                string lastItemId = "";
                var state = root.Element("State");
                if (state != null)
                {
                    lastItemId = state.Attribute("lastItem")?.Value ?? "";
                    controller.cfg.LastSearchQuery = state.Attribute("lastSearch")?.Value ?? "";
                    string historyStr = state.Attribute("tabHistory")?.Value ?? "";
                    if (!string.IsNullOrEmpty(historyStr))
                    {
                        controller.TabHistory.Clear();
                        controller.TabHistory.AddRange(historyStr.Split(',', StringSplitOptions.RemoveEmptyEntries));
                    }
                    controller.cfg.RawXmlMode = ParseBool(state.Attribute("rawXml")?.Value);
                    controller.cfg.XmlFontScale = ParseFloat(state.Attribute("xmlScale")?.Value, 0.9f);
                }

                // MedicalSim
                var simulator = root.Element("MedicalSim");
                if (simulator != null)
                {
                    controller.cfg.DummyDeathCount = ParseInt(simulator.Attribute("deathCount")?.Value);
                    controller.cfg.DummySimulated = ParseBool(simulator.Attribute("simulated")?.Value);
                    var dummyNode = simulator.Elements().FirstOrDefault();
                    if (dummyNode != null)
                        controller.cfg.DummyCharacterXML = dummyNode;
                }

                // Tracker
                var tracker = root.Element("Tracker");
                if (tracker != null)
                {
                    string targetId = tracker.Attribute("targetId")?.Value ?? "";
                    _ = uint.TryParse(tracker.Attribute("recipeHash")?.Value, out uint hash);
                    controller.Tracker.AddRecipe(targetId, hash);
                }

                //TODO: Layout [DEPRECATED]
                /*var layout = root.Element("Layout");
                if (layout != null)
                {
                    int winX = ParseInt(layout.Attribute("winX")?.Value, -1);
                    int winY = ParseInt(layout.Attribute("winY")?.Value, -1);
                    if (winX >= 0 && winY >= 0)
                        controller.WindowPosition = new Point(winX, winY);

                    int winW = ParseInt(layout.Attribute("winW")?.Value);
                    int winH = ParseInt(layout.Attribute("winH")?.Value);
                    if (winW > 0 && winH > 0)
                        controller.WindowSize = new Point(winW, winH);

                    int leftW = ParseInt(layout.Attribute("leftW")?.Value);
                    if (leftW > 0) controller.LeftPanelWidth = leftW;

                    int rightW = ParseInt(layout.Attribute("rightW")?.Value);
                    if (rightW > 0) controller.RightPanelWidth = rightW;
                }

                // Layouts
                var layouts = root.Element("Layouts")?.Elements("Preset");
                if (layouts != null)
                {
                    controller.CustomLayouts.Clear();
                    foreach (var l in layouts)
                    {
                        string name = l.Attribute("name")?.Value ?? "Unnamed";
                        controller.CustomLayouts[name] = new SavedLayout
                        {
                            WindowSize = new Point(
                                ParseInt(l.Attribute("winW")?.Value),
                                ParseInt(l.Attribute("winH")?.Value)),
                            LeftPanelWidth = ParseInt(l.Attribute("leftW")?.Value),
                            RightPanelWidth = ParseInt(l.Attribute("rightW")?.Value)
                        };
                    }
                }*/

                // Restore last selected item
                if (!string.IsNullOrEmpty(lastItemId))
                {
                    controller.CurrentTarget = (Prefab?)ItemPrefab.Prefabs
                        .FirstOrDefault(p => p.Identifier.Value == lastItemId)
                        ?? (Prefab?)AfflictionPrefab.List
                            .FirstOrDefault(a => a.Identifier.Value == lastItemId);
                }

                // save
                controller.SaveSettings();

                Logger.Log(Texts.Get("sos.migration.success", "[SOS] Previous configuration imported successfully.").Value);
            }
            catch (Exception e)
            {
                Logger.LogError(Texts.Get("sos.migration.error",
                    "[SOS] Error importing previous configuration: [error]")
                    .Replace("[error]", e.Message).Value);
            }

            RenameOldFile();
            Close();

            return true;
        }

        private static bool DiscardAction(GUIButton button, object userdata)
        {
            RenameOldFile();
            Close();
            return true;
        }

        private static bool IgnoreAction(GUIButton button, object userdata)
        {
            Close();
            return true;
        }

        private static void RenameOldFile()
        {
            const string backupPath = "Data/sossettings_old.xml";

            try
            {
                if (File.Exists(LegacyConfigPath))
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Move(LegacyConfigPath, backupPath);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"[SOS] Failed to rename old settings file: {e.Message}");
            }
        }

        public static void Close()
        {
            SOSController.MigrationPending = false;

            overlay?.RemoveFromGUIUpdateList();
            overlay?.Parent?.RemoveChild(overlay);
            overlay = null;
            dialog = null;
            SOSController.Instance.ToggleUI();
        }

        private static int ParseInt(string? value, int fallback = 0)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return int.TryParse(value, out int result) ? result : fallback;
        }

        private static bool ParseBool(string? value, bool fallback = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return bool.TryParse(value, out bool result) ? result : fallback;
        }

        private static float ParseFloat(string? value, float fallback = 0f)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return float.TryParse(value, out float result) ? result : fallback;
        }
    }
}
