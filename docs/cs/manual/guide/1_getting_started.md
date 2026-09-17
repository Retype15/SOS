# Getting Started {#getting_started}

## 1. Requisitos mínimos

El proyecto integral principal, y el único binario sobre el que deberá trabajar cualquier mod es el proyecto `SDK`(`SOS.SDK.dll`). Está pensado para ser cargado Client-Side only y compatible para cualquier plataforma. Este implementa la mayoría de lógicas, contratos, patrones y componentes GUI personalizados compartidos. Puede descargarlo desde [AQUÍ](https://github.com/Retype15/SOS/releases/latest/download/SOS.SDK.dll).

Requisitos específicos del proyecto dependen de LuaCsForBarotrauma, véase la [guía de introducción de `LuaCsForBarotrauma`](https://evilfactory.github.io/LuaCsForBarotrauma) para crear mods compatibles.

## 2. Setup del proyecto

> [!TIP]
> Si su proyecto es Lua, puede saltar este paso.

La forma más fácil de iniciar un mod que dependa del nuestro es linkear el binario `SOS.SDK.dll` compilado para DEBUG a su proyecto o mod. Si ha seguido la guía de LuaCsForBarotrauma para [Assembly CSharp Mods](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/html/md_manual_assemblymod.html), recomendamos incluir el binario en la carpeta /Refs y referenciar desde ahí , si no es el caso o desea una referencia opcional usando reflexión puede ignorar este paso. (Más información a continuación y en la [sección Registration](registration.html).)

Puede el binario con la flag DEBUG [AQUÍ](//). <!-- TODO: FALTA RUTA AQUÍ -->

## Example mod

Cualquier mod que desee implementar una pestaña de información solamente necesita crear una clase base que herede de la interfaz **ISOS\*** correspondiente e implemente el contenido que quiera añadir (e.g: Una nueva sección en los stat sections necesita una clase pública que implemente el contrato `SOS.ISOSStatInfo`), y posteriormente registrarla.

### Ejemplo de uso

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

HelloTab.Id     = "MyMod.HelloTab"

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

function HelloTab.Dispose() -- Opcionalmente, si necesita liberar recursos, puede definir el método Dispose, y será invocado cuando se elimine el objeto en C#.
    text = nil
end

API.RegisterTab(HelloTab, HelloTab.Id, 105)
```

> [!NOTE]
> Si en su caso específico no puede usar referencias duras, puede usar reflexion y registrar clases genéricas o instancias de las mismas que implementen de forma indirecta los contratos, o sea métodos por nombre y parámetros. Más información en la [sección Registration](registration.html).

### Registro de binarios opcionales que usen el SDK

Si su mod no requiere estrictamente de SOS, recomendamos configurar su ModConfig.xml para poder usar la lógica condicional y activar el archivo que contiene lógica SOS sólo cuando el mod SOS está activo.

#### Ejemplo

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModConfig>
    <FileGroup>  
        <Conditional Dependencies="S.O.S - Standard Operations Schematics" IsLoaded="true" /> 
        <!-- Ejemplo ensamblado --> 
        <Assembly File="%ModDir%/bin/Client/Linux/MyModSOS.dll" Target="Client" Platform="Linux" Optional="true" />
        <Assembly File="%ModDir%/bin/Client/OSX/MyModSOS.dll" Target="Client" Platform="OSX" Optional="true" />
        <Assembly File="%ModDir%/bin/Client/Windows/MyModSOS.dll" Target="Client" Platform="Windows" Optional="true" />
        <!-- Ejemplo Lua -->
        <Lua File="%ModDir%/lua/Autorun/MyModSOS.lua" IsAutorun="true" Target="Client" Optional="true" />
        <!-- Example In-mermory file -->
        <Script File="%ModDir%/cs/MyModSOS.cs" Target="Client" Optional="true" />
    </FileGroup>
    <!-- Otros archivos... -->
</ModConfig>
```

En resumen, solamente necesita agrupar en un *FileGroup* todos los archivos afectados, luego definir una item *Conditional* tal y como está definido en el ejemplo. Luego incluya la etiqueta '*Optional*="true"', y eso es todo.

## Otros recursos

**Next to: [SOS Modules](sos_modules.html)**

**Back to: [Introduction](index.html)**
