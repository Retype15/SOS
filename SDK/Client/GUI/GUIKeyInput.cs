// Original solution extracted from LuaCsForBarotrauma.
// This file doesn't affect the GNU GPLv3 Licence.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using BGUI = Barotrauma.GUI;

namespace SOS.GUI
{
    public class GUIKeyInput : GUIButton
    {
        private static GUICustomComponent? InputListener;

        private KeyOrMouse? _value;
        public KeyOrMouse? Value
        {
            get => _value;
            set => ApplyValue(value);
        }
        public KeyOrMouse? DefaultValue;
        public Action<KeyOrMouse?>? OnChangedValue;

        public GUIKeyInput(RectTransform rectT, KeyOrMouse? value = null, Alignment textAlignment = Alignment.Center, string style = "", Color? color = null)
        : base(rectT, LocalizedString.EmptyString, textAlignment, string.IsNullOrWhiteSpace(style) ? "GUIKeyInput" : style, color)
        {
            Value = value;
            OnClicked = (_, _) =>
            {
                if (InputListener is not null)
                {
                    // Another button is active
                    return true;
                }
                CoroutineManager.Invoke(() =>
                {
                    CreateListener();
                }, 0f); // delay one frame for button inputs
                return true;
            };
        }

        private static void ClearListener()
        {
            InputListener?.Parent.RemoveChild(InputListener);
            InputListener = null;
        }

        void CreateListener()
        {
            ClearListener();
            InputListener = new GUICustomComponent(new RectTransform(Vector2.Zero, this.RectTransform),
                onUpdate: (deltaTime, component) =>
                {
                    var pressedKeys = PlayerInput.GetKeyboardState.GetPressedKeys();
                    if (pressedKeys?.Length > 0)
                    {
                        if (pressedKeys.Contains(Keys.Escape))
                        {
                            ClearListener();
                            return;
                        }

                        ApplyValue(pressedKeys.First());
                        return;
                    }

                    if (PlayerInput.PrimaryMouseButtonClicked() &&
                        (BGUI.MouseOn == null || BGUI.MouseOn is not GUIButton || BGUI.MouseOn.IsChildOf(this)))
                    {
                        Value = MouseButton.PrimaryMouse;
                        return;
                    }
                    else if (PlayerInput.SecondaryMouseButtonClicked())
                    {
                        Value = MouseButton.SecondaryMouse;
                        return;
                    }
                    else if (PlayerInput.MidButtonClicked())
                    {
                        Value = MouseButton.MiddleMouse;
                        return;
                    }
                    else if (PlayerInput.Mouse4ButtonClicked())
                    {
                        Value = MouseButton.MouseButton4;
                        return;
                    }
                    else if (PlayerInput.Mouse5ButtonClicked())
                    {
                        Value = MouseButton.MouseButton5;
                        return;
                    }
                    else if (PlayerInput.MouseWheelUpClicked())
                    {
                        Value = MouseButton.MouseWheelUp;
                        return;
                    }
                    else if (PlayerInput.MouseWheelDownClicked())
                    {
                        Value = MouseButton.MouseWheelDown;
                        return;
                    }
                });
        }

        void ApplyValue(KeyOrMouse? input)
        {
            _value = input;
            this.Text = input?.ToString() ?? Texts.Get("sos.guitextimput.not_defined", "Not Defined");
            CoroutineManager.Invoke(() => OnChangedValue?.Invoke(input), 0.1f);
            ClearListener();
        }
    }
}