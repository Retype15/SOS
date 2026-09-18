// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#if DEBUG

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.Panels.PreviewPanel
{

    // MARK: Preview Tab
    //[AutoRegister("SOS.Tab.PreviewPanel", 10)]
    public class PreviewPanelTab : ISOSTab
    {
        public string Id => "SOS.Tab.PreviewPanel";

        private GUITextBlock _nameBlock = null!;
        private GUITextBlock _idBlock = null!;
        private Prefab _currentPrefab = null!;

        public bool CanHandle(Prefab prefab) => prefab is ItemPrefab || prefab is AfflictionPrefab;

        public void Init(GUIFrame container, GUIButton _)
        {
            var layout = new GUILayoutGroup(new RectTransform(Vector2.One, container.RectTransform)) { Stretch = true, AbsoluteSpacing = 10 };

            _nameBlock = new GUITextBlock(new RectTransform(new Vector2(1f, 0.07f), layout.RectTransform), "", font: GUIStyle.LargeFont, textAlignment: Alignment.Center);
            _idBlock = new GUITextBlock(new RectTransform(new Vector2(1f, 0.04f), layout.RectTransform), "", font: GUIStyle.SmallFont, textAlignment: Alignment.Center, textColor: Color.Gray);

            var spriteContainer = new GUIFrame(new RectTransform(new Vector2(1f, 0.75f), layout.RectTransform), style: null)
            {
                Color = Color.Black * 0.25f
            };
            var __ = new GUICustomComponent(new RectTransform(Vector2.One, spriteContainer.RectTransform),
                onDraw: (sb, comp) =>
                {
                    var sprite = _currentPrefab.Icon();
                    if (sprite == null) return;
                    Vector2 center = comp.Rect.Location.ToVector2() + comp.Rect.Size.ToVector2() * 0.5f;
                    float scale = Math.Min(
                        comp.Rect.Width / sprite.SourceRect.Width,
                        comp.Rect.Height / sprite.SourceRect.Height) * 0.85f;
                    sb.Draw(sprite.Texture, center, sprite.SourceRect, Color.White, 0f, new Vector2(sprite.SourceRect.Width * 0.5f, sprite.SourceRect.Height * 0.5f), scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
                });
        }

        public void Update(Prefab target)
        {
            _currentPrefab = target;
            _nameBlock.Text = target.Name();
            _idBlock.Text = target.Identifier.Value;
        }
    }
}

#endif