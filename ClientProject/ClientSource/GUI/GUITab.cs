// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS.GUI
{

    public interface ITab
    {
        string TabName { get; }
        string TabTooltip => "";
        bool CanHandle(Prefab prefab);
        void Initialize(GUIComponent contentContainer);
        void Activate(Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary);
        void Deactivate();

        GUIButton CreateTabButton(string text, RectTransform parent, bool isActive, Action onClick)
        {
            Vector2 textSize = GUIStyle.SmallFont.MeasureString(text);
            int width = (int)textSize.X + 24;

            var tabBtn = new GUIButton(new RectTransform(new Point(width, 32), parent) { IsFixedSize = true }, text, style: "MainMenuNotificationButton") //MainMenuNotificationButton,
            {
                Selected = isActive,
                ToolTip = TextSOS.Get(TabTooltip, TabTooltip.Length > 0 ? TabTooltip : ""),
                OnClicked = (_, _) => { onClick(); return true; },
            };
            //tabBtn.ExBlink(3f, 0.5f, 1f, 0.5f).WaitFinish();

            return tabBtn;
        }
    }

    public class GUITabWidget : GUIFrame
    {
        private readonly GUILayoutGroup _verticalLayout;
        private readonly GUIListBox _buttonArea;
        private readonly GUIFrame _contentArea;
        private readonly List<ITab> _tabs = [];
        private ITab? _activeTab;

        private Prefab? _currentTarget;
        private Action<Prefab>? _onPrimary;
        private Action<Prefab>? _onSecondary;

        public GUITabWidget(RectTransform rectT) : base(rectT, style: null)
        {
            CanBeFocused = false;

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

        public void RegisterTab(ITab tab)
        {
            _tabs.Add(tab);
            tab.Initialize(_contentArea);
        }

        public void UpdateTabs(Prefab target, Action<Prefab> onPrimary, Action<Prefab> onSecondary)
        {
            _currentTarget = target;
            _onPrimary = onPrimary;
            _onSecondary = onSecondary;

            _buttonArea.Content.ClearChildren();
            List<ITab> validTabs = [.. _tabs.Where(t => t.CanHandle(target))];

            ITab? resolved = null;
            foreach (var uid in SOSController.Instance.TabHistory)
            {
                resolved = validTabs.FirstOrDefault(t => t.GetType().Name == uid);
                if (resolved != null) break;
            }

            if (resolved != null)
            {
                _activeTab = resolved;
            }
            else if (_activeTab == null || !validTabs.Contains(_activeTab))
            {
                _activeTab = validTabs.FirstOrDefault();
            }

            if (validTabs.Count > 1)
            {
                _buttonArea.Visible = true;
                _buttonArea.RectTransform.MinSize = new Point(0, 32);
                _buttonArea.RectTransform.MaxSize = new Point(int.MaxValue, 32);
                _contentArea.RectTransform.RelativeSize = new Vector2(1f, 0.92f);
                foreach (var tab in validTabs)
                {
                    _ = tab.CreateTabButton(tab.TabName, _buttonArea.Content.RectTransform, tab == _activeTab, () => SelectTab(tab));
                }
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

        public void SelectTab(ITab tab)
        {
            if (_activeTab == tab) return;
            _activeTab = tab;

            SOSController.Instance.PushTabHistory(tab.GetType().Name);

            if (_currentTarget != null && _onPrimary != null && _onSecondary != null)
            {
                UpdateTabs(_currentTarget, _onPrimary, _onSecondary);
            }
        }

        private void RefreshTabContent()
        {
            if (_currentTarget == null || _onPrimary == null || _onSecondary == null) return;

            foreach (var tab in _tabs)
            {
                if (tab == _activeTab)
                {
                    tab.Activate(_currentTarget, _onPrimary, _onSecondary);
                }
                else
                {
                    tab.Deactivate();
                }
            }
        }

        public void Clear()
        {
            _activeTab = null;
            _currentTarget = null;
            _buttonArea.Content.ClearChildren();
            foreach (var tab in _tabs) tab.Deactivate();
        }
    }
}