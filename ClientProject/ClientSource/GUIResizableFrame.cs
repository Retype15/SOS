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
    [Flags]
    public enum ResizeDirection
    {
        None = 0,
        Top = 1, Bottom = 2, Left = 4, Right = 8,
        Horizontal = Left | Right,
        Vertical = Top | Bottom,
        All = Top | Bottom | Left | Right
    }

    public class GUIResizableFrame : GUIFrame
    {
        public ResizeDirection AllowedDirections { get; set; } = ResizeDirection.All;
        public bool ClampToParentBounds { get; set; } = true;
        public bool ForceSymmetric { get; set; } = false;
        public bool IsFixed { get; set; } = false;
        public int ResizeMargin { get; set; } = 12;

        private bool isDragging;
        private ResizeDirection currentDragDir;
        private Vector2 dragStartMouse;
        private Rectangle startRect;
        private Point startOffset;
        private float startAspectRatio;
        private bool wasMouseDown;

        public GUIResizableFrame(RectTransform rectT, string style = "", Color? color = null)
            : base(rectT, style, color)
        {
            CanBeFocused = true;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            if (!Visible) return;

            Vector2 mousePos = PlayerInput.MousePosition;
            bool isMouseHeld = PlayerInput.PrimaryMouseButtonHeld();
            bool isMouseDownNow = isMouseHeld && !wasMouseDown;

            if (!isDragging)
            {
                currentDragDir = GetHoverDirection(mousePos, Rect) & AllowedDirections;

                if (currentDragDir != ResizeDirection.None)
                {
                    bool isCorner = IsCorner(currentDragDir);
                    bool isShift = PlayerInput.IsShiftDown();

                    GUI.MouseCursor = (!isCorner && isShift && !IsFixed) ? CursorState.Hand : CursorState.Move;

                    if (isMouseDownNow)
                    {
                        isDragging = true;
                        dragStartMouse = mousePos;
                        startRect = Rect;
                        startOffset = RectTransform.AbsoluteOffset;
                        startAspectRatio = (float)startRect.Width / startRect.Height;
                        GUI.ForceMouseOn(this);
                    }
                }
            }
            else
            {
                GUI.MouseCursor = CursorState.Dragging;
                if (!isMouseHeld) { isDragging = false; return; }
                ApplyInteraction(mousePos);
            }
            wasMouseDown = isMouseHeld;
        }

        private void ApplyInteraction(Vector2 mousePos)
        {
            float deltaX = mousePos.X - dragStartMouse.X;
            float deltaY = mousePos.Y - dragStartMouse.Y;

            if (Math.Abs(deltaX) < 0.01f && Math.Abs(deltaY) < 0.01f) return;

            bool isCorner = IsCorner(currentDragDir);
            bool isShift = PlayerInput.IsShiftDown();
            bool isAlt = PlayerInput.IsAltDown();
            bool isSymmetric = ForceSymmetric || isAlt;

            if (!isCorner && isShift && !IsFixed)
            {
                int newX = (int)(startOffset.X + deltaX);
                int newY = (int)(startOffset.Y + deltaY);

                if (ClampToParentBounds && RectTransform.Parent != null)
                {
                    Rectangle pRect = RectTransform.ParentRect;
                    Rectangle myRect = new(Rect.X + (newX - RectTransform.AbsoluteOffset.X),
                                                   Rect.Y + (newY - RectTransform.AbsoluteOffset.Y),
                                                   Rect.Width, Rect.Height);

                    if (myRect.Left < pRect.Left) newX += (pRect.Left - myRect.Left);
                    if (myRect.Right > pRect.Right) newX -= (myRect.Right - pRect.Right);
                    if (myRect.Top < pRect.Top) newY += (pRect.Top - myRect.Top);
                    if (myRect.Bottom > pRect.Bottom) newY -= (myRect.Bottom - pRect.Bottom);
                }

                RectTransform.AbsoluteOffset = new Point(newX, newY);
                return;
            }

            float rawDeltaX = 0, rawDeltaY = 0;

            if (currentDragDir.HasFlag(ResizeDirection.Right)) rawDeltaX = deltaX;
            else if (currentDragDir.HasFlag(ResizeDirection.Left)) rawDeltaX = -deltaX;

            if (currentDragDir.HasFlag(ResizeDirection.Bottom)) rawDeltaY = deltaY;
            else if (currentDragDir.HasFlag(ResizeDirection.Top)) rawDeltaY = -deltaY;

            if (isSymmetric) { rawDeltaX *= 2; rawDeltaY *= 2; }

            float targetW = startRect.Width + rawDeltaX;
            float targetH = startRect.Height + rawDeltaY;

            bool maintainRatio = isCorner && isShift;
            if (maintainRatio)
            {
                if (Math.Abs(rawDeltaX) / startRect.Width > Math.Abs(rawDeltaY) / startRect.Height)
                    targetH = targetW / startAspectRatio;
                else
                    targetW = targetH * startAspectRatio;
            }

            Point minLimit = RectTransform.MinSize;
            Point maxLimit = RectTransform.MaxSize;
            int limitWX = maxLimit.X <= 0 ? int.MaxValue : maxLimit.X;
            int limitHY = maxLimit.Y <= 0 ? int.MaxValue : maxLimit.Y;

            targetW = MathHelper.Clamp(targetW, minLimit.X, limitWX);
            targetH = MathHelper.Clamp(targetH, minLimit.Y, limitHY);

            if (ClampToParentBounds && RectTransform.Parent != null)
            {
                Rectangle pRect = RectTransform.ParentRect;
                float maxGrowR = pRect.Right - startRect.Right;
                float maxGrowL = startRect.Left - pRect.Left;
                float maxGrowB = pRect.Bottom - startRect.Bottom;
                float maxGrowT = startRect.Top - pRect.Top;

                if (currentDragDir.HasFlag(ResizeDirection.Right) && (targetW - startRect.Width) > maxGrowR) targetW = startRect.Width + maxGrowR;
                if (currentDragDir.HasFlag(ResizeDirection.Left) && (targetW - startRect.Width) > maxGrowL) targetW = startRect.Width + maxGrowL;
                if (currentDragDir.HasFlag(ResizeDirection.Bottom) && (targetH - startRect.Height) > maxGrowB) targetH = startRect.Height + maxGrowB;
                if (currentDragDir.HasFlag(ResizeDirection.Top) && (targetH - startRect.Height) > maxGrowT) targetH = startRect.Height + maxGrowT;

                if (maintainRatio)
                {
                    if (targetW / startRect.Width < targetH / startRect.Height) targetH = targetW / startAspectRatio;
                    else targetW = targetH * startAspectRatio;
                }
            }

            float finalCX = targetW - startRect.Width;
            float finalCY = targetH - startRect.Height;
            float moveL = 0, moveT = 0;

            if (isSymmetric) { moveL = -finalCX / 2f; moveT = -finalCY / 2f; }
            else
            {
                if (currentDragDir.HasFlag(ResizeDirection.Left)) moveL = -finalCX;
                if (currentDragDir.HasFlag(ResizeDirection.Top)) moveT = -finalCY;
            }

            Vector2 pivot = GetPivotVector(RectTransform.Pivot);
            RectTransform.NonScaledSize = new Point((int)Math.Round(targetW), (int)Math.Round(targetH));
            RectTransform.AbsoluteOffset = new Point(
                (int)Math.Round(startOffset.X + moveL + finalCX * pivot.X),
                (int)Math.Round(startOffset.Y + moveT + finalCY * pivot.Y)
            );
        }

        private static bool IsCorner(ResizeDirection dir) =>
            (dir.HasFlag(ResizeDirection.Left) || dir.HasFlag(ResizeDirection.Right)) &&
            (dir.HasFlag(ResizeDirection.Top) || dir.HasFlag(ResizeDirection.Bottom));

        private ResizeDirection GetHoverDirection(Vector2 mouse, Rectangle rect)
        {
            Rectangle detectRect = rect;
            detectRect.Inflate(ResizeMargin, ResizeMargin);
            if (!detectRect.Contains(mouse.ToPoint())) return ResizeDirection.None;

            ResizeDirection dir = ResizeDirection.None;
            if (Math.Abs(mouse.X - rect.Left) <= ResizeMargin) dir |= ResizeDirection.Left;
            else if (Math.Abs(mouse.X - rect.Right) <= ResizeMargin) dir |= ResizeDirection.Right;
            if (Math.Abs(mouse.Y - rect.Top) <= ResizeMargin) dir |= ResizeDirection.Top;
            else if (Math.Abs(mouse.Y - rect.Bottom) <= ResizeMargin) dir |= ResizeDirection.Bottom;
            return dir;
        }

        private static Vector2 GetPivotVector(Pivot pivot) => pivot switch
        {
            Pivot.TopLeft => new Vector2(0, 0),
            Pivot.TopCenter => new Vector2(0.5f, 0),
            Pivot.TopRight => new Vector2(1, 0),
            Pivot.CenterLeft => new Vector2(0, 0.5f),
            Pivot.Center => new Vector2(0.5f, 0.5f),
            Pivot.CenterRight => new Vector2(1, 0.5f),
            Pivot.BottomLeft => new Vector2(0, 1),
            Pivot.BottomCenter => new Vector2(0.5f, 1),
            Pivot.BottomRight => new Vector2(1, 1),
            _ => Vector2.Zero
        };
    }
}