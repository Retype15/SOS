# Introductory Guide {#guide}

Esta es la guía manual donde se abarcará con el mayor detalle posible todo lo que necesita saber sobre la creacion de Módulos SOS o plug-ins para el mod **S.O.S - Standard Operations Schematics**.

## Lista de Contenido

1. @subpage getting_started - Aquí se explica lo más básico como la forma de crear un mod, requisitos, e incluye un ejemplo en código tanto C# como Lua de cómo desarrollar con nuestro SDK.
2. @subpage sos_modules - Esta página define lo que consideramos un Módulo SOS (o plug-in SOS), y posteriormente se explica cada tipo de Módulo admitido y cómo usted puede implementar sus própios módulos de forma vanilla.
3. @subpage registration - Registrar su módulo por primera vez es muy sencillo, esta página explica en detalle cada forma que existe actualmente para registrar un módulo.
4. @subpage data_sharing - Para una comunicación efectiva entre mods de forma descentralizada, es que aquí se explica la forma canónica para enganchar sus delegados como hooks para una ejecución predecible y ordenada justo cuando lo necesites, además de también cómo guardar datos compartidos para todos de forma sencilla y thread-safe.
5. @subpage ui_comps - Para liberar todo el poder del SDK, este expone varios componentes GUIs personalizados y varios helpers para hacer la vida del modder más fácil, esta sección se encarga de explicar todos los miembros nuevos más importantes que debería conocer.

> [!NOTE]
> Esta documentación aún está en construccion. Puede ver la documentacion auto-generada para conocer a detalle cada clase y uso a partir del *namespace SOS*.
>
> Ante cualquier duda, recomendamos unirte al [Discord oficial](getting_started.html) de SOS y pedir ayuda.
