// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.Panels.ItemPanel;

namespace SOS.GUI
{
    //TODO: Generado por AI, sin revisar (WIP)
    internal class GUIDesplegableBox
    {
        public GUIDesplegableBox(GUIComponent parent, Action<string> onBadgeClick, string labelText, IEnumerable<string> tags, IEnumerable<Prefab> items, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            var row = new GUIFrame(new RectTransform(new Vector2(1f, 0f), parent.RectTransform) { MinSize = new Point(0, 24) }, style: null);

            _ = new GUITextBlock(new RectTransform(new Vector2(0.3f, 1f), row.RectTransform, Anchor.CenterLeft), labelText, font: GUIStyle.SmallFont, textColor: Color.Gray) { CanBeFocused = false };

            var tagsList = tags.ToList();

            RichString tagsRich = tagsList.JoinToRichString(", ", t => t, t => Color.LightSkyBlue);
            var tagsText = new GUITextBlock(new RectTransform(new Vector2(0.55f, 1f), row.RectTransform, Anchor.TopLeft) { RelativeOffset = new Vector2(0.3f, 0f) }, "", wrap: true, font: GUIStyle.SmallFont, textAlignment: Alignment.CenterLeft);
            tagsText.SetRichText(tagsRich);

            tagsText.BindHyperlinks(tagsList, tag => onBadgeClick?.Invoke(tag));

            var dropDown = new GUIDropDown2(new RectTransform(new Point(36, 24), row.RectTransform, Anchor.TopRight) { IsFixedSize = true }, elementCount: Math.Min((int)items.Count(), 8), listBoxWidth: (int)(parent.Rect.Width * 0.95f), style: "GUIDropDown", expandToRight: false);

            foreach (var item in items)
            {
                bool isFav = SOSController.Instance.FavoritedItems.Contains(item.Identifier.Value);
                string prefix = isFav ? " *" : "";

                CardBuilder.DrawCompactItemRow(dropDown.ListBox.Content, item, 1, true, prefix, isFav ? Color.Gold : Color.White,
                onPrimaryClick: (p) => { onPrimary?.Invoke(p); dropDown.Dropped = false; },
                onSecondaryClick: onSecondary);
            }

            void UpdateHeight()
            {
                int h = Math.Max(24, (int)tagsText.TextSize.Y + 4);
                row.RectTransform.MinSize = new Point(0, h);
                row.RectTransform.MaxSize = new Point(int.MaxValue, h);
            }
            UpdateHeight();
            row.RectTransform.SizeChanged += UpdateHeight;
        }
    }
}
