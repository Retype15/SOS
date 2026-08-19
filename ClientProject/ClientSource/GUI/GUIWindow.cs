// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.
// Code maded for AI, but revised and tested.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.GUI
{
    [Flags]
    public enum WindowButtons
    {
        None = 0,
        Minimize = 1 << 0,
        Maximize = 1 << 1,
        Close = 1 << 2,
        MinClose = Minimize | Close,
        MaxClose = Maximize | Close,
        All = Minimize | Maximize | Close
    }

    public enum WindowState
    {
        Normal,
        Maximized,
        Minimized
    }

    /// <summary>
    /// Standardized resizable window with a fixed top bar (toolbox, title, custom and system buttons)
    /// and a content area that fills the remaining vertical space.
    /// </summary>
    public class GUIWindow : GUIResizableFrame
    {
        public const int DefaultHeaderHeight = 42;

        private const int SystemButtonSize = 32;

        public GUIFrame TopBar { get; } = null!;
        public GUITextBlock Title { get; } = null!;
        public GUILayoutGroup ToolBox { get; } = null!;
        public GUILayoutGroup RightArea { get; } = null!;
        public GUILayoutGroup ControlBox { get; } = null!;
        public GUILayoutGroup SystemBox { get; } = null!;
        public GUIFrame ContentArea { get; } = null!;

        public GUIButton? MinimizeButton { get; }
        public GUIButton? MaximizeButton { get; }
        public GUIButton? CloseButton { get; }

        public Point NormalSize { get; private set; }
        public Point NormalOffset { get; private set; }

        public event Action? OnMinimize;
        public event Action? OnMaximize;
        public event Action? OnRestore;
        public event Action? OnClose;

        private WindowState _windowState = WindowState.Normal;
        private ResizeDirection _normalResizeDirections = ResizeDirection.All;

        public override bool CanMove => State != WindowState.Maximized;

        public new WindowState State
        {
            get => _windowState;
            set => SetState(value);
        }

        public GUIWindow(RectTransform rectT, LocalizedString titleText, string style = "InnerFrame", Color? color = null, WindowButtons buttons = WindowButtons.All)
            : base(rectT, style, color)
        {
            CanBeFocused = true;
            AllowedDirections = ResizeDirection.All;

            try
            {
                TopBar = new GUIFrame(new RectTransform(new Vector2(1f, 0f), RectTransform, Anchor.TopCenter)
                {
                    MinSize = new Point(0, DefaultHeaderHeight),
                    MaxSize = new Point(int.MaxValue, DefaultHeaderHeight)
                }, style: "GUIFrameBottom")
                {
                    CanBeFocused = false
                };

                Title = new GUITextBlock(new RectTransform(Vector2.One, TopBar.RectTransform), titleText, font: GUIStyle.LargeFont, textAlignment: Alignment.Center)
                {
                    CanBeFocused = false,
                    Wrap = false
                };

                ToolBox = new GUILayoutGroup(new RectTransform(new Vector2(0.32f, 0.8f), TopBar.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(10, 0) }, isHorizontal: true)
                {
                    Stretch = false,
                    AbsoluteSpacing = 5,
                    CanBeFocused = false
                };

                RightArea = new GUILayoutGroup(new RectTransform(new Vector2(0.2f, 0.8f), TopBar.RectTransform, Anchor.CenterRight) { AbsoluteOffset = new Point(10, 0) }, isHorizontal: true)
                {
                    Stretch = false,
                    RelativeSpacing = 0.05f,
                    ChildAnchor = Anchor.CenterRight,
                    CanBeFocused = false
                };

                SystemBox = new GUILayoutGroup(new RectTransform(new Vector2(0.4f, 1f), RightArea.RectTransform), isHorizontal: true, childAnchor: Anchor.CenterRight)
                {
                    Stretch = false,
                    RelativeSpacing = 0.05f,
                    CanBeFocused = false
                };

                ControlBox = new GUILayoutGroup(new RectTransform(new Vector2(0.6f, 1f), RightArea.RectTransform), isHorizontal: true, childAnchor: Anchor.CenterRight)
                {
                    Stretch = false,
                    RelativeSpacing = 0f,
                    CanBeFocused = false
                };

                if (buttons.HasFlag(WindowButtons.Close))
                {
                    CloseButton = CreateSystemButton("", "GUICancelButton", "sos.gen.close", "Close [Esc]", Close);
                }

                if (buttons.HasFlag(WindowButtons.Maximize))
                {
                    MaximizeButton = CreateSystemButton("M", "GUIButtonSmall", "sos.window.maximize", "Maximize", ToggleMaximize);
                }

                if (buttons.HasFlag(WindowButtons.Minimize))
                {
                    MinimizeButton = CreateSystemButton("-", "GUIButtonSmall", "sos.window.minimize", "Minimize", Minimize);
                }

                ContentArea = new GUIFrame(new RectTransform(new Point(0, 0), RectTransform, Anchor.TopLeft, isFixedSize: true)
                {
                    AbsoluteOffset = new Point(0, DefaultHeaderHeight)
                }, style: null);

                RectTransform.SizeChanged += ResizeContentArea;
                ResizeContentArea();
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] GUIWindow constructor failed\n{ex}", level: LogLevel.Error);
            }
        }

        private GUIButton CreateSystemButton(string text, string style, string tooltipKey, string tooltipFallback, Action onClick)
        {
            try
            {
                var button = new GUIButton(new RectTransform(new Point(SystemButtonSize, SystemButtonSize), SystemBox.RectTransform, isFixedSize: true), text, style: style)
                {
                    ToolTip = Texts.Get(tooltipKey, tooltipFallback).Value,
                    OnClicked = (_, _) =>
                    {
                        try
                        {
                            onClick();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebugError($"[SOS] GUIWindow system button action failed\n{ex}", level: LogLevel.Error);
                        }
                        return true;
                    }
                };
                return button;
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] GUIWindow.CreateSystemButton failed\n{ex}", level: LogLevel.Error);
                return null!;
            }
        }

        public void Open(WindowState forceState = WindowState.Normal)
        {
            Visible = true;
            State = forceState;
        }

        public void Minimize() => State = WindowState.Minimized;

        public void Maximize() => State = WindowState.Maximized;

        public void Restore() => State = WindowState.Normal;

        public void ToggleMaximize() => State = State == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        public void Close()
        {
            Visible = false;
            OnClose?.Invoke();
        }

        public override void Update(float deltaTime)
        {
            try
            {
                base.Update(deltaTime);
                if (!Visible || State == WindowState.Maximized) return;

                NormalSize = RectTransform.NonScaledSize;
                NormalOffset = RectTransform.AbsoluteOffset;
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] GUIWindow.Update failed\n{ex}", level: LogLevel.Error);
            }
        }

        private void SetState(WindowState newState)
        {
            if (_windowState == newState) return;

            try
            {
                if (_windowState == WindowState.Maximized && newState != WindowState.Maximized)
                {
                    ExitMaximized();
                }

                _windowState = newState;

                switch (_windowState)
                {
                    case WindowState.Minimized:
                        Visible = false;
                        UpdateSystemButtons();
                        OnMinimize?.Invoke();
                        break;
                    case WindowState.Maximized:
                        EnterMaximized();
                        UpdateSystemButtons();
                        OnMaximize?.Invoke();
                        break;
                    case WindowState.Normal:
                        Visible = true;
                        UpdateSystemButtons();
                        OnRestore?.Invoke();
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] GUIWindow.SetState failed\n{ex}", level: LogLevel.Error);
            }
        }

        private void EnterMaximized()
        {
            NormalSize = RectTransform.NonScaledSize;
            NormalOffset = RectTransform.AbsoluteOffset;
            _normalResizeDirections = AllowedDirections;

            AllowedDirections = ResizeDirection.None;
            RectTransform.NonScaledSize = RectTransform.ParentRect.Size;
            RectTransform.AbsoluteOffset = Point.Zero;
        }

        private void ExitMaximized()
        {
            AllowedDirections = _normalResizeDirections;
            RectTransform.NonScaledSize = NormalSize;
            RectTransform.AbsoluteOffset = NormalOffset;
        }

        private void UpdateSystemButtons()
        {
            if (MaximizeButton == null) return;

            bool isMaximized = State == WindowState.Maximized;
            MaximizeButton.Text = Texts.Get(isMaximized ? "sos.window.restore_btn" : "sos.window.maximize_btn", isMaximized ? "R" : "M");
            MaximizeButton.ToolTip = Texts.Get(isMaximized ? "sos.window.restore" : "sos.window.maximize", isMaximized ? "Restore" : "Maximize").Value;
        }

        private void ResizeContentArea()
        {
            if (ContentArea == null) return;

            try
            {
                int width = RectTransform.Rect.Width;
                int height = Math.Max(0, RectTransform.Rect.Height - DefaultHeaderHeight);
                ContentArea.RectTransform.NonScaledSize = new Point(width, height);
            }
            catch (Exception ex)
            {
                Logger.LogDebugError($"[SOS] GUIWindow.ResizeContentArea failed\n{ex}", level: LogLevel.Error);
            }
        }
    }
}