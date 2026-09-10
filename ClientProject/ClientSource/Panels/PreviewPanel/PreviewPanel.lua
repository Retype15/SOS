-- Copyright (c) 2026 Retype15
-- This file is licensed under the GNU GPLv3.
-- See the LICENSE file in the project root for details.

local API           = LuaUserData.CreateStatic("SOS.API")
local Texts         = LuaUserData.CreateStatic("SOS.Texts")
local Logger        = LuaUserData.CreateStatic("SOS.Logger")

local PreviewTab    = {}

local container     = nil
local nameBlock     = nil
local idBlock       = nil
local currentPrefab = nil

PreviewTab.Id       = "SOS.PreviewPanel"
PreviewTab.TabName  = Texts.Get("sos.tab.preview", "PREVIEW").Value
PreviewTab.ToolTip  = Texts.Get("sos.tab.preview_tooltip", "Shows the visual sprite of the selected prefab.").Value

function PreviewTab.CanHandle(prefab)
    if prefab == nil then return false end

    return LuaUserData.IsTargetType(prefab, "Barotrauma.ItemPrefab")
        or LuaUserData.IsTargetType(prefab, "Barotrauma.AfflictionPrefab")
end

local function GetPrefabIcon(pf)
    if pf == nil then return nil end
    if LuaUserData.IsTargetType(pf, "Barotrauma.ItemPrefab") then
        return pf.InventoryIcon or pf.Sprite
    elseif LuaUserData.IsTargetType(pf, "Barotrauma.AfflictionPrefab") then
        return pf.Icon
    end
    return nil
end

local function GetPrefabName(pf)
    if pf == nil then return "" end
    local ok, name = pcall(function() return pf.Name.Value end)
    if ok and name then return name end
    return tostring(pf.Identifier.Value or "")
end

function PreviewTab.Init(parentContainer)
    container = GUI.Frame(GUI.RectTransform(Vector2(1, 1), parentContainer.RectTransform), nil)
    container.Visible = false

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
        if currentPrefab == nil then return end

        local sprite = GetPrefabIcon(currentPrefab)
        if sprite == nil or sprite.Texture == nil then return end

        local rect = comp.Rect
        local center = Vector2(rect.X + rect.Width * 0.5, rect.Y + rect.Height * 0.5)
        local sourceRect = sprite.SourceRect
        local origin = Vector2(sourceRect.Width * 0.5, sourceRect.Height * 0.5)

        local scaleX = rect.Width / sourceRect.Width
        local scaleY = rect.Height / sourceRect.Height
        local scale = math.min(scaleX, scaleY) * 0.85

        sb.Draw(sprite.Texture, center, sourceRect, Color.White, 0, origin, scale, 0, 0)
    end)
end

function PreviewTab.Show(prefab)
    if container == nil or prefab == nil then return end
    container.Visible = true
    currentPrefab = prefab

    nameBlock.Text = GetPrefabName(prefab)
    idBlock.Text = tostring(prefab.Identifier.Value)
end

function PreviewTab.Hide()
    if container ~= nil then
        container.Visible = false
    end
end

function PreviewTab.Dispose()
    if container ~= nil and container.Parent ~= nil then
        container.Parent.RemoveChild(container)
        container = nil
    end
    currentPrefab = nil
end

API.RegisterTab(PreviewTab, "SOS.PreviewPanel", 100)

Logger.LogDebug("[SOS] PreviewPanel registered!", Color.LightGreen)

return PreviewTab
