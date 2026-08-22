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

    [EditorBrowsable(EditorBrowsableState.Never), DefaultClass<TabDefaults>]
    public interface ITab : IIdentifierOrdenable
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

    public interface ISOSStatSection : IIdentifierOrdenable, IBaseStatSection;


    public interface ISOSTab : ITab;

    [DefaultClass<ConfigDefaults>]
    public interface ISOSConfig : IIdentifierOrdenable
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

    public interface ISOSPrefab : IIdentifierOrdenable
    {
        Type PrefabType { get; }
        string Header { get; }
        IEnumerable<Prefab> GetAll(ISOSPrefabFilter filter);

        [DefaultClass<PrefabDefaults>]
        List<ContextMenuOption> BuildContextOptions(Prefab prefab) => PrefabDefaults.BuildContextOptions(prefab);
    }

    public interface ISOSWindowProfile : IIdentifierOrdenable, IDisposable
    {
        string DisplayName => Texts.Get($"{Id}.DisplayName", Id).Value;
        string Description => Texts.Get($"{Id}.Description", "").Value;
        Sprite? ProfileIcon => null;
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

            if (API.GetConfig("SOS.Core") is IHaveFavoritedItems config)
            {

                var favoritedItems = config.FavoritedItems;
                string targetId = prefab.Identifier.Value;
                bool isFav = favoritedItems.Contains(targetId);
                string favText = isFav ? Texts.Get("sos.context.remove_favorite", "Remove from Favorites").Value : Texts.Get("sos.context.add_favorite", "Add to Favorites").Value;

                options.Add(new ContextMenuOption(favText, true, () =>
                {
                    if (isFav) favoritedItems.Remove(targetId);
                    else favoritedItems.Add(targetId);

                    API.Emit(CommKeys.RefreshSearch);
                }));
            }

            return options;
        }
    }

    #endregion

    #region balls

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHaveFavoritedItems
    {
        HashSet<string> FavoritedItems { get; }
    }

    #endregion
}
