# Data Sharing {#data_sharing}

Un sistema de suscripcion de eventos y almacen de datos compartidos bidireccional para todos los mods para evitar referencias duras entre todos, así como asegurar orden lógico y predecible de ejecución.

Cada método necesita recibir como primer parámetro `string key`: clave o identificador de puente como primer parámetro. Las claves comúnes están definidas y documentadas en la clase estática [`CommKeys`](class_s_o_s_1_1_comm_keys.html).

Ver: [`CommKeys`](class_s_o_s_1_1_comm_keys.html).

## 1. Transmisores de datos compartido

Métodos de suscripción y llamadas usando delegados.

Los Layers recomendados a usar lo define la clase estática [`EventPriority`](class_s_o_s_1_1_event_priority.html).

Cada uno de los métodos aceptan opcionalmente `double order = 0` para añadir/eliminar/llamar el método de forma atómica en el layer de orden definido y define el orden de ejecución de los delegados.

Cada grupo de delegados agrupado por el mismo valor **order** lo llamaremos **layer**. Los Layers recomendados a usar lo define la clase estática [`EventPriority`](class_s_o_s_1_1_event_priority.html)

> [!NOTE]
> Recomendado pasar el mismo **layer** usado en [`API.On`](class_s_o_s_1_1_a_p_i.html#aa6529ff033dce9cc632edbfadb61f756) a la desuscripción [`API.Off`](class_s_o_s_1_1_a_p_i.html#ad3720675f2c4f4f556285e73ff1882aa) por optimización, pero no es obligatorio.
>
> `double order` se acota la parte de punto flotante desde *.0* a *.9999* para evitar errores relacionados con la precisión y establecer un límite claro y seguro para layers intermedios.

- [`API.On`](class_s_o_s_1_1_a_p_i.html#aa6529ff033dce9cc632edbfadb61f756): Permite suscribir delegados que son llamados al emitir usando [`API.Emit`](class_s_o_s_1_1_a_p_i.html#a7649725a030e85aeff6208c4de1ef64f) y ejecutados en orden definido por su **layer**. acepta `Action`/`Action<T>` *handler* como segundo parámetro, y es el método que es llamado cuando se emite con su misma `key`. `T` Describe el tipo que recibe el método, si este requiere `T`, solo será ejecutado si el emisor [`API.Emit<T>`](class_s_o_s_1_1_a_p_i.html#a0594ed40c2bb3c423ed7475d55a388b1) lo haga pasando un objeto `T`, si no recibe objetos, puede ser ejecutado tanto por [`API.Emit`](class_s_o_s_1_1_a_p_i.html#a7649725a030e85aeff6208c4de1ef64f) como por [`API.Emit<T>`](class_s_o_s_1_1_a_p_i.html#a0594ed40c2bb3c423ed7475d55a388b1)

- [`API.Off`](class_s_o_s_1_1_a_p_i.html#ad3720675f2c4f4f556285e73ff1882aa): Permite desuscribir un delegado del bus de eventos para limpiar referencias. Requiere key, y el mismo *handler* que suscribió usando [`API.On`](class_s_o_s_1_1_a_p_i.html#aa6529ff033dce9cc632edbfadb61f756) anteriormente. Recuerde llamar apropiadamente este método cuando deje de necesitar escuchar. Si recibe **order** solo desuscribirá el **handler** del layer específico proporcionado, si recibe `null` intenta desuscribir el **handler** de cada uno de los layers registrados.

- [`API.Emit`](class_s_o_s_1_1_a_p_i.html#a7649725a030e85aeff6208c4de1ef64f): Permite emitir un valor y llamar a todos los métodos suscritos por la `key` proporcionada. Acepta opcionalmente **value : `T`** para emitir pasando a los métodos registrados un objeto de tipo `T`. Acepta **order** para llamar todos los métodos que estén suscritos únicamente a ese **layer**. Si es parametrizado `T` acepta `bool setState = true` el cual guardará usando [`API.SetState<T>`](class_s_o_s_1_1_a_p_i.html#ad29189f71d4e67c7c7f1551b2bc12529) luego de emitir satisfactoriamente si valor si es `true`, otherwise ejecuta sin guardar.

- [`API.EmitRange`](class_s_o_s_1_1_a_p_i.html#a22525743a1d9a8e8bfffe60ef69a94ce): Permite emitir un valor y llamar a todos los métodos suscritos por la `key` proporcionada. Acepta opcionalmente `double min = double.MinValue`, `double max = double.MaxValue` para llamar a los métodos de un rango de layers para casos de uso avanzados. Si es parametrizado `T` acepta `bool setState = true` el cual guardará usando [`API.SetState<T>`](class_s_o_s_1_1_a_p_i.html#ad29189f71d4e67c7c7f1551b2bc12529) luego de emitir satisfactoriamente si valor si es `true`, otherwise ejecuta sin gaurdar/actualizar el valor.

> [!IMPORTANT]
> Emitir pasando un objeto `T` ejecutará todo método suscrito que acepte el parámetro `T` o no tenga parámetro, pero emitir sin parámetros solo llamará a los metodos sin parámetros, téngalo en cuenta.

### Ejemplos {#1_examples}

  ...
  <!-- TODO: Añadir los ejemplos... -->

## 2. Almacén de datos

Para guardar objetos o datos compartidos públicos para todos los mods

- [`API.SetState<T>`](class_s_o_s_1_1_a_p_i.html#ad29189f71d4e67c7c7f1551b2bc12529): Permite guardar una variable de tipo `T` en un estado compartido público fácilmente accesibles desde cualquier mod.

- [`API.GetState<T>`](class_s_o_s_1_1_a_p_i.html#ab3f8813d7c96a86e528a705e148e8df2): Permite obtener la variable de tipo `T` guardada en el estado compartido público antes guardada por [`API.SetState<T>`](class_s_o_s_1_1_a_p_i.html#ad29189f71d4e67c7c7f1551b2bc12529).

- [`API.RemoveState`](class_s_o_s_1_1_a_p_i.html#a1836877804dc9638623a40a6acc5f8b8): Permite eliminar por completo un estado guardado.

### Ejemplos {#2_examples}

    ...
    <!-- TODO: Añadir los ejemplos... -->

## Otros recursos

- Ver: [`EventPriority`](class_s_o_s_1_1_event_priority.html)
- Ver: [`CommKeys`](class_s_o_s_1_1_comm_keys.html)

**Next to: [UI and GUI Components](ui_comps.html)**

**Back to: [Registration](registration.html)**
