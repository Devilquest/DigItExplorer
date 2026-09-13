# Animations

Select an animated entry in **Resources**, such as Dug (player) or an enemy, to display the playback
controls below the preview.

## An animation is a script, not a list of pictures

The game does not store an animation as a sequence of duplicate images. It stores a sprite sheet of
unique cells and a separate script specifying which cell to display at each step. For example, Dug (player)'s
`skid` animation alternates between two foot-sliding cells before finishing, repeating frames in the
script rather than duplicating them in the sprite sheet.

The frame counter therefore counts **steps of the script**, and multiple steps can display the same cell.

## The controls

| Control | What it does |
|---|---|
| Play / Pause `Space` | Runs the script, or pauses at the current step |
| Previous Frame, Next Frame | Moves one frame at a time, in either direction |
| Step progress | Displays the current step, total step count, and step duration (such as `Step 3/12 · 70 ms`) |
| Timeline | Scrub or jump to any step by clicking its thumbnail |
| Loop | Repeats continuously when active; plays once and stops on the last frame when inactive |
| Checkerboard Background | Toggles a checkerboard pattern or solid background behind the sprite |
| Speed | Adjusts playback speed (from ×0.10 to ×4) |
| Regenerate | Generates a new random variation of the animation sequence |

## Speed

**×1 is the reference speed**, matching the game's original playback rate. Playback speed can be
adjusted from **×0.10** (one-tenth speed) up to **×4** (four times normal speed).

Playback speed is an inspection setting and does not affect exported files, which always preserve the
game's original timing. See [Exporting](exporting.md).

## Loop, and animations that were never meant to repeat

Some animations loop continuously during gameplay (such as walking or swimming), while others play once
and stop (such as Dug (player) crouching down, or a Slugger emerging from its shell).

With **Loop off**, playback plays once and stops on the last frame. With **Loop on**, a single-play
animation pauses briefly on its final frame before repeating.

Whether an animation loops in the game is a property of the animation, and it is shown in
[The Info Panel](the-info-panel.md) under **Contents**.

## Skins

Dug (player) and several enemies appear with different costumes and skins across worlds. In the
navigation tree, selecting a world under a character displays the animations available for that specific skin.

If a copy of the game lacks the file for a particular skin, the corresponding node does not appear in the tree.

## Cutscenes and story scenes

Cutscenes and story scenes use specialized playback controls:

- **Cutscenes** (the title sequence and the intro) provide Play/Pause `Space`, frame-by-frame stepping, a frame
  slider, loop, and playback speed.
- **Story scenes** (such as "Traces of Dugette") display narrative text over the scene background, providing
  Play/Pause `Space`, line-by-line stepping, loop, and speed controls.

These sequences play in Dig It! Explorer, but only the frame currently on screen can be exported. See [Exporting](exporting.md).

## Where to go next

- [Viewing](viewing.md) covers zoom controls, framing modes, and viewport navigation.
- [The Info Panel](the-info-panel.md) details identity metadata, file sources, and item properties.
- [Exporting](exporting.md) explains export formats and scaling options.

