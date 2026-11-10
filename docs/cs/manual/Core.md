# S.O.S - Core {#core}

This file is only for reference to the base project. The modder works against the SDK; here you only see what the vanilla Core implements in order to copy patterns.

It is the base which fulfills two functions: Manage the main window, crucial initialization, cycles and hard internal keyboard events through `SOSController`, and implement the mod's vanilla components, like for example locating items and their recipes by means of the project's tools, the vanilla Profile, etc.

It is composed of the controller `SOSController`, who is in charge of managing the key events and initialization, and the base components that the previous versions already show, with the exception of `SimulatorManager` who was pushed as an optional mod separated from the `Core`. *see:* **[SOS - Dummy Manager](https://github.com/retype15/SOS-DummyManager)**.

You will never work with this one, but we will document it anyway because it is useful to understand the internal functioning and to see examples of the components already implemented by the base SOS.

  **Implements:**

- **Prefabs to track (ISOSPrefab):**
  - [`ItemPrefabProvider`:](https://github.com/Retype15/SOS/blob/main/ClientProject/ClientSource/Prefabs/ItemPrefab.cs) Adds all the Items.
  - [`AfflictionPrefabProvider`:](https://github.com/Retype15/SOS/blob/main/ClientProject/ClientSource/Prefabs/AfflictionPrefab.cs) Adds all the afflictions. It makes a distinction about the Husk afflictions using another empty subscriber [`AfflictionHuskPrefabProvider`:](https://github.com/Retype15/SOS/blob/main/ClientProject/ClientSource/Prefabs/AfflictionPrefab.cs).

- **Central panels (ISOSTab):**
  - [ItemPanel:](https://github.com/Retype15/SOS/blob/main/ClientProject/ClientSource/Panels/ItemPanel/ItemPanel.cs) Component that inherits from `SOS.ISOSTab` and implements the list of recipes of the selected item. *See:* **[Barotrauma.ItemPrefab](//)**
  - [`PreviewPanel`:](//) It is a simple example panel that simply shows the main image of the selected component. Written as an individual component in Lua to demonstrate full and clean execution capabilities of components that come from Lua.

- **Window Profiles (ISOSWindowProfile):**
  - [`ThreeColumnWindowProfile`:](//) Adds the classic visual profile of the mod with a resizable window.

- **Information and statistics badges (ISOSStatInfo):**
  - [`*StatInfo`:](//) They add clean information about Items or afflictions.
  - ...

> [!NOTE]
> This document is incomplete, considere seeing the auto-generated documentation, or if you wish you can ask for help in the [official discord](//) of the mod.
