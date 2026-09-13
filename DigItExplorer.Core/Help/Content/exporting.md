# Exporting

Select **File > Export…** `Ctrl + E` to export the current preview. This option is disabled when viewing content that cannot be exported, such as audio, plain text, or an item that failed to load.

## Export options

The available export formats depend on the type of content selected in the preview.

## Still images

Still images, including level maps, world maps, minigame boards, and individual sprites, export as a single `.PNG` image matching what is displayed on screen.

Selected layers in [The Layers Panel](the-layers-panel.md) are flattened into the exported image, while unselected layers are excluded. For example, disabling the Collision layer produces an exported map without collision overlay.

Areas not covered by a selected layer remain transparent, preserving the full dimensions of the level or screen. A 640×400 map remains 640×400 even when only a few objects are selected, and surrounding areas are left transparent rather than cropped. This ensures that layers exported individually align when stacked.

The background visible behind transparent areas in Dig It! Explorer is the window background, not part of the exported graphic. Areas that the game itself renders in black remain black in the export, as black is an active palette color rather than empty space.

## Multi-frame raw files

In the **Raw** tab, decoded files containing multiple frames offer two export options:

- **Current frame**: exports the single frame displayed on screen as a `.PNG` image.
- **All frames**: exports every frame in the file as numbered `.PNG` files in the order they appear in the file.

Because raw files do not define animation sequences or frame timing, their frames are exported in the order they appear in the file. Formats that represent playback sequences, such as animated GIFs and sprite strips, are available under animated entries in **Resources**.

## Animations

Animated resources provide three export formats:

- **Animated `.GIF`**: exports the full playback sequence at the game's original speed, looping if the animation loops in the game. This includes repeated frames in the order defined by the game's animation script.
- **Horizontal `.PNG` strip**: places each unique frame edge to edge on a transparent background with no spacing or padding between frames.
- **Numbered `.PNG` files**: exports each unique frame once into a selected folder.

The animated GIF reflects the full playback sequence, while strips and numbered files contain each unique frame once, without duplicates.

Exported GIF animations preserve the game's original timing and looping behavior. Live playback speed adjustments and loop toggle settings in the playback bar do not affect the exported file.

## Scale

All formats support scaling at 1×, 2×, or 4×.

Scaling uses nearest-neighbor interpolation by repeating whole pixels, preserving sharp pixel edges. The dialog displays the resulting image dimensions before exporting, and sprite strips also show the dimensions of an individual frame at the chosen scale.

## Default filenames

A default filename is suggested based on the item hierarchy in the tree, such as `Dry Lands - Warm Up Run - LVL000`.

- `_2x` or `_4x` is appended when the scale is not 1×.
- `_frames` is appended to horizontal strips.
- Numbered sequences use two-digit padding, such as `_01` and `_02`.

Characters not supported by the file system, as well as dots, are replaced with underscores.

## Remembered preferences

Selected export scale and format choices are remembered across sessions and when opening another copy of the game.

When switching between content types, the format selection adjusts to a supported format without overwriting saved preferences for other types.

## Limitations

- **Audio**: Sound effects and music tracks cannot be exported to audio files. See [Audio](audio.md).
- **Cutscenes and story scenes**: Full-screen cutscenes and story scenes export only the current frame displayed on screen.

## Where to go next

- [The Window](the-window.md) introduces the main interface layout and panels.
- [Viewing](viewing.md) covers zoom controls, framing modes, and viewport navigation.
- [The Layers Panel](the-layers-panel.md) explains layer visibility, collision overlays, and search filtering.
