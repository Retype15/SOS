// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    public interface IIdentifier
    {
        string Id => GetType().FullName ?? GetType().Name;
    }

    public interface IOrdenable
    {
        double Order { get; }
    }

    public interface IIdentifierOrdenable : IIdentifier, IOrdenable;

    public interface IBaseStatSection
    {
        bool Analyze(Prefab item);
        void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary);
    }

    public interface ISOSStatSection : IIdentifierOrdenable, IBaseStatSection;

    public interface ITab : IIdentifierOrdenable
    {
        string TabName { get; }
        [FallbackMethod(typeof(TabDefaults), nameof(TabDefaults.ToolTip))]
        string? ToolTip => null;
        bool CanHandle(Prefab item);
        void Init(GUIComponent contentContainer);
        void Show(Prefab item, Action<Prefab> onPrimary, Action<Prefab> onSecondary);
        void Hide();

        [FallbackMethod(typeof(TabDefaults), nameof(TabDefaults.CreateTabButton))]
        GUIButton CreateTabButton(string tabName, RectTransform parent, bool isActive, Action onClick, string? toolTip = null)
        {
            Vector2 textSize = GUIStyle.SmallFont.MeasureString(tabName);
            int width = (int)textSize.X + 24;
            var tabBtn = new GUIButton(new RectTransform(new Point(width, 32), parent) { IsFixedSize = true }, tabName, style: "MainMenuNotificationButton")
            {
                Selected = isActive,
                OnClicked = (_, _) => { onClick(); return true; },
            };
            if (toolTip != null && toolTip.Length > 0)
                tabBtn.ToolTip = toolTip;
            return tabBtn;
        }
    }

    public static class TabDefaults
    {
        public static string? ToolTip => null;
        public static GUIButton CreateTabButton(string tabName, RectTransform parent, bool isActive, Action onClick, string? toolTip = null)
        {
            Vector2 textSize = GUIStyle.SmallFont.MeasureString(tabName);
            int width = (int)textSize.X + 24;
            var tabBtn = new GUIButton(new RectTransform(new Point(width, 32), parent) { IsFixedSize = true }, tabName, style: "MainMenuNotificationButton")
            {
                Selected = isActive,
                OnClicked = (_, _) => { onClick(); return true; },
            };
            if (toolTip != null && toolTip.Length > 0)
                tabBtn.ToolTip = toolTip;
            return tabBtn;
        }
    }

    public interface ISOSCenterTab : ITab;
}

