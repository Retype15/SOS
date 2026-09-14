// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#if DEBUG

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.GUI;

namespace SOS.Configs
{
    internal sealed class TestGUILayoutBuilderOnConfigs : ConfigDirtySaver, ISOSConfig
    {
        public const string ID = "SOS._TestGUILayoutBuilderOnConfigs";

        private static TestGUILayoutBuilderOnConfigs? _instance;
        public static TestGUILayoutBuilderOnConfigs Instance = _instance ??= new();

        public void Load() { }

        public void Save() { }

        public void Draw(RectTransform rectT)
        {
            using var l = new GUILayoutBuilder(rectT);
            l.Header("TEST — GUILayoutBuilder", Color.Cyan);

            l.Separator(spacing: 6);
            l.Separator(spacing: 6, color: Color.IndianRed * 0.6f);

            var tb = l.TextBox("TextBox:", "hello", v => Logger.LogDebug($"TextBox: {v}"), tooltip: "Hello, World!", maxLength: 20);
            tb.OnDeselected += (comp, key) => { Logger.LogDebug($"TextBox deselected: {tb.Text}"); };

            l.Dropdown("Dropdown1:", ["pip", "cargo", "dotnet"], "dotnet", v => Logger.LogDebug($"Dropdown1: {v}"), ["First", "Second", "Third"], tooltip: "Dropdown1 tooltip");
            l.Dropdown("Dropdown2:", ["One", "Two"], null, v => Logger.LogDebug($"Dropdown2: {v}"));

            l.TickBox("TickBox:", true, v => Logger.LogDebug($"Tickbox: {v}"), tooltip: "tooltip");

            l.NumberInput("Number (int):", 42, v => Logger.LogDebug($"NumberInt: {v}"), min: 0, max: 100, step: 5, tooltip: "Int numbers, step 5, from 0 to 100");
            l.NumberInput("Number (float):", 0.8f, v => Logger.LogDebug($"NumberFloat: {v}"), min: 0f, max: 3f, step: 0.1f, decimals: 1, tooltip: "Float numbers, step 0.1, from 0 to 3f, decimals 1.");

            l.Slider("Slider cont:", new Vector2(0f, 1f), 0.5f, v => Logger.LogDebug($"Slider cont: {v}"), format: v => v.ToString("F4"), tooltip: "Weeee...");
            l.Slider("Slider step 0.1:", new Vector2(0f, 3f), 0.8f, v => Logger.LogDebug($"Slider step: {v}"), step: 0.1f);
            l.Slider("Slider int step:", new Vector2(0f, 12f), 4f, v => Logger.LogDebug($"Slider int: {v}"), step: 1f);
            l.Slider("Slider %:", new Vector2(0f, 1f), 0.5f, v => Logger.LogDebug($"Slider %: {v}"), step: 0.05f, format: v => $"{MathUtils.RoundToInt(v * 100f)}%", tooltip: "Custom format %");

            l.Button("Button", () => Logger.LogDebug("Button clicked..."), tooltip: "Click here if u are... gay?");
            l.Button("Apply me", () => Logger.LogDebug("Apply clicked..."), "Apply tooltip", () => Logger.LogDebug("Delete clicked"), "Delete tooltip");

            l.Separator();
        }
    }
}

#endif