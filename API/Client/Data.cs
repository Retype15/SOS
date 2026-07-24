// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.ComponentModel;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    #region AutoRegister

    public interface IAutoRegister;

    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegisterAttribute : Attribute;

    #endregion

    #region Interfaces

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IIdentifier
    {
        string Id => GetType().FullName ?? GetType().Name;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IOrdenable
    {
        [DefaultClass<IdentifierOrdenableDefaults>]
        double Order => 0;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IIdentifierOrdenable : IIdentifier, IOrdenable;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IBaseStatSection
    {
        bool Analyze(Prefab item);
        void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [DefaultClass<TabDefaults>]
    public interface ITab : IIdentifierOrdenable
    {
        string TabName { get; }

        string ToolTip => "";
        bool CanHandle(Prefab item);
        void Init(GUIComponent contentContainer);
        void Show(Prefab item, Action<Prefab> onPrimary, Action<Prefab> onSecondary);
        void Hide();

        GUIButton CreateTabButton(string tabName, RectTransform parent, bool isActive, Action onClick, string toolTip = "")
        {
            Vector2 textSize = GUIStyle.SmallFont.MeasureString(tabName);
            int width = (int)textSize.X + 24;
            var tabBtn = new GUIButton(new RectTransform(new Point(width, 32), parent) { IsFixedSize = true }, tabName, style: "MainMenuNotificationButton")
            {
                Selected = isActive,
                OnClicked = (_, _) => { onClick(); return true; },
            };
            if (toolTip.IsNullOrEmpty())
                tabBtn.ToolTip = toolTip;
            return tabBtn;
        }
    }

    public interface ISOSStatSection : IIdentifierOrdenable, IBaseStatSection;


    public interface ISOSCenterTab : ITab;

    #endregion

    #region Default Proxy Classes

    internal sealed class IdentifierOrdenableDefaults
    {
        private IdentifierOrdenableDefaults() { }
        public static double Order => 0;
    }

    internal sealed class TabDefaults
    {
        private TabDefaults() { }
        public static string ToolTip => "";
        public static GUIButton CreateTabButton(string tabName, RectTransform parent, bool isActive, Action onClick, string toolTip = "")
        {
            Vector2 textSize = GUIStyle.SmallFont.MeasureString(tabName);
            int width = (int)textSize.X + 24;
            var tabBtn = new GUIButton(new RectTransform(new Point(width, 32), parent) { IsFixedSize = true }, tabName, style: "MainMenuNotificationButton")
            {
                Selected = isActive,
                OnClicked = (_, _) => { onClick(); return true; },
            };
            if (toolTip.IsNullOrEmpty())
                tabBtn.ToolTip = toolTip;
            return tabBtn;
        }
    }

    #endregion
}

