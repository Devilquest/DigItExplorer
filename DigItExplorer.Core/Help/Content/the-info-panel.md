# The Info Panel

The **Info** tab in the side panel displays technical metadata and properties for the currently selected item. Information is structured into three standard sections: **Identity**, **Source**, and **Contents**.

## Identity

The **Identity** section places the item within the game structure. Depending on the item type, it displays the world, node number, and title of a level, or the character, skin, and action of an animation.

## Source

The **Source** section lists the original archive files behind the resource, their sizes, and associated palette data.

File names displayed as links navigate to that file in the **Raw** tab. When a file link is selected, a back link (`←`) appears at the top of the panel to return to the original item.

## Contents

The **Contents** section details the intrinsic properties and metrics of the item:

- **Level maps**: terrain dimensions, collision status, background music track, and entity census counts.
- **Animations**: cell size in pixels, playback mode, step count, and unique frame count.
- **Slab screens**: screen stop positions.
- **Audio**: audio format, sample rate, duration, and playback mode.
- **Graphic sheets**: frame dimensions in pixels, and frame count.

## Available sections

Sections appear only when applicable to the selected item. For example, raw files display a **Source** section without an **Identity** section.

All values shown in the panel are read directly from the game files.

## Notes and diagnostics

- **Notes**: When an item carries relevant context or findings about the game data, a **Notes** section appears in the panel.
- **Damaged files**: If a file in the game folder is damaged or unrecognized, an alert appears in the panel with diagnostic details. See [When Something Is Wrong](when-something-is-wrong.md).

## Panel persistence

Dig It! Explorer remembers whether **Info** or **Layers** was active, restoring the selected tab across sessions. The **Side Panel** button in the preview toolbar toggles the visibility of the side panel.

## Where to go next

- [The Layers Panel](the-layers-panel.md) explains layer visibility, collision overlays, and search filtering.
- [Viewing](viewing.md) covers zoom controls, framing modes, and viewport navigation.
- [When Something Is Wrong](when-something-is-wrong.md) covers diagnostics for damaged or unrecognized files.
