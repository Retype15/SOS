// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.Panels.PreviewPanel
{

    // MARK: Preview Tab
    [AutoRegister]
    public class PreviewPanelTab : ISOSTab, IDisposable
    {
        public double Order => 10;
        public string TabName => Texts.Get("sos.tab.preview", "PREVIEW").Value;
        public string ToolTip => Texts.Get("sos.tab.preview_tooltip", "Shows the visual sprite of the selected prefab.").Value;

        private GUIFrame? _container;
        private GUITextBlock _nameBlock = null!;
        private GUITextBlock _idBlock = null!;
        private Prefab _currentPrefab = null!;

        public bool CanHandle(Prefab prefab) => prefab is ItemPrefab || prefab is AfflictionPrefab;

        public void Init(GUIComponent parentContainer)
        {
            _container = new GUIFrame(new RectTransform(Vector2.One, parentContainer.RectTransform), style: null) { Visible = false };

            var layout = new GUILayoutGroup(new RectTransform(Vector2.One, _container.RectTransform)) { Stretch = true, AbsoluteSpacing = 10 };

            _nameBlock = new GUITextBlock(new RectTransform(new Vector2(1f, 0.07f), layout.RectTransform), "", font: GUIStyle.LargeFont, textAlignment: Alignment.Center);
            _idBlock = new GUITextBlock(new RectTransform(new Vector2(1f, 0.04f), layout.RectTransform), "", font: GUIStyle.SmallFont, textAlignment: Alignment.Center, textColor: Color.Gray);

            var spriteContainer = new GUIFrame(new RectTransform(new Vector2(1f, 0.75f), layout.RectTransform), style: null)
            {
                Color = Color.Black * 0.25f
            };
            var _ = new GUICustomComponent(new RectTransform(Vector2.One, spriteContainer.RectTransform),
                onDraw: (sb, comp) =>
                {
                    var sprite = _currentPrefab.Icon();
                    if (sprite == null) return;
                    Vector2 center = comp.Rect.Location.ToVector2() + comp.Rect.Size.ToVector2() * 0.5f;
                    float scale = Math.Min(
                        comp.Rect.Width / (float)sprite.SourceRect.Width,
                        comp.Rect.Height / (float)sprite.SourceRect.Height) * 0.85f;
                    sb.Draw(sprite.Texture, center, sprite.SourceRect, Color.White, 0f, new Vector2(sprite.SourceRect.Width * 0.5f, sprite.SourceRect.Height * 0.5f), scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                });
        }

        public void Show(Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            if (_container == null) return;
            _container.Visible = true;
            _currentPrefab = prefab;
            _nameBlock.Text = prefab.Name();
            _idBlock.Text = prefab.Identifier.Value;
        }

        public void Hide()
        {
            if (_container != null) _container.Visible = false;
        }

        public void Dispose()
        {
            _container?.Parent?.RemoveChild(_container);
            _currentPrefab = null!;
            GC.SuppressFinalize(this);
        }
    }
}