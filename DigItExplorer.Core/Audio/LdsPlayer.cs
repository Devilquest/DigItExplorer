/*
 * Ported from AdPlug's lds.cpp/lds.h: "LOUDNESS Player" by Simon Peter <dn.tlp@gmx.net>.
 * AdPlug - Replayer for many OPL2/OPL3 audio file formats.
 * Copyright (C) 1999 - 2004 Simon Peter, <dn.tlp@gmx.net>, et al.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * See the LICENSE file in this folder (Core/Audio) for the full text.
 *
 * C# port notes (DigItExplorer): field/local names deliberately keep AdPlug's originals so
 * the port can be audited line-by-line against lds.cpp. C's unsigned char / unsigned short
 * truncation semantics are reproduced with explicit masks/casts at each site. Two deliberate
 * divergences from upstream, both flagged inline:
 *   1. LOOP FIX: 0xF9 flags songlooped on `jumppos <= posplay` (upstream: `<`), so a
 *      single-position tune that jumps to itself (Dig It!'s J_LOOP/TUNE13: numposi=1, jump 0->0)
 *      is detected as a loop instead of playing "forever" (AdPlug shows its 10-minute cap).
 *   2. Out-of-range position reads (posplay >= numposi) stop playback instead of reading out of
 *      bounds (undefined behavior in the C original; never triggered by valid files).
 **/

namespace DigItExplorer.Core.Audio;

/// <summary>Sink for the player's OPL2 register writes (AdPlug's <c>Copl</c> in miniature).</summary>
internal interface IOplPort
{
    /// <summary>Reset the chip to its power-on state (AdPlug's <c>opl->init()</c>).</summary>
    void Reset();

    void Write(int register, byte value);
}

/// <summary>An <see cref="IOplPort"/> that discards everything, for control-flow-only simulation
/// (duration/loop probing) without FM synthesis.</summary>
internal sealed class NullOplPort : IOplPort
{
    /// <summary>The shared no-op instance.</summary>
    public static readonly NullOplPort Instance = new();

    /// <summary>No-op.</summary>
    public void Reset() { }

    /// <summary>No-op.</summary>
    public void Write(int register, byte value) { }
}

/// <summary>Sequencer for the LDS audio format that emits OPL2 register writes on each tick.</summary>
internal sealed class LdsPlayer
{
    // Note frequency table (16 notes / octave)
    private static readonly ushort[] frequency = {
        343, 344, 345, 347, 348, 349, 350, 352, 353, 354, 356, 357, 358,
        359, 361, 362, 363, 365, 366, 367, 369, 370, 371, 373, 374, 375,
        377, 378, 379, 381, 382, 384, 385, 386, 388, 389, 391, 392, 393,
        395, 396, 398, 399, 401, 402, 403, 405, 406, 408, 409, 411, 412,
        414, 415, 417, 418, 420, 421, 423, 424, 426, 427, 429, 430, 432,
        434, 435, 437, 438, 440, 442, 443, 445, 446, 448, 450, 451, 453,
        454, 456, 458, 459, 461, 463, 464, 466, 468, 469, 471, 473, 475,
        476, 478, 480, 481, 483, 485, 487, 488, 490, 492, 494, 496, 497,
        499, 501, 503, 505, 506, 508, 510, 512, 514, 516, 518, 519, 521,
        523, 525, 527, 529, 531, 533, 535, 537, 538, 540, 542, 544, 546,
        548, 550, 552, 554, 556, 558, 560, 562, 564, 566, 568, 571, 573,
        575, 577, 579, 581, 583, 585, 587, 589, 591, 594, 596, 598, 600,
        602, 604, 607, 609, 611, 613, 615, 618, 620, 622, 624, 627, 629,
        631, 633, 636, 638, 640, 643, 645, 647, 650, 652, 654, 657, 659,
        662, 664, 666, 669, 671, 674, 676, 678, 681, 683
    };

    // Vibrato (sine) table
    private static readonly byte[] vibtab = {
        0, 13, 25, 37, 50, 62, 74, 86, 98, 109, 120, 131, 142, 152, 162,
        171, 180, 189, 197, 205, 212, 219, 225, 231, 236, 240, 244, 247,
        250, 252, 254, 255, 255, 255, 254, 252, 250, 247, 244, 240, 236,
        231, 225, 219, 212, 205, 197, 189, 180, 171, 162, 152, 142, 131,
        120, 109, 98, 86, 74, 62, 50, 37, 25, 13
    };

    // Tremolo (sine * sine) table
    private static readonly byte[] tremtab = {
        0, 0, 1, 1, 2, 4, 5, 7, 10, 12, 15, 18, 21, 25, 29, 33, 37, 42, 47,
        52, 57, 62, 67, 73, 79, 85, 90, 97, 103, 109, 115, 121, 128, 134,
        140, 146, 152, 158, 165, 170, 176, 182, 188, 193, 198, 203, 208,
        213, 218, 222, 226, 230, 234, 237, 240, 243, 245, 248, 250, 251,
        253, 254, 254, 255, 255, 255, 254, 254, 253, 251, 250, 248, 245,
        243, 240, 237, 234, 230, 226, 222, 218, 213, 208, 203, 198, 193,
        188, 182, 176, 170, 165, 158, 152, 146, 140, 134, 127, 121, 115,
        109, 103, 97, 90, 85, 79, 73, 67, 62, 57, 52, 47, 42, 37, 33, 29,
        25, 21, 18, 15, 12, 10, 7, 5, 4, 2, 1, 1, 0
    };

    // AdPlug CPlayer::op_table (OPL2 operator offset per channel)
    private static readonly byte[] op_table = { 0x00, 0x01, 0x02, 0x08, 0x09, 0x0a, 0x10, 0x11, 0x12 };

    private const int maxsound = 0x3f; // maximum number of patches (instruments)
    private const int maxpos = 0xff;   // maximum number of entries in position list (orderlist)

    private sealed class SoundBank
    {
        public int mod_misc, mod_vol, mod_ad, mod_sr, mod_wave,
            car_misc, car_vol, car_ad, car_sr, car_wave, feedback, keyoff,
            portamento, glide, finetune, vibrato, vibdelay, mod_trem, car_trem,
            tremwait, arpeggio;
        public readonly int[] arp_tab = new int[12];
        public int start, size, fms, transp;
        public int midinst, midvelo, midkey, midtrans, middum1, middum2;
    }

    private sealed class Channel
    {
        public int gototune, lasttune, packpos;                      // unsigned short in C
        public int finetune, glideto, portspeed, nextvol, volmod, volcar,
            vibwait, vibspeed, vibrate, trmstay, trmwait, trmspeed, trmrate, trmcount,
            trcwait, trcspeed, trcrate, trccount, arp_size, arp_speed, keycount,
            vibcount, arp_pos, arp_count, packwait;                  // unsigned char in C
        public readonly int[] arp_tab = new int[12];
        public int chancheat_chandelay, chancheat_sound, chancheat_high;
    }

    private readonly struct Position
    {
        public readonly int patnum;    // already divided by 2 (word index), as in AdPlug's load
        public readonly int transpose;
        public Position(int patnum, int transpose) { this.patnum = patnum; this.transpose = transpose; }
    }

    private SoundBank[] soundbank = [];
    private readonly Channel[] channel = new Channel[9];
    private Position[] positions = [];
    private readonly int[] fmchip = new int[0xff];
    private int jumping, fadeonoff, allvolume, hardfade, tempo_now, pattplay, tempo, regbd, mode, pattlen;
    private readonly int[] chandelay = new int[9];
    private int posplay, jumppos, speed;
    private ushort[] patterns = [];
    private bool playing, songlooped;
    private int numpatch, numposi, patterns_size, mainvolume;

    private IOplPort opl = NullOplPort.Instance;

    private LdsPlayer()
    {
        for (int i = 0; i < 9; i++) channel[i] = new Channel();
    }

    /// <summary>Ticks per second: the header's <c>speed</c> is a PIT divisor (every Dig It! tune ≈ 69.5 Hz).</summary>
    public double RefreshRate => 1193182.0 / speed;

    /// <summary>True once a backward (or self, see loop fix) 0xF9 jump ran: the tune loops.</summary>
    public bool SongLooped => songlooped;

    /// <summary>Number of times the looping 0xF9 jump command has executed.</summary>
    public int LoopPassCount { get; private set; }

    /// <summary>False after a 0xFC stop command: the tune is a one-shot and has ended.</summary>
    public bool Playing => playing;

    /// <summary>Number of order-list rows (each holding one pattern per channel).</summary>
    public int PositionCount => numposi;

    /// <summary>Number of instruments (patches) in the module.</summary>
    public int InstrumentCount => numpatch;

    /// <summary>Parses an LDS music module from raw bytes.</summary>
    /// <param name="data">The raw LDS file bytes.</param>
    /// <param name="player">Outputs the initialized <see cref="LdsPlayer"/> if valid; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if successfully loaded; otherwise, <c>false</c>.</returns>
    public static bool TryLoad(byte[] data, out LdsPlayer? player)
    {
        player = null;
        try
        {
            var p = new LdsPlayer();
            p.Load(data);
            player = p;
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private void Load(byte[] data)
    {
        int pos = 0;
        int U8() => pos < data.Length ? data[pos++] : throw new InvalidDataException("truncated");
        int U16() { int lo = U8(), hi = U8(); return lo | hi << 8; }

        // header
        mode = U8();
        if (mode > 2) throw new InvalidDataException("bad mode");
        speed = U16();
        if (speed == 0) throw new InvalidDataException("zero speed");
        tempo = U8();
        pattlen = U8();
        for (int i = 0; i < 9; i++) chandelay[i] = U8();
        regbd = U8();

        // patches (the game's files always carry the MIDI mapping bytes: AdPlug's !no_midi path)
        numpatch = U16();
        if (numpatch == 0) throw new InvalidDataException("no instruments");
        soundbank = new SoundBank[numpatch];
        for (int i = 0; i < numpatch; i++)
        {
            var sb = soundbank[i] = new SoundBank();
            sb.mod_misc = U8(); sb.mod_vol = U8(); sb.mod_ad = U8(); sb.mod_sr = U8();
            sb.mod_wave = U8(); sb.car_misc = U8(); sb.car_vol = U8(); sb.car_ad = U8();
            sb.car_sr = U8(); sb.car_wave = U8(); sb.feedback = U8(); sb.keyoff = U8();
            sb.portamento = U8(); sb.glide = U8(); sb.finetune = U8(); sb.vibrato = U8();
            sb.vibdelay = U8(); sb.mod_trem = U8(); sb.car_trem = U8(); sb.tremwait = U8();
            sb.arpeggio = U8();
            for (int j = 0; j < 12; j++) sb.arp_tab[j] = U8();
            sb.start = U16(); sb.size = U16(); sb.fms = U8(); sb.transp = U16();
            sb.midinst = U8(); sb.midvelo = U8(); sb.midkey = U8();
            sb.midtrans = U8(); sb.middum1 = U8(); sb.middum2 = U8();
        }

        // positions (patnum is a byte offset into the 16-bit pattern space: store the word index)
        numposi = U16();
        if (numposi == 0) throw new InvalidDataException("no positions");
        positions = new Position[9 * numposi];
        for (int i = 0; i < numposi; i++)
            for (int j = 0; j < 9; j++)
                positions[i * 9 + j] = new Position(U16() / 2, U8());

        // patterns
        U16(); // # of digital sounds (not played by this player)
        int remaining = data.Length - pos;
        if (remaining < 0 || (remaining & 1) != 0) throw new InvalidDataException("odd pattern stream");
        patterns_size = remaining / 2;
        patterns = new ushort[patterns_size];
        for (int i = 0; i < patterns_size; i++) patterns[i] = (ushort)U16();

        Rewind(NullOplPort.Instance);
    }

    /// <summary>Resets all sequencer state to the top of the song and attaches
    /// <paramref name="newopl"/> as the register-write sink, re-initializing the chip.</summary>
    public void Rewind(IOplPort newopl)
    {
        opl = newopl;

        // init all with 0
        tempo_now = 3; playing = true; songlooped = false;
        LoopPassCount = 0;
        jumping = fadeonoff = allvolume = hardfade = pattplay = posplay = jumppos = mainvolume = 0;
        foreach (var c in channel)
        {
            c.gototune = c.lasttune = c.packpos = 0;
            c.finetune = c.glideto = c.portspeed = c.nextvol = c.volmod = c.volcar = 0;
            c.vibwait = c.vibspeed = c.vibrate = c.trmstay = c.trmwait = c.trmspeed = c.trmrate = c.trmcount = 0;
            c.trcwait = c.trcspeed = c.trcrate = c.trccount = c.arp_size = c.arp_speed = c.keycount = 0;
            c.vibcount = c.arp_pos = c.arp_count = c.packwait = 0;
            Array.Clear(c.arp_tab);
            c.chancheat_chandelay = c.chancheat_sound = c.chancheat_high = 0;
        }
        Array.Clear(fmchip);

        // OPL2 init
        opl.Reset();
        opl.Write(1, 0x20);
        opl.Write(8, 0);
        opl.Write(0xbd, (byte)regbd);

        for (int i = 0; i < 9; i++)
        {
            opl.Write(0x20 + op_table[i], 0);
            opl.Write(0x23 + op_table[i], 0);
            opl.Write(0x40 + op_table[i], 0x3f);
            opl.Write(0x43 + op_table[i], 0x3f);
            opl.Write(0x60 + op_table[i], 0xff);
            opl.Write(0x63 + op_table[i], 0xff);
            opl.Write(0x80 + op_table[i], 0xff);
            opl.Write(0x83 + op_table[i], 0xff);
            opl.Write(0xe0 + op_table[i], 0);
            opl.Write(0xe3 + op_table[i], 0);
            opl.Write(0xa0 + i, 0);
            opl.Write(0xb0 + i, 0);
            opl.Write(0xc0 + i, 0);
        }
    }

    /// <summary>Advances the sequencer by one tick.</summary>
    /// <returns><c>true</c> if playback is still active; <c>false</c> if stopped or looped.</returns>
    public bool Update()
    {
        if (!playing) return false;

        // handle fading
        if (fadeonoff != 0)
        {
            if (fadeonoff <= 128)
            {
                if (allvolume > fadeonoff || allvolume == 0)
                {
                    allvolume = (allvolume - fadeonoff) & 0xff; // unsigned char wrap, as in C
                }
                else
                {
                    allvolume = 1;
                    fadeonoff = 0;
                    if (hardfade != 0)
                    {
                        playing = false;
                        hardfade = 0;
                        for (int i = 0; i < 9; i++) channel[i].keycount = 1;
                    }
                }
            }
            else
            {
                if (((allvolume + (0x100 - fadeonoff)) & 0xff) <= mainvolume)
                {
                    allvolume = (allvolume + 0x100 - fadeonoff) & 0xff;
                }
                else
                {
                    allvolume = mainvolume;
                    fadeonoff = 0;
                }
            }
        }

        // handle channel delay
        for (int chan = 0; chan < 9; chan++)
        {
            var cd = channel[chan];
            if (cd.chancheat_chandelay != 0)
                if (--cd.chancheat_chandelay == 0)
                    playsound(cd.chancheat_sound, chan, cd.chancheat_high);
        }

        // handle notes
        if (tempo_now == 0)
        {
            bool vbreak = false;
            bool loopJump = false; // a looping 0xF9 executed on THIS row (may sit in several channels)
            for (int chan = 0; chan < 9; chan++)
            {
                var c = channel[chan];
                if (c.packwait == 0)
                {
                    // Guard out-of-range position reads to stop playback safely.
                    if (posplay >= numposi) { playing = false; break; }

                    int patnum = positions[posplay * 9 + chan].patnum;
                    int transpose = positions[posplay * 9 + chan].transpose;

                    int comword = patnum + c.packpos < patterns_size
                        ? patterns[patnum + c.packpos]
                        : 0x8001;

                    int comhi = comword >> 8, comlo = comword & 0xff;
                    if (comword != 0)
                    {
                        if (comhi == 0x80)
                        {
                            c.packwait = comlo;
                        }
                        else if (comhi >= 0x80)
                        {
                            switch (comhi)
                            {
                                case 0xff:
                                    c.volcar = (((c.volcar & 0x3f) * comlo) >> 6) & 0x3f;
                                    if ((fmchip[0xc0 + chan] & 1) != 0)
                                        c.volmod = (((c.volmod & 0x3f) * comlo) >> 6) & 0x3f;
                                    break;
                                case 0xfe:
                                    tempo = comword & 0x3f;
                                    break;
                                case 0xfd:
                                    c.nextvol = comlo;
                                    break;
                                case 0xfc:
                                    playing = false;
                                    // in real player there's also full keyoff here, but we don't need it
                                    break;
                                case 0xfb:
                                    c.keycount = 1;
                                    break;
                                case 0xfa:
                                    vbreak = true;
                                    jumppos = (posplay + 1) & maxpos;
                                    break;
                                case 0xf9:
                                    vbreak = true;
                                    jumppos = comlo & maxpos;
                                    jumping = 1;
                                    // LOOP FIX: jumppos <= posplay detects single-position self-jumps (e.g. J_LOOP/TUNE13) as loops.
                                    if (jumppos <= posplay) { songlooped = true; loopJump = true; }
                                    break;
                                case 0xf8:
                                    c.lasttune = 0;
                                    break;
                                case 0xf7:
                                    c.vibwait = 0;
                                    // PASCAL: c.vibspeed = ((comlo >> 4) & 15) + 2;
                                    c.vibspeed = (comlo >> 4) + 2;
                                    c.vibrate = (comlo & 15) + 1;
                                    break;
                                case 0xf6:
                                    c.glideto = comlo;
                                    break;
                                case 0xf5:
                                    c.finetune = comlo;
                                    break;
                                case 0xf4:
                                    if (hardfade == 0)
                                    {
                                        allvolume = mainvolume = comlo;
                                        fadeonoff = 0;
                                    }
                                    break;
                                case 0xf3:
                                    if (hardfade == 0) fadeonoff = comlo;
                                    break;
                                case 0xf2:
                                    c.trmstay = comlo;
                                    break;
                                case 0xf1: // panorama
                                case 0xf0: // progch
                                    // MIDI commands (unhandled)
                                    break;
                                default:
                                    if (comhi < 0xa0)
                                        c.glideto = comhi & 0x1f;
                                    // else: unknown command, ignored (AdPlug just logs it)
                                    break;
                            }
                        }
                        else
                        {
                            // Signed transpose: C duplicates bit 6 into bit 7 of a signed char.
                            int transp = unchecked((sbyte)((transpose & 127) | ((transpose & 64) != 0 ? 128 : 0)));

                            int sound, high;
                            if ((transpose & 128) != 0)
                            {
                                sound = (comlo + transp) & maxsound;
                                high = comhi << 4;
                            }
                            else
                            {
                                sound = comlo & maxsound;
                                high = (comhi + transp) << 4;
                            }

                            if (chandelay[chan] == 0)
                            {
                                playsound(sound, chan, high);
                            }
                            else
                            {
                                c.chancheat_chandelay = chandelay[chan];
                                c.chancheat_sound = sound;
                                c.chancheat_high = high;
                            }
                        }
                    }

                    c.packpos = (c.packpos + 1) & 0xffff;
                }
                else
                {
                    c.packwait--;
                }
            }

            // Count unique row loop passes; multiple channels may contain the same 0xF9 jump opcode.
            if (loopJump) LoopPassCount++;

            tempo_now = tempo;
            pattplay = (pattplay + 1) & 0xff;
            if (vbreak)
            {
                pattplay = 0;
                for (int i = 0; i < 9; i++) channel[i].packpos = channel[i].packwait = 0;
                posplay = jumppos;
            }
            else if (pattplay >= pattlen)
            {
                pattplay = 0;
                for (int i = 0; i < 9; i++) channel[i].packpos = channel[i].packwait = 0;
                posplay = (posplay + 1) & maxpos;
            }
        }
        else
        {
            tempo_now--;
        }

        // make effects
        for (int chan = 0; chan < 9; chan++)
        {
            var c = channel[chan];
            int regnum = op_table[chan];
            if (c.keycount > 0)
            {
                if (c.keycount == 1)
                    setregs_adv(0xb0 + chan, 0xdf, 0);
                c.keycount--;
            }

            // arpeggio
            int arpreg; // unsigned short in C
            if (c.arp_size == 0)
            {
                arpreg = 0;
            }
            else
            {
                arpreg = (c.arp_tab[c.arp_pos] << 4) & 0xffff;
                if (arpreg == 0x800)
                {
                    if (c.arp_pos > 0) c.arp_tab[0] = c.arp_tab[c.arp_pos - 1];
                    c.arp_size = 1; c.arp_pos = 0;
                    arpreg = (c.arp_tab[0] << 4) & 0xffff;
                }

                if (c.arp_count == c.arp_speed)
                {
                    c.arp_pos++;
                    if (c.arp_pos >= c.arp_size) c.arp_pos = 0;
                    c.arp_count = 0;
                }
                else
                {
                    c.arp_count = (c.arp_count + 1) & 0xff;
                }
            }

            int freq, octave, tune; // unsigned short in C

            // glide & portamento
            if (c.lasttune != 0 && c.lasttune != c.gototune)
            {
                if (c.lasttune > c.gototune)
                {
                    if (c.lasttune - c.gototune < c.portspeed)
                        c.lasttune = c.gototune;
                    else
                        c.lasttune = (c.lasttune - c.portspeed) & 0xffff;
                }
                else
                {
                    if (c.gototune - c.lasttune < c.portspeed)
                        c.lasttune = c.gototune;
                    else
                        c.lasttune = (c.lasttune + c.portspeed) & 0xffff;
                }

                if (arpreg >= 0x800)
                    arpreg = (c.lasttune - (arpreg ^ 0xff0) - 16) & 0xffff;
                else
                    arpreg = (arpreg + c.lasttune) & 0xffff;

                freq = frequency[arpreg % (12 * 16)];
                octave = (arpreg / (12 * 16) - 1) & 0xffff;
                setregs(0xa0 + chan, freq & 0xff);
                setregs_adv(0xb0 + chan, 0x20, ((octave << 2) + (freq >> 8)) & 0xdf);
            }
            else
            {
                // vibrato
                if (c.vibwait == 0)
                {
                    if (c.vibrate != 0)
                    {
                        int wibc = (vibtab[c.vibcount & 0x3f] * c.vibrate) & 0xffff;

                        if ((c.vibcount & 0x40) == 0)
                            tune = (c.lasttune + (wibc >> 8)) & 0xffff;
                        else
                            tune = (c.lasttune - (wibc >> 8)) & 0xffff;

                        if (arpreg >= 0x800)
                            tune = (tune - (arpreg ^ 0xff0) - 16) & 0xffff;
                        else
                            tune = (tune + arpreg) & 0xffff;

                        freq = frequency[tune % (12 * 16)];
                        octave = (tune / (12 * 16) - 1) & 0xffff;
                        setregs(0xa0 + chan, freq & 0xff);
                        setregs_adv(0xb0 + chan, 0x20, ((octave << 2) + (freq >> 8)) & 0xdf);
                        c.vibcount = (c.vibcount + c.vibspeed) & 0xff;
                    }
                    else if (c.arp_size != 0)
                    {
                        // no vibrato, just arpeggio
                        if (arpreg >= 0x800)
                            tune = (c.lasttune - (arpreg ^ 0xff0) - 16) & 0xffff;
                        else
                            tune = (c.lasttune + arpreg) & 0xffff;

                        freq = frequency[tune % (12 * 16)];
                        octave = (tune / (12 * 16) - 1) & 0xffff;
                        setregs(0xa0 + chan, freq & 0xff);
                        setregs_adv(0xb0 + chan, 0x20, ((octave << 2) + (freq >> 8)) & 0xdf);
                    }
                }
                else
                {
                    // no vibrato, just arpeggio
                    c.vibwait--;

                    if (c.arp_size != 0)
                    {
                        if (arpreg >= 0x800)
                            tune = (c.lasttune - (arpreg ^ 0xff0) - 16) & 0xffff;
                        else
                            tune = (c.lasttune + arpreg) & 0xffff;

                        freq = frequency[tune % (12 * 16)];
                        octave = (tune / (12 * 16) - 1) & 0xffff;
                        setregs(0xa0 + chan, freq & 0xff);
                        setregs_adv(0xb0 + chan, 0x20, ((octave << 2) + (freq >> 8)) & 0xdf);
                    }
                }
            }

            // tremolo (modulator)
            if (c.trmwait == 0)
            {
                if (c.trmrate != 0)
                {
                    int tremc = (tremtab[c.trmcount & 0x7f] * c.trmrate) & 0xffff;
                    int level = (tremc >> 8) <= (c.volmod & 0x3f)
                        ? (c.volmod & 0x3f) - (tremc >> 8)
                        : 0;

                    if (allvolume != 0 && (fmchip[0xc0 + chan] & 1) != 0)
                        setregs_adv(0x40 + regnum, 0xc0, ((level * allvolume) >> 8) ^ 0x3f);
                    else
                        setregs_adv(0x40 + regnum, 0xc0, level ^ 0x3f);

                    c.trmcount = (c.trmcount + c.trmspeed) & 0xff;
                }
                else if (allvolume != 0 && (fmchip[0xc0 + chan] & 1) != 0)
                {
                    setregs_adv(0x40 + regnum, 0xc0, ((((c.volmod & 0x3f) * allvolume) >> 8) ^ 0x3f) & 0x3f);
                }
                else
                {
                    setregs_adv(0x40 + regnum, 0xc0, (c.volmod ^ 0x3f) & 0x3f);
                }
            }
            else
            {
                c.trmwait--;
                if (allvolume != 0 && (fmchip[0xc0 + chan] & 1) != 0)
                    setregs_adv(0x40 + regnum, 0xc0, ((((c.volmod & 0x3f) * allvolume) >> 8) ^ 0x3f) & 0x3f);
            }

            // tremolo (carrier)
            if (c.trcwait == 0)
            {
                if (c.trcrate != 0)
                {
                    int tremc = (tremtab[c.trccount & 0x7f] * c.trcrate) & 0xffff;
                    int level = (tremc >> 8) <= (c.volcar & 0x3f)
                        ? (c.volcar & 0x3f) - (tremc >> 8)
                        : 0;

                    if (allvolume != 0)
                        setregs_adv(0x43 + regnum, 0xc0, ((level * allvolume) >> 8) ^ 0x3f);
                    else
                        setregs_adv(0x43 + regnum, 0xc0, level ^ 0x3f);
                    c.trccount = (c.trccount + c.trcspeed) & 0xff;
                }
                else if (allvolume != 0)
                {
                    setregs_adv(0x43 + regnum, 0xc0, ((((c.volcar & 0x3f) * allvolume) >> 8) ^ 0x3f) & 0x3f);
                }
                else
                {
                    setregs_adv(0x43 + regnum, 0xc0, (c.volcar ^ 0x3f) & 0x3f);
                }
            }
            else
            {
                c.trcwait--;
                if (allvolume != 0)
                    setregs_adv(0x43 + regnum, 0xc0, ((((c.volcar & 0x3f) * allvolume) >> 8) ^ 0x3f) & 0x3f);
            }
        }

        return playing && !songlooped;
    }

    private void playsound(int inst_number, int channel_number, int tunehigh)
    {
        var c = channel[channel_number];          // current channel
        var i = soundbank[inst_number];           // current instrument
        int regnum = op_table[channel_number];    // channel's OPL2 register

        // set fine tune
        tunehigh += ((i.finetune + c.finetune + 0x80) & 0xff) - 0x80;

        // arpeggio handling
        if (i.arpeggio == 0)
        {
            int arpcalc = i.arp_tab[0] << 4;

            if (arpcalc > 0x800)
                tunehigh = tunehigh - (arpcalc ^ 0xff0) - 16;
            else
                tunehigh += arpcalc;
        }

        // glide handling
        if (c.glideto != 0)
        {
            c.gototune = tunehigh & 0xffff;
            c.portspeed = c.glideto;
            c.glideto = c.finetune = 0;
            return;
        }

        // set modulator registers
        setregs(0x20 + regnum, i.mod_misc);
        int volcalc = i.mod_vol;
        if (c.nextvol == 0 || (i.feedback & 1) == 0)
            c.volmod = volcalc;
        else
            c.volmod = (volcalc & 0xc0) | (((volcalc & 0x3f) * c.nextvol) >> 6);

        if ((i.feedback & 1) == 1 && allvolume != 0)
            setregs(0x40 + regnum, ((c.volmod & 0xc0) | (((c.volmod & 0x3f) * allvolume) >> 8)) ^ 0x3f);
        else
            setregs(0x40 + regnum, c.volmod ^ 0x3f);
        setregs(0x60 + regnum, i.mod_ad);
        setregs(0x80 + regnum, i.mod_sr);
        setregs(0xe0 + regnum, i.mod_wave);

        // set carrier registers
        setregs(0x23 + regnum, i.car_misc);
        volcalc = i.car_vol;
        if (c.nextvol == 0)
            c.volcar = volcalc;
        else
            c.volcar = (volcalc & 0xc0) | (((volcalc & 0x3f) * c.nextvol) >> 6);

        if (allvolume != 0)
            setregs(0x43 + regnum, ((c.volcar & 0xc0) | (((c.volcar & 0x3f) * allvolume) >> 8)) ^ 0x3f);
        else
            setregs(0x43 + regnum, c.volcar ^ 0x3f);
        setregs(0x63 + regnum, i.car_ad);
        setregs(0x83 + regnum, i.car_sr);
        setregs(0xe3 + regnum, i.car_wave);
        setregs(0xc0 + channel_number, i.feedback);
        setregs_adv(0xb0 + channel_number, 0xdf, 0); // key off

        // Guard negative modulo index before looking up note frequencies.
        int noteIndex = tunehigh % (12 * 16);
        if (noteIndex < 0) noteIndex += 12 * 16;
        int freq = frequency[noteIndex];
        int octave = (byte)(tunehigh / (12 * 16) - 1); // unsigned char in C
        if (i.glide == 0)
        {
            if (i.portamento == 0 || c.lasttune == 0)
            {
                setregs(0xa0 + channel_number, freq & 0xff);
                setregs(0xb0 + channel_number, (octave << 2) + 0x20 + (freq >> 8));
                c.lasttune = c.gototune = tunehigh & 0xffff;
            }
            else
            {
                c.gototune = tunehigh & 0xffff;
                c.portspeed = i.portamento;
                setregs_adv(0xb0 + channel_number, 0xdf, 0x20); // key on
            }
        }
        else
        {
            setregs(0xa0 + channel_number, freq & 0xff);
            setregs(0xb0 + channel_number, (octave << 2) + 0x20 + (freq >> 8));
            c.lasttune = tunehigh & 0xffff;
            c.gototune = (tunehigh + ((i.glide + 0x80) & 0xff) - 0x80) & 0xffff; // set destination
            c.portspeed = i.portamento;
        }

        if (i.vibrato == 0)
        {
            c.vibwait = c.vibspeed = c.vibrate = 0;
        }
        else
        {
            c.vibwait = i.vibdelay;
            // PASCAL: c.vibspeed = ((i.vibrato >> 4) & 15) + 1;
            c.vibspeed = (i.vibrato >> 4) + 2;
            c.vibrate = (i.vibrato & 15) + 1;
        }

        if ((c.trmstay & 0xf0) == 0)
        {
            c.trmwait = (i.tremwait & 0xf0) >> 3;
            // PASCAL: c.trmspeed = (i.mod_trem >> 4) & 15;
            c.trmspeed = i.mod_trem >> 4;
            c.trmrate = i.mod_trem & 15;
            c.trmcount = 0;
        }

        if ((c.trmstay & 0x0f) == 0)
        {
            c.trcwait = (i.tremwait & 15) << 1;
            // PASCAL: c.trcspeed = (i.car_trem >> 4) & 15;
            c.trcspeed = i.car_trem >> 4;
            c.trcrate = i.car_trem & 15;
            c.trccount = 0;
        }

        c.arp_size = i.arpeggio & 15;
        c.arp_speed = i.arpeggio >> 4;
        Array.Copy(i.arp_tab, c.arp_tab, 12);
        c.keycount = i.keyoff;
        c.nextvol = c.glideto = c.finetune = c.vibcount = c.arp_pos = c.arp_count = 0;
    }

    private void setregs(int reg, int val)
    {
        reg &= 0xff; val &= 0xff; // C: unsigned char parameters
        if (fmchip[reg] == val) return;

        fmchip[reg] = val;
        opl.Write(reg, (byte)val);
    }

    private void setregs_adv(int reg, int mask, int val)
        => setregs(reg, (fmchip[reg & 0xff] & mask) | val);
}
