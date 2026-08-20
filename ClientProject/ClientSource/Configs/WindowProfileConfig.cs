// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;
using SOS.GUI;

namespace SOS.Configs
{
    internal sealed class WindowProfileConfig : ConfigDirtySaver, ISOSConfig
    {
        public string Id => "SOS.WindowProfile";
        public double Order => 1;

        private static WindowProfileConfig? _instance;
        public static WindowProfileConfig Instance = _instance ??= new();

        private bool _loaded = false;

        public void Load()
        {
            SOSController.Instance.ActiveProfile?.ProfileConfig?.Load();
            if (_loaded) return;
            _loaded = true;

        }

        public void Save()
        {
            if (_settingsWindowSize.HasValue)
            {
                _settingsWindowSizeX.SetIfNotEqual(_settingsWindowSize.Value.X);
                _settingsWindowSizeY.SetIfNotEqual(_settingsWindowSize.Value.Y);
            }
            if (_settingsWindowPosition.HasValue)
            {
                _settingsWindowPositionX.SetIfNotEqual(_settingsWindowPosition.Value.X);
                _settingsWindowPositionY.SetIfNotEqual(_settingsWindowPosition.Value.Y);
            }

            SaveChanges();

            SOSController.Instance.ActiveProfile?.ProfileConfig?.Save();
        }

        public bool DrawSettings(GUIListBox container)
        {
            using var l = new GUILayoutBuilder(container);
            l.Header("ACTIVE VISUAL PROFILE", Color.Gold);

            var profiles = API.CreateWindowProfiles().ToList();
            var profileNames = profiles.Select(p => p.DisplayName);
            var currentProfile = profiles.FirstOrDefault(p => p.Id == ActiveProfileId)?.DisplayName ?? profiles.FirstOrDefault()?.DisplayName ?? throw new KeyNotFoundException($"Not match Profile ID: '{ActiveProfileId}'");

            l.Dropdown("Profile:", profileNames, currentProfile, selectedName =>
            {
                var targetProfile = profiles.FirstOrDefault(p => p.DisplayName == selectedName);
                if (targetProfile != null && targetProfile.Id != ActiveProfileId)
                {
                    API.Emit<string>(CommKeys.ChangeProfile, targetProfile.Id);
                }
            });

            l.Separator();

            SOSController.Instance.ActiveProfile?.ProfileConfig?.DrawSettings(container);
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

        private Point? _settingsWindowSize;
        internal Point SettingsWindowSize
        {
            get => _settingsWindowSize ??= new(_settingsWindowSizeX.Value, _settingsWindowSizeY.Value);
            set => _settingsWindowSize = value;
        }

        private Point? _settingsWindowPosition;
        internal Point SettingsWindowPosition
        {
            get => _settingsWindowPosition ??= new(_settingsWindowPositionX.Value, _settingsWindowPositionY.Value);
            set => _settingsWindowPosition = value;
        }

        public WindowProfileConfig()
        {
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
