# S.O.S - Core {#core}

Este archivo es solamente de referencia al proyecto base. El modder trabaja contra el SDK; aquí solo ve qué implementa el Core vanilla para copiar patrones.

Es el base el cual cumple dos funciones: Gestionar la ventana principal, inicialización crucial, ciclos y eventos de teclado internos duros a través de `SOSController`, e implementar los componentes vanilla del mod, como por ejemplo localizar items y sus recetas por medio de las herramientas del proyecto, el Profile vanilla, etc.

Está compuesto por el controlador `SOSController`, quien se encarga gestionar los eventos de teclas e inicialización, y los componentes base que ya muestran las versiones anteriores, a excepción de `SimulatorManager` quien fue empujado como mod opcional separado del `Core`. *ver:* **[SOS - Dummy Manager](https://github.com/retype15/SOS-DummyManager)**.

Nunca trabajará con este, pero documentaremos igual porque es útil para entender funcionamiento interno y ver ejemplos de los componentes ya implementados por SOS base.

  **Implementa:**

- **Prefabs a rastrear (ISOSPrefab):**
  - [`ItemPrefabProvider`:](https://github.com/Retype15/SOS/blob/main/ClientProject/ClientSource/Prefabs/ItemPrefab.cs) Agrega todos los Items.
  - [`AfflictionPrefabProvider`:](https://github.com/Retype15/SOS/blob/main/ClientProject/ClientSource/Prefabs/AfflictionPrefab.cs) Agrega todas las aflicciones. Hace distinción sobre las aflicciones Husk usando otro subscriptor vacío [`AfflictionHuskPrefabProvider`:](https://github.com/Retype15/SOS/blob/main/ClientProject/ClientSource/Prefabs/AfflictionPrefab.cs).

- **Paneles centrales (ISOSTab):**
  - [ItemPanel:](https://github.com/Retype15/SOS/blob/main/ClientProject/ClientSource/Panels/ItemPanel/ItemPanel.cs) Componente que hereda de `SOS.ISOSTab` e implementa la lista de recetas del item seleccionado. *Ver:* **[Barotrauma.ItemPrefab](//)**
  - [`PreviewPanel`:](//) Es un simple panel de ejemplo que simplemente muestra la imagen principal del componente seleccionado. Escrito como componente individual en Lua para demostrar capacidades de ejecución completa y limpia de componentes que provienen de Lua.

- **Perfiles de Ventana (ISOSWindowProfile):**
  - [`ThreeColumnWindowProfile`:](//) Agrega el perfil visual clásico del mod con ventana redimensionable.

- **Badges de información y estadísticas (ISOSStatInfo):**
  - [`*StatInfo`:](//) Agregan información limpia sobre Items o aflicciones.
  - ...

> [!NOTE]
> Este documento está incompleto, cosidere ver la documentación auto-generada, o si lo desea puede pedir ayuda en el [discord oficial](//) del mod.
