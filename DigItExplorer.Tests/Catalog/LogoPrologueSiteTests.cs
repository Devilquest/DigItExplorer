using System.IO;
using System.Text;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Guards the reads of the logo prologue, whose code only one build carries, against an executable
/// fabricated to hold it.</summary>
public sealed class LogoPrologueSiteTests : IDisposable
{
    private const int Seg1 = FakeMainExe.ManaccomSeg1;

    private readonly string _root = Directory.CreateTempSubdirectory("digit-logo-prologue-tests").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    /// <summary>Only the build carrying the prologue reports playing one.</summary>
    [Fact]
    public void A_build_without_the_prologue_says_it_plays_none()
    {
        Assert.False(LayoutOf(FakeMainExe.FullRelease()).PlaysLogoPrologue);
        Assert.True(LayoutOf(FakeMainExe.Manaccom()).PlaysLogoPrologue);
    }

    /// <summary>The prologue's addresses are the offsets of the build that carries it, so the shift the rest
    /// of seg1 takes has nothing to do to them.</summary>
    [Fact]
    public void An_address_inside_the_added_code_is_not_shifted_on_top()
    {
        var layout = LayoutOf(FakeMainExe.Manaccom());

        Assert.Equal(Seg1 + 0x2820, layout.At(ExeLayout.LogoNameSite));
        Assert.Equal(Seg1 + 0x2861, layout.At(ExeLayout.LogoTickSite));
        Assert.Equal(Seg1 + 0x2882, layout.At(ExeLayout.LogoPassSite));
        Assert.Equal(Seg1 + 0x28B2, layout.At(ExeLayout.LogoHoldSite));

        // The same offset carried as an address of this application's own does take the shift, which is what
        // makes the two kinds of address worth telling apart.
        Assert.Equal(Seg1 + 0x2820 + 226, layout.At(ExeLayout.Seg1(0x2820)));
    }

    /// <summary>The four values the leaf plays by come off the prologue's own instructions.</summary>
    [Fact]
    public void The_prologue_reads_out_its_file_its_pace_its_passes_and_its_hold()
    {
        var reader = ReaderOn(WithPrologue());

        Assert.True(reader.PlaysLogoPrologue);
        Assert.True(reader.TryReadPointer(ExeLayout.LogoNameSite, out var name));
        Assert.True(reader.TryReadPascalString(name, out var fileName));
        Assert.Equal("MANLOGO.ANI", Encoding.Latin1.GetString(fileName));
        Assert.True(reader.TryReadTickWait(ExeLayout.LogoTickSite, out int ticks));
        Assert.Equal(9, ticks);
        Assert.True(reader.TryReadLocalThreshold(ExeLayout.LogoPassSite, out int passes));
        Assert.Equal(58, passes);
        Assert.True(reader.TryReadTickWait(ExeLayout.LogoHoldSite, out int hold));
        Assert.Equal(300, hold);
    }

    /// <summary>A threshold written as the branch that leaves the loop is the same threshold, and a
    /// comparison under neither branch is not one at all.</summary>
    [Fact]
    public void A_wait_reads_the_same_whether_the_branch_stays_in_the_loop_or_leaves_it()
    {
        var exe = FakeMainExe.Manaccom();
        FakeMainExe.PutInSeg1(exe, Seg1, 0x2861, 0x3D, 0x09, 0x00, 0x72); // cmp ax, 9; jb
        FakeMainExe.PutInSeg1(exe, Seg1, 0x28B2, 0x3D, 0x09, 0x00, 0x73); // cmp ax, 9; jae
        FakeMainExe.PutInSeg1(exe, Seg1, 0x2820, 0x3D, 0x09, 0x00, 0x75); // cmp ax, 9; jne
        var reader = ReaderOn(exe);

        Assert.True(reader.TryReadTickWait(ExeLayout.LogoTickSite, out int stayingIn));
        Assert.True(reader.TryReadTickWait(ExeLayout.LogoHoldSite, out int leaving));
        Assert.Equal(stayingIn, leaving);
        Assert.False(reader.TryReadTickWait(ExeLayout.LogoNameSite, out _));
    }

    /// <summary>An executable holding the prologue's own five instructions and the literal they point at.</summary>
    private static byte[] WithPrologue()
    {
        var exe = FakeMainExe.Manaccom();
        FakeMainExe.PutInSeg1(exe, Seg1, 0x27E9, [11, .. "MANLOGO.ANI"u8]);
        FakeMainExe.PutInSeg1(exe, Seg1, 0x2820, 0xBF, 0xE9, 0x27);       // mov di, 0x27E9
        FakeMainExe.PutInSeg1(exe, Seg1, 0x2861, 0x3D, 0x09, 0x00, 0x72); // cmp ax, 9; jb
        FakeMainExe.PutInSeg1(exe, Seg1, 0x2882, 0x83, 0x7E, 0xFE, 0x3A); // cmp word [bp-2], 0x3A
        FakeMainExe.PutInSeg1(exe, Seg1, 0x28B2, 0x3D, 0x2C, 0x01, 0x73); // cmp ax, 0x12C; jae
        return exe;
    }

    private ExeReader ReaderOn(byte[] bytes)
    {
        var exe = Open(bytes);
        Assert.True(BuildLayout.TryRecognize(exe, out var layout));
        return new ExeReader(exe, layout);
    }

    private BuildLayout LayoutOf(byte[] bytes)
    {
        Assert.True(BuildLayout.TryRecognize(Open(bytes), out var layout));
        return layout;
    }

    private GameExecutable Open(byte[] bytes)
    {
        var name = $"{Guid.NewGuid():N}.EXE";
        File.WriteAllBytes(Path.Combine(_root, name), bytes);
        return GameExecutable.Open(_root, name);
    }
}
