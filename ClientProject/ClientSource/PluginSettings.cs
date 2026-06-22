// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Barotrauma;
using Barotrauma.LuaCs;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;

namespace SOS
{
    public partial class Plugin
    {
        public class ClientConfig
        {
            private readonly IConfigService _configService;
            private readonly HashSet<ISettingBase> _dirtySettings = [];

            private void MarkDirty(ISettingBase setting) => _dirtySettings.Add(setting);
            public bool HasChanges => _dirtySettings.Count > 0;

            public void SaveAll()
            {
                if (!HasChanges) return;

                if (_configService is Barotrauma.LuaCs.ConfigService cs)
                {
                    foreach (var setting in _dirtySettings)
                        cs.SaveConfigValue(setting);
                }
                _dirtySettings.Clear();
            }

            private readonly ISettingBase<string> _lastSearchQuery = null!;
            public string LastSearchQuery
            {
                get => _lastSearchQuery.Value;
                set { if (_lastSearchQuery.Value != value) { _lastSearchQuery.TrySetValue(value); MarkDirty(_lastSearchQuery); } }
            }

            private readonly ISettingBase<string> _lastItemId = null!;
            public string LastItemId
            {
                get => _lastItemId.Value;
                set { if (_lastItemId.Value != value) { _lastItemId.TrySetValue(value); MarkDirty(_lastItemId); } }
            }

            private readonly ISettingBase<bool> _rawXmlMode = null!;
            public bool RawXmlMode
            {
                get => _rawXmlMode.Value;
                set { if (_rawXmlMode.Value != value) { _rawXmlMode.TrySetValue(value); MarkDirty(_rawXmlMode); } }
            }

            private readonly ISettingBase<float> _xmlFontScale = null!;
            public float XmlFontScale
            {
                get => _xmlFontScale.Value;
                set { if (_xmlFontScale.Value != value) { _xmlFontScale.TrySetValue(value); MarkDirty(_xmlFontScale); } }
            }

            private readonly ISettingBase<string> _trackedItemId = null!;
            public string TrackedItemId
            {
                get => _trackedItemId.Value;
                set { if (_trackedItemId.Value != value) { _trackedItemId.TrySetValue(value); MarkDirty(_trackedItemId); } }
            }

            private readonly ISettingBase<int> _trackedRecipeHash = null!;
            public uint TrackedRecipeHash
            {
                get => (uint)_trackedRecipeHash.Value;
                set { if ((uint)_trackedRecipeHash.Value != value) { _trackedRecipeHash.TrySetValue((int)value); MarkDirty(_trackedRecipeHash); } }
            }

            private readonly ISettingBase<int> _dummyDeathCount = null!;
            public int DummyDeathCount
            {
                get => _dummyDeathCount.Value;
                set { if (_dummyDeathCount.Value != value) { _dummyDeathCount.TrySetValue(value); MarkDirty(_dummyDeathCount); } }
            }

            private readonly ISettingBase<bool> _dummySimulated = null!;
            public bool DummySimulated
            {
                get => _dummySimulated.Value;
                set { if (_dummySimulated.Value != value) { _dummySimulated.TrySetValue(value); MarkDirty(_dummySimulated); } }
            }

            private readonly ISettingBase<string> _dummyCharacterXML = null!;
            public string? DummyCharacterXML
            {
                get => string.IsNullOrEmpty(_dummyCharacterXML.Value) ? null : _dummyCharacterXML.Value;
                set
                {
                    string normalized = value ?? "";
                    if (_dummyCharacterXML.Value != normalized) { _dummyCharacterXML.TrySetValue(normalized); MarkDirty(_dummyCharacterXML); }
                }
            }

            // ─── Batch-save (window geometry) ───

            private readonly ISettingBase<int> _windowSizeX = null!;
            private readonly ISettingBase<int> _windowSizeY = null!;
            private readonly ISettingBase<int> _windowPositionX = null!;
            private readonly ISettingBase<int> _windowPositionY = null!;
            private readonly ISettingBase<int> _leftPanelWidth = null!;
            private readonly ISettingBase<int> _rightPanelWidth = null!;

            internal int WindowSizeX { get => _windowSizeX.Value; set { if (_windowSizeX.Value != value) { _windowSizeX.TrySetValue(value); MarkDirty(_windowSizeX); } } }
            internal int WindowSizeY { get => _windowSizeY.Value; set { if (_windowSizeY.Value != value) { _windowSizeY.TrySetValue(value); MarkDirty(_windowSizeY); } } }
            internal int WindowPositionX { get => _windowPositionX.Value; set { if (_windowPositionX.Value != value) { _windowPositionX.TrySetValue(value); MarkDirty(_windowPositionX); } } }
            internal int WindowPositionY { get => _windowPositionY.Value; set { if (_windowPositionY.Value != value) { _windowPositionY.TrySetValue(value); MarkDirty(_windowPositionY); } } }
            internal int LeftPanelWidth { get => _leftPanelWidth.Value; set { if (_leftPanelWidth.Value != value) { _leftPanelWidth.TrySetValue(value); MarkDirty(_leftPanelWidth); } } }
            internal int RightPanelWidth { get => _rightPanelWidth.Value; set { if (_rightPanelWidth.Value != value) { _rightPanelWidth.TrySetValue(value); MarkDirty(_rightPanelWidth); } } }

            // ─── Batch-save (complex serialized) ───

            private readonly ISettingBase<string> _favoritesRaw = null!;
            private readonly ISettingBase<string> _tabHistoryRaw = null!;
            private readonly ISettingBase<string> _customLayoutsRaw = null!;

            internal string FavoritesRaw { get => _favoritesRaw.Value; set { if (_favoritesRaw.Value != value) { _favoritesRaw.TrySetValue(value); MarkDirty(_favoritesRaw); } } }
            internal string TabHistoryRaw { get => _tabHistoryRaw.Value; set { if (_tabHistoryRaw.Value != value) { _tabHistoryRaw.TrySetValue(value); MarkDirty(_tabHistoryRaw); } } }
            internal string CustomLayoutsRaw { get => _customLayoutsRaw.Value; set { if (_customLayoutsRaw.Value != value) { _customLayoutsRaw.TrySetValue(value); MarkDirty(_customLayoutsRaw); } } }

            // ─── CSV Serialization Helpers ───

            internal static string FavsToCsv(HashSet<string> favs)
            {
                return string.Join(",", favs);
            }

            internal static HashSet<string> CsvToFavs(string csv)
            {
                if (string.IsNullOrEmpty(csv)) return [];
                return [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
            }

            internal static string HistoryToCsv(List<string> history)
            {
                return string.Join(",", history);
            }

            internal static List<string> CsvToHistory(string csv)
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

            public ClientConfig(IConfigService configService, ContentPackage package)
            {
                _configService = configService;

                TryGetConfig("LastSearchQuery", out _lastSearchQuery);
                TryGetConfig("LastItemId", out _lastItemId);
                TryGetConfig("RawXmlMode", out _rawXmlMode);
                TryGetConfig("XmlFontScale", out _xmlFontScale);
                TryGetConfig("Favorites", out _favoritesRaw);
                TryGetConfig("TabHistory", out _tabHistoryRaw);
                TryGetConfig("CustomLayouts", out _customLayoutsRaw);
                TryGetConfig("TrackedItemId", out _trackedItemId);
                TryGetConfig("TrackedRecipeHash", out _trackedRecipeHash);
                TryGetConfig("WindowSizeX", out _windowSizeX);
                TryGetConfig("WindowSizeY", out _windowSizeY);
                TryGetConfig("WindowPositionX", out _windowPositionX);
                TryGetConfig("WindowPositionY", out _windowPositionY);
                TryGetConfig("LeftPanelWidth", out _leftPanelWidth);
                TryGetConfig("RightPanelWidth", out _rightPanelWidth);
                TryGetConfig("DummyDeathCount", out _dummyDeathCount);
                TryGetConfig("DummySimulated", out _dummySimulated);
                TryGetConfig("DummyCharacterXML", out _dummyCharacterXML);

                bool TryGetConfig<T>(string name, [NotNullWhen(true)] out T setting)
                    where T : ISettingBase
                {
                    if (!configService.TryGetConfig(package, name, out setting))
                    {
                        RLogger.LogError($"Failed to find config named {name}!");
                        return false;
                    }
                    return true;
                }
            }
        }
    }

    // ─── Layout DTO ───

    public class SavedLayout
    {
        public Point WindowSize { get; set; }
        public int LeftPanelWidth { get; set; }
        public int RightPanelWidth { get; set; }
    }
}
