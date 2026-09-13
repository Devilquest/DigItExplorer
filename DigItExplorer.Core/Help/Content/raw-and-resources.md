# Resources and Raw

The navigation panel provides two distinct tabs: **Resources** and **Raw**, representing two different
ways to explore the game's data in Dig It! Explorer.

## Resources shows composite assets

**Resources** reconstructs game assets the same way the game engine assembles them at runtime.

A level under **Worlds** combines the terrain grid, the collision map, and every entity into a single
interactive map. An animation under **Enemies** plays a sequence of frames in the order and timing
defined by the game's animation script.

To inspect the source files that compose any resource, view the **Source** section in [The Info Panel](the-info-panel.md).

Resources only displays assets that Dig It! Explorer can fully decode and reconstruct.

## Raw shows the original data

**Raw** lists the game's files as stored in the game folder: the `.XRS` archives, and every individual file
packed inside each one (`LVL000F.MPF`, `WO_DRG00.SPF`, `MENU01.PAL`).

Selecting an entry decodes and displays that single file. A sprite sheet appears as a full image, with
every cell laid out in its original storage grid. Files holding multiple frames display a frame toolbar (for example, `Frame 3 of 12`) to step through frames in the order they appear in the file.

**Raw** provides an unmodified view of the archives. Everything in the game folder is accessible here,
including formats that are not parsed into composite resources.

## The branches

| Branch | What is in it |
|---|---|
| Worlds | Every level, world maps, and story scenes |
| Screens & UI | The main menu, title and intro cutscenes, ending sequence, and minigames |
| Dug (player) | Dug (player), with per-world costumes and animation sequences |
| Enemies | Each enemy, its per-world skins, and its animations |
| Objects & Hazards | Platforms, rocks, plants, drains, dig spots, and world map signposts |
| Goodies | Gems, gold items, and silver items |
| Effects & VFX | Projectiles, impacts, and particle effects |
| UI & HUD | HUD and on-screen interface elements |
| Audio | Music tracks (OPL2 FM synthesis) and digital sound effects (PCM samples) |

If a copy of the game lacks certain assets, the corresponding node does not appear in the tree.

## Which one to use

- **To inspect reconstructed game elements as they appear in-game**, select **Resources**.
- **To inspect individual files in their original storage format**, select **Raw**.

Some assets appear in both views: a sprite sheet in **Raw** displays the raw storage grid of cells,
whereas the corresponding entry in **Resources** plays the timed animation sequence assembled from
those cells.

## Consistent naming across panels

Category and entity names in the Resources tree match the layer names in [The Layers Panel](the-layers-panel.md).
The navigation tree categorizes what exists, while the Layers panel controls what is drawn, sharing identical
terminology across the application.

## Where to go next

- [The Window](the-window.md) introduces the main interface layout and panels.
- [Viewing](viewing.md) covers zoom controls, framing modes, and viewport navigation.
- [Exporting](exporting.md) explains export formats and scaling options.

