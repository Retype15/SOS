// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.GUI
{
    public sealed class GUIAccordion : GUILayoutGroupAuto
    {
        public const int HeaderHeight = 28;

        private readonly GUIButton _headerButton;
        private readonly GUIImage? _icon;
        private readonly Action<bool>? _onToggle;
        private bool _collapsed;

        public GUILayoutBuilder Content { get; }
        public bool IsCollapsed => _collapsed;

        public GUIAccordion(GUIComponent header, RectTransform parent, bool collapsed = false, Action<bool>? onToggle = null, Anchor iconAnchor = Anchor.CenterRight)
            : base(new RectTransform(new Vector2(1f, 0f), parent))
        {
            _collapsed = collapsed;
            _onToggle = onToggle;

            header.CanBeFocused = false;

            _headerButton = new GUIButton(new RectTransform(Vector2.One, header.RectTransform), style: null)
            {
                CanBeFocused = true,
                HoverCursor = CursorState.Hand,
                Color = Color.Transparent,
                HoverColor = Color.Transparent,
                PressedColor = Color.Transparent,
                SelectedColor = Color.Transparent,
                TextColor = Color.Transparent,
                HoverTextColor = Color.Transparent,
                SelectedTextColor = Color.Transparent,
                OnClicked = (_, _) => { Toggle(); return true; }
            };

            if (GUIStyle.GetComponentStyle("GUIDropDown")?.ChildStyles.TryGetValue("dropdownicon".ToIdentifier(), out GUIComponentStyle? style) ?? false)
            {
                _icon = new GUIImage(new RectTransform(new Vector2(0.6f, 0.6f), _headerButton.RectTransform, iconAnchor, scaleBasis: ScaleBasis.BothHeight) { AbsoluteOffset = new Point(5, 0) }, null, scaleToFit: true)
                {
                    CanBeFocused = false
                };
                _icon.ApplyStyle(style);
                _icon.Scale = 1.0f;
                _icon.Rotation = _collapsed ? MathHelper.ToRadians(-90f) : 0;
            }
            else
                Logger.LogDebugWarning("Style: 'GUIDropDown.dropdownicon' not encountered.");

            Content = new GUILayoutBuilder(new RectTransform(new Vector2(1f, 0f), RectTransform))
            {
                Visible = !_collapsed
            };

            Visible = !_collapsed;
        }

        public GUIAccordion(string title, RectTransform parent, string? tooltip, bool collapsed = false, Action<bool>? onToggle = null, Anchor iconAnchor = Anchor.CenterRight)
            : this(CreateHeader(parent, title), parent, collapsed, onToggle, iconAnchor)
        {
            if (tooltip != null) _headerButton.ToolTip = tooltip.Rich();
        }

        private static GUITextBlock CreateHeader(RectTransform parent, string title)
        {
            var block = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), parent), title, font: GUIStyle.SubHeadingFont, textColor: Color.White, textAlignment: Alignment.Left)
            {
                CanBeFocused = false
            };
            block.RectTransform.MinSize = new Point(0, HeaderHeight);
            block.RectTransform.MaxSize = new Point(int.MaxValue, HeaderHeight);
            block.Padding = new Vector4(10, 0, 0, 0);
            return block;
        }

        public bool Toggle()
        {
            SetCollapsed(!_collapsed);
            return true;
        }

        public void SetCollapsed(bool collapsed)
        {
            if (_collapsed == collapsed) return;

            _collapsed = collapsed;
            if (_icon != null) _icon.Rotation = _collapsed ? MathHelper.ToRadians(-90f) : 0; // TODO: Agregar un nuevo Animacion para animar el cambio de rotacion.
            Content.Visible = !_collapsed;
            Visible = !_collapsed;
            NeedsToRecalculate = true;
            if (Parent is GUILayoutGroup layoutGroup) layoutGroup.NeedsToRecalculate = true;
            Logger.LogDebug($"GUIAccordion.SetCollapsed: collapsed={_collapsed}", level: LogLevel.Trace);
            _onToggle?.Invoke(_collapsed);
        }
    }
}
