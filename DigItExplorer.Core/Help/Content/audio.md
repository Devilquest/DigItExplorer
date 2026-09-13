# Audio

Under **Audio** in the Resources tree there are two groups: **Music** and **Sound Effects**.

## Two different things

The game's music consists of FM synthesis instructions for the Yamaha YM3812 (OPL2) sound chip: a sequence
of notes and instrument settings that the chip turns into sound. Dig It! Explorer emulates the sound chip
and synthesizes the music in real time, reproducing the sound of the original hardware.

Sound effects are 8-bit PCM digital audio samples stored in the game files, played back through
the system audio.

## The controls

Select a music track or sound effect to display the playback bar: **Play / Pause** `Space`, **Stop**,
a seek bar, elapsed time, **Loop**, a **Mute** button `M`, and a volume slider.

**Loop** reflects how each item behaves in the game: looping music tracks repeat
continuously, while sound effects and single-play tracks such as the player death theme play once and
stop. Clicking **Loop** overrides this, toggling continuous repetition on or off for any track.

Technical properties such as audio format, sample rate, duration, and default loop mode appear in the **Contents** section of [The Info Panel](the-info-panel.md).

Volume and mute settings are remembered across restarts: if closed while muted, Dig It! Explorer
starts silent.

## Audio cannot be exported

Music tracks and sound effects cannot be exported to audio files. The audio player is intended for
in-app listening.

## Where to go next

- [The Window](the-window.md) introduces the main interface layout and panels.
- [The Info Panel](the-info-panel.md) details identity metadata, file sources, and item properties.
- [Resources and Raw](raw-and-resources.md) explains the distinction between original archive files and reconstructed game assets.
- [Exporting](exporting.md) explains export formats and scaling options.

