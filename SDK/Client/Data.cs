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
        public AutoRegisterAttribute(string? id = null, double order = 0.0)
        {
            Id = id;
            Order = order;
        }
    }

    #endregion

    #region Interfaces

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IIdentifier
    {
        string Id => GetType().FullOrName();
    }

    public interface ISingleton<out T> where T : class, new()
    {
        private static T? instance;
        public static T Instance => instance ??= new();
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
        bool Analyze(Prefab item);
        void Draw(GUIListBox contentPanel, Action<Prefab> onPrimary, Action<Prefab> onSecondary);
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

    public interface ISOSWindowProfile : IIdentifier, IDisposable
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

            string targetId = prefab.Identifier.Value;
            bool isFav = PrefabHelper.IsFavorite(targetId);
            string favText = isFav ? Texts.Get("sos.context.remove_favorite", "Remove from Favorites").Value : Texts.Get("sos.context.add_favorite", "Add to Favorites").Value;

            options.Add(new ContextMenuOption(favText, true, () => { PrefabHelper.ToggleFavorite(targetId); }));

            return options;
        }
    }

    #endregion
}
