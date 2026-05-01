[h1]QoL Mod - JEI for Barotrauma[/h1]
[h2]S.O.S. - Standard Operations Schematics[/h2]

[b]S.O.S.[/b] is a high-performance recipe browser and material tracking utility for Barotrauma. Designed to be the ultimate companion for both vanilla and heavily modded campaigns, it provides a seamless, integrated interface to explore the complex systems of Europa. It's like JEI for Barotrauma.

[h2]Key Features[/h2]
[list]
[*] [b]Comprehensive Browser:[/b] View Fabrication, Deconstruction, and "Used In" recipes for any item in the game.
[*] [b]NEW! Now Includes Afflictions![/b] View information of ALL afflictions (Status Effects), including those from mods.
[*] [b]HUD Tracker:[/b] Track ingredients in real-time with an on-screen checklist that updates as you gather materials.
[*] [b]Dynamic Meta-Info:[/b] View base prices, item tags, stack sizes, and detailed descriptions in a structured Wiki-style panel.
[*] [b]Favorites System:[/b] Pin your most-used items to the top of the search results for instant access.
[*] [b]Smart Navigation:[/b] Web-browser style history (Back/Forward) with full keyboard and mouse shortcut support.
[*] [b]Multi-language:[/b] Native support for English, Spanish, French, Chinese and Russian (Last 3 were AI-generated; manual translations are welcome!).
[/list]

[h2]Controls[/h2]
[b]General[/b]
[list]
[*] [b][J][/b]: Open / Close the SOS Menu. When used over an item(on inventories, recipe in crafting menu, and any item in shop), it will open the SOS Menu with the item the mouse is hovering over.
[*] [b][Shift + J][/b]: Open / Close the SOS Menu with the world object (light, deconstructor, walls, items in world, etc) the mouse is hovering over.
[*] [b][Alt + Left Arrow][/b], [b][Backspace][/b] or [b][Mouse 4][/b]: Navigate to previous item.
[*] [b][Alt + Right Arrow][/b], [b][Shift + Backspace][/b] or [b][Mouse 5][/b]: Navigate to next item.
[*] [b][Left Click][/b]: Select item / Navigate to ingredient.
[*] [b][Right Click][/b]: Open context menu (Track item, Toggle Favorite, etc.).
[*] [b][Escape][/b]: Close window.
[/list]

[b]Window[/b]
[list]
[*] [b][Drag Title Bar][/b]: Move the window.
[*] [b][Drag Borders or Corners][/b]: Resize the window.
[*] [b][Ctrl + Drag Borders][/b]: Resize with parallel aspect ratio.
[*] [b][Shift + Drag Borders][/b]: Move the window.
[/list]

[b]In XML View[/b]
[list]
[*] [b][Mouse Wheel][/b]: Move Vertically.
[*] [b][Shift + Mouse Wheel][/b]: Move Horizontally.
[*] [b][Ctrl + Mouse Wheel][/b]: Apply Zoom.
[/list]

[h2]Search Tab[/h2]
Search by Name, ID, Category, Tags, ModName, ItemType, and other filters.

[b]Advanced Filters:[/b]
[code]
| Filter    | Description                 | Example                     |
|-----------|-----------------------------|-----------------------------|
| @Mod      | Mod Name                    | @Vanilla @Neuro             |
| #Category | Category                    | #Medical #Weapon            |
| $Tag      | Tag                         | $smallitem $pill            |
| &Slot     | Slot                        | &Head &Inner                |
| !ID       | Item ID                     | !weldingtool                |
| %Type     | Class Type Name             | %item %affliction           |
[/code]
[i]Example:[/i] [code]Brain @NT #Medical $surgery %item[/code]

[h2]Project Status: Beta[/h2]
S.O.S. is currently in its [b]Beta stage[/b]. While the core functionality is stable and high-performing, we are working towards deep integration with the game's mechanics and immersion. Stay tuned for the 1.0 Full Release.

[h2]Common Questions (FAQ)[/h2]
[b]Q: Can it be used on vanilla servers?[/b]
A: Absolutely, nothing will change for other players.

[b]Q: Is this a client-only mod?[/b]
A: Currently, it's entirely client-only. You can use it even on a completely vanilla server without mods.

[b]Q: Is it compatible with ALL in-game items?[/b]
A: Yes! Everything, including submarine parts and items from mods. If certain items bother you, please report it on GitHub or itch.io.

---
[b]Github Project:[/b] [url=https://github.com/Retype15/SOS]SOS Repository[/url]
[b]Developed by:[/b] [url=https://github.com/Retype15]@Retype15[/url]