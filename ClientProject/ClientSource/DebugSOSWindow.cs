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

        public DebugSOSWindow()
        {
            if (GUI.Canvas == null) return;
            Instance = this;

            mainFrame = new GUIResizableFrame(new RectTransform(new Point(1000, 700), GUI.Canvas, Anchor.Center))
            {
                Color = Color.Black * 0.8f,
                AllowedDirections = ResizeDirection.All
            };
            mainFrame.RectTransform.MinSize = new Point(600, 400);

            var horizontalLayout = new GUILayoutGroup(new RectTransform(Vector2.One, mainFrame.RectTransform), isHorizontal: true)
            {
                Stretch = true,
                CanBeFocused = false
            };

            var sidebar = new GUIResizableFrame(new RectTransform(new Vector2(0.3f, 1f), horizontalLayout.RectTransform), style: "InnerFrame")
            {
                AllowedDirections = ResizeDirection.Right,
                IsFixed = true,
                ClampToParentBounds = true,
                Color = Color.CornflowerBlue * 0.2f
            };
            sidebar.RectTransform.MinSize = new Point(150, 400);
            sidebar.RectTransform.MaxSize = new Point(500, 1200);

            var sideContainer = new GUIFrame(new RectTransform(new Vector2(0.9f, 0.9f), sidebar.RectTransform, Anchor.Center), style: null);

            var sideLayout = new GUILayoutGroup(new RectTransform(Vector2.One, sideContainer.RectTransform))
            {
                Stretch = true,
                CanBeFocused = false
            };

            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), sideLayout.RectTransform), "NAVEGATION", font: GUIStyle.SubHeadingFont);

            var sideList = new GUIListBox(new RectTransform(new Vector2(1f, 0.8f), sideLayout.RectTransform), style: "GUIListBox")
            {
                CanBeFocused = true
            };

            for (int i = 0; i < 15; i++)
            {
                var text = new GUITextBlock(new RectTransform(new Point(sideList.Content.Rect.Width, 25), sideList.Content.RectTransform),
                    $"Item de Prueba {i}", style: "ListBoxElement");
            }

            var contentArea = new GUIFrame(new RectTransform(new Vector2(0.7f, 1f), horizontalLayout.RectTransform), style: "InnerFrame")
            {
                Color = Color.White * 0.05f
            };

            var mainContent = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.8f), contentArea.RectTransform, Anchor.Center))
            {
                Stretch = true,
                CanBeFocused = false
            };

            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.2f), mainContent.RectTransform), "CENTRAL PANEL", font: GUIStyle.LargeFont, textAlignment: Alignment.Center);
            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0.6f), mainContent.RectTransform),
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAHHH NOT WORKKKKKKK??? .",
                wrap: true, textAlignment: Alignment.Center);

            // Aaaa close
            _ = new GUIButton(new RectTransform(new Point(24, 24), mainFrame.RectTransform, Anchor.TopRight) { AbsoluteOffset = new Point(8, 8) }, "X", style: "GUICancelButton")
            {
                OnClicked = (_, _) => { Destroy(); return true; }
            };

            mainFrame.ForceLayoutRecalculation();
        }

        public void Update()
        {
            if (mainFrame == null) return;
            mainFrame.AddToGUIUpdateList();
        }

        public void Destroy()
        {
            if (mainFrame?.Parent != null) mainFrame.Parent.RemoveChild(mainFrame);
            mainFrame = null;
            Instance = null;
        }
    }
}