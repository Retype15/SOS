// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using Barotrauma;
using Barotrauma.LuaCs.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SOS
{
    public class GUIRecipeTracker : GUIFrame
    {
        // CONSTS
        private const ushort TIMECACHERESET = 60;

        private readonly Dictionary<FabricationRecipe, ItemUIRecipe> trackedRecipes = [];
        private readonly Dictionary<Identifier, int> cache = [];
        private ushort timeCache = 0;

        private readonly GUILayoutGroup contentLayout;
        private readonly GUITextBlock emptyLabel;
        private bool layoutDirty = false;
        private sealed class IngredientUI
        {
            public GUIFrame row;
            public GUIImage icon;
            public GUITextBlock text;

            public IngredientUI(GUIFrame row, GUIImage icon, GUITextBlock text)
            {
                this.row = row;
                this.icon = icon;
                this.text = text;
            }
        }

        private sealed class ItemUIRecipe
        {
            public GUIFrame itemRow;
            public GUIImage indicatorImage;
            public GUIImage? itemIcon;
            public GUITextBlock nameText;
            public Dictionary<FabricationRecipe.RequiredItem, IngredientUI> reqList;

            public ItemUIRecipe(GUIFrame itemRow, GUIImage indicatorImage, GUIImage? itemIcon, GUITextBlock nameText, Dictionary<FabricationRecipe.RequiredItem, IngredientUI> reqList)
            {
                this.itemRow = itemRow;
                this.indicatorImage = indicatorImage;
                this.itemIcon = itemIcon;
                this.nameText = nameText;
                this.reqList = reqList;
            }

            public void Destroy()
            {
                itemRow.RectTransform.Parent = null;
                itemRow.RemoveFromGUIUpdateList();
                foreach (var ing in reqList.Values)
                {
                    ing.row.RectTransform.Parent = null;
                    ing.row.RemoveFromGUIUpdateList();
                }
            }
        }

        private static ItemPrefab? GetItemPrefab() => ItemPrefab.Prefabs.FirstOrDefault();
        private static ItemPrefab? GetItemPrefab(string? itemString) => (itemString == null) ? GetItemPrefab() : ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == itemString);

        private static FabricationRecipe? GetFabricationRecipe(ItemPrefab itemPrefab) => itemPrefab.FabricationRecipes?.Values.FirstOrDefault();
        private static FabricationRecipe? GetFabricationRecipe(ItemPrefab itemPrefab, uint? recipeHash = null)
        {
            if (recipeHash == null) return GetFabricationRecipe(itemPrefab);
            return itemPrefab.FabricationRecipes?.Values.FirstOrDefault(rec => rec.RecipeHash == recipeHash);
        }

        public bool AddRecipe(FabricationRecipe? recipe)
        {
            bool added = recipe != null && !trackedRecipes.ContainsKey(recipe) && trackedRecipes.TryAdd(recipe, BuildUIRecipe(recipe));
            if (added)
            {
                layoutDirty = true;
                emptyLabel.Visible = false;
                this.Visible = true;
            }
            return added;
        }
        public bool AddRecipe(ItemPrefab? itemPrefab, uint? recipeHash = null)
        {
            if (itemPrefab == null) return false;
            var recipe = GetFabricationRecipe(itemPrefab, recipeHash);
            return AddRecipe(recipe);
        }
        public bool AddRecipe(string? itemString, uint? recipeHash = null)
        {
            if (string.IsNullOrEmpty(itemString)) return false;

            var itemPrefab = GetItemPrefab(itemString);
            return AddRecipe(itemPrefab, recipeHash);
        }

        public bool AddRecipes(IEnumerable<FabricationRecipe>? recipes)
        {
            bool response = false;
            if (recipes == null) return response;
            foreach (var recipe in recipes)
            {
                if (AddRecipe(recipe)) response = true;
            }
            return response;
        }
        public bool AddRecipes(ItemPrefab? itemPrefab) => AddRecipes(itemPrefab?.FabricationRecipes.Values);
        public bool AddRecipes(string? itemString)
        {
            if (string.IsNullOrEmpty(itemString)) return false;
            var itemPrefab = GetItemPrefab(itemString);
            return AddRecipes(itemPrefab);
        }

        public bool RemoveRecipe(FabricationRecipe? recipe)
        {
            if (recipe == null) return false;
            trackedRecipes.GetValueOrDefault(recipe)?.Destroy();
            layoutDirty |= trackedRecipes.Remove(recipe);
            if (trackedRecipes.Count == 0) emptyLabel.Visible = true;
            return layoutDirty;
        }
        public bool RemoveRecipe(ItemPrefab? itemPrefab, uint? recipeHash = null)
        {
            if (itemPrefab == null) return false;
            var recipe = GetFabricationRecipe(itemPrefab, recipeHash);
            return RemoveRecipe(recipe);
        }
        public bool RemoveRecipe(string? itemString, uint? recipeHash = null)
        {
            if (string.IsNullOrEmpty(itemString)) return false;

            var itemPrefab = GetItemPrefab(itemString);
            return RemoveRecipe(itemPrefab, recipeHash);
        }

        public bool RemoveRecipes(IEnumerable<FabricationRecipe>? recipes)
        {
            bool response = false;
            if (recipes == null) return response;
            foreach (var recipe in recipes)
            {
                if (RemoveRecipe(recipe)) response = true;
            }
            return response;
        }
        public bool RemoveRecipes(ItemPrefab? itemPrefab) => RemoveRecipes(itemPrefab?.FabricationRecipes.Values);
        public bool RemoveRecipes(string? itemString)
        {
            if (string.IsNullOrEmpty(itemString)) return false;
            var itemPrefab = GetItemPrefab(itemString);
            return RemoveRecipes(itemPrefab);
        }
        public bool RemoveRecipes()
        {
            foreach (var recipe in trackedRecipes.Values) recipe.Destroy();
            trackedRecipes.Clear();
            emptyLabel.Visible = true;
            layoutDirty = true;
            return true;
        }

        public bool ContainsRecipe(FabricationRecipe? recipe) => recipe != null && trackedRecipes.ContainsKey(recipe);
        public bool ContainsRecipe(ItemPrefab itemPrefab, uint? recipeHash = null)
        {
            var recipe = GetFabricationRecipe(itemPrefab, recipeHash);
            return ContainsRecipe(recipe);
        }
        public bool ContainsRecipe(string itemString, uint? recipeHash = null)
        {
            var itemPrefab = GetItemPrefab(itemString);
            if (itemPrefab == null) return false;
            return ContainsRecipe(itemPrefab, recipeHash);
        }

        public bool ContainsAnyRecipe(FabricationRecipe? recipe) => ContainsRecipe(recipe);
        public bool ContainsAnyRecipe(ItemPrefab? itemPrefab) => itemPrefab != null && itemPrefab!.FabricationRecipes.Values.Intersect(trackedRecipes.Keys).Any();
        public bool ContainsAnyRecipe(string itemString)
        {
            var itemPrefab = GetItemPrefab(itemString);
            return ContainsAnyRecipe(itemPrefab);
        }

        public bool AddOrRemoveRecipe(FabricationRecipe recipe) => ContainsRecipe(recipe) ? RemoveRecipe(recipe) : AddRecipe(recipe);
        public bool AddOrRemoveRecipe(ItemPrefab itemPrefab, uint? recipeHash = null)
        {
            var recipe = GetFabricationRecipe(itemPrefab, recipeHash);
            if (recipe == null) return false;
            return AddOrRemoveRecipe(recipe);
        }
        public bool AddOrRemoveRecipe(string itemString, uint? recipeHash = null)
        {
            var itemPrefab = GetItemPrefab(itemString);
            if (itemPrefab == null) return false;
            return AddOrRemoveRecipe(itemPrefab, recipeHash);
        }

        public static LocalizedString GetTrackOrUntrack(bool state) => state ? TextSOS.Get("sos.context.track", "Track to HUD") : TextSOS.Get("sos.context.untrack", "Remove from HUD");

        public LocalizedString GetStringTrackToHUD(FabricationRecipe recipe) => GetTrackOrUntrack(!ContainsRecipe(recipe));
        public LocalizedString GetStringTrackToHUD(ItemPrefab itemPrefab) => GetTrackOrUntrack(!ContainsAnyRecipe(itemPrefab));

        public void Clear() => RemoveRecipes();

        public List<ContextMenuOption> GetManageHudContextMenuOptions()
        {
            var options = new List<ContextMenuOption>
            {
                new(
                TextSOS.Get("sos.window.remove_all", "Remove All"),
                isEnabled: trackedRecipes.Count > 0,
                onSelected: Clear)
            };

            foreach (var recipe in trackedRecipes.Keys)
            {
                options.Add(new ContextMenuOption(
                    recipe.TargetItem.Name,
                    isEnabled: true,
                    onSelected: () => RemoveRecipe(recipe)));
            }

            return options;
        }

        private ItemUIRecipe BuildUIRecipe(FabricationRecipe recipe)
        {
            var itemRow = new GUIFrame(
                new RectTransform(new Vector2(1f, 0f), contentLayout.RectTransform) { MinSize = new Point(0, 22) },
                style: null)
            { CanBeFocused = false };

            var indicator = new GUIImage(
                new RectTransform(new Point(22, 22), itemRow.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(4, 0) },
                "ObjectiveIndicatorIncomplete", scaleToFit: true)
            { CanBeFocused = false };

            GUIImage? iconImage = null;
            var iconSprite = recipe.TargetItem.InventoryIcon;
            if (iconSprite != null)
            {
                iconImage = new GUIImage(
                    new RectTransform(new Point(16, 16), itemRow.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(28, 0) },
                    iconSprite, scaleToFit: true)
                { CanBeFocused = false, Color = recipe.TargetItem.InventoryIconColor };
            }

            var nameText = new GUITextBlock(
                new RectTransform(new Vector2(1f, 1f), itemRow.RectTransform) { AbsoluteOffset = new Point(48, 0) },
                recipe.TargetItem.Name.Value, font: GUIStyle.SmallFont, textColor: Color.Cyan)
            { CanBeFocused = false, ToolTip = TextSOS.Get("sos.hud.tracked_item_tooltip", "Currently tracked item.") };

            Dictionary<FabricationRecipe.RequiredItem, IngredientUI> reqList = [];
            foreach (var req in recipe.RequiredItems)
            {
                var ingRow = new GUIFrame(
                    new RectTransform(new Vector2(1f, 0f), contentLayout.RectTransform) { MinSize = new Point(0, 18) },
                    style: null)
                { CanBeFocused = false };

                var ingIconSprite = req.FirstMatchingPrefab?.InventoryIcon;
                GUIImage ingIcon;
                if (ingIconSprite != null)
                {
                    ingIcon = new GUIImage(
                        new RectTransform(new Point(16, 16), ingRow.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(28, 0) },
                        ingIconSprite, scaleToFit: true)
                    { CanBeFocused = false };
                }
                else
                {
                    ingIcon = new GUIImage(
                        new RectTransform(new Point(16, 16), ingRow.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(28, 0) },
                        style: null)
                    { CanBeFocused = false, Color = Color.Transparent };
                }

                var ingText = new GUITextBlock(
                    new RectTransform(new Vector2(1f, 1f), ingRow.RectTransform) { AbsoluteOffset = new Point(48, 0) },
                    "", font: GUIStyle.SmallFont)
                {
                    CanBeFocused = false,
                    ToolTip = TextSOS.Get("sos.hud.ingredient_tooltip", "Required ingredient. Shows how many you have in your inventory.")
                };

                reqList.Add(req, new IngredientUI(ingRow, ingIcon, ingText));
            }

            return new ItemUIRecipe(itemRow, indicator, iconImage, nameText, reqList);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Visible == false || Screen.Selected is not GameScreen) return;
            base.Draw(spriteBatch);

            if (timeCache > TIMECACHERESET) { timeCache = 0; cache.Clear(); }
            else timeCache++;
            foreach (var (_, itemUIRecipe) in trackedRecipes)
            {
                bool allComplete = true;
                foreach (var (req, ingUI) in itemUIRecipe.reqList)
                {
                    if (!cache.TryGetValue(req.ToIdentifier(), out var owned))
                    {
                        owned = GetPlayerCount(req);
                        cache[req.ToIdentifier()] = owned;
                    }

                    bool hasEnough = owned >= req.Amount;
                    if (!hasEnough) allComplete = false;

                    string? value = req.FirstMatchingPrefab?.Name.Value;
                    string name = value.IsNullOrEmpty() ? TextSOS.Get("sos.gen.unknown", "???").Value : value;
                    ingUI.text.Text = $"{name}: {owned}/{req.Amount}";
                    ingUI.text.TextColor = hasEnough ? Color.LightGreen : Color.Salmon;
                }

                var style = GUIStyle.GetComponentStyle(allComplete ? "ObjectiveIndicatorCompleted" : "ObjectiveIndicatorIncomplete");
                if (style != null) itemUIRecipe.indicatorImage.ApplyStyle(style);
            }

            if (layoutDirty) RecalculateSize();
        }

        public void Update() => AddToGUIUpdateList(order: -1);


        public GUIRecipeTracker(RectTransform rectT, string style = "", Color? color = null) : base(rectT, style, color)
        {
            contentLayout = new GUILayoutGroup(
                new RectTransform(new Vector2(1f, 1f), RectTransform) { AbsoluteOffset = new Point(8, 8) })
            {
                AbsoluteSpacing = 4,
                CanBeFocused = false
            };

            _ = new GUITextBlock(new RectTransform(new Vector2(1f, 0f), contentLayout.RectTransform) { MinSize = new Point(0, 22) },
                TextSOS.Get("sos.hud.tracking", "TRACKING:").Value, font: GUIStyle.SubHeadingFont, textColor: Color.Gold)
            {
                CanBeFocused = false,
                ToolTip = TextSOS.Get("sos.hud.tracking_tooltip", "Active crafting tracker. Shows required ingredients and amounts.")
            };

            emptyLabel = new GUITextBlock(
                new RectTransform(new Vector2(1f, 0f), contentLayout.RectTransform) { MinSize = new Point(0, 22) },
                TextSOS.Get("sos.hud.nothing_tracked", "Nothing tracked here."), font: GUIStyle.SmallFont, textColor: Color.Gray)
            { CanBeFocused = false };

            ClientConfig.Instance.OnTrackerVisibleValueChanged += RegisterIfChange;
        }

        public static GUIRecipeTracker InstantiateWithDefault()
        {
            return new(new RectTransform(new Point(280, 180), GUI.Canvas, Anchor.TopRight) { AbsoluteOffset = new Point(20, 150) }, style: "InnerFrame")
            {
                CanBeFocused = false,
                Color = Color.Black * 0.6f
            }
            ;
        }

        private void RegisterIfChange(ISettingBase trk) => this.Visible = ClientConfig.Instance.TrackerVisible;

        public void Destroy()
        {
            RemoveFromGUIUpdateList();
            RemoveRecipes();
            ClientConfig.Instance.OnTrackerVisibleValueChanged -= RegisterIfChange;
        }

        private static int GetPlayerCount(FabricationRecipe.RequiredItem req)
        {
            if (Character.Controlled?.Inventory == null) return 0;

            return Character.Controlled.Inventory.AllItems
                .Count(item => req.ItemPrefabs.Any(p => p.Identifier == item.Prefab.Identifier));
        }

        private void RecalculateSize()
        {
            contentLayout.Recalculate();
            int height = 8;
            int childCount = 0;
            foreach (var child in contentLayout.Children)
            {
                if (!child.Visible) continue;
                height += child.Rect.Height;
                childCount++;
            }
            height += contentLayout.AbsoluteSpacing * Math.Max(0, childCount - 1);
            height += 32;
            RectTransform.NonScaledSize = new Point(Rect.Width, Math.Max(height, 60));
            layoutDirty = false;
        }

        public static FabricationRecipe? TryGetRecipe(ItemPrefab itemPrefab, uint? recipeHash = null)
        {

            if (recipeHash != null) return itemPrefab.FabricationRecipes?.Values.FirstOrDefault(r => r.RecipeHash == recipeHash);
            return itemPrefab.FabricationRecipes?.Values.FirstOrDefault();
        }
        public static FabricationRecipe? TryGetRecipe(string itemString, uint? recipeHash = null)
        {
            if (!string.IsNullOrEmpty(itemString))
            {
                var targetPrefab = ItemPrefab.Prefabs.FirstOrDefault(p => p.Identifier.Value == itemString);
                if (targetPrefab != null)
                {

                    if (recipeHash != null) return targetPrefab.FabricationRecipes?.Values.FirstOrDefault(r => r.RecipeHash == recipeHash);
                    return targetPrefab.FabricationRecipes?.Values.FirstOrDefault();
                }
            }
            return null;
        }

        public string ToCsv(char itemSeparator = ',', char recipeSeparator = '|')
        {
            var parts = trackedRecipes.Keys
                .Select(r => $"{r.TargetItem.Identifier.Value}{itemSeparator}{r.RecipeHash}");
            return string.Join(recipeSeparator.ToString(), parts);
        }

        public void FromCsv(string raw, char itemSeparator = ',', char recipeSeparator = '|')
        {
            RemoveRecipes();
            if (string.IsNullOrEmpty(raw)) return;

            foreach (var entry in raw.Split(recipeSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = entry.Split(itemSeparator);
                if (parts.Length >= 2 && uint.TryParse(parts[1], out var hash))
                    AddRecipe(parts[0], hash);
                else if (parts.Length == 1) AddRecipe(parts[0]);
            }
        }
    }
}