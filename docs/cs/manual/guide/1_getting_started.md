# Getting Started {#getting_started}

## 1. Requisitos mínimos

Proyecto integral principal, y el único binario sobre el que deberá trabajar cualquier mod. Está pensado para ser cargado Client-Side only y compatible para cualquier plataforma(Solo contiene referencias a Barotrauma y código multiplataforma). Este implementa la mayoría de logicas, contratos, patrones y componentes GUI personalizados compartidos. El archivo que deberá referenciar en proyectos derivados debe ser únicamente de este proyecto, archivo `SOS.SDK.dll`, y puede obtenerlo [AQUÍ](https://github.com/Retype15/SOS/releases/latest/download/SOS.SDK.dll).

Requisitos específicos del proyecto dependen de LuaCsForBarotrauma, véase la [guía de introducción de `LuaCsForBarotrauma`](https://evilfactory.github.io/LuaCsForBarotrauma) para crear mods compatibles.

## 2. Setup del proyecto

La forma más fácil de iniciar un mod que dependa del nuestro es referenciando el binario `SOS.SDK.dll` a su proyecto. Recomendamos incluirlo en la carpeta /Refs y referenciar desde ahí si ha seguido la guía de LuaCsForBarotrauma para [Assembly CSharp Mods](https://evilfactory.github.io/LuaCsForBarotrauma/cs-docs/html/md_manual_assemblymod.html), si no es el caso o desea una referencia opcional usando reflexión puede ignorar este paso. (Más información a continuación.)

## Example mod

Cualquier mod que desee implementar una pestaña de información solamente necesita crear una clase base que herede de la interfaz correspondiente e implemente el contenido que quiera añadir (e.g: Una nueva seccion en los stat sections necesita una clase pública que implemente el contrato de `ISOSStatInfo`), y posteriormente registrarla.

### Ejemplo de uso

- **Csharp:**

  ```csharp
  using Barotrauma;

  using Microsoft.Xna.Framework;
  using SOS;
  using SOS.GUI;

  # pragma warning disable IDE0130
  # pragma warning disable IDE0290

  namespace MyMod;

  [AutoRegister("MyMod.UsageMessage", order: -1)] // Usar [AutoRegister] sin parámetros daría como resultado (id: "MyMod.TreatmentStatInfo", order: 0, active: true).
  public class UsageMessageStatInfo : ISOSStatInfo
  {
      public bool Draw(GUIListBox contentPanel, Prefab prefab)
      {
          string? text = prefab switch
          {
              ItemPrefab item => $"Artie Dolittle is using {item.Name.Value}...",
              AfflictionPrefab aff when aff.CauseOfDeathDescription.Loaded => $"Artie Dolittle has {aff.CauseOfDeathDescription.Value}...",
              _ => null
          };

          if (text.IsNullOrWhiteSpace()) return false;

          using var l = new GUILayoutBuilder(contentPanel);
          l.Header("OBITUARY", Color.Crimson);
          l.RichText(RichString.Rich(text));
          return true;
      }
  }

  ```

- **Lua:**

  ```lua
  local API = LuaUserData.CreateStatic("SOS.API")

  local HelloTab = {}
  HelloTab.Id = "MyMod.HelloTab"
  HelloTab.TabName = "HELLO"

  local container = nil

  function HelloTab.CanHandle(prefab)
      return prefab ~= nil
  end

  function HelloTab.Init(parent)
      container = GUI.Frame(GUI.RectTransform(Vector2(1, 1), parent.RectTransform), nil)
      GUI.TextBlock(GUI.RectTransform(Vector2(1, 0.1), container.RectTransform, 4), "Hello, World!", nil,
          GUI.GUIStyle.LargeFont, GUI.Alignment.Center)
  end

  function HelloTab.Show(prefab, onPrimary, onSecondary)
      if container ~= nil then container.Visible = true end
  end

  function HelloTab.Hide()
      if container ~= nil then container.Visible = false end
  end

  function HelloTab.Dispose()
      if container ~= nil and container.Parent ~= nil then
          container.Parent.RemoveChild(container)
          container = nil
      end
  end

  API.RegisterTab(HelloTab, HelloTab.Id, 10)
  ```

Ahora si, podemos comenzar a aprender los métodos y atributos que te serán de utilidad para construir tus futuros componentes personalizados.

## Otros recursos

## Next to: [Registration](registration.html)
