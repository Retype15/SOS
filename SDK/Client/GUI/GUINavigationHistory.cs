// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.GUI
{
    public sealed class GUINavigationHistory<T> : GUICustomComponent where T : notnull
    {
        public readonly List<T> _history = [];
        private bool _isNavigating = false;
        private readonly GUIButton _btnBack;
        private readonly GUIButton _btnForward;
        private readonly GUILayoutGroup _layoutGroup;

        public event Action<T?>? OnNavigateBack;
        public event Action<T?>? OnNavigateForward;
        public event Action? OnHistoryChanged;

        public bool CanNavigateBack => Index > 0;
        public bool CanNavigateForward => Index >= 0 && Index < _history.Count - 1;
        public IReadOnlyList<T> History => _history;
        public int Index { get; private set; }
        public int CurrentIndex => Index;
        public int Count => _history.Count;

        public Func<T?, RichString>? OnChangeToolTipBack;
        public Func<T?, RichString>? OnChangeToolTipForward;

        public T? PeekBack() => CanNavigateBack ? _history[Index - 1] : default;
        public T? PeekForward() => CanNavigateForward ? _history[Index + 1] : default;

        public GUINavigationHistory(RectTransform recT, IEnumerable<T>? history = null, int index = 0) : base(recT)
        {
            CanBeFocused = false;
            if (history != null) _history.AddRange(history);
            Index = index;

            _layoutGroup = new GUILayoutGroup(new RectTransform(Vector2.One, RectTransform), isHorizontal: true)
            {
                Stretch = true,
                AbsoluteSpacing = 4,
                CanBeFocused = false
            };

            _btnBack = new GUIButton(new RectTransform(new Vector2(0.5f, 1f), _layoutGroup.RectTransform), "", style: "GUIButtonToggleLeft")
            {
                ToolTip = $"{Texts.Get("sos.window.back", "Back").SetColor(Color.Gray)}\n{Texts.Get("sos.window.back.shortcuts", "Shortcuts:\n- Alt + Left Arrow\n- Backspace\n- Mouse 4")}".Rich(),
                OnClicked = (_, _) => { NavigateBack(); return true; },
            };
            if (_btnBack.Children.FirstOrDefault() is GUIImage imgB) imgB.SpriteEffects = Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally;

            _btnForward = new GUIButton(new RectTransform(new Vector2(0.5f, 1f), _layoutGroup.RectTransform), "", style: "GUIButtonToggleRight")
            {
                ToolTip = $"{Texts.Get("sos.window.forward", "Forward").SetColor(Color.Gray)}\n{Texts.Get("sos.window.forward.shortcuts", "Shortcuts:\n- Alt + Right Arrow\n- Shift + Backspace\n- Mouse 5")}".Rich(),
                OnClicked = (_, _) => { NavigateForward(); return true; },
            };

            UpdateButtonStates();
        }

        public void Push(T? item)
        {
            if (item == null || _isNavigating ||
                (Index >= 0 && Index < _history.Count && (_history[Index].Equals(item))))
                return;

            if (CanNavigateForward)
                _history.RemoveRange(Index + 1, _history.Count - Index - 1);

            _history.Remove(item);
            _history.Add(item);
            Index = _history.Count - 1;

            Logger.LogDebug($"[NavigationHistory] Pushed '{item}' to History. Index: {Index}, Count: {_history.Count}", level: LogLevel.Trace);
            OnHistoryChanged?.Invoke();
            UpdateButtonStates();
        }

        public void NavigateBack()
        {
            if (!CanNavigateBack) return;
            Index--;
            var prev = _history[Index];
            _isNavigating = true;
            OnNavigateBack?.Invoke(prev);
            _isNavigating = false;
            OnHistoryChanged?.Invoke();
            UpdateButtonStates();
        }

        public void NavigateForward()
        {
            if (!CanNavigateForward) return;
            Index++;
            var next = _history[Index];
            _isNavigating = true;
            OnNavigateForward?.Invoke(next);
            _isNavigating = false;
            OnHistoryChanged?.Invoke();
            UpdateButtonStates();
        }

        public void Clear()
        {
            _history.Clear();
            Index = -1;
            OnHistoryChanged?.Invoke();
            UpdateButtonStates();
        }

        public void SetIndex(int index)
        {
            if (index < -1 || index >= _history.Count) return;
            Index = index;
            OnHistoryChanged?.Invoke();
            UpdateButtonStates();
        }

        public void UpdateButtonStates()
        {
            _btnBack.Enabled = CanNavigateBack;
            _btnForward.Enabled = CanNavigateForward;
            if (OnChangeToolTipBack != null) _btnBack.ToolTip = OnChangeToolTipBack(PeekBack());
            if (OnChangeToolTipForward != null) _btnForward.ToolTip = OnChangeToolTipForward(PeekForward());
        }
    }
}