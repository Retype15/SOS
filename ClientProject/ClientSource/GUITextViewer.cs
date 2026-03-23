// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SOS
{
    public class GUITextViewer : GUITextBlock
    {
        public GUIScrollBar HorizontalScrollBar { get; private set; }
        public GUIScrollBar VerticalScrollBar { get; private set; }

        private bool scrollBarsNeedsRecalculation = true;
        private Vector4 originalPadding;
        private bool paddingInitialized = false;

        private int ScrollBarThickness
        {
            get
            {
                float desiredSize = 20.0f;
                float scaledSize = desiredSize * GUI.Scale;
                return (int)Math.Min((desiredSize + scaledSize) / 2.0f, Rect.Height / 3);
            }
        }

        public GUITextViewer(RectTransform rectT, string text = "", GUIFont? font = null, string style = "InnerFrame")
            : base(rectT, text, font: font, textAlignment: Alignment.TopLeft, wrap: false, style: style)
        {
            OverflowClip = true;
            Wrap = false;
            CanBeFocused = true;

            HorizontalScrollBar = new GUIScrollBar(new RectTransform(Vector2.One, rectT), isHorizontal: true, style: "GUIScrollBar", color: Color.White);
            VerticalScrollBar = new GUIScrollBar(new RectTransform(Vector2.One, rectT), isHorizontal: false, style: "GUIScrollBar", color: Color.White);

            RectTransform.SizeChanged += () => scrollBarsNeedsRecalculation = true;
            RectTransform.ScaleChanged += () => scrollBarsNeedsRecalculation = true;

            Enabled = true;
        }

        public override void ApplyStyle(GUIComponentStyle componentStyle)
        {
            if (componentStyle == null) return;
            base.ApplyStyle(componentStyle);

            originalPadding = componentStyle.Padding;
            paddingInitialized = true;

            if (HorizontalScrollBar != null && VerticalScrollBar != null)
            {
                if (TextColor.A == 0 || TextColor == Color.Black) TextColor = Color.White;
                if (Color.A < 100) Color = Color.Black * 0.6f;
                scrollBarsNeedsRecalculation = true;
            }
        }

        public new RichString Text
        {
            get => base.Text;
            set
            {
                if (base.Text != value)
                {
                    base.Text = value;
                    scrollBarsNeedsRecalculation = true;
                }
            }
        }

        private void UpdateScrollbars()
        {
            scrollBarsNeedsRecalculation = false;
            if (HorizontalScrollBar == null || VerticalScrollBar == null) return;

            int sbSize = ScrollBarThickness;
            Vector4 currentPadding = paddingInitialized ? originalPadding : Padding;

            float availableWidth = Rect.Width - currentPadding.X - currentPadding.Z;
            float availableHeight = Rect.Height - currentPadding.Y - currentPadding.W;
            float actualWidth = TextSize.X * TextScale;
            float actualHeight = TextSize.Y * TextScale;

            bool needH = actualWidth > availableWidth;
            bool needV = actualHeight > availableHeight;

            if (needH && (actualHeight > (availableHeight - sbSize))) needV = true;
            if (needV && (actualWidth > (availableWidth - sbSize))) needH = true;

            Padding = new Vector4(
                currentPadding.X,
                currentPadding.Y,
                currentPadding.Z + (needV ? sbSize : 0),
                currentPadding.W + (needH ? sbSize : 0)
            );

            HorizontalScrollBar.Visible = needH;
            HorizontalScrollBar.Enabled = needH;
            if (needH)
            {
                HorizontalScrollBar.RectTransform.Resize(new Point((int)(Rect.Width - currentPadding.X - currentPadding.Z - (needV ? sbSize : 0)), sbSize));
                HorizontalScrollBar.RectTransform.SetPosition(Anchor.BottomLeft);
                HorizontalScrollBar.RectTransform.AbsoluteOffset = new Point((int)currentPadding.X, (int)currentPadding.W);
                HorizontalScrollBar.BarSize = Math.Max(0.1f, (Rect.Width - Padding.X - Padding.Z) / actualWidth);
            }

            VerticalScrollBar.Visible = needV;
            VerticalScrollBar.Enabled = needV;
            if (needV)
            {
                VerticalScrollBar.RectTransform.Resize(new Point(sbSize, (int)(Rect.Height - currentPadding.Y - currentPadding.W - (needH ? sbSize : 0))));
                VerticalScrollBar.RectTransform.SetPosition(Anchor.TopRight);
                VerticalScrollBar.RectTransform.AbsoluteOffset = new Point((int)currentPadding.Z, (int)currentPadding.Y);
                VerticalScrollBar.BarSize = Math.Max(0.1f, (Rect.Height - Padding.Y - Padding.W) / actualHeight);
            }
        }

        public override void Update(float deltaTime)
        {
            if (scrollBarsNeedsRecalculation) UpdateScrollbars();
            base.Update(deltaTime);

            if (!Visible) return;

            if (GUI.IsMouseOn(this) && PlayerInput.ScrollWheelSpeed != 0)
            {
                if (PlayerInput.IsShiftDown() && HorizontalScrollBar.Enabled)
                    HorizontalScrollBar.BarScroll -= Math.Sign(PlayerInput.ScrollWheelSpeed) * 0.1f;
                else if (VerticalScrollBar.Enabled)
                    VerticalScrollBar.BarScroll -= Math.Sign(PlayerInput.ScrollWheelSpeed) * 0.1f;
            }

            float maxScrollX = Math.Max(0, (TextSize.X * TextScale) - (Rect.Width - Padding.X - Padding.Z));
            float maxScrollY = Math.Max(0, (TextSize.Y * TextScale) - (Rect.Height - Padding.Y - Padding.W));

            TextOffset = new Vector2(
                (int)(-HorizontalScrollBar.BarScroll * maxScrollX),
                (int)(-VerticalScrollBar.BarScroll * maxScrollY)
            );
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            base.Draw(spriteBatch);

            if (HorizontalScrollBar.Visible) HorizontalScrollBar.DrawManually(spriteBatch, alsoChildren: true, recursive: true);
            if (VerticalScrollBar.Visible) VerticalScrollBar.DrawManually(spriteBatch, alsoChildren: true, recursive: true);
        }

        public override void SetAlpha(float a)
        {
            base.SetAlpha(a);
            HorizontalScrollBar?.SetAlpha(a);
            VerticalScrollBar?.SetAlpha(a);
        }
    }
}