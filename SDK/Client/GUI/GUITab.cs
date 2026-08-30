// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.GUI
{
    public class GUITab<T> : GUIFrame, IDisposable
    {
        private readonly GUILayoutGroup _verticalLayout;
        private readonly GUIListBox _buttonArea;
        private readonly GUIFrame _contentArea;
        private readonly List<ITab<T>> tabs = [];
        public ITab<T>? ActiveTab { get; private set; }

        public Action<ITab<T>>? OnTabSelected;

        private T? _currentTarget;
        public Action<T> OnPrimary;
        public Action<T> OnSecondary;

        public GUITab(RectTransform rectT, Action<T> onPrimary, Action<T> onSecondary) : base(rectT, style: null)
        {
            CanBeFocused = false;
            OnPrimary = onPrimary;
            OnSecondary = onSecondary;

            _verticalLayout = new GUILayoutGroup(new RectTransform(Vector2.One, RectTransform))
            {
                Stretch = true,
                CanBeFocused = false
            };

            _buttonArea = new GUIListBox(new RectTransform(new Vector2(1f, 0.08f), _verticalLayout.RectTransform), isHorizontal: true, style: null)
            {
                Spacing = 5,
                Padding = new Vector4(5, 0, 5, 0),
                CanBeFocused = true
            };
            _buttonArea.RectTransform.MinSize = new Point(0, 32);
            _buttonArea.RectTransform.MaxSize = new Point(int.MaxValue, 32);

            _contentArea = new GUIFrame(new RectTransform(new Vector2(1f, 0.92f), _verticalLayout.RectTransform), style: null)
            {
                CanBeFocused = false
            };
        }

        public void RegisterTab(ITab<T> tab)
        {
            try
            {
                tab.Init(_contentArea);
                tabs.Add(tab);
            }
            catch (Exception ex)
            {
                Logger.LogReleaseError(ex.Message);
                Logger.LogDebugError(ex.StackTrace ?? ex.Message);
            }
        }

        public void UpdateTabs(T target)
        {
            _currentTarget = target;

            _buttonArea.Content.ClearChildren();
            List<ITab<T>> validTabs = [.. tabs.Where(t => t.CanHandle(target))];

            if (ActiveTab == null || !validTabs.Contains(ActiveTab))
                ActiveTab = validTabs.FirstOrDefault();

            if (validTabs.Count > 1)
            {
                _buttonArea.Visible = true;
                _buttonArea.RectTransform.MinSize = new Point(0, 32);
                _buttonArea.RectTransform.MaxSize = new Point(int.MaxValue, 32);
                _contentArea.RectTransform.RelativeSize = new Vector2(1f, 0.92f);

                foreach (var tab in validTabs)
                    _ = tab.CreateTabButton(tab.TabName, _buttonArea.Content.RectTransform, tab == ActiveTab, () => SelectTab(tab), tab.ToolTip);

                _buttonArea.RecalculateChildren();
            }
            else
            {
                _buttonArea.Visible = false;
                _buttonArea.RectTransform.MinSize = Point.Zero;
                _buttonArea.RectTransform.MaxSize = Point.Zero;
                _contentArea.RectTransform.RelativeSize = Vector2.One;
            }

            _verticalLayout.Recalculate();
            RefreshTabContent();
        }

        public bool TrySelectTab(string tabId)
        {
            if (_currentTarget == null) return false;
            var tab = tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab == null) return false;
            if (!tab.CanHandle(_currentTarget)) return false;
            return SelectTab(tab);
        }

        public bool TrySelectTab(ITab<T> tab)
        {
            if (!tabs.Contains(tab)) return false;
            if (_currentTarget != null && !tab.CanHandle(_currentTarget)) return false;
            return SelectTab(tab);
        }

        public bool SelectTab(ITab<T> tab)
        {
            if (ActiveTab == tab) return true;

            ActiveTab = tab;
            OnTabSelected?.Invoke(tab);
            if (_currentTarget != null)
            {
                var validTabs = tabs.Where(t => t.CanHandle(_currentTarget));
                _buttonArea.Content.ClearChildren();
                if (validTabs.Count() > 1)
                {
                    foreach (var t in validTabs)
                        _ = t.CreateTabButton(t.TabName, _buttonArea.Content.RectTransform, t == ActiveTab, () => SelectTab(t), t.ToolTip);
                    _buttonArea.RecalculateChildren();
                }
                RefreshTabContent();
            }
            else
            {
                RefreshTabContent();
            }
            return true;
        }

        private void RefreshTabContent()
        {
            if (_currentTarget == null) return;

            foreach (var tab in tabs)
            {
                if (tab == ActiveTab)
                    tab.Show(_currentTarget, OnPrimary, OnSecondary);
                else
                    tab.Hide();
            }
        }

        public void Dispose()
        {
            _buttonArea.Content.ClearChildren();
            foreach (var tab in tabs)
                if (tab is IDisposable d) d.Dispose();
            tabs.Clear();
            GC.SuppressFinalize(this);
        }

        ~GUITab() => Dispose();
    }
}