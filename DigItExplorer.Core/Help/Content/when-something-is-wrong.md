# When Something Is Wrong

Dig It! Explorer includes diagnostic detection for damaged, incomplete, or unrecognized game files. When file integrity issues occur, the application displays diagnostic notices and continues to decode and present all intact data.

## Structural validation

Dig It! Explorer validates file structures during decoding, detecting issues such as:

- Headers with impossible frame counts or dimensions.
- Compressed graphic streams that terminate before declared dimensions are reached.
- Files shorter than required for their declared format.

Validation relies on structural integrity rather than fixed checksum catalogs, allowing structural checks to function across uncataloged copies without identifying release versions.

## Partial decoding and visual presentation

When an asset is damaged, the application decodes and displays all intact data rather than suppressing the asset:

- In the viewport, partially decoded graphics are displayed with `damaged` indicated in the info bar. If decoding fails completely, a placeholder notice appears explaining the failure.
- In [The Info Panel](the-info-panel.md), a dedicated **Damaged** section describes the specific defect detected in the open resource.
- For level maps with missing blocks, the diagnostic notice identifies the specific affected layer and the count of missing blocks.

## Semantic and visual corruption

Damage detection relies on structural decoding checks. If data corruption alters graphical content while leaving file headers, compression tables, and data lengths structurally valid, the asset decodes and displays without a diagnostic warning.

## Resources view unavailable

The **Resources** tab requires metadata extracted from the game executable (`MAIN.EXE`), including level names, world assignments, character identities, and animation scripts. When `MAIN.EXE` cannot provide this data, the status bar displays a diagnostic notification, the **Resources** tab is disabled, and only the **Raw** tab is available to browse archive files.

This condition occurs in three situations:

- **`MAIN.EXE` could not be read**: the executable is missing, locked, or truncated.
- **`MAIN.EXE` is from an unrecognized release of the game**: the executable belongs to a build with different internal offsets.
- **`MAIN.EXE` does not contain the expected game data**: the executable does not contain Dig It! game data tables.

## The shareware release

The shareware release of Dig It! uses a distinct executable build with different internal memory offsets and data locations. Because resource tables are currently extracted only from the registered release, a shareware copy cannot populate the **Resources** tab (which appears disabled). All archives within a shareware copy can still be browsed and exported through the **Raw** tab.

## Repairing damaged copies

Dig It! Explorer operates in read-only mode and never modifies files within the game folder. Some known file defects can be repaired using [Dig It! Patcher](https://github.com/Devilquest/DigItPatcher), a companion utility in the Dig It! toolset.

## Where to go next

- [The Window](the-window.md) introduces the main interface layout and panels.
- [Resources and Raw](raw-and-resources.md) explains the distinction between original archive files and reconstructed game assets.
- [The Info Panel](the-info-panel.md) details identity metadata, file sources, and item properties.
