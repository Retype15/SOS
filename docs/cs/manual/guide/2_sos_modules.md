# SOS Modules {#sos_modules}

This section will teach you the different types of interfaces or contracts that any SOS Module needs or admits.

An SOS module is any class that implements some **ISOS\*** interface (`SOS.ISOSTab`, `SOS.ISOSStatInfo`, `SOS.ISOSPrefab`, `SOS.ISOSConfig`, `SOS.ISOSWindowProfile`).

> [!NOTE]
> For Lua the contract is practically the same as in C#, except the registration: It does not admit the `SOS.AutoRegisterAttribute` attribute so it is mandatory to pass a Lua table that implements the contract's methods using any [`API.Register*`](class_s_o_s_1_1_a_p_i.html) method (e.g: the [`HelloTab` example from Getting Started](getting_started.html#lua_example)). That is why the later examples are written only in C#, but they apply in the same way to Lua.

<!-- - -->

> [!TIP]
> To create your own SOS modules, you need to create a public class that implements the corresponding contract either directly or indirectly(generic by name/parameters of methods) and afterwards register it via `SOS.AutoRegisterAttribute` or [`API.Register*`](class_s_o_s_1_1_a_p_i.html). See: [Registration](registration.html).

## 1. Stat Info

[`ISOSStatInfo`](interface_s_o_s_1_1_i_s_o_s_stat_info.html)

Section of the inspector wiki that draws rows of information about the selected prefab. If the *rectT* has no children, it is cleaned.

### Usage example

```csharp
using Barotrauma;

using Microsoft.Xna.Framework;
using SOS;
using SOS.GUI;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

// Using [AutoRegister] without parameters results in:
// (id: "MyMod.RadiationStatInfo", order: 0, active: true).
[AutoRegister]
public class RadiationStatInfo : ISOSStatInfo
{
    public void Draw(RectTransform rectT, Prefab prefab)
    {
        if (prefab is not ItemPrefab item || !item.Tags.Contains("radioactive"))
            return;

        using var l = new GUILayoutBuilder(rectT);
        l.Header(Texts.Get("mymod.radiation.header", "RADIATION HAZARD").Value, Color.GreenYellow);
        l.Row(Texts.Get("mymod.radiation.output", "Radiation Output:").Value, "High", Color.Red);
    }
}
```

And its localization in your mod's `Localization/English.xml`:

```xml
<mymod.radiation.header>RADIATION HAZARD</mymod.radiation.header>
<mymod.radiation.output>Radiation Output:</mymod.radiation.output>
```

> [!TIP]
> We recommend returning early with `return` when the prefab does not contain relevant data, so the section is not drawn with nothing relevant.

## 2. Central Tabs

[`ISOSTab`](interface_s_o_s_1_1_i_s_o_s_tab.html)

Central inspection tab for an entity type. It appears in the tab bar only when `SOS.ISOSTab.CanHandle` returns `true` for the current target.

### Usage example {#ej1}

```csharp
using Barotrauma;

using Microsoft.Xna.Framework;
using SOS;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

[AutoRegister("MyMod.BiomeTab", order: 10)]
public class BiomeTab : ISOSTab
{
    private GUITextBlock? text = null;

    // If it is true, it draws its Tab Button, if it is False it will not be shown. It is optional, and if it is not defined it will always return true.
    public bool CanHandle(Prefab prefab) => prefab is Biome;

    // Receives the instances of the container and the button created using the optional CreateTabButton method. It is treated as a constructor for GUITab.
    public void Init(GUIFrame container, GUIButton tabButton) 
    {
        // If you do not define your own `CreateTabButton` method or doesn't to use localization strings, you can define the properties using the returned instance.
        // tabButton.Text = "Biome Visualizer";
        // tabButton.ToolTip = "A Biome information...";

        // Initializes and creates the components that it needs.
        text = new GUITextBlock(new RectTransform(new Vector2(1f, 0.1f), container.RectTransform, 4), "", null,
        GUIStyle.LargeFont, Alignment.Center);
    }

    // Called when it is the main Tab, CanHandle returns true and the prefab has changed. It is optional.
    public void Update(Prefab prefab)
    {
        if (text == null) return;
        var biome = GameMain.GameSession.LevelData.Biome;
        text.Text =
        $"""Actual Biome is: {biome.DisplayName}
        With difficulty: {biome.ActualMaxDifficulty}
        Description: {biome.Description}""";
        // ...
    }
}
```

And its localization in the `Localization/English.xml`.

```xml
<mymod.biometab.title>BIOME</mymod.biometab.title>
<mymod.biometab.tooltip>Shows the current level biome.</mymod.biometab.tooltip>
```

> [!NOTE]
> GUITab ensures that `Update` is never called before `Init`, but it is always good practice to check for nulls.

## 3. Prefab Providers

[`ISOSPrefab`](interface_s_o_s_1_1_i_s_o_s_prefab.html)

Data provider that feeds the side browser with a new type of entity (e.g: [`JobPrefab`](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/baro-client/html/class_barotrauma_1_1_job_prefab.html), [`EventPrefab`](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/baro-client/html/class_barotrauma_1_1_event_prefab.html), [`TalentPrefab`](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/baro-client/html/class_barotrauma_1_1_talent_prefab.html), [`CharacterPrefab`](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/baro-client/html/class_barotrauma_1_1_character_prefab.html), etc). The browser groups its prefabs by runtime type and resolves each header from localization (`{Type}.header`); you only need to serve them with `SOS.ISOSPrefab.GetPrefabs`.

### Usage example {#ej2}

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
    public IEnumerable<Prefab> GetPrefabs(IPrefabFilter filter)
    {
        return JobPrefab.Prefabs.Where(j => filter.General.Count == 0 || j.Name.Value.Contains(filter.General[0]));
    }
}
```

> [!TIP]
> The header of your section is resolved by itself from localization (`sos.prefab.{Type}.header`, e.g: `sos.prefab.jobprefab.header` → "Jobs"). If the key is missing, the humanized type name is used.
>
> Use the filter tokens (`filter.General`, `filter.Mod`, `filter.Category`, etc.) so that your provider responds to the browser's advanced search.

## 4. Configs

[`ISOSConfig`](interface_s_o_s_1_1_i_s_o_s_config.html)

Extensible configuration unit with persistence and declarative rendering in the settings window. Inherit from `SOS.Configs.ConfigDirtySaver` for automated management and saving of changes.

### Usage example {#ej3}

```csharp
using Barotrauma;
using Microsoft.Xna.Framework;

using SOS;
using SOS.Configs;
using SOS.Extensions;
using SOS.GUI;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

[AutoRegister("MyMod.Settings", order: 5)]
public class MyModSettings : ConfigDirtySaver, ISOSConfig
{
    // ISettingBase Properties

    private readonly ISettingBase<bool> showHints;
    public bool ShowHints
    {
        get => showHints.Value;
        set => showHints.SetIfNotEqual(value);
    }

    public MyModSettings()
    {
        // Plugin is your own IAssemblyPlugin: it exposes your mod's LuaCs services.
        var configService = Plugin.Instance.ConfigService;
        var package = Plugin.Instance.Package;

        TryInitConfig("ShowHints", out showHints, configService, package);
    }

    // It runs when SOS decides that it should load data, for example when opening the S.O.S window.
    // The values already live in the service; use it to synchronize your own UI if you have one.
    public void Load() { }

    // It runs when SOS decides that it should save the data, for example when closing the S.O.S window.
    // It only persists what is marked as dirty (via SetIfNotEqual).
    public void Save() => SaveChanges(Plugin.Instance.ConfigService);

    // Restores the factory values of this configuration.
    public void Reset() => ShowHints = true;

    // Draws the custom configuration options for this configuration directly in the configuration panel.
    public void Draw(RectTransform rectT)
    {
        using var l = new GUILayoutBuilder(rectT);
        l.Header("MY MOD CONFIG", Color.Gold);
        l.TickBox(Texts.Get("mymod.settings.showhints", "Show Hints").Value, ShowHints, v => ShowHints = v);
        l.ButtonToResetSection(this);
    }
}
```

Each setting must be declared in your mod's configuration XML:

```xml
<Setting Name="ShowHints" Type="bool" Value="true" ShowInMenus="false" />
```

And its localization in your mod's `Localization/English.xml`:

```xml
<mymod.settings.showhints>Show Hints</mymod.settings.showhints>
```

> [!TIP]
>
> 1. We recommend that you convert your `SOS.ISOSConfig` into a Singleton + manual registration so that the initialization only happens a single time, besides allowing direct accesses like most of our base configuration Modules do.
>
> 2. *SOS.ISettingBaseExt.SetIfNotEqual* is a helper that saves the property only if there was no real change, allowing *SOS.Configs.ConfigDirtySaver* to do its work. *SOS.Configs.ConfigDirtySaver* will be discussed in depth in the future.
>
> 3. *SOS.ISOSConfig.Reset* and *SOS.ISOSConfig.Draw* bring a default implementation, you can obviate them if your SOS module does not need visible settings.

## 5. Window Profiles

[`ISOSWindowProfile`](interface_s_o_s_1_1_i_s_o_s_window_profile.html)

Interchangeable visual layout of the SOS window. You can create any visual style, for example a single-column view, a compact inspector, or a floating tool with only the essentials. It inherits from `SOS.GUI.GUIWindow` and can be changed via **SOS.API.Emit(SOS.CommKeys.ChangeProfile, id)**.

It is completely free to modify the appearance of the window completely.

### Usage example {#ej4}

```csharp
using Barotrauma;
using SOS;

# pragma warning disable IDE0130
# pragma warning disable IDE0290

namespace MyMod;

[AutoRegister("MyMod.CompactProfile", order: 20)]
public class CompactProfile : GUIWindow, ISOSWindowProfile
{
    private bool _eventsRegistered;

    public CompactProfile() : base(
        new RectTransform(new Vector2(0.5f, 0.8f), GUI.Canvas, Anchor.Center),
        Texts.Get("mymod.compact.title", "Compact Inspector"),
        style: "CircuitBoxFrame",
        color: Color.Black * 0.85f)
    {
    }

    public void Init()
    {
        if (_eventsRegistered) return;
        API.On(CommKeys.ToggleWindow, Toggle, EventPriority.UI);
        API.On(CommKeys.OpenWindow, Open, EventPriority.UI);
        API.On<Prefab?>(CommKeys.SelectTarget, OnTarget, EventPriority.UI);
        _eventsRegistered = true;

        // Build your UI here and catch up with the current selection:
        OnTarget(API.GetState<Prefab?>(CommKeys.SelectTarget));
    }

    public void Update() { /* Per-frame logic: layout, input, etc... */ }

    public void Dispose()
    {
        API.Off(CommKeys.ToggleWindow, Toggle, EventPriority.UI);
        API.Off(CommKeys.OpenWindow, Open, EventPriority.UI);
        API.Off<Prefab?>(CommKeys.SelectTarget, OnTarget, EventPriority.UI);
        _eventsRegistered = false;
    }

    private void Toggle() => Visible = !Visible;
    private void Open() => Visible = true;
    private void OnTarget(Prefab? target) { /* Rebuilds the content for target... */ }
}
```

The name and the description of the dropdown are resolved from localization (`{id}.name` / `{id}.desc`), in your mod's `Localization/English.xml`:

```xml
<mymod.compactprofile.name>Compact Inspector</mymod.compactprofile.name>
<mymod.compactprofile.desc>Minimalist floating window.</mymod.compactprofile.desc>
```

You can read the current code in the repository for more detail.

> [!TIP]
> Optionally, you can define *SOS.ISOSWindowProfile.ProfileConfig*, a specific configuration for this Module, where it will be called after the SOS.ISOSConfig **WindowProfileConfig** module is saved, and it will draw its *SOS.ISOSConfig.Draw()* inside this configuration in the specific area given by it.
>
> **Definition example:**
>
> ```csharp
> public class CompactProfile : GUIWindow, ISOSWindowProfile
> {
>   // ...
>   private CompactProfileConfig? config; 
>   public ISOSConfig ProfileConfig => config ??= new();
>   // ...
> }
> ```
>
> Reset is affected by **WindowProfileConfig** and the general reset.

## Recommendations and good practices

We do NOT recommend implementing several types of SOS modules in the same class, we consider this an anti-pattern and currently it will lead to creating 2 instances of the same class for each type of contract(Unless you manually register the same instance or delegate for both, but we do not recommend it either).

<!-- - -->

> [!IMPORTANT]
> Each SOS module works as a single-instance builder by default, which means that the same instance is used until the SOS window is closed/reopened. Keep it in mind if you consider saving information in the class, that it does not interfere when being called in different contexts.

## Other resources

**Next to: [Registration](registration.html)**

**Back to: [Getting Started](getting_started.html)**
