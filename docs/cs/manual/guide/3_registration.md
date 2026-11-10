# Registration {#registration}

After creating and defining your class, you will have to register it to the SOS API so that it is recognized and validated.

For that, there are two main ways to register a new module to SOS:

## 1. Automatic Registration

Using the `SOS.AutoRegisterAttribute` attribute over an SOS module (class that implements some [ISOS*](sos_modules.html) interface), for automatic registration the first time the SOS window is opened (never before) passing the optional registration properties:

- `string id`: The unique identifier for your type, if another object of the same type tries to register later with the same identifier, it will be overwritten. **Default: type.FullName ?? type.Name**. If you register an SOS module with the same name as a previous one, it will be overwritten, keep it in mind if you wish to overwrite SOS modules from other mods for custom logic.

- `double order`: Order of execution or call. Ascending order. they are called from lowest to highest, if it is equal it is treated alphabetically by the Id, allowing them to be ordered in the calls in a predictable way. **Default: 0**

- `bool active`: Indicates if by default this SOS module will be executed or not. It allows adding activation/deactivation logic of SOS modules for conditional logic, or allowing the player to consciously deactivate SOS modules so as not to overload their interface with information that they do not need. **Default: True**

> [!TIP]
> Using this method, the class is discovered automatically. It is the idiomatic way to register classes that implement our interfaces.
>
> Since this method does not require executing manual code to register the classes, the Plugin class can be completely obviated if your mod's objective is only to add content to SOS.

## 2. Manual registration

Using any `API.Register*` method (e.g: SOS.API.RegisterStatInfo) and passing an object that represents the instantiation method and the same optional properties previously described after the object (***obj***, id, order, active). You can register generic objects and Lua tables that implement directly or indirectly the contract's methods.
  
There are several ways to register an SOS module
  
- `class\<T\>`: Direct instance of an object that implements the contract. This is the simplest way to pass a single global instance and avoid instantiating.

  ```csharp
  SOS.API.RegisterStatInfo(new TreatmentStatInfo(), "MyMod.TreatmentInfo", 1);
  ```

- `Func\<T\>`: Pass a function that returns the instance that is wanted, useful to pass a single instance with a custom method for instantiation, for example compatibility with the singleton pattern or a custom factory.

  ```csharp
  SOS.API.RegisterConfig(() => MyModConfig.Instance, "MyMod.MainConfig", 0);
  ```

- `object`: Generic objects that do not implement the contract directly, or Lua tables(Lua compatibility). In these cases reflection is used to try to link the methods of the received instance with the methods that the contract requires using a custom proxy [`object.Cast<T>()`](lua_interop.html), allowing to register indirectly classes without the need of making a hard reference to the S.O.S SDK, simply obtaining by reflection the manual registration method `API.Register*` and passing an object that fulfills the contract indirectly (by method name).

  ```csharp
  using Barotrauma;

  public class MyStatInfoSoft
  {
      public static string ID => "MyMod.MyStatInfoSoft";
      public void Draw(GUIListBox contentPanel, Prefab prefab)
      {
          //...
      }
  }

  //IMPORTANT: The last assembly loaded by the assembly leak of LuaCsForBarotrauma must be obtained.
  // There are cleaner ways to do it. This is only an example.
  var apiType = AppDomain.CurrentDomain.GetAssemblies().LastOrDefault(t => t.GetType("SOS.API") != null)?.GetType("SOS.API");
  if (apiType == null)
  {
      LuaCsLogger.LogError("[MyMod] No S.O.S loaded");
      return;
  }

  var registerMethod = apiType.GetMethod("RegisterStatInfo", BindingFlags.Public | BindingFlags.Static, null, [typeof(object), typeo(string), typeof(double), typeof(bool)], null);

  if (registerMethod == null)
  {
      LuaCsLogger.LogError("[MyMod] API.RegisterStatInfo method not found!");
      return;
  }

  registerMethod.Invoke(apiType, [new MyStatInfoSoft(), MyStatInfoSoft.ID, -1]);
  ```

  > [!NOTE]
  > Every unknown pattern falls in this branch, and therefore it will try to be cast using the property and the method names over this object(or its target if it is a DuckProxy). Do not try to register arbitrary types that clearly do not fulfill the contract nor register in an indefinite loop because it could penalize the performance. Make sure that they are valid.

- `Func\<object\>`: Delegate to register an instance builder using a generic object that implements the interface indirectly, or a Lua table.

  ```csharp
  registerMethod.Invoke(apiType, [() => new MyStatInfoSoft(), MyStatInfoSoft.ID, 1]);
  ```

- `Type : T`: Pass a class type that implements the instance's contract. It is the default method used by the automatic registration.

  ```csharp
  SOS.API.RegisterStatInfo(typeof(TreatmentStatInfo), "MyMod.TreatmentInfo", 0.2);
  ```

- `Type : object`: Pass a generic type that implements the instance's contract indirectly.

  ```csharp
  registerMethod.Invoke(apiType, [typeof(MyStatInfoSoft), MyStatInfoSoft.ID]);
  ```

<!-- TODO: Explain about DefaultClassAtribute and default methods for Lua?(redirect to lua_interop for more info) -->

<!-- TODO: Remember to talk about IDisposable for generic objects. -->

## Other resources

**Next to: [Data Sharing](data_sharing.html)**

**Back to: [SOS Modules](sos_modules.html)**
