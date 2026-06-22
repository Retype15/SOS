// Copyright (c) 2026 Retype15
// AI GENERATED FILE. Don't AFFECT the Licence

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    public static class MigrationDialog
    {
        private static GUIFrame? overlay;
        private static GUIFrame? dialog;

        public static void Show()
        {
            if (overlay != null) return;

            SOSController.migrationPending = true;

            // ─── Full-screen semi-transparent overlay ───

            overlay = new GUIFrame(new RectTransform(Vector2.One, GUI.Canvas), style: null)
            {
                Color = Color.Black * 0.5f
            };

            // ─── Dialog frame ───

            dialog = new GUIFrame(new RectTransform(new Vector2(0.48f, 0.30f), overlay.RectTransform, Anchor.Center), style: "InnerFrame");

            // ─── Title ───

            var titleArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.30f), dialog.RectTransform), style: null);
            _ = new GUITextBlock(new RectTransform(Vector2.One, titleArea.RectTransform, Anchor.Center),
                TextSOS.Get("sos.migration.title", "SOS — Migración de Configuración"),
                font: GUIStyle.LargeFont, textAlignment: Alignment.Center);

            // ─── Description ───

            var descArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.35f), dialog.RectTransform), style: null);
            _ = new GUITextBlock(new RectTransform(Vector2.One, descArea.RectTransform, Anchor.Center),
                TextSOS.Get("sos.migration.description",
                    "Se encontró una configuración anterior de SOS.\n" +
                    "¿Deseas importar tus datos al nuevo sistema?"),
                font: GUIStyle.SmallFont, textAlignment: Alignment.Center);

            // ─── Buttons ───

            var btnArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.35f), dialog.RectTransform, Anchor.BottomCenter), style: null);
            var btnLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.8f, 0.8f), btnArea.RectTransform, Anchor.Center), isHorizontal: true)
            {
                RelativeSpacing = 0.1f,
                Stretch = true
            };

            // Import button
            var importBtn = new GUIButton(new RectTransform(new Vector2(0.4f, 1f), btnLayout.RectTransform),
                TextSOS.Get("sos.migration.import", "Importar"));
            importBtn.OnClicked += ImportAction;

            // Discard button
            var discardBtn = new GUIButton(new RectTransform(new Vector2(0.4f, 1f), btnLayout.RectTransform),
                TextSOS.Get("sos.migration.discard", "Descartar"));
            discardBtn.OnClicked += DiscardAction;
        }

        private static bool ImportAction(GUIButton button, object userdata)
        {
            var controller = SOSController.Instance;

            try
            {
                var data = SettingsManager.Load();
                if (data != null)
                {
                    // Transfer favorites
                    foreach (var fav in data.Favorites)
                        controller.FavoritedItems.Add(fav);

                    // Transfer tab history
                    controller.TabHistory.Clear();
                    controller.TabHistory.AddRange(data.TabHistory);

                    // Transfer simple fields (auto-persists via cfg delegates)
                    controller.LastSearchQuery = data.LastSearchQuery;
                    controller.RawXmlMode = data.RawXmlMode;
                    controller.XmlFontScale = data.XmlFontScale;
                    controller.DummyDeathCount = data.DummyDeathCount;
                    controller.DummyCharacterXML = data.DummyCharacterXML;
                    controller.DummySimulated = data.DummySimulated;

                    // Transfer window geometry
                    controller.WindowSize = data.WindowSize;
                    controller.WindowPosition = data.WindowPosition;
                    controller.LeftPanelWidth = data.LeftPanelWidth;
                    controller.RightPanelWidth = data.RightPanelWidth;

                    // Transfer custom layouts
                    controller.CustomLayouts.Clear();
                    foreach (var kvp in data.CustomLayouts)
                        controller.CustomLayouts[kvp.Key] = kvp.Value;

                    // Transfer tracker
                    if (!string.IsNullOrEmpty(data.TrackedItemId))
                    {
                        var targetPrefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == data.TrackedItemId);
                        if (targetPrefab != null)
                        {
                            var specificRecipe = targetPrefab.FabricationRecipes?.Values
                                .FirstOrDefault(r => r.RecipeHash == data.TrackedRecipeHash);
                            controller.Tracker.SetTrackedItem(targetPrefab, specificRecipe);
                        }
                    }

                    // Restore last selected item
                    if (!string.IsNullOrEmpty(data.LastItemId))
                    {
                        controller.CurrentTarget = (Prefab?)ItemPrefab.Prefabs
                            .FirstOrDefault(p => p.Identifier.Value == data.LastItemId)
                            ?? (Prefab?)AfflictionPrefab.List
                                .FirstOrDefault(a => a.Identifier.Value == data.LastItemId);
                    }

                    // Persist all imported data to the new config system
                    controller.SaveSettings();

                    RLogger.Log(TextSOS.Get("sos.migration.success", "[SOS] Previous configuration imported successfully.").Value);
                }
            }
            catch (Exception e)
            {
                RLogger.LogError(TextSOS.Get("sos.migration.error",
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

        private static void RenameOldFile()
        {
            const string oldPath = "Data/sossettings.xml";
            const string backupPath = "Data/sossettings_old.xml";

            try
            {
                if (File.Exists(oldPath))
                {
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);
                    File.Move(oldPath, backupPath);
                }
            }
            catch (Exception e)
            {
                RLogger.LogError($"[SOS] Failed to rename old settings file: {e.Message}");
            }
        }

        private static void Close()
        {
            SOSController.migrationPending = false;

            overlay?.Parent?.RemoveChild(overlay);
            overlay = null;
            dialog = null;
        }
    }
}
