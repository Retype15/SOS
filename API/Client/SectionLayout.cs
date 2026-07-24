// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Text;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    public class SectionLayout(GUIListBox listBox) : IDisposable
    {
        private readonly GUIListBox _listBox = listBox;
        private GUILayoutGroup? _currentGroup;
        private int _rowsCreated;

        public void Header(string title, Color color)
        {
            _currentGroup = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0f), _listBox.Content.RectTransform, Anchor.TopCenter))
            {
                AbsoluteSpacing = 2,
                CanBeFocused = false,
                Stretch = true
            };

            var titleBlock = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), _currentGroup.RectTransform), title, font: GUIStyle.SubHeadingFont, textColor: color, textAlignment: Alignment.Left)
            {
                CanBeFocused = false
            };
            titleBlock.RectTransform.MinSize = new Point(0, 30);
            titleBlock.RectTransform.MaxSize = new Point(int.MaxValue, 30);
            titleBlock.Padding = new Vector4(10, 0, 0, 0);

            _rowsCreated = 0;
        }

        public void Row(string label, string value, Color valueColor)
        {
            if (string.IsNullOrEmpty(value) || _currentGroup == null) return;

            var row = new GUIButton(new RectTransform(new Vector2(1f, 0f), _currentGroup.RectTransform), style: null)
            {
                CanBeFocused = false
            };

            float rowWidth = _currentGroup.Rect.Width > 0 ? _currentGroup.Rect.Width : 400f;
            float labelW = GUIStyle.SmallFont.MeasureString(label).X + 8f;
            float labelRatio = Math.Min(labelW / rowWidth, 0.70f);

            var lblBlock = new GUITextBlock(
                new RectTransform(new Vector2(labelRatio, 1f), row.RectTransform, Anchor.CenterLeft),
                label, font: GUIStyle.SmallFont, textColor: Color.Gray, wrap: false)
            { CanBeFocused = false };

            var valBlock = new GUITextBlock(
                new RectTransform(new Vector2(1f - labelRatio, 1f), row.RectTransform, Anchor.CenterRight),
                value, font: GUIStyle.SmallFont, textColor: valueColor, textAlignment: Alignment.Right, wrap: true)
            { CanBeFocused = false };

            void UpdateHeight()
            {
                float h = Math.Max(lblBlock.TextSize.Y, valBlock.TextSize.Y) + 4f;
                row.RectTransform.MinSize = new Point(0, (int)h);
                row.RectTransform.MaxSize = new Point(int.MaxValue, (int)h);
            }
            UpdateHeight();
            lblBlock.RectTransform.SizeChanged += UpdateHeight;
            valBlock.RectTransform.SizeChanged += UpdateHeight;

            _rowsCreated++;
        }

        public void BadgeRow(
            string label,
            IEnumerable<string> values,
            IEnumerable<string>? displayNames = null,
            char? filterPrefix = null,
            Color? linkColor = null,
            Action<string>? onSearchFilter = null)
        {
            if (values == null || !values.Any() || _currentGroup == null) return;

            var valList = values.ToList();
            var dispList = displayNames?.ToList();

            var data = valList.Select((val, i) => new
            {
                Target = filterPrefix.HasValue ? $"{filterPrefix}{val}" : val,
                Display = dispList != null && i < dispList.Count ? dispList[i] : val
            }).ToList();

            var row = new GUIButton(new RectTransform(new Vector2(1f, 0f), _currentGroup.RectTransform), style: null)
            {
                CanBeFocused = false
            };
            _ = new GUITextBlock(new RectTransform(new Vector2(0.40f, 1f), row.RectTransform, Anchor.CenterLeft), label, font: GUIStyle.SmallFont, textColor: Color.Gray) { CanBeFocused = false };

            var sb = new StringBuilder();
            for (int i = 0; i < data.Count; i++)
            {
                var d = data[i];
                var c = linkColor ?? Color.LightSkyBlue;
                sb.Append($"‖color:{c.R},{c.G},{c.B}‖{d.Display}‖end‖");
                if (i < data.Count - 1)
                    sb.Append(", ");
            }

            var textBlock = new GUITextBlock(new RectTransform(new Vector2(0.60f, 0f), row.RectTransform, Anchor.CenterRight), "", wrap: true, font: GUIStyle.SmallFont, textAlignment: Alignment.TopLeft);
            textBlock.SetRichText(RichString.Rich(sb.ToString()));

            BindHyperlinks(textBlock, data, d => onSearchFilter?.Invoke(d.Target));

            void UpdateLayout()
            {
                int maxW = (int)(row.Rect.Width * 0.60f);
                if (maxW > 0)
                {
                    textBlock.RectTransform.NonScaledSize = new Point(maxW, (int)textBlock.TextSize.Y);
                    float lineH = GUIStyle.SmallFont.LineHeight;
                    if (textBlock.TextSize.Y <= lineH * 1.5f)
                    {
                        int paddingH = (int)(textBlock.Padding.X + textBlock.Padding.Z);
                        int fitW = Math.Min((int)Math.Ceiling(textBlock.TextSize.X + paddingH) + 4, maxW);
                        textBlock.RectTransform.NonScaledSize = new Point(fitW, (int)textBlock.TextSize.Y);
                    }
                }

                int h = Math.Max(24, (int)textBlock.TextSize.Y + 4);
                row.RectTransform.MinSize = new Point(0, h);
                row.RectTransform.MaxSize = new Point(int.MaxValue, h);
            }
            UpdateLayout();
            row.RectTransform.SizeChanged += UpdateLayout;

            if (data.Count == 1)
            {
                var single = data[0];
                row.CanBeFocused = true;
                row.HoverCursor = CursorState.Hand;
                row.OnClicked = (comp, obj) => { onSearchFilter?.Invoke(single.Target); return true; };
            }

            _rowsCreated++;
        }

        public void SelectorRow(
            string label,
            IEnumerable<string> ids,
            IEnumerable<string>? displayNames = null,
            char? fallbackFilterPrefix = null,
            Color? labelColor = null,
            Action<Prefab>? onPrimary = null,
            Action<Prefab>? onSecondary = null,
            Action<string>? onSearchFilter = null)
        {
            if (ids == null || !ids.Any() || _currentGroup == null) return;

            var idList = ids.ToList();
            var nameList = displayNames?.ToList();

            var data = new List<object>();
            for (int i = 0; i < idList.Count; i++)
            {
                string id = idList[i];
                Prefab? found = (Prefab?)AfflictionPrefab.List.FirstOrDefault(a => a.Identifier.Value == id)
                             ?? ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == id);

                if (found != null) data.Add(found);
                else data.Add(fallbackFilterPrefix.HasValue ? $"{fallbackFilterPrefix}{id}" : id);
            }

            string GetText(object obj, int index)
            {
                if (obj is ItemPrefab ip) return ip.Name.Value;
                if (obj is AfflictionPrefab ap) return ap.Name.Value;
                return (nameList != null && index < nameList.Count) ? nameList[index] : obj.ToString()!;
            }

            Color GetColor(object obj)
            {
                if (obj is ItemPrefab ip) return ip.InventoryIconColor;
                if (obj is AfflictionPrefab ap) return ap.IconColors?.FirstOrDefault() ?? Color.White;
                return Color.LightSkyBlue;
            }

            var row = new GUIButton(new RectTransform(new Vector2(1f, 0f), _currentGroup.RectTransform), style: null)
            {
                CanBeFocused = false
            };
            _ = new GUITextBlock(new RectTransform(new Vector2(0.40f, 1f), row.RectTransform, Anchor.CenterLeft), label, font: GUIStyle.SmallFont, textColor: labelColor ?? Color.Gray) { CanBeFocused = false };

            var sb = new StringBuilder();
            for (int i = 0; i < data.Count; i++)
            {
                var obj = data[i];
                var c = GetColor(obj);
                sb.Append($"‖color:{c.R},{c.G},{c.B}‖{GetText(obj, i)}‖end‖");
                if (i < data.Count - 1)
                    sb.Append(", ");
            }

            var textBlock = new GUITextBlock(new RectTransform(new Vector2(0.60f, 0f), row.RectTransform, Anchor.CenterRight), "", wrap: true, font: GUIStyle.SmallFont, textAlignment: Alignment.TopLeft);
            textBlock.SetRichText(RichString.Rich(sb.ToString()));

            BindHyperlinks(
                textBlock,
                data,
                onPrimaryClick: obj => { if (obj is Prefab p) onPrimary?.Invoke(p); else onSearchFilter?.Invoke(obj.ToString()!); },
                onSecondaryClick: obj => { if (obj is Prefab p) onSecondary?.Invoke(p); }
            );

            void UpdateLayout()
            {
                int maxW = (int)(row.Rect.Width * 0.60f);
                if (maxW > 0)
                {
                    textBlock.RectTransform.NonScaledSize = new Point(maxW, (int)textBlock.TextSize.Y);
                    float lineH = GUIStyle.SmallFont.LineHeight;
                    if (textBlock.TextSize.Y <= lineH * 1.5f)
                    {
                        int paddingH = (int)(textBlock.Padding.X + textBlock.Padding.Z);
                        int fitW = Math.Min((int)Math.Ceiling(textBlock.TextSize.X + paddingH) + 4, maxW);
                        textBlock.RectTransform.NonScaledSize = new Point(fitW, (int)textBlock.TextSize.Y);
                    }
                }

                int h = Math.Max(24, (int)textBlock.TextSize.Y + 4);
                row.RectTransform.MinSize = new Point(0, h);
                row.RectTransform.MaxSize = new Point(int.MaxValue, h);
            }
            UpdateLayout();
            row.RectTransform.SizeChanged += UpdateLayout;

            if (data.Count == 1)
            {
                var single = data[0];
                row.CanBeFocused = true;
                row.HoverCursor = CursorState.Hand;
                row.OnClicked = (comp, obj) =>
                {
                    if (single is Prefab p) onPrimary?.Invoke(p);
                    else onSearchFilter?.Invoke(single.ToString()!);
                    return true;
                };
                row.OnSecondaryClicked = (comp, obj) =>
                {
                    if (single is Prefab p) onSecondary?.Invoke(p);
                    return true;
                };
            }

            _rowsCreated++;
        }

        public void RichText(RichString text)
        {
            if (_currentGroup == null || text.IsNullOrEmpty()) return;

            var block = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), _currentGroup.RectTransform), RichString.Rich(text), font: GUIStyle.SmallFont, wrap: true, textAlignment: Alignment.Left)
            {
                CanBeFocused = false
            };

            void UpdateHeight()
            {
                int h = (int)block.TextSize.Y + 10;
                block.RectTransform.MinSize = new Point(0, h);
                block.RectTransform.MaxSize = new Point(int.MaxValue, h);
            }
            UpdateHeight();
            block.RectTransform.SizeChanged += UpdateHeight;

            _rowsCreated++;
        }

        private static void BindHyperlinks<T>(
            GUITextBlock textBlock,
            IEnumerable<T> items,
            Action<T> onPrimaryClick,
            Action<T>? onSecondaryClick = null)
        {
            var list = items.ToList();

            void ApplyLinks()
            {
                if (textBlock.RichTextData == null) return;
                textBlock.ClickableAreas.Clear();

                int index = 0;
                foreach (var data in textBlock.RichTextData)
                {
                    if (data.StartIndex >= data.EndIndex || data.StartIndex < 0 || data.Color == null) continue;
                    if (index >= list.Count) break;

                    var target = list[index];
                    textBlock.ClickableAreas.Add(new GUITextBlock.ClickableArea()
                    {
                        Data = data,
                        OnClick = (tb, area) => { onPrimaryClick?.Invoke(target); },
                        OnSecondaryClick = onSecondaryClick != null ? ((tb, area) => { onSecondaryClick.Invoke(target); }) : null
                    });
                    index++;
                }
            }

            ApplyLinks();
            textBlock.RectTransform.SizeChanged += ApplyLinks;
        }

        public void Dispose()
        {
            if (_currentGroup == null) return;

            if (_rowsCreated == 0 && _currentGroup.CountChildren <= 1)
            {
                _listBox.Content.RemoveChild(_currentGroup);
            }
            else
            {
                int totalHeight = 0;
                foreach (var child in _currentGroup.Children)
                {
                    totalHeight += child.Rect.Height + _currentGroup.AbsoluteSpacing;
                }
                _currentGroup.RectTransform.MinSize = new Point(0, totalHeight + 10);
                _currentGroup.RectTransform.MaxSize = new Point(int.MaxValue, totalHeight + 10);
            }

            _currentGroup = null;
            GC.SuppressFinalize(this);
        }

        ~SectionLayout() => Dispose();
    }
}

