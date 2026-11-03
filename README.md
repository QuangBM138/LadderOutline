# LadderOutline

LadderOutline makes finding ladders and shafts (holes) in the mines easier by highlighting them with customizable outlines and directional pointers. Never lose track of where a ladder or hole spawned again!

## 🚀 What's New in v1.1.0

- **Directional Pointers (Con trỏ chỉ hướng)**:
  - Displays directional arrow indicators pointing toward ladders and shafts/holes.
  - Supports displaying multiple indicators simultaneously if multiple ladders or holes exist on the floor.
  - Multi-pointer overlap prevention (arrows separate neatly along the screen edge).
  - Customizable pointer styles: `ScreenEdge` (edges of the screen), `PlayerOrbit` (circling the farmer), or `Both`.
  - Floating bouncing markers above ladders/holes when visible on screen.
  - Optional distance display in tiles/meters (e.g. `15m`).
  - Optional target type display (`Ladder` vs `Hole`).
- **Support for Shafts / Holes (Hố nhảy)**:
  - Fully supports both ladders (tile 173) and shafts/jumping holes in Skull Cavern (tile 174).
  - Distinct outline and pointer colors for ladders vs shafts.
- **Full Internationalization (i18n)**:
  - Supports SMAPI's standard `i18n` localization system.
  - Built-in translations: English (`default.json`) and Vietnamese (`vi.json`).
  - All GMCM options, in-game notifications, and pointer labels are fully translatable.
- **Enhanced Configuration**:
  - Full Generic Mod Config Menu (GMCM) support with organized sections (General, Outline, Pointer).
  - Customizable colors, opacity, thickness, scale, and pulse animations.

## 🌟 Features

- **Highlight ladders & shafts**: Adds visual outline borders around ladder and hole tiles in the mines.
- **Directional indicators**: Points the way toward all active ladders and holes on the current floor, even when far offscreen.
- **Multiple targets**: If breaking rocks or killing monsters spawns multiple ladders or shafts, all are tracked and pointed to.
- **Fully customizable settings** via GMCM:
  - Outline colors, opacity, and thickness.
  - Pointer colors, styles, distance display, and scaling.
  - Quick toggle key.
- **Enhanced mining efficiency**: Saves time and food while exploring the Mines, Skull Cavern, and Quarry Mine.

## 🌐 Translations (i18n)

LadderOutline supports community translations through SMAPI's standard `i18n` format:
- English (`default.json`) - *included*
- Vietnamese (`vi.json`) - *included*

To contribute a new translation:
1. Copy `i18n/default.json`.
2. Rename it to your language code (e.g., `zh.json`, `es.json`, `ru.json`, `fr.json`, etc.).
3. Translate the string values.
4. Submit a Pull Request on [GitHub](https://github.com/QuangBM138/LadderOutline)!

## 📥 Installation

1. Download the latest release package (`LadderOutline-v1.1.0.zip`).
2. Extract the contents into your Stardew Valley `Mods` folder.
3. Launch the game with SMAPI and enjoy!

## 🎮 How to Use

- Open the in-game **Generic Mod Config Menu (GMCM)** to customize settings (outline colors, pointer style, distance display, etc.).
- Quickly toggle the mod on or off using your configured key (default: `None`).

## 🛠 Compatibility

- Compatible with **Stardew Valley 1.6+** and **SMAPI 3.18.6+ / 4.0+**.
- Works in The Mines, Skull Cavern, and Quarry Mine.

## 📦 Links

- [GitHub Repository](https://github.com/QuangBM138/LadderOutline)
- [Nexus Mods Page](https://www.nexusmods.com/stardewvalley/mods/33976)
