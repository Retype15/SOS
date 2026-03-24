# S.O.S. - Standard Operations Schematics

![Banner](Assets/SOS_Background.png)

---

[![GitHub – Download](https://img.shields.io/badge/GitHub-Download-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Retype15/SOS/releases/latest/download/SOS.zip)
[![Itch.io – Visit Site](https://img.shields.io/badge/Itch.io-Visit%20Site-FA5C5C?style=for-the-badge&logo=itch.io&logoColor=white)](https://retype15.itch.io/sos)
[![Steam Workshop](https://img.shields.io/badge/Steam_Workshop-Add_here-1B2838?style=for-the-badge&logo=steam&logoColor=white)](https://steamcommunity.com/sharedfiles/filedetails/?id=3682891282)

---

**S.O.S.** is a high-performance recipe browser and material tracking utility for **Barotrauma**. Designed to be the ultimate companion for both vanilla and heavily modded campaigns (like Neurotrauma or BaroCraftables), it provides a seamless, integrated interface to explore the complex economy of Europa.

[![Add from Workshop](https://img.shields.io/badge/Add_From-Steam_Workshop-1B2838?style=for-the-badge&logo=steam&logoColor=white)](https://steamcommunity.com/sharedfiles/filedetails/?id=3682891282)

[![Manually Download Latest Version](https://img.shields.io/badge/Manually_Download_Latest_Version-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Retype15/SOS/releases/latest/download/SOS.zip)

## Key Features

- **Comprehensive Browser:** View Fabrication, Deconstruction, and "Used In" recipes for any item in the game.
- **HUD Tracker:** Track ingredients in real-time with an on-screen checklist that updates as you gather materials.
- **Dynamic Meta-Info:** View base prices, item tags, stack sizes, and detailed descriptions in a structured Wiki-style panel.
- **Responsive UI:** High-precision resizable interface that ensures the UI is always displayed in any position, scale or aspect ratio do you want, as a real window.
- **Adaptive Layout Modes:** Smart "Compact" view that intelligently wraps icon grids and scales content to fit any window dimension.
- **Detailed Recipe Analytics:** Refined "Obtain" and "Usage" sections with context-aware filtering and smart ingredient wrapping.
- **Favorites & History:** Web-browser style navigation (Back/Forward) and a pinning system for quick access to frequent items.
- **Multi-language Support:** Native support for English, Spanish, Russian, French, and Chinese. (Last 3 are translated by AI, if anyone wants to correct them, are free to make a pull request and help us.)

## Controls

### General

- **[J]**: Open / Close the SOS Menu.
- **[Backspace]** or **[Mouse 4]**: Navigate to previous item.
- **[Ctrl + Backspace]** or **[Mouse 5]**: Navigate to next item.
- **[Left Click]**: Select item / Navigate to ingredient.
- **[Right Click]**: Open context menu (Track item, Toggle Favorite, etc.).
- **[Escape]**: Close window.

### Window

- **[Drag Title Bar]**: Move the window.
- **[Drag Borders or Corners]**: Resize the window.
- **[Ctrl + Drag Borders or Corners]**: Resize the window with a parallel aspect ratio.
- **[Shift + Drag Borders or Corners]**: Move the window.

### Search Tab

Search by Name, ID, Category, Tags, ModName, ItemType, and other filters.

Advanced Filters:

| Filter      | Description     | Example           |
|-------------|-----------------|-------------------|
| `@Mod`      | Mod Name        | `@Vanilla @Neuro` |
| `#Category` | Category        | `#Medical #Weapon`|
| `$Tag`      | Tag             | `$smallitem $pill`|
| `&Slot`     | Slot            | `&Head &Inner`    |
| `!ID`       | Item ID         | `!weldingtool`    |

**Example:** `Brain @NT #Medical $surgery`

## Project Status: Beta

**S.O.S.** is currently in its Beta stage. While the core functionality is stable and high-performing, we are working towards deep integration with the game's mechanics and immersion.

### Late Mod Roadmap: The Neural Link System

In future updates, access to the S.O.S. interface will be gated behind a **Chip Progression System**. Players will need to craft and consume specialized neural chips to unlock different modules of the database.

#### Tiered Modules

- **Fabricator Chip (Lv. 1):** Unlocks the ability to view crafting recipes (Compatible with vanilla and modded stations).
- **Fabricator Chip (Lv. 2):** Unlocks "Reverse Lookup" (View what items can be crafted using the selected material).
- **Deconstructor Chip (Lv. 1):** Unlocks deconstruction yield data.
- **Deconstructor Chip (Lv. 2):** Unlocks "Reverse Deconstruction" (View which items provide this material when recycled).
- **Medical Chip (Lv. 1 & 2):** Specialized modules for medical recipes and advanced chemistry.

> Note: This proposal is merely an idea at the moment; there is no guarantee that it will be implemented in this specific manner in the future—it could take a different, more convenient form.

*Stay tuned for these updates as we move toward the 1.0 Full Release.*

---

## Common questions

**Q:** *Can it be used on vanilla servers?*

- **A:** Absolutely. The most it will require from the server is new items, nothing more.

**Q:** *Is this a client-only mod, or does it need to be included on the server?*

- **A:** Currently, it's entirely client-only. We plan for it to always be client-only, so you can use it even on a completely vanilla server without mods. If the server doesn't have it, then the full system unlocked in the beta version will be used.

**Q:** *Is it really compatible with ALL in-game items?*

- **A:** *Yes! Absolutely everything, including submarine parts that are impossible to obtain. I've decided not to exclude these items for now for descriptions and another useful metadata. If they bother you, you can create an issue in the Git project, leave a comment on itch.io, or contact me directly, and I'll prioritize it.*

## License & Copyright

This project is licensed under the **GNU General Public License v3.0 (GPLv3)**.  
See the [LICENSE](LICENCE) file in the project root for the full text of the license.

### Key Terms

- **Freedom to Use and Modify** — You may use, study, modify, and run this software for any purpose, both privately and publicly.
- **Attribution Required** — Any redistribution or publication of this project or its derivatives must retain the original copyright notice and clearly credit the author (**[@Retype15](https://github.com/Retype15)**).
- **Copyleft Protection** — Any modified version that is distributed must also be licensed under **GPLv3**, ensuring that all derivatives remain free and open.
- **Source Availability** — If you distribute a modified version, you must also provide access to the corresponding source code under the same license terms.

---
*Developed by [@Retype15](https://github.com/Retype15)*
