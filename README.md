# Ladder & Volcano Outline

**Ladder & Volcano Outline** makes finding ladders, shafts (holes), and Volcano Dungeon exits and switches much easier by highlighting them with customizable outlines and directional pointers. Never lose track of where an exit spawned, or wander around looking for hidden switches again!

---

## 🚀 What's New in v1.2.0 (Volcano Dungeon Expansion)

- **Ginger Island Volcano Dungeon Support (Hỗ trợ Hầm Núi Lửa)**:
  - **Exit / Next Floor Stairs (Cửa lên tầng kế)**: Directional pointers and outlines pointing directly to the exit stairs/cave opening leading to the next floor.
  - **Floor Switches / Buttons (Nút bấm mở cổng)**: Directional pointers and outlines for floor buttons needed to unlock dwarf gates.
    - Shows progress notes when multiple switches exist (e.g. `Nút mở cổng (0/2)`).
  - **Locked Dwarf Gates (Cổng lùn bị khóa)**: Pointers and outlines for locked gates that are currently blocking your path.
  - **Auto-Hide Opened Targets (Tự động ẩn mục tiêu đã hoàn thành)**: Once you step on a button or the gate opens, its indicator is automatically hidden to keep your screen clean and clutter-free.
- **Dedicated Volcano Customization in GMCM**:
  - Independent toggles for Volcano Support, Exits, Switches, and Gates.
  - Customizable colors for Volcano Exit (default: Green), Switches (default: Yellow), and Gates (default: Red).
- **Search-Friendly Name**:
  - Updated mod title and keywords so players searching for either "Ladder Outline" or "Volcano Exit / Button Tracker" can easily discover it.

---

## 🌟 Core Features

- **Highlight ladders & shafts**: Visual outline borders around ladder and hole tiles in The Mines and Skull Cavern.
- **Volcano Dungeon assistance**: Highlights exits, switches, and locked gates in Ginger Island Volcano Dungeon.
- **Directional indicators**: Arrow pointers pointing the way toward all active targets on the current floor, even when far off-screen.
- **Multiple targets**: If multiple ladders, shafts, or switches exist, all are tracked and pointed to simultaneously with smart overlap prevention.
- **Customizable pointer styles**:
  - `ScreenEdge`: Clamped along the screen border with distance (e.g. `15m`) and target type label.
  - `PlayerOrbit`: Arrows circling around the farmer pointing directly toward targets.
  - `Both`: Screen edge and orbit pointers together.
  - Floating bouncing markers in world space directly above targets when visible on-screen.
- **Fully customizable settings via GMCM**:
  - Colors, opacity, border thickness, scale, and pulse bobbing animation.
  - Quick toggle key (default: `None`).
- **Full Internationalization (i18n)**:
  - English (`default.json`) & Vietnamese (`vi.json`) included.

---

## 🌐 Translations (i18n)

Ladder & Volcano Outline supports community translations through SMAPI's standard `i18n` format:
- English (`default.json`) - *included*
- Vietnamese (`vi.json`) - *included*

To contribute a new translation:
1. Copy `i18n/default.json`.
2. Rename it to your language code (e.g., `zh.json`, `es.json`, `ru.json`, `fr.json`, etc.).
3. Translate the string values.
4. Submit a Pull Request on [GitHub](https://github.com/QuangBM138/LadderOutline)!

---

## 📥 Installation

1. Download the latest release package (`LadderOutline 1.2.0.zip`).
2. Extract the contents into your Stardew Valley `Mods` folder.
3. Launch the game with SMAPI and enjoy!

---

## 🎮 How to Use

- Open the in-game **Generic Mod Config Menu (GMCM)** to customize settings (outline colors, pointer style, distance display, volcano toggles, etc.).
- Quickly toggle the mod on or off using your configured key (default: `None`).

---

## 🛠 Compatibility

- Compatible with **Stardew Valley 1.6+** and **SMAPI 3.18.6+ / 4.0+**.
- Works in:
  - The Mines
  - Skull Cavern
  - Quarry Mine
  - Ginger Island Volcano Dungeon (Floors 0–9)

---

## 📦 Links

- [GitHub Repository](https://github.com/QuangBM138/LadderOutline)
- [Nexus Mods Page](https://www.nexusmods.com/stardewvalley/mods/33976)
