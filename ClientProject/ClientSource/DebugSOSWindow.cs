// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0079
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    public class DebugSOSWindow
    {
        public static DebugSOSWindow? Instance { get; private set; }
        private GUIResizableFrame? mainFrame;

#pragma warning disable CS8618
        public DebugSOSWindow()
        {
            if (GUI.Canvas == null) return;
            Instance = this;

            mainFrame = new GUIResizableFrame(new RectTransform(new Point(800, 600), GUI.Canvas, Anchor.Center), "CircuitBoxFrame")
            {
                Color = Color.Black * 0.9f,
                AllowedDirections = ResizeDirection.All,
                ForceSymmetric = false,
            };
            mainFrame.RectTransform.MinSize = new Point(600, 400);
            mainFrame.RectTransform.MaxSize = new Point(1600, 1000);

            var mainLayout = new GUILayoutGroup(new RectTransform(Vector2.One, mainFrame.RectTransform), isHorizontal: true)
            {
                Stretch = true,
                CanBeFocused = false
            };

            var sidebar = new GUIResizableFrame(new RectTransform(new Vector2(0.3f, 1f), mainLayout.RectTransform), style: "CircuitBoxFrame")
            {
                AllowedDirections = ResizeDirection.Right,
                IsFixed = true
            };
            sidebar.RectTransform.MinSize = new Point(150, 0);
            sidebar.RectTransform.MaxSize = new Point(400, 0);

            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), sidebar.RectTransform), "SIDEBAR", font: GUIStyle.SubHeadingFont, textAlignment: Alignment.Center);
            var sideList = new GUIListBox(new RectTransform(new Vector2(0.9f, 0.85f), sidebar.RectTransform, Anchor.BottomCenter));
            for (int i = 0; i < 10; i++) _ = new GUITextBlock(new RectTransform(new Point(sideList.Content.Rect.Width, 25), sideList.Content.RectTransform), $"Tool {i}", style: "ListBoxElement");

            var workspace = new GUIFrame(new RectTransform(new Vector2(0.7f, 1f), mainLayout.RectTransform), style: null)
            {
                CanBeFocused = false
            };

            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.05f), workspace.RectTransform), "WORKSPACE (Clamped Area)", font: GUIStyle.SmallFont, textAlignment: Alignment.Center) { TextColor = Color.Gray };

            var nestedWindow = new GUIResizableFrame(new RectTransform(new Point(300, 200), workspace.RectTransform, Anchor.Center), style: "CircuitBoxFrame")
            {
                AllowedDirections = ResizeDirection.All,
                ClampToParentBounds = true,
                Color = Color.DarkSlateBlue * 0.5f
            };
            nestedWindow.RectTransform.MinSize = new Point(100, 80);

            var nestedContent = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), nestedWindow.RectTransform, Anchor.Center)) { Stretch = true };
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.3f), nestedContent.RectTransform), "NESTED", font: GUIStyle.SmallFont, textAlignment: Alignment.Center);
            _ = new GUIButton(new RectTransform(new Vector2(1f, 0.5f), nestedContent.RectTransform), "Inner Action", style: "GUIButtonSmall");

            var bottomConsole = new GUIResizableFrame(new RectTransform(new Vector2(1f, 0.2f), mainFrame.RectTransform, Anchor.BottomCenter), style: "CircuitBoxFrame")
            {
                AllowedDirections = ResizeDirection.Top,
                IsFixed = true,
                Color = Color.DarkRed * 0.2f
            };
            bottomConsole.RectTransform.MinSize = new Point(0, 50);
            bottomConsole.RectTransform.MaxSize = new Point(0, 300);
            _ = new GUITextBlock(new RectTransform(Vector2.One, bottomConsole.RectTransform), "CONSOLE (Resize Top Only)", font: GUIStyle.SmallFont, textAlignment: Alignment.Center);

            // Aaaa close
            var closeBtn = new GUIButton(new RectTransform(new Point(20, 20), mainFrame.RectTransform, Anchor.TopRight) { AbsoluteOffset = new Point(5, 5) }, "X", style: "GUICancelButton")
            {
                OnClicked = (_, _) => { Destroy(); return true; }
            };
        }

        public void Update()
        {
            mainFrame?.AddToGUIUpdateList();
        }

        public void Destroy()
        {
            mainFrame?.Parent.RemoveChild(mainFrame);
            mainFrame = null;
            Instance = null;
        }
    }
}