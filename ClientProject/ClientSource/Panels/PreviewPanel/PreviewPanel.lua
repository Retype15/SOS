-- Copyright (c) 2026 Retype15
-- This file is licensed under the GNU GPLv3.
-- See the LICENSE file in the project root for details.

local API          = LuaUserData.CreateStatic("SOS.API")
local Logger       = LuaUserData.CreateStatic("SOS.Logger")
local PrefabHelper = LuaUserData.CreateStatic("SOS.Prefabs.PrefabHelper")

local PreviewTab   = {}

local nameBlock    = nil
local idBlock      = nil
local sprite       = nil
local spriteColor  = Color.White

function PreviewTab.CanHandle(prefab)
    if prefab == nil then return false end

    return LuaUserData.IsTargetType(prefab, "Barotrauma.ItemPrefab")
        or LuaUserData.IsTargetType(prefab, "Barotrauma.AfflictionPrefab")
end

function PreviewTab.Init(container, tabButton)
    local layout = GUI.LayoutGroup(GUI.RectTransform(Vector2(1, 1), container.RectTransform), false)
    layout.Stretch = true
    layout.AbsoluteSpacing = 10

    nameBlock = GUI.TextBlock(GUI.RectTransform(Vector2(1, 0.07), layout.RectTransform), "", nil, GUI.GUIStyle.LargeFont,
        GUI.Alignment.Center)

    idBlock = GUI.TextBlock(GUI.RectTransform(Vector2(1, 0.04), layout.RectTransform), "", nil, GUI.GUIStyle.SmallFont,
        GUI.Alignment.Center)
    idBlock.TextColor = Color.Gray

    local spriteContainer = GUI.Frame(GUI.RectTransform(Vector2(1, 0.75), layout.RectTransform), nil)
    spriteContainer.Color = Color(0, 0, 0, 64)

    GUI.CustomComponent(GUI.RectTransform(Vector2(1, 1), spriteContainer.RectTransform), function(sb, comp)
        if sprite == nil or sprite.Texture == nil then return end

        local rect = comp.Rect
        local center = Vector2(rect.X + rect.Width * 0.5, rect.Y + rect.Height * 0.5)
        local sourceRect = sprite.SourceRect
        local origin = Vector2(sourceRect.Width * 0.5, sourceRect.Height * 0.5)

        local scaleX = rect.Width / sourceRect.Width
        local scaleY = rect.Height / sourceRect.Height
        local scale = math.min(scaleX, scaleY) * 0.85

        sb.Draw(sprite.Texture, center, sourceRect, spriteColor, 0, origin, scale, 0, 0)
    end)
end

function PreviewTab.Update(prefab)
    nameBlock.Text = PrefabHelper.GetNameOrFallback(prefab)
    idBlock.Text = tostring(prefab.Identifier.Value)
    sprite = PrefabHelper.GetIcon(prefab)
    spriteColor = PrefabHelper.GetIconColor(prefab) or Color.White
end

function PreviewTab.Dispose()
    sprite = nil
    nameBlock = nil
    idBlock = nil
end

API.RegisterTab(PreviewTab, "SOS.Tab.PreviewPanel", 100)

Logger.LogDebug("[SOS] PreviewPanel registered!", Color.LightGreen)
