using System.Security.Cryptography;
using System.Text;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Cutscenes;
using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Tests;

/// <summary>Verifies cutscene animation composition, frame counts, and caption sliding against reference frame hashes.</summary>
public class CutsceneCompositorTests
{
    // (piece, .ANI file, total iterations): total iterations equals frame count for every piece except
    // Intro01, whose caption holds the first frame for CaptionHoldIterations extra iterations.
    private static readonly (CutscenePiece Piece, string File, int TotalIterations)[] AniPieces =
    [
        (CutscenePiece.Intro01, "INTRO01.ANI", 500),
        (CutscenePiece.Intro03, "INTRO03.ANI", 100),
        (CutscenePiece.Intro02, "INTRO02.ANI", 274),
        (CutscenePiece.DigTitle, "DIGTITLE.ANI", 420),
    ];

    // (piece, [(iteration, SHA-256 of Compose's raw index buffer)]): pinned once from a known-good
    // compose at each sampled iteration.
    private static readonly (CutscenePiece Piece, (int Iteration, string Sha)[] Samples)[] ComposeSamples =
    [
        (CutscenePiece.DigTitle, [
            (0, "ff401ea039047459df0e25c626c09599ddf29b7d177ce794cc9dcf1b48ac577d"),
            (100, "8769b7d41266ce90e8f175d6ed179d03cd9c91088f63e79a6d63c981afb67065"),
            (419, "ff401ea039047459df0e25c626c09599ddf29b7d177ce794cc9dcf1b48ac577d"), // loops back to frame 0
        ]),
        (CutscenePiece.Intro03, [
            (0, "1b434a7dd08e61f88c8ae7f2938dcc5adb0baea131c10e6f17902d57f757ad22"),
            (50, "029a564cc3b673b34eea7714fd3cd2464d01376a0b2d10bcfd4455eb40b75bcf"),
            (99, "942569d6de2bf493ee29249549a6fcb39c724ada65c013c66e11bc15af4fc239"),
        ]),
        (CutscenePiece.Intro02, [
            (0, "7aec0d00251a5a965fdba6e28179c6dd1ed13136c0f308bffbf9b56802426d63"),
            (137, "324631982e2f5c385dd200943a9d81da374ab6e77ca2b3653f41b4f32859cd80"),
            (273, "001940331212b4b68361c107ec1b608e118ab76616687f940a1b537d738c583a"),
        ]),
        (CutscenePiece.Intro01, [
            (0, "0150cada41328a42686c965723f796bd6066cc1224debabc20b54c76fd9f6c2f"),    // frame 0, caption not yet sliding
            (69, "0150cada41328a42686c965723f796bd6066cc1224debabc20b54c76fd9f6c2f"),   // still frame 0, caption about to slide
            (70, "0150cada41328a42686c965723f796bd6066cc1224debabc20b54c76fd9f6c2f"),   // slide starts this iteration
            (71, "a66ea08ed3771fae0fbf1bd046755d351c6fa7a8d07bc1784aec4ac72492c85a"),
            (76, "0d1dd4b1f73ff00ebac62ce885d78443e10272de7acf0d85b228fe36feb9e915"),
            (77, "a885f78572b78bf9a7b8abc05863b1c8dd84bb283e7c6ca7756caa88a8860e06"),   // caption off-screen
            (99, "a885f78572b78bf9a7b8abc05863b1c8dd84bb283e7c6ca7756caa88a8860e06"),   // last held iteration
            (100, "a885f78572b78bf9a7b8abc05863b1c8dd84bb283e7c6ca7756caa88a8860e06"),  // animation starts, frame 0 again
            (101, "84eb4448d6556324e2c8f5b536343ff25f51268ff85770f30313f36e9b82614b"),
            (150, "6323f97f9932215aac4422e309c1e1f05a1c34bca82842a34b869af4d41d1ba4"),
            (499, "906a6c599db2e59ebc8f6673dab0a3c556573d8837d6d4588a53b3d14ca739eb"),
        ]),
    ];

    private const string CardSha = "bb2cc71b1ac15a849eb124e749406b29dc424a6be0a5d54fe4f85b92b1441a1c";

    private static GameFont LoadFont(string gameDir) =>
        GameFont.LoadFromMainExe(GameExecutable.Open(gameDir, "MAIN.EXE"));

    /// <summary>Guards total iteration counts across all animated cutscene pieces.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Ani_pieces_have_the_expected_total_iterations()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);

        foreach (var (piece, file, total) in AniPieces)
        {
            Assert.Equal(file, data.Cutscenes.Pieces[piece].FileName);
            var ani = SheetImage.Read(library.Read(file));
            var clip = CutsceneCompositor.Build(piece, ani, font, data.Cutscenes);
            Assert.Equal(total, clip.FrameOrder.Count);
        }
    }

    /// <summary>Guards cutscene frame composition against reference pixel hashes across sample iterations.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Compose_matches_reference_at_sample_iterations()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);

        foreach (var (piece, samples) in ComposeSamples)
        {
            var file = data.Cutscenes.Pieces[piece].FileName!;
            var ani = SheetImage.Read(library.Read(file));
            var clip = CutsceneCompositor.Build(piece, ani, font, data.Cutscenes);

            foreach (var (iteration, sha) in samples)
            {
                var frame = CutsceneCompositor.Compose(clip, iteration, data.Cutscenes);
                Assert.Equal(sha, Convert.ToHexString(SHA256.HashData(frame)).ToLowerInvariant());
            }
        }
    }

    /// <summary>Guards the logo prologue's leaf against whichever build the copy in hand is: the passes its
    /// own loop makes and the hold after them, or no piece at all.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void The_logo_prologue_plays_its_own_passes_and_then_holds_the_frame_they_leave_up()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(TestPaths.TryGetGameData(out var data));
        var cutscenes = data.Cutscenes;

        if (!cutscenes.Pieces.TryGetValue(CutscenePiece.ManLogo, out var info))
        {
            // A build that plays no prologue has no piece to compose, and the file it would play, if the
            // copy even ships one, stays where an unplayed file belongs: the Raw view.
            Assert.Equal(0, cutscenes.LogoPasses);
            Assert.Equal(0, cutscenes.LogoHoldTicks);
            return;
        }

        Assert.Equal("MANLOGO.ANI", info.FileName);
        Assert.Equal(9, info.Ticks);
        Assert.Equal(58, cutscenes.LogoPasses);
        Assert.Equal(300, cutscenes.LogoHoldTicks);

        var ani = SheetImage.Read(library.Read(info.FileName!));
        var clip = CutsceneCompositor.Build(CutscenePiece.ManLogo, ani, LoadFont(gameDir), cutscenes);

        // The loop presents before it advances and never presents again, so the frames past its passes are
        // never on screen and the last one it did present is what the hold leaves up.
        Assert.Equal(60, ani.Frames.Count);
        Assert.Equal(91, clip.FrameOrder.Count);
        Assert.Equal([.. Enumerable.Range(0, 58)], clip.FrameOrder.Take(58));
        Assert.All(clip.FrameOrder.Skip(58), f => Assert.Equal(57, f));
    }

    /// <summary>Verifies that cutscene text cards hold a single static frame across all iterations.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Card_is_one_static_frame_held_for_every_iteration()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));
        using var library = ResourceLibrary.Open(gameDir);
        Assert.True(TestPaths.TryGetGameData(out var data));
        var font = LoadFont(gameDir);

        var palBytes = library.Read(data.Cutscenes.CardPaletteFile);
        var palette = VgaPalette.From6Bit(palBytes.AsSpan(0, 768));
        var clip = CutsceneCompositor.BuildCard(font, palette, data.Cutscenes);

        Assert.Equal(data.Cutscenes.CardHoldIterations, clip.FrameOrder.Count);
        Assert.Single(clip.Frames);
        Assert.All(clip.FrameOrder, f => Assert.Equal(0, f));

        foreach (int iteration in new[] { 0, data.Cutscenes.CardHoldIterations - 1 })
        {
            var frame = CutsceneCompositor.Compose(clip, iteration, data.Cutscenes);
            Assert.Equal(CardSha, Convert.ToHexString(SHA256.HashData(frame)).ToLowerInvariant());
        }
    }

    /// <summary>Build fingerprint test verifying decoded cutscene metadata and timings against the reference build.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Values_read_from_the_executable_match_the_known_build()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));
        var cutscenes = data.Cutscenes;

        Assert.Equal("Early morning at the Double Dug Ranch ...",
            Encoding.Latin1.GetString(cutscenes.Pieces[CutscenePiece.Intro01].Caption!));
        Assert.Equal("Later that day ...", Encoding.Latin1.GetString(cutscenes.CardText));
        Assert.Equal("LVL900.PAL", cutscenes.CardPaletteFile);
        Assert.Equal((160, 92), (cutscenes.CardX, cutscenes.CardY));
        Assert.Equal(79, cutscenes.CardHoldIterations);

        Assert.Equal(100, cutscenes.CaptionHoldIterations);
        Assert.Equal(160, cutscenes.CaptionCenterX);
        Assert.Equal(180, cutscenes.CaptionStartY);
        Assert.Equal(70, cutscenes.CaptionSlideFrom);
        Assert.Equal(3, cutscenes.CaptionSlidePx);

        // The title's wait loop branches on jbe where the other two branch on jb, so it genuinely waits one
        // tick longer than the operand it compares against.
        Assert.Equal(8, cutscenes.Pieces[CutscenePiece.Intro01].Ticks);
        Assert.Equal(6, cutscenes.Pieces[CutscenePiece.Card].Ticks);
        Assert.Equal(7, cutscenes.Pieces[CutscenePiece.DigTitle].Ticks);
    }

    /// <summary>Verifies that uncaptioned cutscene pieces expose null captions rather than empty strings.</summary>
    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild)]
    public void Uncaptioned_pieces_have_no_caption()
    {
        Assert.True(TestPaths.TryGetGameData(out var data));

        Assert.NotNull(data.Cutscenes.Pieces[CutscenePiece.Intro01].Caption);
        Assert.Null(data.Cutscenes.Pieces[CutscenePiece.Intro03].Caption);
        Assert.Null(data.Cutscenes.Pieces[CutscenePiece.Intro02].Caption);
        Assert.Null(data.Cutscenes.Pieces[CutscenePiece.DigTitle].Caption);
    }
}
