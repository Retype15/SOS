// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;

namespace SOS.Configs
{
    [AutoRegister]
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
            SaveChanges();

            SOSController.Instance.ActiveProfile?.ProfileConfig?.Save();
        }

        public void Reset() { }

        public void DrawSettings(GUIListBox container)
        {
            using var l = new LayoutBuilder(container);
            l.Header("ACTIVE VISUAL PROFILE", Color.Gold);

            var profiles = API.CreateWindowProfiles().ToList();
            var profileNames = profiles.Select(p => p.DisplayName);
            var currentProfile = profiles.FirstOrDefault(p => p.Id == ActiveProfileId)?.DisplayName ?? profiles.FirstOrDefault()?.DisplayName ?? throw new KeyNotFoundException($"Not match Profile ID: '{ActiveProfileId}'");

            l.Dropdown("Profile:", profileNames, currentProfile, selectedName =>
            {
                var targetProfile = profiles.FirstOrDefault(p => p.DisplayName == selectedName);
                if (targetProfile != null && targetProfile.Id != ActiveProfileId)
                {
                    API.Emit<string>(CommKeys.ChangeProfile, ActiveProfileId);
                }
            });

            l.Separator();

            // Dibujar el más allá...
            SOSController.Instance.ActiveProfile?.ProfileConfig?.DrawSettings(container);
        }

        private readonly ISettingBase<string> _activeProfileId;

        internal string ActiveProfileId
        {
            get => _activeProfileId.Value;
            set => _activeProfileId.SetIfNotEqual(value);
        }

        public WindowProfileConfig()
        {
            TryInitConfig("ActiveProfileId", out _activeProfileId);
        }

        public static void Destroy()
        {
            _instance = null;
        }
    }
}
