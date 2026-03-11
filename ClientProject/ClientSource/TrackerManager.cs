// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

using Barotrauma;
using Microsoft.Xna.Framework;

namespace SOS
{
    public class TrackerManager
    {
        public ItemPrefab? TrackedItem { get; private set; }
        public FabricationRecipe? TrackedRecipe { get; private set; }
        private GUIFrame? hudFrame;

        public void SetTrackedItem(ItemPrefab? item, FabricationRecipe? recipe = null)
        {
            TrackedItem = item;
            TrackedRecipe = recipe ?? item?.FabricationRecipes?.Values.FirstOrDefault();

            if (item == null) { hudFrame = null; }
        }

        public void UpdateHUD()
        {
            if (TrackedItem == null || TrackedRecipe == null) return;
            if (Screen.Selected is not GameScreen) return;

            if (hudFrame == null)
            {
                hudFrame = new GUIFrame(new RectTransform(new Point(250, 150), GUI.Canvas, Anchor.TopRight) { AbsoluteOffset = new Point(20, 150) }, style: "InnerFrame")
                {
                    CanBeFocused = false,
                    Color = Color.Black * 0.5f
                };
            }

            hudFrame.ClearChildren();
            var layout = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.9f), hudFrame.RectTransform, Anchor.Center)) { AbsoluteSpacing = 2, CanBeFocused = false };

            new GUITextBlock(new RectTransform(new Vector2(1f, 0.2f), layout.RectTransform), TextSOS.Get("sos.hud.tracking", "TRACKING:"), font: GUIStyle.SubHeadingFont, textColor: Color.Gold) { CanBeFocused = false };
            new GUITextBlock(new RectTransform(new Vector2(1f, 0.2f), layout.RectTransform), TrackedItem.Name.Value, font: GUIStyle.SmallFont, textColor: Color.Cyan) { CanBeFocused = false };

            foreach (var req in TrackedRecipe.RequiredItems)
            {
                int owned = GetPlayerCount(req);
                bool hasEnough = owned >= req.Amount;

                var row = new GUILayoutGroup(new RectTransform(new Vector2(1f, 0.15f), layout.RectTransform), isHorizontal: true) { CanBeFocused = false };
                new GUITextBlock(new RectTransform(Vector2.One, row.RectTransform),
                    $"{(hasEnough ? "(OK)" : "(  )")} {req.FirstMatchingPrefab?.Name.Value}: {owned}/{req.Amount}",
                    font: GUIStyle.SmallFont,
                    textColor: hasEnough ? Color.LightGreen : Color.LightPink)
                { CanBeFocused = false };
            }

            hudFrame.AddToGUIUpdateList();
        }

        private int GetPlayerCount(FabricationRecipe.RequiredItem req)
        {
            if (Character.Controlled == null) return 0;
            int count = 0;

            foreach (var item in Character.Controlled.Inventory.AllItems)
            {
                if (req.ItemPrefabs.Any(p => p.Identifier == item.Prefab.Identifier))
                {
                    count += 1;
                }
            }
            return count;
        }
    }

}
