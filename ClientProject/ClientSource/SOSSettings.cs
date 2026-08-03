// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Barotrauma;
using Barotrauma.LuaCs.Data;

namespace SOS
{
    public sealed class ClientConfig : ConfigDirtySaver, ISOSConfig, IDisposable
    {
        public string Id => "SOS.Core";
        public double Order => 0;

        private static ClientConfig? _instance;
        public static ClientConfig Instance => _instance ??= new ClientConfig();

        private bool _loaded = false;
        public void Load()
        {
            if (_loaded) return;

            var ctr = SOSController.Instance;

            ctr.TabHistory.Clear();
            ctr.TabHistory.AddRange(ConfigHelper.CsvToList(TabHistoryRaw));

            // Restore tracker
            ctr.Tracker.FromCsv(TrackedRecipesRaw);
            ctr.Tracker.Visible = TrackerVisible;

            API.SetState<Prefab?>(CommKeys.SelectTarget, CurrentTarget);

            _loaded = true;

            Logger.LogDebug($"[SOS] CONFIG LOADED..!");
        }

        public void Save()
        {
            var ctr = SOSController.Instance;
            if (ClinicalSimulatorManager.Patient != null)
            {
                DummyDeathCount = ClinicalSimulatorManager.DeathCount;
                var dummyCharacterXML = ClinicalSimulatorManager.ExportSaveData();
                if (dummyCharacterXML != null) DummyCharacterXML = dummyCharacterXML;
                DummySimulated = !ClinicalSimulatorManager.HasStarted;
            }

            if (CurrentTarget != null) _lastItemId.SetIfNotEqual(CurrentTarget.Identifier.Value);

            TabHistoryRaw = ctr.TabHistory.ToCsv();
            TrackedRecipesRaw = ctr.Tracker.ToCsv();
            TrackerVisible = ctr.Tracker.Visible;

            _favoritesRaw.SetIfNotEqual(FavoritedItems.ToCsv());
            _dummyCharacterXMLRaw.SetIfNotEqual(DummyCharacterXML.ToString());

            SaveChanges();

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
        private Prefab? _currentTarget;
        public Prefab? CurrentTarget
        {
            get => _currentTarget ??= (Prefab?)ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == _lastItemId.Value)
                             ?? (Prefab?)AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == _lastItemId.Value)
                             ?? (Prefab?)ItemPrefab.Prefabs.FirstOrDefault();
            set => _currentTarget = value;
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

        private readonly ISettingBase<string> _dummyCharacterXMLRaw;
        private XElement? _dummyCharacterXML = null;
        public XElement DummyCharacterXML
        {
            get => _dummyCharacterXML ??= XElement.Parse(_dummyCharacterXMLRaw.Value);
            set => _dummyCharacterXML = value;
        }

        // ─── Batch-save (complex serialized) ───

        private readonly ISettingBase<string> _favoritesRaw;
        private readonly ISettingBase<string> _tabHistoryRaw;

        private HashSet<string>? _favoritedItems;
        internal HashSet<string> FavoritedItems
        {
            get => _favoritedItems ??= ConfigHelper.CsvToHashSet(_favoritesRaw.Value);
            set => _favoritedItems = value;
        }

        internal string TabHistoryRaw { get => _tabHistoryRaw.Value; set => _tabHistoryRaw.SetIfNotEqual(value); }

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
            TryInitConfig("TrackedRecipes", out _trackedRecipesRaw);
            TryInitConfig("TrackerVisible", out _trackerVisible);
            TryInitConfig("DummyDeathCount", out _dummyDeathCount);
            TryInitConfig("DummySimulated", out _dummySimulated);
            TryInitConfig("DummyCharacterXML", out _dummyCharacterXMLRaw);
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

    public abstract class ConfigDirtySaver
    {

        protected readonly HashSet<ISettingBase> _dirtySettings = [];

        protected void MarkDirty(ISettingBase setting)
            => _dirtySettings.Add(setting);

        protected bool HasChanges => _dirtySettings.Count > 0;

        protected void SaveChanges()
        {
            if (!HasChanges) return;

            foreach (ISettingBase setting in _dirtySettings)
                Plugin.Instance.ConfigService.SaveConfigValue(setting);

            _dirtySettings.Clear();
        }

        protected bool TryInitConfig<T>(string name, out T setting) where T : ISettingBase
            => ConfigHelper.TryInitConfig<T>(name, out setting, MarkDirty);
    }

    public static class ConfigHelper
    {
        // ─── CSV Serialization Helpers ───

        internal static HashSet<string> CsvToHashSet(string? csv)
        {
            if (string.IsNullOrEmpty(csv)) return [];
            return [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        internal static List<string> CsvToList(string? csv)
        {
            if (string.IsNullOrEmpty(csv)) return [];
            return [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        // TryInitConfig

        internal static bool TryInitConfig<T>(string name, [NotNullWhen(true)] out T setting, Action<ISettingBase>? onValueChanged = null)
                where T : ISettingBase
        {
            if (!Plugin.Instance.ConfigService.TryGetConfig(Plugin.Instance.Package, name, out setting))
            {
                Logger.LogError($"Failed to find config named {name}!");
                return false;
            }
            if (onValueChanged != null) setting.OnValueChanged += onValueChanged;
#if DEBUG
            setting.OnValueChanged += setting => Logger.LogDebug($"Changed: {setting.InternalName} To: {setting.GetStringValue(),128}", level: LogLevel.Trace);
#endif
            return true;
        }
    }
}
