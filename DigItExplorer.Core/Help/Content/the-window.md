# The Window

The Dig It! Explorer window is divided into three main areas: the navigation panel on the left, the preview stage in the center, and the collapsible side panel on the right, framed by the title bar at the top and the status bar at the bottom.

```
+--------------------------------------------------------------------------+
| [D] File  Help                                        -    []     X      |
+-------------------+----------------------------------+-------------------+
| search            | active item label  - 100% + [fit]|  Info  |  Layers  |
| +---------------+ |                                  |                   |
| | Resources|Raw | |                                  |  Identity         |
| +---------------+ |          preview stage           |  Source           |
| |               | |                                  |  Contents         |
| |     tree      | |                                  |                   |
| |               | |                                  |                   |
| +---------------+ |                                  |                   |
|                   |  playback bar, when it applies   |                   |
+-------------------+----------------------------------+-------------------+
| the folder that is open · resource count                                 |
+--------------------------------------------------------------------------+
```

## Title bar

The title bar contains the application menu and standard window controls:

- **File**: includes options to open or close a game folder, export the current preview, and exit the application.
- **Help**: provides access to this user guide `F1`, keyboard shortcuts, toolset links, and application version information.

## Navigation panel

The navigation panel on the left selects items to display on the preview stage:

- **Search box**: searches items in the active tab by name.
- **Resources and Raw tabs**: switches between high-level categorized game assets (**Resources**) and original game archive files (**Raw**). When opening an unrecognized release or shareware copy, the **Resources** tab is disabled and only the **Raw** tab is available. See [Resources and Raw](raw-and-resources.md).
- **Navigation tree**: browses the directory or asset hierarchy.

## Preview stage

The preview stage in the center displays the selected graphic, animation, or asset:

- **Viewport toolbar**: provides zoom controls (**Zoom Out**, zoom percentage readout, **Zoom In**), framing buttons (**Fit to View**, **Original Size**), and the **Side Panel** button to toggle the right-hand panel. See [Viewing](viewing.md).
- **Info bar**: sits in the viewport toolbar, displaying the label and location of the active item.
- **Playback bar**: is shown below the viewport when previewing animated or playable assets, including animations, music tracks, sound effects, cutscenes, and story scenes. When inspecting multi-frame graphics in **Raw**, the frame toolbar appears in this same position. See [Animations](animations.md), [Audio](audio.md), and [Resources and Raw](raw-and-resources.md).

## Side panel

The collapsible side panel on the right provides secondary inspectors for the active resource:

- **Info**: displays technical metadata, source archive files, palette data, and item metrics. See [The Info Panel](the-info-panel.md).
- **Layers**: toggles individual visual layers for composite assets, such as level maps, world maps, minigames, and menus. See [The Layers Panel](the-layers-panel.md).

The side panel can be toggled using the **Side Panel** button in the viewport toolbar or the keyboard shortcut `P`.

## Status bar

The status bar at the bottom displays:

- The path of the currently opened game folder.
- Total resource count loaded from the game data.
- Confirmation messages when export operations complete.
- Diagnostic notifications, such as when an unrecognized game version is loaded.

## Remembered preferences

Dig It! Explorer preserves several settings across restarts:

- Window dimensions, screen position, and maximized state.
- Last opened game folder path.
- Active side panel tab selection (**Info** or **Layers**) and panel visibility.
- Audio playback volume and mute state.
- Selected export scale factor and format preference.

Viewport zoom and framing settings apply dynamically to the active item and reset to defaults when navigating between different content types.

## Where to go next

- [Resources and Raw](raw-and-resources.md) explains the distinction between original archive files and reconstructed game assets.
- [Viewing](viewing.md) covers zoom controls, framing modes, and viewport navigation.
- [The Info Panel](the-info-panel.md) details identity metadata, file sources, and item properties.
- [The Layers Panel](the-layers-panel.md) explains layer visibility, collision overlays, and search filtering.
