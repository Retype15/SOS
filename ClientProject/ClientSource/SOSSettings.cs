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

namespace SOS
{
    public sealed class ClientConfig
    {
        private static ClientConfig? _instance;
        public static ClientConfig Instance => _instance ??= new ClientConfig();

        private readonly HashSet<ISettingBase> _dirtySettings = [];

        private void MarkDirty(ISettingBase setting) => _dirtySettings.Add(setting);

        public bool HasChanges => _dirtySettings.Count > 0;

        public void SaveAll()
        {
            if (!HasChanges) return;

            if (Plugin.Instance.ConfigService is Barotrauma.LuaCs.ConfigService cs)
            {
                foreach (ISettingBase setting in _dirtySettings)
                    cs.SaveConfigValue(setting);
            }
            _dirtySettings.Clear();
        }

        private readonly ISettingControl _sosOpenKey = null!;
        public KeyOrMouse SOSOpenKey { get => _sosOpenKey.Value; set => _sosOpenKey.TrySetValue(value); }
        public bool SOSOpenKeyHit => _sosOpenKey.IsHit();
        public bool SOSOpenKeyDown => _sosOpenKey.IsDown();

        private readonly ISettingBase<string> _lastSearchQuery = null!;
        public string LastSearchQuery
        {
            get => _lastSearchQuery.Value;
            set { if (_lastSearchQuery.Value != value) _lastSearchQuery.TrySetValue(value); }
        }

        private readonly ISettingBase<string> _lastItemId = null!;
        public string LastItemId
        {
            get => _lastItemId.Value;
            set { if (_lastItemId.Value != value) _lastItemId.TrySetValue(value); }
        }

        private readonly ISettingBase<bool> _rawXmlMode = null!;
        public bool RawXmlMode
        {
            get => _rawXmlMode.Value;
            set { if (_rawXmlMode.Value != value) _rawXmlMode.TrySetValue(value); }
        }

        private readonly ISettingBase<float> _xmlFontScale = null!;
        public float XmlFontScale
        {
            get => _xmlFontScale.Value;
            set { if (_xmlFontScale.Value != value) _xmlFontScale.TrySetValue(value); }
        }

        private readonly ISettingBase<string> _trackedRecipesRaw = null!;
        public string TrackedRecipesRaw
        {
            get => _trackedRecipesRaw.Value;
            set { if (_trackedRecipesRaw.Value != value) _trackedRecipesRaw.TrySetValue(value); }
        }

        private readonly ISettingBase<bool> _trackerVisible = null!;
        public bool TrackerVisible
        {
            get => _trackerVisible.Value;
            set { if (_trackerVisible.Value != value) _trackerVisible.TrySetValue(value); }
        }
        public event Action<ISettingBase> OnTrackerVisibleValueChanged
        {
            add => _trackerVisible.OnValueChanged += value;
            remove => _trackerVisible.OnValueChanged -= value;
        }

        private readonly ISettingBase<int> _dummyDeathCount = null!;
        public int DummyDeathCount
        {
            get => _dummyDeathCount.Value;
            set { if (_dummyDeathCount.Value != value) _dummyDeathCount.TrySetValue(value); }
        }

        private readonly ISettingBase<bool> _dummySimulated = null!;
        public bool DummySimulated
        {
            get => _dummySimulated.Value;
            set { if (_dummySimulated.Value != value) _dummySimulated.TrySetValue(value); }
        }

        private readonly ISettingBase<string> _dummyCharacterXML = null!;
        public string? DummyCharacterXML
        {
            get => string.IsNullOrEmpty(_dummyCharacterXML.Value) ? null : _dummyCharacterXML.Value;
            set
            {
                string normalized = value ?? "";
                if (_dummyCharacterXML.Value != normalized) _dummyCharacterXML.TrySetValue(normalized);
            }
        }

        // ─── Batch-save (window geometry) ───

        private readonly ISettingBase<int> _windowSizeX = null!;
        private readonly ISettingBase<int> _windowSizeY = null!;
        private readonly ISettingBase<int> _windowPositionX = null!;
        private readonly ISettingBase<int> _windowPositionY = null!;
        private readonly ISettingBase<int> _leftPanelWidth = null!;
        private readonly ISettingBase<int> _rightPanelWidth = null!;

        internal int WindowSizeX { get => _windowSizeX.Value; set { if (_windowSizeX.Value != value) _windowSizeX.TrySetValue(value); } }
        internal int WindowSizeY { get => _windowSizeY.Value; set { if (_windowSizeY.Value != value) _windowSizeY.TrySetValue(value); } }
        internal int WindowPositionX { get => _windowPositionX.Value; set { if (_windowPositionX.Value != value) _windowPositionX.TrySetValue(value); } }
        internal int WindowPositionY { get => _windowPositionY.Value; set { if (_windowPositionY.Value != value) _windowPositionY.TrySetValue(value); } }
        internal int LeftPanelWidth { get => _leftPanelWidth.Value; set { if (_leftPanelWidth.Value != value) _leftPanelWidth.TrySetValue(value); } }
        internal int RightPanelWidth { get => _rightPanelWidth.Value; set { if (_rightPanelWidth.Value != value) _rightPanelWidth.TrySetValue(value); } }

        // ─── Batch-save (complex serialized) ───

        private readonly ISettingBase<string> _favoritesRaw = null!;
        private readonly ISettingBase<string> _tabHistoryRaw = null!;
        private readonly ISettingBase<string> _customLayoutsRaw = null!;

        internal string FavoritesRaw { get => _favoritesRaw.Value; set { if (_favoritesRaw.Value != value) _favoritesRaw.TrySetValue(value); } }
        internal string TabHistoryRaw { get => _tabHistoryRaw.Value; set { if (_tabHistoryRaw.Value != value) _tabHistoryRaw.TrySetValue(value); } }
        internal string CustomLayoutsRaw { get => _customLayoutsRaw.Value; set { if (_customLayoutsRaw.Value != value) _customLayoutsRaw.TrySetValue(value); } }

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

        // ─── XML Serialization Helpers (CustomLayouts) ───

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
                RLogger.LogError($"Failed to find config named {name}!");
                return false;
            }
            setting.OnValueChanged += MarkDirty;
#if DEBUG
            setting.OnValueChanged += setting => RLogger.LogDebug($"Changed: {setting.InternalName} To: {setting.GetStringValue()}");
#endif
            return true;
        }


        public static void Destroy() => _instance = null;

        ~ClientConfig() => Destroy();
    }

    // ─── Layout DTO ───

    internal class SavedLayout
    {
        public Point WindowSize { get; set; }
        public int LeftPanelWidth { get; set; }
        public int RightPanelWidth { get; set; }
    }
}
