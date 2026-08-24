// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.GUI
{
    public class GUILayoutGroupAuto : GUILayoutGroup
    {
        public GUILayoutGroupAuto(RectTransform rectT, bool isHorizontal = false, Anchor childAnchor = Anchor.TopLeft) : base(rectT, isHorizontal: isHorizontal, childAnchor: childAnchor) { }

        public override void Update(float deltaTime)
        {
            if (NeedsToRecalculate && Recalculate()) RefreshAncestors();
            base.Update(deltaTime);
        }

        public new bool Recalculate()
        {
            int totalHeight = 0;
            foreach (GUIComponent child in Children)
            {
                if (!child.Visible) { continue; }
                totalHeight += child.Rect.Height + AbsoluteSpacing;
            }
            totalHeight += 10;

            var min = new Point(0, totalHeight);
            bool changed = RectTransform.MinSize != min;
            if (changed)
            {
                RectTransform.MinSize = min;
                RectTransform.MaxSize = new Point(int.MaxValue, totalHeight);
                Logger.LogDebug($"LayoutGroupAuto.Recalculate {GetType().Name}: MinSize.Y={RectTransform.MinSize.Y} (childs={Children.Count()})", level: LogLevel.Trace);
            }
            else
            {
                Logger.LogDebug($"LayoutGroupAuto.Recalculate {GetType().Name}: No changes (childs={Children.Count()})", level: LogLevel.Trace);
            }
            base.Recalculate();

            if (changed && Parent is GUILayoutGroup group)
            {
                group.NeedsToRecalculate = true;
            }

            return changed;
        }

        private void RefreshAncestors()
        {
            for (RectTransform? ancestor = RectTransform.Parent; ancestor != null; ancestor = ancestor.Parent)
            {
                if (ancestor.GUIComponent is GUIListBox listBox)
                {
                    Logger.LogDebug($"LayoutGroupAuto: refresh all of {nameof(GUIListBox)}", level: LogLevel.Trace);
                    listBox.dimensionsNeedsRecalculation = true;
                }
            }
        }
    }
}