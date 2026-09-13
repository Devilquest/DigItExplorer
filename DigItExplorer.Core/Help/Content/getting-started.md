# Getting Started

Dig It! Explorer reads a local copy of Dig It! and displays its contents: level maps, sprites, animations, screens, and audio, decoded from the original game files. No emulator is required.

An existing copy of Dig It! is required. Dig It! Explorer operates in read-only mode and never modifies files within the game folder.

## Opening a game folder

Select **File > Open Game Folder…** `Ctrl + O` and select the folder holding the game files.

Two folder layouts are recognized:

- A folder holding `MAIN.EXE` and the `.XRS` archives.
- A parent folder containing a `DIGIT` subfolder holding those files.

If Dig It! Explorer is located directly in the game folder or beside a `DIGIT` subfolder, the game is detected on startup. This application does not scan any other locations.

## Automatic reopening

When a game folder is opened successfully, its path is saved and reopened on subsequent launches.

If the saved folder is unavailable at startup (for example, if a removable drive is disconnected), Dig It! Explorer opens to the welcome screen. The path remains saved, restoring access on the next launch once the drive is reconnected.

## Unrecognized folders

If the selected folder does not contain recognizable game archives, an error message appears and the current session remains unchanged. Any previously loaded game remains open.

If a valid game folder fails to load, files within it may be damaged. See [When Something Is Wrong](when-something-is-wrong.md) for diagnostic details.

## The welcome screen

When no game is loaded, the viewport displays a centered welcome screen with a link to **File > Open Game Folder…**. This screen appears on first launch and whenever a game folder is closed.

## Closing and exiting

- **File > Close Game Folder**: unloads the active game, clears the saved folder preference, and returns to the welcome screen. This command is disabled when no game is open.
- **File > Exit**: closes Dig It! Explorer, equivalent to clicking Close on the title bar.

## Where to go next

- [The Window](the-window.md) introduces the main interface layout and panels.
- [Resources and Raw](raw-and-resources.md) explains the distinction between original archive files and reconstructed game assets.
- [When Something Is Wrong](when-something-is-wrong.md) covers diagnostics for damaged or unrecognized files.
