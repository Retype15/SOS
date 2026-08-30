// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;
using SOS.GUI;
using SOS.Profiles;

namespace SOS.Configs
{
    internal sealed class WindowProfileConfig : ConfigDirtySaver, ISOSConfig
    {
        public const string ID = "SOS.WindowProfile";

        private static WindowProfileConfig? _instance;
        public static WindowProfileConfig Instance = _instance ??= new();

        private bool _loaded = false;

        public void Load()
        {
            SOSController.Instance.ActiveProfile?.ProfileConfig?.Load();
            if (_loaded) return;
            ProfileHelper.SettingsWindowSize = new(_settingsWindowSizeX.Value, _settingsWindowSizeY.Value);
            ProfileHelper.SettingsWindowPosition = new(_settingsWindowPositionX.Value, _settingsWindowPositionY.Value);
            _loaded = true;

        }

        public void Save()
        {
            _settingsWindowSizeX.SetIfNotEqual(ProfileHelper.SettingsWindowSize.X);
            _settingsWindowSizeY.SetIfNotEqual(ProfileHelper.SettingsWindowSize.Y);
            _settingsWindowPositionX.SetIfNotEqual(ProfileHelper.SettingsWindowPosition.X);
            _settingsWindowPositionY.SetIfNotEqual(ProfileHelper.SettingsWindowPosition.Y);

            SaveChanges(Plugin.Instance.ConfigService);

            SOSController.Instance.ActiveProfile?.ProfileConfig?.Save();
        }

        public void Reset()
        {
            ActiveProfileId = "SOS.Default3Column";
            _settingsWindowSizeX.SetIfNotEqual(_settingsWindowSizeX.DefaultValue);
            _settingsWindowSizeY.SetIfNotEqual(_settingsWindowSizeY.DefaultValue);
            _settingsWindowPositionX.SetIfNotEqual(_settingsWindowPositionX.DefaultValue);
            _settingsWindowPositionY.SetIfNotEqual(_settingsWindowPositionY.DefaultValue);
            ProfileHelper.SettingsWindowSize = new(_settingsWindowSizeX.DefaultValue, _settingsWindowSizeY.DefaultValue);
            ProfileHelper.SettingsWindowPosition = new(_settingsWindowPositionX.DefaultValue, _settingsWindowPositionY.DefaultValue);
        }

        public bool DrawSettings(GUIListBox container)
        {
            using var l = new GUILayoutBuilder(container);
            l.Header("ACTIVE VISUAL PROFILE", Color.Gold);

            var profiles = API.GetAllWindowProfiles().ToList();
            var profileNames = profiles.Select(p => p.DisplayName).ToList();
            var profileDesc = profiles.Select(p => p.Description).ToList();
            var currentProfile = profiles.FirstOrDefault(p => p.Id == ActiveProfileId)?.DisplayName ?? profiles.FirstOrDefault()?.DisplayName ?? throw new KeyNotFoundException($"Not match Profile ID: '{ActiveProfileId}'");

            l.Dropdown("Profile:", profileNames, currentProfile, selectedName =>
            {
                var targetProfile = profiles.FirstOrDefault(p => p.DisplayName == selectedName);
                if (targetProfile != null && targetProfile.Id != ActiveProfileId)
                {
                    API.Emit<string>(CommKeys.ChangeProfile, targetProfile.Id);
                }
            }, profileDesc);

            l.Separator();

            SOSController.Instance.ActiveProfile?.ProfileConfig?.DrawSettings(container);

            l.Separator();
            l.ButtonToResetSection(this);

            return true;
        }

        private readonly ISettingBase<string> _activeProfileId;

        internal string ActiveProfileId
        {
            get => _activeProfileId.Value;
            set => _activeProfileId.SetIfNotEqual(value);
        }

        //MARK: Settings Window Dimensions

        private readonly ISettingBase<int> _settingsWindowSizeX;
        private readonly ISettingBase<int> _settingsWindowSizeY;
        private readonly ISettingBase<int> _settingsWindowPositionX;
        private readonly ISettingBase<int> _settingsWindowPositionY;

        private WindowProfileConfig()
        {
            var pms = Plugin.Instance.ConfigService;
            var p = Plugin.Instance.Package;
            void TryInitConfig<T>(string name, out T setting) where T : ISettingBase => base.TryInitConfig(name, out setting, pms, p);

            TryInitConfig("ActiveProfileId", out _activeProfileId);
            TryInitConfig("SettingsWindowSizeX", out _settingsWindowSizeX);
            TryInitConfig("SettingsWindowSizeY", out _settingsWindowSizeY);
            TryInitConfig("SettingsWindowPositionX", out _settingsWindowPositionX);
            TryInitConfig("SettingsWindowPositionY", out _settingsWindowPositionY);
        }

        public static void Destroy()
        {
            _instance = null;
        }
    }
}
