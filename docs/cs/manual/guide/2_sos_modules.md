# SOS Modules {#sos_modules}

Esta sección te enseñarán los diferente tipos de interfaces o contratos que cualquier Módulo SOS necesita o admite.

Un módulo SOS es cualquier clase que implemente alguna interfaz **ISOS\*** (`SOS.ISOSTab`, `SOS.ISOSStatInfo`, `SOS.ISOSPrefab`, `SOS.ISOSConfig`, `SOS.ISOSWindowProfile`).

> [!NOTE]
> Para Lua el contrato es prácticamente el mismo que en C#, salvo el registro: No admite el atributo `SOS.AutoRegisterAttribute` por lo que es obligatorio pasar una tabla Lua que implemente los métodos del contrato usando cualquier método [`API.Register*`](class_s_o_s_1_1_a_p_i.html) (e.g: el ejemplo [`HelloTab` de Getting Started](getting_started.html#lua_example)). Es por ello que los ejemplos posteriores están escritos solo en C#, pero aplican de igual manera a Lua.

<!-- - -->

> [!TIP]
> Para crear sus propios módulos SOS, necesita crear una clase pública que implemente el contrato correspondiente ya sea directa o indirectamente(genérico por nombre/parámetros de métodos) y posteriormente registrarla vía `SOS.AutoRegisterAttribute` o [`API.Register*`](class_s_o_s_1_1_a_p_i.html). Ver: [Registration](registration.html).

## 1. Stat Info ([`ISOSStatInfo`](interface_s_o_s_1_1_i_s_o_s_stat_info.html))

Sección de la wiki del inspector que dibuja filas de informacion sobre el prefab seleccionado. Si el *rectT* no tiene hijos, se limpia.

### Ejemplo de uso

```csharp
using Barotrauma;

using Microsoft.Xna.Framework;
using SOS;
using SOS.GUI;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

// Usar [AutoRegister] sin parámetros da como resultado:
// (id: "MyMod.RadiationStatInfo", order: 0, active: true).
[AutoRegister]
public class RadiationStatInfo : ISOSStatInfo
{
    public void Draw(RectTransform rectT, Prefab prefab)
    {
        if (prefab is not ItemPrefab item || !item.Tags.Contains("radioactive"))
            return;

        // GUILayoutBuilder es un helper para crear componentes de forma funcional y rápida.
        using var l = new GUILayoutBuilder(rectT);
        l.Header("RADIATION", Color.GreenYellow);
        l.Row("Radiation Output:", "High", Color.Red);
    }
}
```

> [!TIP]
> Recomendamos devolver pronto con `return` cuando el prefab no contenga datos relevantes, así la sección no se dibuja sin nada relevante.

## 2. Central Tabs ([`ISOSTab`](interface_s_o_s_1_1_i_s_o_s_tab.html))

Pestaña central de inspección para un tipo de entidad. Aparece en la tab bar solo cuando `SOS.ISOSTab.CanHandle` devuelve `true` para el objetivo actual.

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
    public string TabName => "A Biome information...";

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

Proveedor de datos que alimenta el navegador lateral con un nuevo tipo de entidad (e.g: [`JobPrefab`](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/baro-client/html/class_barotrauma_1_1_job_prefab.html), [`EventPrefab`](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/baro-client/html/class_barotrauma_1_1_event_prefab.html), [`TalentPrefab`](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/baro-client/html/class_barotrauma_1_1_talent_prefab.html), [`CharacterPrefab`](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/baro-client/html/class_barotrauma_1_1_character_prefab.html), etc). Cada proveedor define su `SOS.ISOSPrefab.Header` y filtra con `SOS.ISOSPrefab.GetAll`.

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

Unidad de configuración extensible con persistencia y renderizado declarativo en la ventana de settings. Herede de `SOS.Configs.ConfigDirtySaver` para gestión automatizada y guardado de cambios.

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
    // ISettingBase Properties

    private readonly ISettingBase<string> property;
    public string Property
    {
        get => property.Value;
        set => property.SetIfNotEqual(value);
    }

    public MyModSettings()
    {
        var configService = Plugin.Instance.ConfigService;
        var package = Plugin.Instance.Package;

        TryInitConfig("ActiveProfileId", out property, configService, package);
    }

    // Se ejecuta cuando SOS decide que debe cargar datos, por ejemplo al abrir la ventana S.O.S.
    public void Load() { /* ... */ }
    
    // Se ejecuta cuando SOS decide que debe guardar los datos, por ejemplo al cerrar la ventana S.O.S.
    public void Save() { /* ... */ }

    // Se ejecuta cuando el usuario decide limpiar la configuración desde un nivel superior, por ejemplo, desde la configuración `SOS.WindowProfile` o general.
    public void Reset() { /* ... */ }

    // Dibuja las opciones de configuración personalizada para esta configuración directamente en el panel de configuración.
    public void Draw(RectTransform rectT)
    {
        using var l = new GUILayoutBuilder(rectT);
        l.Header("MY MOD CONFIG", Color.Gold);
        l.ButtonToResetSection(this);
    }
}
```

> [!TIP]
>
> 1. Recomendamos que convierta sus `SOS.ISOSConfig` en un Singleton + registro manual para que la inicialización solo ocurra una única vez, además de permitir accesos directos como hace la mayoría de nuestros Modulos de configuracion base.
>
> 2. *SOS.ISettingBaseExt.SetIfNotEqual* es un helper que guarda la propiedad solo si no hubo cambio real, permitiendo que *SOS.Configs.ConfigDirtySaver* haga su trabajo. Se hablará en profundidad de *SOS.Configs.ConfigDirtySaver* a futuro.
>
> 3. *SOS.ISOSConfig.Reset* y *SOS.ISOSConfig.Draw* traen implementación por defecto, puede obviarlos si su módulo SOS no necesita settings visibles.

## 5. Window Profiles ([`ISOSWindowProfile`](interface_s_o_s_1_1_i_s_o_s_window_profile.html))

Layout visual intercambiable de la ventana SOS. Puede crear cualquier estilo visual, ejemplo una vista de una única columna, un inspector compacto, o una herramienta flotante con solo lo esencial. Hereda de `SOS.GUI.GUIWindow` y se puede cambiar vía **SOS.API.Emit(SOS.CommKeys.ChangeProfile, id)**.

Es completamente libre de modificar la apariencia por completo de la ventana.

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

    public void Init() { /* Build UI */ }
    public void Update() { /* Frame update */ }
}
```

> [!TIP]
> Opcionalmente, puede definir *SOS.ISOSWindowProfile.ProfileConfig*, una configuracion específica para este Módulo, donde se llamará luego de guardarse el módulo SOS.ISOSConfig **WindowProfileConfig**, y dibujará su *SOS.ISOSConfig.Draw()* dentro de esta configuración en el área específica dada por esta.
>
> **Ejemplo de definición:**
>
> ```csharp
> public class LeftFixedWindow : GUIWindow, ISOSWindowProfile
> {
>   // ...
>   private LeftFixedWindowConfig? config; 
>   public ISOSConfig ProfileConfig => config ??= new();
>   // ...
> }
> ```
>
> Reset es afectado por **WindowProfileConfig** y el reset general.

---

## Recomendaciones y buenas prácticas

> NO recomendamos implementar en una misma clase varios tipos de módulos SOS, esto lo consideramos un anti-patrón y actualmente llevará a crear 2 instancias de la misma clase para cada tipo de contrato(A menos que registre manualmente una misma instancia o delegado para ambos, pero igualmente no lo recomendamos).

<!-- - -->

> [!IMPORTANT]
> Cada módulo SOS funciona como un builder de instancia única por defecto, lo que significa que se usa la misma instancia hasta cerrar/reabrir la ventana SOS. Téngalo en cuenta si considera guardar informacion en la clase, que esta no interfiera al ser llamada en diferentes contextos.

## Otros recursos

**Next to: [Registration](registration.html)**

**Back to: [Getting Started](getting_started.html)**
