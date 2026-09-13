# Core/Audio: LGPL-2.1 licensed module

Unlike the rest of this repository (MIT), **everything in this folder is licensed under LGPL-2.1**
(see the [LICENSE](LICENSE) file here), because it contains code derived from LGPL-2.1 upstreams:

- The LDS ("Loudness Sound System") music player, ported from
  [AdPlug](https://github.com/adplug/adplug)'s `lds.cpp`.
- The OPL2/OPL3 FM synthesis core, from
  [SaxxonPike's NukedOpl](https://github.com/SaxxonPike/NukedOpl), a C# port of
  [nukeykt's Nuked-OPL3](https://github.com/nukeykt/Nuked-OPL3).

Each ported file carries a provenance header naming its upstream. Original audio code with no
upstream derivation (e.g. the raw-PCM SMP sound-effect decoder) lives in `../Formats/` under the
repository's MIT license instead.
