# Data Sharing {#data_sharing}

A bidirectional event subscription system and shared data store for all mods to avoid hard references between all of them, as well as ensuring a logical and predictable order of execution.

Every method needs to receive as first parameter **string key**: bridge key or identifier as first parameter. The common keys are defined and documented in the static class *SOS.CommKeys*.

## 1. Shared data transmitters

Subscription and call methods using delegates.

The Layers recommended to use are defined by the static class `SOS.EventPriority`.

Each one of the methods optionally accept **double order = 0** to add/remove/call the method atomically in the defined order layer and define the execution order of the delegates.

Each group of delegates grouped by the same **order** value we will call **layer**. The Layers recommended to use are defined by the static class `SOS.EventPriority`.

> [!NOTE]
> Recommended to pass the same **layer** used in `SOS.API.On` to the unsubscription `SOS.API.Off` for optimization, but it is not mandatory.
>
> `double order` clamps the floating point part from *.0* to *.9999* to avoid errors related to precision and establish a clear and safe limit for intermediate layers.

- `SOS.API.On`: Allows subscribing delegates that are called when emitting using `SOS.API.Emit` and executed in the order defined by their **layer**. it accepts `Action`/`Action<T>` *handler* as second parameter, and it is the method that is called when emitting with its same `key`. `T` Describes the type that the method receives, if it requires `T`, it will only be executed if the emitter `SOS.API.Emit<T>` as by `SOS.API.Emit<T>`

- `SOS.API.Off`: Allows unsubscribing a delegate from the event bus to clean references. It requires key, and the same *handler* that subscribed using `SOS.API.On` previously. Remember to call this method appropriately when you stop needing to listen. If it receives **order** it will only unsubscribe the **handler** from the specific provided layer, if it receives `null` it tries to unsubscribe the **handler** from each one of the registered layers.

- `SOS.API.Emit`: Allows emitting a value and calling all the methods subscribed by the provided `key`. It optionally accepts **value : `T`** to emit passing an object of type `T` to the registered methods. It accepts **order** to call all the methods that are subscribed only to that **layer**. If it is parameterized `T` it accepts `bool setState = true` which will save using `SOS.API.SetState<T>` after emitting successfully if the value if it is `true`, otherwise it executes without saving.

- `SOS.API.EmitRange`: Allows emitting a value and calling all the methods subscribed by the provided `key`. It optionally accepts `double min = double.MinValue`, `double max = double.MaxValue` to call the methods of a range of layers for advanced use cases. If it is parameterized `T` it accepts `bool setState = true` which will save using `SOS.SOS.API.SetState<T>` after emitting successfully if the value if it is `true`, otherwise it executes without saving/updating the value.

> [!IMPORTANT]
> Emitting passing an object `T` will execute every subscribed method that accepts the `T` parameter or that has no parameter, but emitting without parameters will only call the methods without parameters, keep that in mind.

### Examples {#examples_1}

...

<!-- TODO: Add the examples... -->

## 2. Data store

To save objects or shared data public for all mods

- `SOS.API.SetState<T>`: Allows saving a variable of type `T` in a public shared state easily accessible from any mod.

- `SOS.API.GetState<T>`: Allows obtaining the variable of type `T` saved in the public shared state previously saved by `SOS.API.SetState<T>`.

- `SOS.API.RemoveState`: Allows deleting a saved state completely.

### Examples {#examples_2}

...

<!-- TODO: Add the examples... -->

## Other resources

**Next to: [UI and GUI Components](ui_comps.html)**

**Back to: [Registration](registration.html)**
