// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;

using SOS.Extensions;
using SOS.GUI;
using SOS.Profiles;

namespace SOS.Configs
{
    internal sealed class WindowProfileConfig : ConfigDirtySaver, ISOSConfig
    {
        public const string ID = "SOS.WindowProfile";

        private static WindowProfileConfig? _instance;
        public static WindowProfileConfig Instance => _instance ??= new();

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
            ResetSettingsWindowGeometry();

            SOSController.Instance.GetOrCreateProfile()?.ProfileConfig?.Reset();
        }

        private void ResetSettingsWindowGeometry()
        {
            _settingsWindowSizeX.SetIfNotEqual(_settingsWindowSizeX.DefaultValue);
            _settingsWindowSizeY.SetIfNotEqual(_settingsWindowSizeY.DefaultValue);
            _settingsWindowPositionX.SetIfNotEqual(_settingsWindowPositionX.DefaultValue);
            _settingsWindowPositionY.SetIfNotEqual(_settingsWindowPositionY.DefaultValue);

            var size = new Point(_settingsWindowSizeX.DefaultValue, _settingsWindowSizeY.DefaultValue);
            var position = new Point(_settingsWindowPositionX.DefaultValue, _settingsWindowPositionY.DefaultValue);
            ProfileHelper.ApplySettingsWindowGeometry(size, position);
        }

        public void Draw(RectTransform rectT)
        {
            using var l = new GUILayoutBuilder(rectT);
            l.Header("ACTIVE VISUAL PROFILE", Color.Gold);

            var entries = API.GetAllWindowProfiles();
            var profileNames = entries.Select(p => p.DisplayName()).ToList();
            var profileDesc = entries.Select(p => p.Description()).ToList();
            var currentProfile = entries.FirstOrDefault(p => p.Id == ActiveProfileId)?.DisplayName() ?? entries.FirstOrDefault()?.DisplayName() ?? throw new KeyNotFoundException($"Not match Profile ID: '{ActiveProfileId}'");

            l.Dropdown("Profile:", profileNames, currentProfile, selectedName =>
            {
                var targetProfile = entries.FirstOrDefault(p => p.DisplayName() == selectedName);
                if (targetProfile != null && targetProfile.Id.Value != ActiveProfileId)
                {
                    API.Emit<string>(CommKeys.ChangeProfile, targetProfile.Id.Value);
                }
            }, profileDesc);

            l.Separator();

            var profileConfig = SOSController.Instance.ActiveProfile?.ProfileConfig;
            if (profileConfig != null)
            {
                ProfileHelper.TryDraw(rectT, profileConfig);
                l.Separator();
            }

            l.ButtonToResetSection(this);
        }

        //MARK: Settings

        private readonly ISettingBase<string> _activeProfileId;

        internal string ActiveProfileId
        {
            get => _activeProfileId.Value;
            set => _activeProfileId.SetIfNotEqual(value);
        }

        // Window Dimensions

        private readonly ISettingBase<int> _settingsWindowSizeX;
        private readonly ISettingBase<int> _settingsWindowSizeY;
        private readonly ISettingBase<int> _settingsWindowPositionX;
        private readonly ISettingBase<int> _settingsWindowPositionY;

        // Reset Button
        private readonly ISettingBase<bool> _settingsWindowReset;

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

            TryInitConfig("SettingsWindowResetToDefault", out _settingsWindowReset);
            _settingsWindowReset.OnValueChanged += (_) =>
            {
                if (!_settingsWindowReset.Value) return;

                _settingsWindowReset.TrySetValue(false);
                Reset();
                Save();
                ProfileHelper.RefreshSettings();
                Logger.LogDebug("Profile configs reset to defaults.");
            };
        }

        public static void Destroy()
        {
            _instance = null;
        }
    }
}
