# SOS Modules {#sos_modules}

Un módulo SOS es cualquier clase que implemente alguna interfaz `ISOS*` (`ISOSTab`, `ISOSStatInfo`, `ISOSPrefab`, `ISOSConfig`, `ISOSWindowProfile`).

Para Lua el contrato es prácticamente el mismo que en C#, salvo el registro: en vez de `[AutoRegister]` se pasa una tabla Lua que implemente los métodos del contrato usando cualquier método `API.Register*` (e.g: el ejemplo `HelloTab` de [Getting Started](getting_started.html)). Es por ello que los ejemplos posteriores están escritos solo en C#, pero aplican de igual manera a Lua.

> [!TIP]
> Para crear sus propios módulos SOS, necesita crear una clase pública que implemente el contrato correspondiente ya sea directa o indirectamente(genérico por nombre/parámetros de métodos) y posteriormente registrarla vía [`[AutoRegister]`](class_s_o_s_1_1_auto_register_attribute.html) o `API.Register*`. Ver: [Registration](registration.html).

## 1. Stat Info ([`ISOSStatInfo`](interface_s_o_s_1_1_i_s_o_s_stat_info.html))

Sección de la wiki del inspector que dibuja filas de informacion sobre el prefab seleccionado. Si el prefab no le aplica, no dibuja nada.

### Ejemplo de uso

```csharp
using Barotrauma;

using Microsoft.Xna.Framework;
using SOS;
using SOS.GUI;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

// Usar [AutoRegister] sin parámetros daría como resultado:
// (id: "MyMod.RadiationStatInfo", order: 0, active: true).
[AutoRegister("MyMod.RadiationInfo", order: 2)]
public class RadiationStatInfo : ISOSStatInfo
{
    public void Draw(RectTransform rectT, Prefab prefab)
    {
        if (prefab is not ItemPrefab item || !item.Tags.Contains("radioactive"))
            return;

        using var l = new GUILayoutBuilder(rectT);
        l.Header("RADIATION", Color.GreenYellow);
        l.Row("Radiation Output:", "High", Color.Red);
    }
}
```

> [!TIP]
> Recomendamos devolver pronto con `return` cuando el prefab no contenga datos relevantes, así la sección nunca se dibuja.

## 2. Central Tabs ([`ISOSTab`](interface_s_o_s_1_1_i_s_o_s_tab.html))

Pestaña central de inspección para un tipo de entidad. Aparece en la tab bar solo cuando `CanHandle` devuelve `true` para el objetivo actual.

### Ejemplo de uso {#ej1}

```csharp
using Barotrauma;

using SOS;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

[AutoRegister("MyMod.BiomeTab", order: 10)]
public class BiomeTab : ISOSTab
{
    public string TabName => "This is a Biome information...";

    private GUIComponent? container;

    public bool CanHandle(Prefab item) => item is Biome;

    public void Init(GUIComponent contentContainer)
    {
        container = contentContainer;
    }

    public void Show(Prefab item)
    {
        if (container == null) return;
        container.Visible = true;
    }

    public void Hide()
    {
        if (container == null) return;
        container.Visible = false;
    }
}
```

> [!NOTE]
> `Init` se llama una sola vez al construir el widget, `Show`/`Hide` cada vez que se cambia de pestaña o de objetivo.

## 3. Prefab Providers ([`ISOSPrefab`](interface_s_o_s_1_1_i_s_o_s_prefab.html))

Proveedor de datos que alimenta el navegador lateral con un nuevo tipo de entidad (e.g: jobs, eventos personalizados, missions, etc). Cada proveedor define su `Header` y filtra con `GetAll`.

### Ejemplo de uso {#ej2}

```csharp
using System.Collections.Generic;
using System.Linq;
using Barotrauma;

using SOS;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

[AutoRegister("MyMod.JobProvider", order: 3)]
public class JobPrefabProvider : ISOSPrefab
{
    public Type PrefabType => typeof(JobPrefab);
    public string Header => "Jobs";

    public IEnumerable<Prefab> GetAll(IPrefabFilter filter)
    {
        return JobPrefab.Prefabs.Where(j => filter.General.Count == 0 || j.Name.Value.Contains(filter.General[0]));
    }
}
```

> [!TIP]
> Use los tokens del filtro (`filter.General`, `filter.Mod`, `filter.Category`, etc.) para que su proveedor responda a la búsqueda avanzada del navegador.

## 4. Configs ([`ISOSConfig`](interface_s_o_s_1_1_i_s_o_s_config.html))

Unidad de configuración extensible con persistencia y renderizado declarativo en la ventana de settings. Herede de `ConfigDirtySaver` para gestión automatizada y guardado solo de cambios.

### Ejemplo de uso {#ej3}

```csharp
using Barotrauma;
using Microsoft.Xna.Framework;

using SOS;
using SOS.Configs;
using SOS.GUI;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

[AutoRegister("MyMod.Settings", order: 5)]
public class MyModSettings : ConfigDirtySaver, ISOSConfig
{
    public void Load() { /* ... */ }
    public void Save() { /* ... */ }
    public void Reset() { /* ... */ }

    public void Draw(RectTransform rectT)
    {
        using var l = new GUILayoutBuilder(rectT);
        l.Header("MY MOD CONFIG", Color.Gold);
        l.ButtonToResetSection(this);
    }
}
```

> [!NOTE]
> `Reset` y `Draw` traen implementación por defecto, puede obviarlos si su módulo SOS no necesita settings visibles.

## 5. Window Profiles ([`ISOSWindowProfile`](interface_s_o_s_1_1_i_s_o_s_window_profile.html))

Layout visual intercambiable de la ventana SOS (e.g: vista de 3 columnas, inspector compacto, herramienta flotante). Hereda de `GUIWindow` y se puede cambiar en caliente vía `API.Emit(CommKeys.ChangeProfile, id)`.

### Ejemplo de uso {#ej4}

```csharp
using SOS;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

[AutoRegister("MyMod.LeftFixedWindow", order: 10)]
public class LeftFixedWindow : GUIWindow, ISOSWindowProfile
{
    public string DisplayName => "Left Fixed Window";
    public string Description => "Fixed window located in top-Left Panel.";

    // Opcionalmente, puede definir `ProfileConfig`, una configuracion específica para este Módulo, donde se llamará luego de guardarse WindowProfileConfig, y dibujará su .Draw() dentro de esta configuración.
    private LeftFixedWindowConfig? config; 
    public ISOSConfig ProfileConfig => config ??= new(); 

    public void Init() { /* Build UI */ }
    public void Update() { /* Frame update */ }
}
```

---

## Recomendaciones y buenas prácticas

> NO recomendamos implementar en una misma clase varios tipos de módulos SOS (interfaces ISOS*), esto lo consideramos un anti-patrón y actualmente llevará a crear 2 instancias de la misma clase para cada tipo de contrato(A menos que registre manualmente una misma instancia o delegado para ambos, pero igualmente no lo recomendamos).

<!-- - -->

> [!IMPORTANT]
> Cada módulo SOS funciona como un builder de instancia única por defecto, lo que significa que se usa la misma instancia hasta cerrar/reabrir la ventana SOS. Téngalo en cuenta si considera guardar informacion en la clase, que esta no interfiera al ser llamada en diferentes contextos.

## Otros recursos

**Next to: [Registration](registration.html)**

**Back to: [Getting Started](getting_started.html)**
