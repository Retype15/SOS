[h1]QoL Mod — JEI for Barotrauma[/h1]
[h2]S.O.S. — Standard Operations Schematics[/h2]

[b]S.O.S.[/b] is a high-performance recipe browser and object tracking utility for Barotrauma. Designed to be the ultimate companion for both vanilla and heavily modded campaigns, it provides a seamless, integrated interface to explore the complex systems of Europa. It's like JEI for Barotrauma.

[h2]Key Features[/h2]
[list]
[*] [b]Comprehensive Browser:[/b] View Fabrication, Deconstruction, and "Used In" recipes for any object in the game.
[*] [b]Affliction Browser:[/b] Browse detailed information of ALL afflictions (Status Effects), including modded ones. View their effects, treatments, contraindications, and more — just like items.
[*] [b]Preview Tab:[/b] Inspect the visual sprite of the selected object.
[*] [b]HUD Tracker:[/b] Track fabrication recipes in real-time with an on-screen checklist. Supports per-recipe tracking — items with multiple recipes show a submenu to pick which to track. Use Ctrl+[SOSKey] or the Show/Hide button to toggle visibility.
[*] [b]Dynamic Meta-Info:[/b] View base prices, object tags, stack sizes, and detailed descriptions in a structured Wiki-style panel.
[*] [b]Detailed Recipe Analytics:[/b] Refined "Obtain" and "Usage" sections with context-aware filtering and smart ingredient wrapping.
[*] [b]Smart List Filters:[/b] Click on the separator bars (Items, Afflictions, etc.) in the results list to instantly filter by that object type.
[*] [b]Responsive UI:[/b] High-precision resizable interface that ensures the UI is always displayed in any position, scale or aspect ratio you want, as a real window.
[*] [b]Favorites & History:[/b] Web-browser style navigation (Back/Forward) and a pinning system for quick access to frequent items.
[*] [b]Multi-language Support:[/b] Native support for English, Spanish, Russian, French, and Chinese. (Last 3 are fully translated by AI, if anyone wants to correct them, are free to make a pull request and help us.)
[/list]

[h2]Controls[/h2]
[b]General[/b]
[list]
[*] [b][J][/b]: Open / Close the SOS Menu. When used over an item (on inventories, recipe in crafting menu, and any item in shop), it will open the SOS Menu with the item the mouse is hovering over.
[*] [b][Shift + J][/b]: Open / Close the SOS Menu with the world object (light, deconstructor, walls, items in world, etc) the mouse is hovering over.
[*] [b][Ctrl + J][/b]: Toggle recipe tracker visibility on the HUD.
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
[*] [b][Ctrl + Drag Borders or Corners][/b]: Resize the window with a parallel aspect ratio.
[*] [b][Shift + Drag Borders or Corners][/b]: Move the window.
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
| Filter    | Description                 | Example                         |
|-----------|-----------------------------|---------------------------------|
| @Mod      | Mod Name                    | @Vanilla @Neuro                 |
| #Category | Category                    | #Medical #Weapon                |
| $Tag      | Tag                         | $smallitem $pill                |
| &Slot     | Slot                        | &Head &Inner                    |
| !ID       | Item ID                     | !weldingtool                    |
| %Type     | Class Type Name             | %Item %Affliction               |
[/code]
[i]Example:[/i] [code]Brain @NT #Medical $surgery %Item[/code]

[h2][NEW] For Modders[/h2]
S.O.S. can now be extended: other mods can add inspector tabs, wiki sections, new browser data providers, their own settings and even full window layouts, in both C# and Lua. You can follow the modding guide to get started.

[h2]Other Plugins[/h2]
[h3]Official Plugins[/h3]
[list]
[*] [url=https://github.com/Retype15/SOS-DummyManager][b]SOS-DummyManager - Beta[/b][/url] — Clinical simulator: test afflictions and treatments on a dummy patient without consuming resources or risking your crew. (Old Clinical Simulator system in Beta was moved here, remains as Beta)
[/list]

[h3]Recommended Community Mods[/h3]
[i]Nothing here at the moment...[/i]

[h2]Common Questions (FAQ)[/h2]
[b]Q: Can it be used on vanilla servers?[/b]
A: Absolutely, this is a client-side mod, so it only matters for you — it doesn't affect any other player or server you play on.

[b]Q: Is it compatible with all mods?[/b]
A: At the moment, yes. It should be compatible with ALL content mods. If you encounter any error or bug with any other mods, please report them on GitHub or the Steam page.

[b]Q: I'm a modder and I have a mod where I want to show special information or custom behaviors for my mod's items — can I?[/b]
A: Of course you can! S.O.S. is designed to be easily extensible by any other mod that wants it, natively. You can add custom behaviors to almost any information window and add your own windows with organized, reactive, and fully customized information! Check the official documentation where we explain in detail how to do it.

[h2]License & Copyright[/h2]
This project is licensed under the [b]GNU General Public License v3.0 (GPLv3)[/b].
See the [url=https://github.com/Retype15/SOS/blob/main/LICENCE]LICENCE[/url] file in the project root for the full text of the license.

[b]Key Terms[/b]
[list]
[*] [b]Freedom to Use and Modify[/b] — You may use, study, modify, and run this software for any purpose, both privately and publicly.
[*] [b]Attribution Required[/b] — Any redistribution or publication of this project or its derivatives must retain the original copyright notice and clearly credit the author ([url=https://github.com/Retype15]@Retype15[/url]).
[*] [b]Copyleft Protection[/b] — Any modified version that is distributed must also be licensed under [b]GPLv3[/b], ensuring that all derivatives remain free and open.
[*] [b]Source Availability[/b] — If you distribute a modified version, you must also provide access to the corresponding source code under the same license terms.
[/list]

---
[b]Github Project:[/b] [url=https://github.com/Retype15/SOS]SOS Repository[/url]
[b]Developed by:[/b] [url=https://github.com/Retype15]@Retype15[/url]


[url=https://steamcommunity.com/sharedfiles/filedetails/?id=3682891282][img]https://img.shields.io/badge/Subscribe-2EA043?style=for-the-badge&logo=steam&logoColor=white[/img][/url]  [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3682891282][img]https://img.shields.io/badge/Add%20to%20Favorites-F0C040?style=for-the-badge&logo=steam&logoColor=white[/img][/url]  [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3682891282][img]https://img.shields.io/badge/Rate%20+1-3B82F6?style=for-the-badge&logo=steam&logoColor=white[/img][/url]

[i]If you enjoy the mod, please consider subscribing, adding to favorites, and leaving a positive rating. It helps a lot![/i]