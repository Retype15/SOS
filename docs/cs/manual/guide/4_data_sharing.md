# Data Sharing {#data_sharing}

Un sistema de suscripcion de eventos y almacen de datos compartidos bidireccional para todos los mods para evitar referencias duras entre todos, así como asegurar orden lógico y predecible de ejecución.

Cada método necesita recibir como primer parámetro **string key**: clave o identificador de puente como primer parámetro. Las claves comúnes están definidas y documentadas en la clase estática *SOS.CommKeys*.

Ver: [`CommKeys`](class_s_o_s_1_1_comm_keys.html).

## 1. Transmisores de datos compartido

Métodos de suscripción y llamadas usando delegados.

Los Layers recomendados a usar lo define la clase estática `SOS.EventPriority`.

Cada uno de los métodos aceptan opcionalmente **double order = 0** para añadir/eliminar/llamar el método de forma atómica en el layer de orden definido y define el orden de ejecución de los delegados.

Cada grupo de delegados agrupado por el mismo valor **order** lo llamaremos **layer**. Los Layers recomendados a usar los define la clase estática `SOS.EventPriority`.

> [!NOTE]
> Recomendado pasar el mismo **layer** usado en `SOS.API.On` a la desuscripción `SOS.API.Off` por optimización, pero no es obligatorio.
>
> `double order` se acota la parte de punto flotante desde *.0* a *.9999* para evitar errores relacionados con la precisión y establecer un límite claro y seguro para layers intermedios.

- `SOS.API.On`: Permite suscribir delegados que son llamados al emitir usando `SOS.API.Emit` y ejecutados en orden definido por su **layer**. acepta `Action`/`Action<T>` *handler* como segundo parámetro, y es el método que es llamado cuando se emite con su misma `key`. `T` Describe el tipo que recibe el método, si este requiere `T`, solo será ejecutado si el emisor `SOS.API.Emit<T>` como por `SOS.API.Emit<T>`

- `SOS.API.Off`: Permite desuscribir un delegado del bus de eventos para limpiar referencias. Requiere key, y el mismo *handler* que suscribió usando `SOS.API.On` anteriormente. Recuerde llamar apropiadamente este método cuando deje de necesitar escuchar. Si recibe **order** solo desuscribirá el **handler** del layer específico proporcionado, si recibe `null` intenta desuscribir el **handler** de cada uno de los layers registrados.

- `SOS.API.Emit`: Permite emitir un valor y llamar a todos los métodos suscritos por la `key` proporcionada. Acepta opcionalmente **value : `T`** para emitir pasando a los métodos registrados un objeto de tipo `T`. Acepta **order** para llamar todos los métodos que estén suscritos únicamente a ese **layer**. Si es parametrizado `T` acepta `bool setState = true` el cual guardará usando `SOS.API.SetState<T>` luego de emitir satisfactoriamente si valor si es `true`, otherwise ejecuta sin guardar.

- `SOS.API.EmitRange`: Permite emitir un valor y llamar a todos los métodos suscritos por la `key` proporcionada. Acepta opcionalmente `double min = double.MinValue`, `double max = double.MaxValue` para llamar a los métodos de un rango de layers para casos de uso avanzados. Si es parametrizado `T` acepta `bool setState = true` el cual guardará usando `SOS.SOS.API.SetState<T>` luego de emitir satisfactoriamente si valor si es `true`, otherwise ejecuta sin gaurdar/actualizar el valor.

> [!IMPORTANT]
> Emitir pasando un objeto `T` ejecutará todo método suscrito que acepte el parámetro `T` o no tenga parámetro, pero emitir sin parámetros solo llamará a los metodos sin parámetros, téngalo en cuenta.

### Ejemplos {#1_examples}

  ...
  <!-- TODO: Añadir los ejemplos... -->

## 2. Almacén de datos

Para guardar objetos o datos compartidos públicos para todos los mods

- `SOS.API.SetState<T>`: Permite guardar una variable de tipo `T` en un estado compartido público fácilmente accesibles desde cualquier mod.

- `SOS.API.GetState<T>`: Permite obtener la variable de tipo `T` guardada en el estado compartido público antes guardada por `SOS.API.SetState<T>`.

- `SOS.API.RemoveState`: Permite eliminar por completo un estado guardado.

### Ejemplos {#2_examples}

    ...
    <!-- TODO: Añadir los ejemplos... -->

## Otros recursos

**Next to: [UI and GUI Components](ui_comps.html)**

**Back to: [Registration](registration.html)**
