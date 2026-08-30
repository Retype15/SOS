// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.ComponentModel;
using Barotrauma;
using Microsoft.Xna.Framework;
using SOS.Prefabs;

namespace SOS
{
    #region AutoRegister

    [AttributeUsage(AttributeTargets.Class)]
    public class AutoRegisterAttribute : Attribute
    {
        public readonly string? Id;
        public readonly double Order;
        public readonly bool Active;
        public AutoRegisterAttribute(string? id = null, double order = 0.0, bool active = true)
        {
            Id = id;
            Order = order;
            Active = active;
        }
    }

    #endregion

    #region Interfaces

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IIdentifier
    {
        string Id => GetType().FullOrName();
    }

    [EditorBrowsable(EditorBrowsableState.Never), DefaultClass<TabDefaults>]
    public interface ITab : IIdentifier
    {
        string TabName { get; }

        string ToolTip => "";
        bool CanHandle(Prefab item);
        void Init(GUIComponent contentContainer);
        void Show(Prefab item, Action<Prefab> onPrimary, Action<Prefab> onSecondary);
        void Hide();

        GUIButton CreateTabButton(string tabName, RectTransform parent, bool isActive, Action onClick, string toolTip = "")
            => TabDefaults.CreateTabButton(tabName, parent, isActive, onClick, toolTip);
    }

    public interface ISOSStatSection
    {
        /// <summary>
        /// Analyzes and renders the specified prefab into the GUI list container.
        /// </summary>
        /// <param name="contentPanel">The GUI list container to draw into.</param>
        /// <param name="prefab">The prefab to analyze and render.</param>
        /// <param name="onPrimary">Primary click handler for the prefab.</param>
        /// <param name="onSecondary">Secondary click handler for the prefab.</param>
        /// <returns>True if this section has valid data to display; otherwise, false.</returns>
        bool Draw(GUIListBox contentPanel, Prefab prefab, Action<Prefab> onPrimary, Action<Prefab> onSecondary);
    }


    public interface ISOSTab : ITab;

    [DefaultClass<ConfigDefaults>]
    public interface ISOSConfig
    {
        void Load();
        void Save();
        void Reset() { }
        bool DrawSettings(GUIListBox container) => false;
    }

    public interface ISOSPrefabFilter
    {
        List<string> General { get; }
        List<string> Mod { get; }
        List<string> Category { get; }
        List<string> Tag { get; }
        List<string> Slot { get; }
        List<string> ID { get; }
        List<string> PrefabType { get; }
    }

    public interface ISOSPrefab
    {
        Type PrefabType { get; }
        string Header { get; }
        IEnumerable<Prefab> GetAll(ISOSPrefabFilter filter);

        [DefaultClass<PrefabDefaults>]
        List<ContextMenuOption> BuildContextOptions(Prefab prefab) => PrefabDefaults.BuildContextOptions(prefab);
    }

    [DefaultClass<WindowProfileDefaults>]
    public interface ISOSWindowProfile : IIdentifier, IDisposable
    {
        string DisplayName { get; }
        string Description { get; }
        ISOSConfig? ProfileConfig => null;
        void Init();
        void Update();
    }

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
            if (!toolTip.IsNullOrEmpty())
                tabBtn.ToolTip = toolTip;
            return tabBtn;
        }
    }

    internal sealed class ConfigDefaults
    {
        public static void Reset() { }
        public static bool DrawSettings(GUIListBox _) => false;
    }

    internal sealed class PrefabDefaults
    {
        public static List<ContextMenuOption> BuildContextOptions(Prefab prefab)
        {
            var options = new List<ContextMenuOption>
            {
                new(Texts.Get("sos.context.view_recipes", "View Recipes").Value, isEnabled: true, onSelected: () => API.Emit(CommKeys.SelectTarget, prefab))
            };

            string targetId = prefab.Identifier.Value;
            bool isFav = PrefabHelper.IsFavorite(targetId);
            string favText = isFav ? Texts.Get("sos.context.remove_favorite", "Remove from Favorites").Value : Texts.Get("sos.context.add_favorite", "Add to Favorites").Value;

            options.Add(new ContextMenuOption(favText, true, () => { PrefabHelper.ToggleFavorite(targetId); }));

            return options;
        }
    }

    internal sealed class WindowProfileDefaults
    {
        public static ISOSConfig? ProfileConfig => null;
    }

    #endregion
}
