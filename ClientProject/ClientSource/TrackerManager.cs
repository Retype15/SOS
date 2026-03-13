// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace SOS
{
    public class TrackerManager
    {
        public ItemPrefab? TrackedItem { get; private set; }
        public FabricationRecipe? TrackedRecipe { get; private set; }

        private GUIFrame? hudFrame;
        private GUILayoutGroup? contentLayout;
        private readonly List<GUITextBlock> ingredientTexts = new List<GUITextBlock>();

        public void SetTrackedItem(ItemPrefab? item, FabricationRecipe? recipe = null)
        {
            if (TrackedItem == item && TrackedRecipe == recipe) return;

            TrackedItem = item;

            TrackedRecipe = recipe ?? item?.FabricationRecipes?.Values.FirstOrDefault();

            if (hudFrame != null)
            {
                if (hudFrame.RectTransform != null)
                {
                    hudFrame.RectTransform.Parent = null;
                }
                hudFrame = null;
            }

            ingredientTexts.Clear();
        }

        public void UpdateHUD()
        {
            if (TrackedItem == null || TrackedRecipe == null) return;
            if (Screen.Selected is not GameScreen) return;

            if (hudFrame == null) CreateHUD();

            int i = 0;
            foreach (var req in TrackedRecipe.RequiredItems)
            {
                if (i >= ingredientTexts.Count) break;

                int owned = GetPlayerCount(req);
                bool hasEnough = owned >= req.Amount;

                string name = req.FirstMatchingPrefab?.Name.Value ?? TextSOS.Get("sos.gen.unknown", "???").Value;
                ingredientTexts[i].Text = $"{(hasEnough ? "•" : "○")} {name}: {owned}/{req.Amount}";
                ingredientTexts[i].TextColor = hasEnough ? Color.LightGreen : Color.Salmon;
                i++;
            }

            hudFrame?.AddToGUIUpdateList();
        }

        private void CreateHUD()
        {
            hudFrame = new GUIFrame(new RectTransform(new Point(280, 180), GUI.Canvas, Anchor.TopRight) { AbsoluteOffset = new Point(20, 150) }, style: "InnerFrame")
            {
                CanBeFocused = false,
                Color = Color.Black * 0.6f
            };

            contentLayout = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), hudFrame.RectTransform, Anchor.Center))
            {
                AbsoluteSpacing = 4,
                CanBeFocused = false
            };

            // enc
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.2f), contentLayout.RectTransform),
                TextSOS.Get("sos.hud.tracking", "TRACKING:").Value, font: GUIStyle.SubHeadingFont, textColor: Color.Gold)
            { CanBeFocused = false };

            new GUITextBlock(new RectTransform(new Vector2(1f, 0.15f), contentLayout.RectTransform),
                TrackedItem?.Name.Value ?? "", font: GUIStyle.SmallFont, textColor: Color.Cyan)
            { CanBeFocused = false };

            ingredientTexts.Clear();
            foreach (var req in TrackedRecipe.RequiredItems)
            {
                var text = new GUITextBlock(new RectTransform(new Vector2(1f, 0.12f), contentLayout.RectTransform),
                    "", font: GUIStyle.SmallFont)
                { CanBeFocused = false };
                ingredientTexts.Add(text);
            }

            int finalHeight = 80 + (TrackedRecipe.RequiredItems.Length * 20);
            hudFrame.RectTransform.NonScaledSize = new Point(hudFrame.Rect.Width, finalHeight);
        }

        private int GetPlayerCount(FabricationRecipe.RequiredItem req)
        {
            if (Character.Controlled?.Inventory == null) return 0;

            return Character.Controlled.Inventory.AllItems
                .Count(item => req.ItemPrefabs.Any(p => p.Identifier == item.Prefab.Identifier));
        }
    }
}