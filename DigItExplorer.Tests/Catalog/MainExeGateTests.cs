using System.IO;
using DigItExplorer.Core.Catalog;
using DigItExplorer.Core.Formats;

namespace DigItExplorer.Tests;

/// <summary>Guards executable version validation and NE segment layout detection.</summary>
public sealed class MainExeGateTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("digit-exe-gate-tests").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact(GameNeeds.SupportedBuild | GameNeeds.FullResourceSet)]
    public void The_games_own_executable_is_a_build_this_application_knows()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        var exe = GameExecutable.Open(gameDir, "MAIN.EXE");

        Assert.True(NeSegmentTable.TryRead(exe, out var segments));
        Assert.Equal(7, segments.Length);
        Assert.True(BuildLayout.TryRecognize(exe, out _));
    }

    /// <summary>The same address is a different file offset in each build, and both are the recorded one.</summary>
    [Theory]
    [InlineData(false, 0x000F00, 0x005900, 0x00AC00, 0x020100)]
    [InlineData(true, 0x000F00, 0x005B00, 0x00AE00, 0x020300)]
    public void Each_known_build_puts_the_four_segments_where_its_own_table_says(
        bool manaccom, int seg1, int seg2, int seg3, int dgroup)
    {
        var layout = LayoutOf(manaccom ? FakeMainExe.Manaccom() : FakeMainExe.FullRelease());

        Assert.Equal(seg1, layout.At(ExeLayout.Seg1(0)));
        Assert.Equal(seg2, layout.At(ExeLayout.Seg2(0)));
        Assert.Equal(seg3, layout.At(ExeLayout.Seg3(0)));
        Assert.Equal(dgroup, layout.At(ExeLayout.DGroup(0)));
    }

    /// <summary>Only the Manaccom edition shifts a seg1 address, and only from the offset its insertion sits at.</summary>
    [Fact]
    public void The_seg1_shift_applies_to_one_build_and_only_above_its_insertion_point()
    {
        var full = LayoutOf(FakeMainExe.FullRelease());
        var manaccom = LayoutOf(FakeMainExe.Manaccom());

        // Below the insertion point both builds hold the address as recorded.
        Assert.Equal(0x000F00 + 0x2809, full.At(ExeLayout.Seg1(0x2809)));
        Assert.Equal(0x000F00 + 0x2809, manaccom.At(ExeLayout.Seg1(0x2809)));

        // At and above it the recorded address moves in the build that grew, and not in the one that did not.
        Assert.Equal(0x000F00 + 0x280A, full.At(ExeLayout.Seg1(0x280A)));
        Assert.Equal(0x000F00 + 0x280A + 226, manaccom.At(ExeLayout.Seg1(0x280A)));
        Assert.Equal(0x000F00 + 0x2825 + 226, manaccom.At(ExeLayout.Seg1(0x2825)));
    }

    /// <summary>An address read out of the executable is already that build's own and is never shifted.</summary>
    [Fact]
    public void An_address_read_from_the_file_is_used_exactly_as_it_was_found()
    {
        var manaccom = LayoutOf(FakeMainExe.Manaccom());

        // The same number, once as an address this application carries and once as one the file handed back.
        Assert.Equal(0x000F00 + 0x280D + 226, manaccom.At(ExeLayout.Seg1(0x280D)));
        Assert.Equal(0x000F00 + 0x280D, manaccom.At(ExeAddress.FromOperand(ExeSegment.Seg1, 0x280D)));
    }

    [Fact]
    public void A_file_that_is_not_a_new_executable_describes_nothing()
    {
        var exe = GameExecutable.Open(_root, WriteForeignExecutable("NotAnExe.EXE"));

        Assert.False(NeSegmentTable.TryRead(exe, out _));
        Assert.False(BuildLayout.TryRecognize(exe, out _));
    }

    /// <summary>A well-formed executable that is not one of the builds is refused rather than read anyway.</summary>
    [Fact]
    public void A_new_executable_of_another_build_is_refused()
    {
        // The shareware's own shape: eight segments, and a seg1 of a length neither build has.
        var shareware = FakeMainExe.WithSegments(0x00F00, 0x04800, 0x09B00, 0x20600, 11350, 0x21000, segmentCount: 8);
        Assert.False(BuildLayout.TryRecognize(Open("Shareware.EXE", shareware), out _));

        // Seven segments, but a seg1 length on no row of the registry.
        var unknown = FakeMainExe.WithSegments(0x00F00, 0x05900, 0x0AC00, 0x20100, 20000, 0x21000);
        Assert.False(BuildLayout.TryRecognize(Open("Unknown.EXE", unknown), out _));
    }

    private BuildLayout LayoutOf(byte[] bytes)
    {
        Assert.True(BuildLayout.TryRecognize(Open($"{Guid.NewGuid():N}.EXE", bytes), out var layout));
        return layout;
    }

    private GameExecutable Open(string name, byte[] bytes)
    {
        File.WriteAllBytes(Path.Combine(_root, name), bytes);
        return GameExecutable.Open(_root, name);
    }

    /// <summary>Verifies graceful fallback when attempting to read font data from an invalid executable.</summary>
    [Fact]
    public void A_foreign_executable_yields_no_font_instead_of_throwing()
    {
        var exe = GameExecutable.Open(_root, WriteForeignExecutable("Foreign.EXE"));

        Assert.False(GameFont.TryLoadFromMainExe(exe, out _));
        // The contrast is the point, and it is why the guarded entry point exists rather than the unguarded
        // one simply being made safe: a caller that has established which build it holds still wants the
        // read to fail loudly if it is wrong about that.
        Assert.ThrowsAny<Exception>(() => GameFont.LoadFromMainExe(exe));
    }

    [Trait("Category", "RequiresGame")]
    [RequiresGameFact]
    public void An_install_whose_executable_is_another_build_opens_with_the_raw_view_and_says_so()
    {
        Assert.True(TestPaths.TryGetGameDir(out var gameDir));

        // One archive is enough to make the folder openable; what is under test is the executable beside it.
        var folder = Directory.CreateDirectory(Path.Combine(_root, "OtherBuild")).FullName;
        File.Copy(Path.Combine(gameDir, "DIGITX.XRS"), Path.Combine(folder, "DIGITX.XRS"));
        WriteForeignExecutable("MAIN.EXE", folder);

        var contents = GameInstall.Open(folder);
        using var library = contents.Library;

        Assert.Equal(MainExeStatus.DifferentBuild, contents.MainExe);
        Assert.NotEmpty(contents.Library.Names);
        Assert.Null(contents.Font);
        Assert.Null(contents.Data);
        Assert.Null(contents.Skins);
    }

    // An executable-sized file holding no executable: every offset this application reads is inside it, so
    // the reads all happen and all have to fail on their own merits rather than on a length check.
    private string WriteForeignExecutable(string name, string? folder = null)
    {
        File.WriteAllBytes(Path.Combine(folder ?? _root, name), new byte[137_216]);
        return name;
    }
}
