# Registration {#registration}

Hay dos formas principales de registrar un nuevo componente:

## 1. AutoRegister

- Usando el atributo `[AutoRegister]`, para registro automático la primera vez que se abra la ventana de SOS(nunca antes) pasando las propiedades opcionales de registro:

  - `string id`: El identificador único para su tipo, si otro objeto del mismo tipo intenta registrarse posteriormente con el mismo identificador, será sobreescrito. **Default: type.FullName ?? type.Name**. Si registra un componente con el mismo nombre de uno anterior, se sobreescribirá, téngalo en cuenta si desea sobreescribir componentes de otros mods para lógica personalizada.
  - `double order`: Orden de ejecución o llamada. Orden ascendente. llaman de menor a mayor, si es igual se trata alfabéticamente por el Id, permitiendo ordenarse en las llamdas de forma predecible. **Default: 0**
  - `bool active`: Indica si por defecto este componente se ejecutará o no. Permite añadir lógica de activación/desactivación de componentes para lógica condicional, o permitir al jugador desactivar componentes a conciencia para no sobrecargar su interfaz con informacion que no necesita. **Default: True**

> [!TIP]
> Usando este método, la clase se descubre automáticamente. Es la forma idiomática de registrar clases que implementen nuestras interfaces.
>
> Dado que este método no requiere ejecutar código manual para registrar las clases, se puede obviar por completo la clase Plugin si el objetivo de tu mod es únicamente añadir contenenido a SOS.
>
> Dada la limpieza, recomendamos además declararlas en archivos In-Memory Compiled at Time.

## 2. Registro manual

- Registro manual, usando `API.Register*` y pasando un objeto que represente el método de instanciación y las mismas propiedades opcionales anteriormente descritas (obj, id, order, active). Puede registrar objetos genéricos y tablas Lua que implementen de forma indirecta los métodos del contrato.
  - `object obj:` Hay varias formas de registrar un componente:
  
    - `class\<T\>`: Instancia directa de un objeto que implemente el contrato. Esta es la forma más simple de pasar una unica instancia global y evitar instanciar.

      ```csharp
      API.RegisterStatInfo(new TreatmentStatInfo(), "MyMod.TreatmentInfo", 1);
      ```

    - `Func\<T\>`: Pasar una función que retorne la instancia que se desea, útil para pasar una instancia única con un método personalizado para instanciación, por ejemplo compatibilidad con el patrón singleton o una factoría personalizada.

      ```csharp
      API.RegisterConfig(() => MyModConfig.Instance, "MyMod.MainConfig", 0);
      ```

    - `object`: Objetos genéricos que no implementan de forma directa el contrato, o tablas de Lua(Compatibilidad con Lua). En estos casos se recurre a la reflexión para intentar vincular los metodos de la instancia recibida con los métodos que exige el contrato usando [`object.Cast<T>()`](lua_interop.html), esto permite registrar de forma indirecta clases sin necesidad de hacer una referencia dura al SDK de S.O.S, simplemente obteniendo por reflexion el método manual de registro `API.Register*` y pasando un objeto que cumpla el contrato de forma indirecta (por nombre de métodos).

      ```csharp
      using Barotrauma;
      public class MyStatInfoSoft
      {
          public static string ID => "MyMod.MyStatInfoSoft";
          public bool Draw(GUIListBox contentPanel, Prefab prefab)
          {
              //...
              return true;
          }
      }
      //IMPORTANT: Se debe obtener el último assembly cargado por el leak de assemblies de LuaCsForBarotrauma.
      var apiType = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(t => t.GetType("SOS.API") != null)?.GetType("SOS.API");
      if (apiType == null)
      {
          LuaCsLogger.LogError("[MyMod] SOS.API type not found — No S.O.S loaded");
          return;
      }
      var registerMethod = apiType.GetMethod("RegisterStatInfo", BindingFlags.Public | BindingFlags.Static, null, [typeof(object), typeo  (string), typeof(double), typeof(bool)], null);
      if (registerMethod == null)
      {
          LuaCsLogger.LogError("[MyMod] API.RegisterStatInfo method not found!");
          return;
      }
      registerMethod.Invoke(apiType, [new MyStatInfoSoft(), MyStatInfoSoft.ID, -1]);
      ```

      > [!NOTE]
      > Todo patrón desconocido cae en esta rama, y por tanto se intentará castear usando la propiedad y los nombres de metodos sobre este object(o su target si es DuckProxy). No intente registrar tipos arbitrarios que claramente no cumplen el contrato. Asegúrese de que sean válidos.

    - `Func\<object\>`: Delegado para registrar un builder de instancia usando un objeto genérico que implemente de forma indirecta la interfaz, o tabla de Lua.

      ```csharp
      registerMethod.Invoke(apiType, [() => new MyStatInfoSoft(), MyStatInfoSoft.ID, 1]);
      ```

    - `Type : T`: Pasar un tipo de clase que implemente el contrato de la instancia. Es el método default que usa el registro automático.

      ```csharp
      API.RegisterStatInfo(typeof(TreatmentStatInfo), "MyMod.TreatmentInfo", 0.2);
      ```

    - `Type : object`: Pasar un tipo genérico que implemente el contrato de la instancia de forma indirecta.

      ```csharp
      registerMethod.Invoke(apiType, [typeof(MyStatInfoSoft), MyStatInfoSoft.ID]);
      ```

<!-- TODO: Explicar sobre DefaultClassAtribute -->

> [!NOTE]
> NO recomendamos implementar en una misma clase varios tipos de interfaces ISOS*, esto lo consideramos un anti-patrón y actualmente llevará a crear 2 instancias de la misma clase para cada tipo de contrato(A menos que registre manualmente una misma instancia o delegado para ambos, pero igualmente no lo recomendamos).

<!-- - -->

> [!IMPORTANT]
> Cada `ISOS*` funciona como un builder de instancia única por defecto, lo que significa que se usa la misma instancia hasta cerrar/reabrir la ventana SOS (A menos que registre por el método manual una instancia o un delegado enves de un type o una factoría (() => new Object())). Téngalo en cuenta si considera guardar información en la clase, que esta no interfiera al ser llamada en diferentes contextos.

<!-- TODO: Recordar hAblar de IDisposable para objetos genéricos. -->

## Otros recursos

**Next to: [Data Sharing](data_sharing.html)**

**Back to: [Getting Started](getting_started.html)**
