// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using SOS.Profiles;
using static SOS.Profiles.TCWP.ThreeColumnWindowProfile;

namespace SOS.Configs.TCWP
{
    //MARK: TCWPConfig
    internal sealed class TCWPConfig : Configs.ConfigDirtySaver, ISOSConfig
    {
        public string Id => "SOS.Profile.Default3Column.Config";
        public double Order => 0;

        internal SortedDictionary<string, TPLayout> CustomLayouts { get; } = new(StringComparer.Ordinal);

        private bool _loaded = false;

        public void Load()
        {
            if (_loaded) return;
            WindowSize = new Point(_windowSizeX.Value, _windowSizeY.Value);
            WindowPosition = new(_windowPositionX.Value, _windowPositionY.Value);
            LeftPanelWidth = _leftPanelWidth.Value;
            RightPanelWidth = _rightPanelWidth.Value;
            CustomLayouts.Clear();
            CustomLayouts.AddRange(WindowConfigHelper.XmlToLayouts(_customLayoutsRaw.Value));
            _loaded = true;
        }

        public void Save()
        {
            _windowSizeX.SetIfNotEqual(WindowSize.X);
            _windowSizeY.SetIfNotEqual(WindowSize.Y);
            _windowPositionX.SetIfNotEqual(WindowPosition.X);
            _windowPositionY.SetIfNotEqual(WindowPosition.Y);
            _customLayoutsRaw.SetIfNotEqual(WindowConfigHelper.LayoutsToXml(CustomLayouts));

            SaveChanges();
        }

        public void Reset()
        {
            _windowSize = null;
            _windowPosition = null;
            LeftPanelWidth = _leftPanelWidth.DefaultValue;
            RightPanelWidth = _rightPanelWidth.DefaultValue;
        }

        public void DrawSettings(GUIListBox container)
        {
            using var l = new LayoutBuilder(container);
            l.Header("LAYOUT PRESETS", Color.Gold);
            l.Button("Minimal", () => ApplyPreset(500, 600, 0, 0));
            l.Button("Medium-List", () => ApplyPreset(850, 650, 220, 0));
            l.Button("Medium-Desc", () => ApplyPreset(850, 650, 0, 250));
            l.Button("Full View", () => ApplyPreset(1450, 850, 250, 300));

            if (CustomLayouts.Count > 0)
            {
                l.Separator();
                l.Header("MY PRESETS", Color.Gold);
                foreach (var kvp in CustomLayouts.ToList())
                {
                    l.Button(kvp.Key,
                        () => ApplyPreset(kvp.Value.WindowSize, kvp.Value.LeftPanelWidth, kvp.Value.RightPanelWidth),
                        Texts.Get("sos.layout.apply_tooltip", "Applies this panel layout preset.").Value,
                        () => DeleteCustomLayout(kvp.Key),
                        Texts.Get("sos.layout.delete_tooltip", "Deletes this layout preset.").Value);
                }
            }

            l.Separator();

            l.Button(Texts.Get("sos.layout.save", "Save ACTUAL").Value, SaveCurrentLayout, tooltip: Texts.Get("sos.layout.save_tooltip", "Saves the current panel layout as a new preset.").Value);

            l.Separator();
        }

        internal void SaveCurrentLayout()
        {
            Logger.LogDebug("TCWPConfig.SaveCurrentLayout: start", level: LogLevel.Trace);

            int layoutNumber = CustomLayouts.Count + 1;
            while (CustomLayouts.ContainsKey($"Layout {layoutNumber}"))
            {
                layoutNumber++;
            }
            string newName = $"Layout {layoutNumber}";

            var layout = new TPLayout
            {
                WindowSize = WindowSize,
                LeftPanelWidth = LeftPanelWidth,
                RightPanelWidth = RightPanelWidth
            };
            CustomLayouts[newName] = layout;
            Save();
            ProfileHelper.RefreshSettingsPopup();
        }

        internal void DeleteCustomLayout(string name)
        {
            if (CustomLayouts.Remove(name)) Save();
            ProfileHelper.RefreshSettingsPopup();
        }

        private void ApplyPreset(int? winW, int? winH, int? leftW, int? rightW)
            => ApplyPreset(new(winW ?? WindowPosition.X, winH ?? WindowPosition.Y), leftW, rightW);

        private void ApplyPreset(Point? windowSize, int? leftW, int? rightW)
        {
            var layout = new TPLayout
            {
                WindowSize = windowSize ?? WindowSize,
                LeftPanelWidth = leftW ?? LeftPanelWidth,
                RightPanelWidth = rightW ?? RightPanelWidth
            };
            ApplyPreset(layout);
        }

        private static void ApplyPreset(TPLayout? layout)
        {
            if (layout == null) return;
            API.Emit<TPLayout>(CommKeys.ApplyLayout, (TPLayout)layout);
        }

        //MARK: ISettingBases

        private readonly ISettingBase<int> _windowSizeX;
        private readonly ISettingBase<int> _windowSizeY;
        private readonly ISettingBase<int> _windowPositionX;
        private readonly ISettingBase<int> _windowPositionY;
        private readonly ISettingBase<int> _leftPanelWidth;
        private readonly ISettingBase<int> _rightPanelWidth;
        private readonly ISettingBase<string> _customLayoutsRaw;

        private Point? _windowSize;
        internal Point WindowSize { get => _windowSize ??= new(_windowSizeX.DefaultValue, _windowSizeY.DefaultValue); set => _windowSize = value; }

        private Point? _windowPosition;
        internal Point WindowPosition { get => _windowPosition ??= new(_windowPositionX.DefaultValue, _windowPositionY.DefaultValue); set => _windowPosition = value; }

        internal int LeftPanelWidth { get => _leftPanelWidth.Value; set => _leftPanelWidth.SetIfNotEqual(value); }
        internal int RightPanelWidth { get => _rightPanelWidth.Value; set => _rightPanelWidth.SetIfNotEqual(value); }

        public TCWPConfig()
        {
            TryInitConfig("ThreeColumnWindowSizeX", out _windowSizeX);
            TryInitConfig("ThreeColumnWindowSizeY", out _windowSizeY);
            TryInitConfig("ThreeColumnWindowPositionX", out _windowPositionX);
            TryInitConfig("ThreeColumnWindowPositionY", out _windowPositionY);
            TryInitConfig("ThreeColumnLeftPanelWidth", out _leftPanelWidth);
            TryInitConfig("ThreeColumnRightPanelWidth", out _rightPanelWidth);
            TryInitConfig("ThreeColumnCustomLayouts", out _customLayoutsRaw);
        }
    }
}