# Viewing

The central preview stage displays the selected game asset, providing zoom, framing, and viewport navigation controls. Viewing settings adjust on-screen presentation without modifying the underlying game assets.

## Zoom controls

Magnification can be adjusted from 10% up to 3200% using toolbar buttons, the mouse wheel, or keyboard shortcuts:

- **Zoom Out** `Ctrl + -`: decreases magnification.
- **Zoom In** `Ctrl + +`: increases magnification.
- **Original Size** `1` or `Ctrl + 1`: resets magnification to 1:1 (100%), where one pixel in the asset corresponds to one screen pixel.
- **Mouse wheel**: scrolling over the viewport zooms in and out with the cursor position as the zoom center.

Scaling uses nearest-neighbor interpolation, repeating whole pixels to preserve sharp pixel art edges without blur or distortion.

## Framing and panning

- **Fit to View** `0` or `Ctrl + 0`: scales the asset to fit within the available viewport space.
- **Panning**: when an image exceeds the viewport dimensions, drag the stage with the left mouse button or use the scrollbars to pan across the graphic.

Previews open fitted to the viewport, adapting the initial framing to the dimensions of each asset type.

## Side panel toggle

Select **Side Panel** `P` in the viewport toolbar to show or hide the side panel. See [The Info Panel](the-info-panel.md) and [The Layers Panel](the-layers-panel.md).

## Info bar

The info bar in the viewport toolbar identifies the item currently displayed and its location within the game hierarchy, such as `Dry Lands · Warm Up Run · LVL000`.

## Stage background and transparency

When previewing sprite animations, the playback bar includes a toggle between a checkerboard pattern and a transparent background. Background settings affect live display only; exported graphics preserve transparency regardless of the selected background.

## Text and non-graphic resources

Plain text files are displayed in a read-only text viewer. If a resource cannot be decoded or is damaged, the viewport displays an explanatory notice. See [When Something Is Wrong](when-something-is-wrong.md).

## Where to go next

- [The Window](the-window.md) introduces the main interface layout and panels.
- [Exporting](exporting.md) explains export formats and scaling options.
- [When Something Is Wrong](when-something-is-wrong.md) covers diagnostics for damaged or unrecognized files.
