// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma.LuaCs.Data;
using SOS.Extensions;

namespace SOS.Configs
{
    internal sealed class MarketConfig : ConfigDirtySaver, ISOSConfig
    {
        public const string ID = "SOS.MarketConfig";

        private static MarketConfig? _instance;
        public static MarketConfig Instance => _instance ??= new();

        private readonly ISettingBase<bool> _outpostCollapsed;
        private readonly ISettingBase<bool> _storesCollapsed;
        private readonly ISettingBase<bool> _factionsCollapsed;

        public bool OutpostCollapsed
        {
            get => _outpostCollapsed.Value;
            set => _outpostCollapsed.SetIfNotEqual(value);
        }

        public bool StoresCollapsed
        {
            get => _storesCollapsed.Value;
            set => _storesCollapsed.SetIfNotEqual(value);
        }

        public bool FactionsCollapsed
        {
            get => _factionsCollapsed.Value;
            set => _factionsCollapsed.SetIfNotEqual(value);
        }

        public void Load() { }

        public void Save() => SaveChanges(Plugin.Instance.ConfigService);

        public void Reset()
        {
            _outpostCollapsed.SetIfNotEqual(_outpostCollapsed.DefaultValue);
            _storesCollapsed.SetIfNotEqual(_storesCollapsed.DefaultValue);
            _factionsCollapsed.SetIfNotEqual(_factionsCollapsed.DefaultValue);
        }

        private MarketConfig()
        {
            var configService = Plugin.Instance.ConfigService;
            var package = Plugin.Instance.Package;
            void TryInitSetting<T>(string name, out T setting) where T : ISettingBase
                => base.TryInitConfig(name, out setting, configService, package);

            TryInitSetting("MarketOutpostCollapsed", out _outpostCollapsed);
            TryInitSetting("MarketStoresCollapsed", out _storesCollapsed);
            TryInitSetting("MarketFactionsCollapsed", out _factionsCollapsed);
        }
    }
}