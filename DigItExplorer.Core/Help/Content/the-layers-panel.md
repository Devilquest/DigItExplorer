# The Layers Panel

The **Layers** tab in the side panel controls the visibility of individual layers in composite graphics, such as level maps, world maps, the main menu, minigame boards, and ending screens.

## Layer hierarchy

The layer tree organizes graphics into logical groups depending on the selected resource:

- **Base**: structural layers, including Terrain and Collision overlays.
- **Entity categories**: grouped by entity type, including Enemies, Goodies, Mechanisms, and Decor, listing only the specific entities present in the current level.
- **Markers**: player start positions, level exits, and bonus warps.
- **Info Overlays**: destination overlay labels for exits and warps.

Groups appear only if the loaded resource contains matching entities or elements. Empty groups are omitted from the tree.

Parent checkboxes toggle all layers within their group and display an indeterminate state when only some child layers are enabled.

## Toolbar controls

The toolbar above the tree provides four buttons:

- **Expand All**: expands every group in the layer tree.
- **Collapse All**: collapses every group in the layer tree.
- **Select All**: enables all visible layers.
- **Deselect All**: disables all visible layers.

To view a single layer in isolation, select **Deselect All** and check the desired layer.

### Selection behavior

- **Mutually exclusive layers**: On screens where elements are mutually exclusive (such as the main menu sub-menus for Setup, Play, and Intro), **Select All** keeps the active selection or selects the first available option.
- **Empty stage**: Deselecting all layers displays a blank preview stage without error. Re-enabling any layer restores the graphic.

## Searching layers

The search box above the toolbar searches the tree by layer name.

- **Selective bulk actions**: While searching, **Select All** and **Deselect All** affect only the matching visible layers, leaving hidden layers unchanged. For example, typing `Gem` and selecting **Select All** enables all matching gem layers without altering other entities.
- **Expansion commands**: **Expand All** and **Collapse All** apply to the entire tree regardless of active search terms.
- **Automatic reset**: Clearing the search box or selecting a different resource in the navigation tree resets the search.

## Keyboard shortcuts

Dedicated keyboard shortcuts allow toggling primary layer groups directly from the keyboard, even when the side panel is hidden:

- `C`: toggles the **Collision** layer group.
- `E`: toggles the **Enemies** layer group.
- `G`: toggles the **Goodies** layer group.

## Destination labels

For level maps, the **Info Overlays** group provides destination labels displayed directly over exits and bonus warps. Labels use color coding to indicate destination types:

- **Red**: exit returning to the world map (`>MAP`).
- **Amber**: exit leading to another level or sub-stage.
- **Gray**: internal exit leading back into the same level, such as in the Spookstone mazes.
- **Green**: warp leading into a bonus zone.

**Exit Destination Info** controls red, amber, and gray labels. **Bonus Destination Info** controls green bonus labels.

## Session persistence

Layer visibility preferences persist throughout the active session. If a layer or group (such as **Collision**) is enabled, it remains enabled when navigating between levels or opening another copy of the game. Layer visibility resets when the application restarts.

## Exporting layered images

Exporting a still image flattens only the currently enabled layers into the final graphic, leaving disabled layers transparent. See [Exporting](exporting.md).

## Where to go next

- [The Info Panel](the-info-panel.md) details identity metadata, file sources, and item properties.
- [Viewing](viewing.md) covers zoom controls, framing modes, and viewport navigation.
- [Exporting](exporting.md) explains export formats and scaling options.
