// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Barotrauma;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace SOS
{
    public sealed class ClientConfig : ISOSConfig, IDisposable
    {
        public string Id => "SOS.Core";
        public double Order => 0;

        private static ClientConfig? _instance;
        public static ClientConfig Instance => _instance ??= new ClientConfig();

        private readonly HashSet<ISettingBase> _dirtySettings = [];

        private void MarkDirty(ISettingBase setting)
            => _dirtySettings.Add(setting);

        public bool HasChanges => _dirtySettings.Count > 0;

        private bool _loaded = false;
        public void Load()
        {
            if (_loaded) return;

            var ctr = SOSController.Instance;

            // Simple fields
            ctr.LastSearchQuery = LastSearchQuery;
            ctr.RawXmlMode = RawXmlMode;
            ctr.XmlFontScale = XmlFontScale;
            ctr.DummyDeathCount = DummyDeathCount;
            ctr.DummyCharacterXML = DummyCharacterXML;
            ctr.DummySimulated = DummySimulated;

            // Window geometry
            int wx = WindowSizeX;
            int wy = WindowSizeY;
            ctr.WindowSize = (wx >= 0 && wy >= 0) ? new Point(wx, wy) : null;

            int px = WindowPositionX;
            int py = WindowPositionY;
            ctr.WindowPosition = (px >= 0 && py >= 0) ? new Point(px, py) : null;

            ctr.LeftPanelWidth = LeftPanelWidth > 0 ? LeftPanelWidth : null;
            ctr.RightPanelWidth = RightPanelWidth > 0 ? RightPanelWidth : null;

            // Complex fields
            ctr.FavoritedItems.Clear();
            foreach (var fav in ClientConfig.CsvToHashSet(FavoritesRaw))
                ctr.FavoritedItems.Add(fav);
            ctr.TabHistory.Clear();
            ctr.TabHistory.AddRange(ClientConfig.CsvToList(TabHistoryRaw));

            ctr.CustomLayouts.Clear();
            //var loaded = ClientConfig.XmlToLayouts(CustomLayoutsRaw);
            ctr.CustomLayouts.AddRange(ClientConfig.XmlToLayouts(CustomLayoutsRaw));
            //foreach (var kvp in loaded) ctr.CustomLayouts[kvp.Key] = kvp.Value;

            // Defaults
            if (!ctr.WindowSize.HasValue) ctr.WindowSize = new Point(1250, 850);
            if (!ctr.LeftPanelWidth.HasValue) ctr.LeftPanelWidth = 250;
            if (!ctr.RightPanelWidth.HasValue) ctr.RightPanelWidth = 300;

            // Restore last selection
            string lastId = LastItemId;
            if (!string.IsNullOrEmpty(lastId))
            {
                ctr.CurrentTarget = (Prefab?)ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == lastId)
                             ?? (Prefab?)AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == lastId)
                             ?? (Prefab?)ItemPrefab.Prefabs.FirstOrDefault();
            }

            // Restore tracker
            ctr.Tracker.FromCsv(TrackedRecipesRaw);
            ctr.Tracker.Visible = TrackerVisible;

            _loaded = true;

            Logger.LogDebug($"[SOS] CONFIG LOADED...");
        }

        public void Save()
        {
            var ctr = SOSController.Instance;
            if (ClinicalSimulatorManager.Patient != null)
            {
                DummyDeathCount = ClinicalSimulatorManager.DeathCount;
                DummyCharacterXML = ClinicalSimulatorManager.ExportSaveData()?.ToString();
                DummySimulated = !ClinicalSimulatorManager.HasStarted;
            }

            if (ctr.CurrentTarget != null) LastItemId = ctr.CurrentTarget.Identifier.Value;
            if (ctr.WindowSize != null)
            {
                WindowSizeX = ctr.WindowSize.Value.X;
                WindowSizeY = ctr.WindowSize.Value.Y;
            }

            if (ctr.WindowPosition != null)
            {
                WindowPositionX = ctr.WindowPosition.Value.X;
                WindowPositionY = ctr.WindowPosition.Value.Y;
            }

            if (ctr.LeftPanelWidth != null) LeftPanelWidth = ctr.LeftPanelWidth.Value;
            if (ctr.RightPanelWidth != null) RightPanelWidth = ctr.RightPanelWidth.Value;

            FavoritesRaw = ctr.FavoritedItems.ToCsv();
            TabHistoryRaw = ctr.TabHistory.ToCsv();
            CustomLayoutsRaw = LayoutsToXml(ctr.CustomLayouts);
            TrackedRecipesRaw = ctr.Tracker.ToCsv();
            TrackerVisible = ctr.Tracker.Visible;

            if (!HasChanges) return;

            foreach (ISettingBase setting in _dirtySettings)
                Plugin.Instance.ConfigService.SaveConfigValue(setting);

            _dirtySettings.Clear();

            Logger.LogDebug($"[SOS] CONFIG SAVED..!");
        }

        public void Reset() { }

        private readonly ISettingControl _sosOpenKey;
        public KeyOrMouse SOSOpenKey { get => _sosOpenKey.Value; set => _sosOpenKey.SetIfNotEqual(value); }
        public bool SOSOpenKeyHit => _sosOpenKey.IsHit();
        public bool SOSOpenKeyDown => _sosOpenKey.IsDown();

        private readonly ISettingBase<string> _lastSearchQuery;
        public string LastSearchQuery
        {
            get => _lastSearchQuery.Value;
            set => _lastSearchQuery.SetIfNotEqual(value);
        }

        private readonly ISettingBase<string> _lastItemId;
        public string LastItemId
        {
            get => _lastItemId.Value;
            set => _lastItemId.SetIfNotEqual(value);
        }

        private readonly ISettingBase<bool> _rawXmlMode;
        public bool RawXmlMode
        {
            get => _rawXmlMode.Value;
            set => _rawXmlMode.SetIfNotEqual(value);
        }

        private readonly ISettingBase<float> _xmlFontScale;
        public float XmlFontScale
        {
            get => _xmlFontScale.Value;
            set => _xmlFontScale.SetIfNotEqual(value);
        }

        private readonly ISettingBase<string> _trackedRecipesRaw;
        public string TrackedRecipesRaw
        {
            get => _trackedRecipesRaw.Value;
            set => _trackedRecipesRaw.SetIfNotEqual(value);
        }

        private readonly ISettingBase<bool> _trackerVisible;
        public bool TrackerVisible
        {
            get => _trackerVisible.Value;
            set => _trackerVisible.SetIfNotEqual(value);
        }
        public event Action<ISettingBase> OnTrackerVisibleValueChanged
        {
            add => _trackerVisible.OnValueChanged += value;
            remove => _trackerVisible.OnValueChanged -= value;
        }

        private readonly ISettingBase<int> _dummyDeathCount;
        public int DummyDeathCount
        {
            get => _dummyDeathCount.Value;
            set => _dummyDeathCount.SetIfNotEqual(value);
        }

        private readonly ISettingBase<bool> _dummySimulated;
        public bool DummySimulated
        {
            get => _dummySimulated.Value;
            set => _dummySimulated.SetIfNotEqual(value);
        }

        private readonly ISettingBase<string> _dummyCharacterXML;
        public string? DummyCharacterXML
        {
            get => string.IsNullOrEmpty(_dummyCharacterXML.Value) ? null : _dummyCharacterXML.Value;
            set => _dummyCharacterXML.SetIfNotEqual(value ?? "");
        }

        // ─── Batch-save (window geometry) ───

        private readonly ISettingBase<int> _windowSizeX;
        private readonly ISettingBase<int> _windowSizeY;
        private readonly ISettingBase<int> _windowPositionX;
        private readonly ISettingBase<int> _windowPositionY;
        private readonly ISettingBase<int> _leftPanelWidth;
        private readonly ISettingBase<int> _rightPanelWidth;

        internal int WindowSizeX { get => _windowSizeX.Value; set => _windowSizeX.SetIfNotEqual(value); }
        internal int WindowSizeY { get => _windowSizeY.Value; set => _windowSizeY.SetIfNotEqual(value); }
        internal int WindowPositionX { get => _windowPositionX.Value; set => _windowPositionX.SetIfNotEqual(value); }
        internal int WindowPositionY { get => _windowPositionY.Value; set => _windowPositionY.SetIfNotEqual(value); }
        internal int LeftPanelWidth { get => _leftPanelWidth.Value; set => _leftPanelWidth.SetIfNotEqual(value); }
        internal int RightPanelWidth { get => _rightPanelWidth.Value; set => _rightPanelWidth.SetIfNotEqual(value); }

        // ─── Batch-save (complex serialized) ───

        private readonly ISettingBase<string> _favoritesRaw;
        private readonly ISettingBase<string> _tabHistoryRaw;
        private readonly ISettingBase<string> _customLayoutsRaw;

        internal string FavoritesRaw { get => _favoritesRaw.Value; set => _favoritesRaw.SetIfNotEqual(value); }
        internal string TabHistoryRaw { get => _tabHistoryRaw.Value; set => _tabHistoryRaw.SetIfNotEqual(value); }
        internal string CustomLayoutsRaw { get => _customLayoutsRaw.Value; set => _customLayoutsRaw.SetIfNotEqual(value); }

        // ─── CSV Serialization Helpers ───

        internal static HashSet<string> CsvToHashSet(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return [];
            return [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        internal static List<string> CsvToList(string csv)
        {
            if (string.IsNullOrEmpty(csv)) return [];
            return [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        // ─── XML Serialization Helpers ───

        internal static string LayoutsToXml(Dictionary<string, SavedLayout> layouts)
        {
            if (layouts.Count == 0) return "";

            var doc = new XDocument(
                new XElement("Layouts",
                    layouts.Select(kvp =>
                        new XElement("Preset",
                            new XAttribute("name", kvp.Key),
                            new XAttribute("winW", kvp.Value.WindowSize.X),
                            new XAttribute("winH", kvp.Value.WindowSize.Y),
                            new XAttribute("leftW", kvp.Value.LeftPanelWidth),
                            new XAttribute("rightW", kvp.Value.RightPanelWidth)
                        )
                    )
                )
            );

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        internal static Dictionary<string, SavedLayout> XmlToLayouts(string xml)
        {
            var result = new Dictionary<string, SavedLayout>();
            if (string.IsNullOrEmpty(xml)) return result;

            try
            {
                var doc = XDocument.Parse(xml);
                XElement? root = doc.Root;
                if (root == null || root.Name != "Layouts") return result;

                foreach (var preset in root.Elements("Preset"))
                {
                    string name = preset.Attribute("name")?.Value ?? "Unnamed";
                    result[name] = new SavedLayout
                    {
                        WindowSize = new Point(
                            int.TryParse(preset.Attribute("winW")?.Value, out int winW) ? winW : 0,
                            int.TryParse(preset.Attribute("winH")?.Value, out int winH) ? winH : 0
                        ),
                        LeftPanelWidth = int.TryParse(preset.Attribute("leftW")?.Value, out int leftW) ? leftW : 0,
                        RightPanelWidth = int.TryParse(preset.Attribute("rightW")?.Value, out int rightW) ? rightW : 0
                    };
                }
            }
            catch { }

            return result;
        }

        // ─── Constructor ───

        private ClientConfig()
        {
            TryInitConfig("SOSOpenKey", out _sosOpenKey);
            TryInitConfig("LastSearchQuery", out _lastSearchQuery);
            TryInitConfig("LastItemId", out _lastItemId);
            TryInitConfig("RawXmlMode", out _rawXmlMode);
            TryInitConfig("XmlFontScale", out _xmlFontScale);
            TryInitConfig("Favorites", out _favoritesRaw);
            TryInitConfig("TabHistory", out _tabHistoryRaw);
            TryInitConfig("CustomLayouts", out _customLayoutsRaw);
            TryInitConfig("TrackedRecipes", out _trackedRecipesRaw);
            TryInitConfig("TrackerVisible", out _trackerVisible);
            TryInitConfig("WindowSizeX", out _windowSizeX);
            TryInitConfig("WindowSizeY", out _windowSizeY);
            TryInitConfig("WindowPositionX", out _windowPositionX);
            TryInitConfig("WindowPositionY", out _windowPositionY);
            TryInitConfig("LeftPanelWidth", out _leftPanelWidth);
            TryInitConfig("RightPanelWidth", out _rightPanelWidth);
            TryInitConfig("DummyDeathCount", out _dummyDeathCount);
            TryInitConfig("DummySimulated", out _dummySimulated);
            TryInitConfig("DummyCharacterXML", out _dummyCharacterXML);
        }

        private bool TryInitConfig<T>(string name, [NotNullWhen(true)] out T setting)
                where T : ISettingBase
        {
            if (!Plugin.Instance.ConfigService.TryGetConfig(Plugin.Instance.Package, name, out setting))
            {
                Logger.LogError($"Failed to find config named {name}!");
                return false;
            }
            setting.OnValueChanged += MarkDirty;
#if DEBUG
            //setting.OnValueChanged += setting => Logger.LogDebug($"Changed: {setting.InternalName} To: {setting.GetStringValue()}");
#endif
            return true;
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Destroy()
        {
            Dispose();
            _instance = null;
        }

        ~ClientConfig() => Dispose();
    }

    // ─── Layout DTO ───

    internal class SavedLayout
    {
        public Point WindowSize { get; set; }
        public int LeftPanelWidth { get; set; }
        public int RightPanelWidth { get; set; }
    }
}
