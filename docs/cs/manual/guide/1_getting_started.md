# Getting Started {#getting_started}

## 1. Minimum requirements

The main integral project, and the only binary on which any mod should work is the `SDK` project(`SOS.SDK.dll`). It is thought to be loaded Client-Side only and compatible for any platform. It implements most of the logics, contracts, patterns and shared custom GUI components. You can download it from [HERE](https://github.com/Retype15/SOS/releases/latest/download/SOS.SDK.dll).

The specific requirements of the project depend on LuaCsForBarotrauma, see the [`LuaCsForBarotrauma` introduction guide](https://evilfactory.github.io/LuaCsForBarotrauma) to create compatible mods.

## 2. Project setup

> [!TIP]
> If your project is Lua, you can skip this step.

The easiest way to start a mod that depends on ours is to link the `SOS.SDK.dll` binary compiled for DEBUG to your project or mod. If you have followed the LuaCsForBarotrauma guide for [Assembly CSharp Mods](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/html/md_manual_assemblymod.html), we recommend including the binary in the /Refs folder and referencing from there, if it is not the case or you wish an optional reference using reflection you can ignore this step. (More information below and in the [Registration section](registration.html).)

<!-- Can the binary with the DEBUG flag [HERE](//). <!-- TODO: MISSING PATH HERE -->

## Example mod

Any mod that wishes to implement an information tab only needs to create a base class that inherits from the corresponding **ISOS\*** interface and implements the content it wants to add (e.g: A new section in the stat sections needs a public class that implements the `SOS.ISOSStatInfo` contract), and afterwards register it.

### Usage example

#### Csharp {#cs_example}

```csharp
using Barotrauma;

using Microsoft.Xna.Framework;
using SOS;
using SOS.GUI;

#pragma warning disable IDE0130
#pragma warning disable IDE0290

namespace MyMod;

[AutoRegister("MyMod.ArtieUsage", order: -1)]
public class UsageMessageStatInfo : ISOSStatInfo
{
    public void Draw(RectTransform rectT, Prefab prefab)
    {
        string? text = prefab switch
        {
            ItemPrefab item when !item.Name.IsNullOrEmpty() => $"Artie Dolittle is using {item.Name.Value}...",
            AfflictionPrefab aff when !aff.CauseOfDeathDescription.IsNullOrEmpty() => $"Artie Dolittle has {aff.CauseOfDeathDescription.Value}...",
            _ => null
        };
  
        if (text == null) return;

        using var l = new GUILayoutBuilder(rectT);
        l.Header("ARTIE STATUS", Color.Crimson);
        l.Text(text);
    }
}
```

#### Lua {#lua_example}

```lua
local API = LuaUserData.CreateStatic("SOS.API")

local HelloTab  = {}

local text      = nil

function HelloTab.CanHandle(prefab)
    return true
end

function HelloTab.Init(container, tabButton)
    tabButton.Text = "HELLO"
    tabButton.ToolTip = "There is a Hello Tab, say Hello!"

    text = GUI.TextBlock(GUI.RectTransform(Vector2(1, 0.1), container.RectTransform, 4), "Hello, World!", nil,
        GUI.Style.LargeFont, GUI.Alignment.Center)
end

function HelloTab.Update(prefab)
    if text ~= nil then
        local ok, name = pcall(function() return prefab.Name.Value end)
        if ok and name then text.Text = string.format("Hello, %s!", name) end
    end
end

function HelloTab.Dispose() -- Optionally, if you need to release resources, you can define the Dispose method, and it will be invoked when the object is deleted in C#.
    text = nil
end

API.RegisterTab(HelloTab, "MyMod.HelloTab", 105)
```

> [!NOTE]
> If in your specific case you cannot use hard references, you can use reflection and register generic classes or instances of them that implement the contracts indirectly, that is, methods by name and parameters. More information in the [Registration section](registration.html).

### Registration of optional binaries that use the SDK

If your mod does not strictly require SOS, we recommend configuring your ModConfig.xml to be able to use the conditional logic and activate the file that contains SOS logic only when the SOS mod is active.

#### Example

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModConfig>
    <FileGroup>  
        <Conditional Dependencies="S.O.S - Standard Operations Schematics" IsLoaded="true" /> 
        <!-- Assembly example --> 
        <Assembly File="%ModDir%/bin/Client/Linux/MyModSOS.dll" Target="Client" Platform="Linux" Optional="true" />
        <Assembly File="%ModDir%/bin/Client/OSX/MyModSOS.dll" Target="Client" Platform="OSX" Optional="true" />
        <Assembly File="%ModDir%/bin/Client/Windows/MyModSOS.dll" Target="Client" Platform="Windows" Optional="true" />
        <!-- Lua example -->
        <Lua File="%ModDir%/lua/Autorun/MyModSOS.lua" IsAutorun="true" Target="Client" Optional="true" />
        <!-- Example In-mermory file -->
        <Script File="%ModDir%/cs/MyModSOS.cs" Target="Client" Optional="true" />
    </FileGroup>
    <!-- Other files... -->
</ModConfig>
```

In summary, you only need to group in a *FileGroup* all the affected files, then define a *Conditional* item just as it is defined in the example. Then include the '*Optional*="true"' tag, and that's it.

## Other resources

**Next to: [SOS Modules](sos_modules.html)**

**Back to: [Introduction](index.html)**
