// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework.Input;
using SOS.Prefabs;
using SOS.Profiles;

namespace SOS.Configs
{
    public sealed class CoreConfig : ConfigDirtySaver, ISOSConfig
    {
        public const string ID = "SOS.Core";

        private static CoreConfig? _instance;
        public static CoreConfig Instance => _instance ??= new();

        private bool _loaded = false;
        public void Load()
        {
            if (_loaded) return;

            var ctr = SOSController.Instance;

            ProfileHelper.TabHistory.Clear();
            ProfileHelper.TabHistory.AddRange(ConfigHelper.CsvToList(TabHistoryRaw));

            PrefabHelper.ClearFavorites(false);
            PrefabHelper.AddRangeFavorite(ConfigHelper.CsvToHashSet(_favoritesRaw.Value), false);

            // Restore tracker
            ctr.Tracker.FromCsv(TrackedRecipesRaw);
            ctr.Tracker.Visible = TrackerVisible;

            API.SetState<Prefab?>(CommKeys.SelectTarget, CurrentTarget);
            if (CurrentTarget != null)
                ProfileHelper.NavigationHistory.Push(CurrentTarget);

            _loaded = true;
        }

        public void Save()
        {
            var ctr = SOSController.Instance;

            var prefab = API.GetState<Prefab?>(CommKeys.SelectTarget);
            if (prefab != null)
            {
                CurrentTarget = prefab;
                _lastItemId.SetIfNotEqual(CurrentTarget.Identifier.Value);
            }

            TabHistoryRaw = ProfileHelper.TabHistory.ToCsv();
            TrackedRecipesRaw = ctr.Tracker.ToCsv();
            TrackerVisible = ctr.Tracker.Visible;

            _favoritesRaw.SetIfNotEqual(PrefabHelper.Favorites.ToCsv());

            SaveChanges(Plugin.Instance.ConfigService);
        }

        public void Reset()
        {
            SOSOpenKey = new KeyOrMouse(Keys.J);
            LastSearchQuery = "";
            _lastItemId.SetIfNotEqual("");
            RawXmlMode = false;
            XmlFontScale = 1.0f;
            TrackedRecipesRaw = "";
            TrackerVisible = true;
            ProfileHelper.ClearTabHistory();
            TabHistoryRaw = "";

            _currentTarget = null;
            _favoritesRaw.SetIfNotEqual(_favoritesRaw.DefaultValue);

            var ctr = SOSController.Instance;
            ctr.Tracker.Clear();
            ctr.Tracker.Visible = TrackerVisible;
        }

        public bool DrawSettings(GUIListBox container)
        {
            using var l = new SOS.GUI.GUILayoutBuilder(container);
            l.Header("CORE SETTINGS", Microsoft.Xna.Framework.Color.Gold);
            l.Separator();
            l.Button(
                Texts.Get("sos.config.reset_section", "Reset Section Defaults").Value,
                onClick: () =>
                {
                    Reset();
                    Save();
                    Profiles.ProfileHelper.RefreshSettings();
                },
                style: "GUIButtonSmall",
                color: Microsoft.Xna.Framework.Color.IndianRed * 0.8f);
            return true;
        }

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

        private void SetCurrentTarget(Prefab? prefab) => CurrentTarget = prefab;

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

        // ─── Batch-save (complex serialized) ───

        private readonly ISettingBase<string> _favoritesRaw;
        private readonly ISettingBase<string> _tabHistoryRaw;

        public string TabHistoryRaw { get => _tabHistoryRaw.Value; set => _tabHistoryRaw.SetIfNotEqual(value); }

        // ─── Constructor ───

        private CoreConfig()
        {
            var pms = Plugin.Instance.ConfigService;
            var p = Plugin.Instance.Package;
            void TryInitConfig<T>(string name, out T setting) where T : ISettingBase => base.TryInitConfig(name, out setting, pms, p);

            TryInitConfig("SOSOpenKey", out _sosOpenKey);
            TryInitConfig("LastSearchQuery", out _lastSearchQuery);
            TryInitConfig("LastItemId", out _lastItemId);
            TryInitConfig("RawXmlMode", out _rawXmlMode);
            TryInitConfig("XmlFontScale", out _xmlFontScale);
            TryInitConfig("Favorites", out _favoritesRaw);
            TryInitConfig("TabHistory", out _tabHistoryRaw);
            TryInitConfig("TrackedRecipes", out _trackedRecipesRaw);
            TryInitConfig("TrackerVisible", out _trackerVisible);

            API.On<Prefab?>(CommKeys.SelectTarget, SetCurrentTarget, EventPriority.State);
        }


        public static void Destroy()
        {
            if (_instance != null)
            {
                API.Off<Prefab?>(CommKeys.SelectTarget, Instance.SetCurrentTarget, EventPriority.State);
                _instance = null;
            }
        }
    }
}
