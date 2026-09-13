using System.IO;
using DigItExplorer.Core.Catalog;

namespace DigItExplorer.Tests;

/// <summary>Tests game directory resolution rules using synthetic folder layouts.</summary>
public sealed class GameInstallTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("digit-install-tests").FullName;

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string MakeFolder(string name, bool withArchives)
    {
        var dir = Directory.CreateDirectory(Path.Combine(_root, name)).FullName;
        if (withArchives) File.WriteAllBytes(Path.Combine(dir, "DIGIT0.XRS"), []);
        return dir;
    }

    [Fact]
    public void TryUseFolder_accepts_the_folder_the_archives_are_in()
    {
        var dir = MakeFolder("Game", withArchives: true);

        Assert.True(GameInstall.TryUseFolder(dir, out var gameDir));
        Assert.Equal(dir, gameDir);
    }

    [Fact]
    public void TryUseFolder_accepts_a_folder_holding_a_DIGIT_subfolder()
    {
        var parent = MakeFolder("Parent", withArchives: false);
        var nested = Directory.CreateDirectory(Path.Combine(parent, "DIGIT")).FullName;
        File.WriteAllBytes(Path.Combine(nested, "DIGIT0.XRS"), []);

        Assert.True(GameInstall.TryUseFolder(parent, out var gameDir));
        Assert.Equal(nested, gameDir);
    }

    [Fact]
    public void TryUseFolder_prefers_the_folder_itself_over_a_DIGIT_subfolder_of_it()
    {
        var dir = MakeFolder("Both", withArchives: true);
        var nested = Directory.CreateDirectory(Path.Combine(dir, "DIGIT")).FullName;
        File.WriteAllBytes(Path.Combine(nested, "DIGIT0.XRS"), []);

        Assert.True(GameInstall.TryUseFolder(dir, out var gameDir));
        Assert.Equal(dir, gameDir);
    }

    [Fact]
    public void TryUseFolder_rejects_a_folder_with_no_archives_anywhere()
    {
        var dir = MakeFolder("Empty", withArchives: false);

        Assert.False(GameInstall.TryUseFolder(dir, out var gameDir));
        Assert.Equal("", gameDir);
    }

    [Fact]
    public void TryUseFolder_rejects_a_folder_that_does_not_exist()
        => Assert.False(GameInstall.TryUseFolder(Path.Combine(_root, "NoSuchFolder"), out _));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryUseFolder_rejects_a_blank_path(string folder)
    {
        // A blank path would otherwise reach Path.Combine and be tested as the relative path "DIGIT",
        // which resolves against whatever the process's current directory happens to be.
        Assert.False(GameInstall.TryUseFolder(folder, out _));
    }

    /// <summary>Guards against path traversal via relative folder paths.</summary>
    [Theory]
    [InlineData("DIGIT")]
    [InlineData(@"..\DIGIT")]
    public void TryUseFolder_rejects_a_relative_path(string folder)
    {
        Assert.False(GameInstall.TryUseFolder(folder, out var gameDir));
        Assert.Equal("", gameDir);
    }

    /// <summary>Ensures malformed paths with invalid characters fail closed without throwing.</summary>
    [Fact]
    public void TryUseFolder_rejects_a_path_with_invalid_characters_without_throwing()
    {
        var folder = Path.Combine(_root, "Bad|<>Folder");
        Assert.False(GameInstall.TryUseFolder(folder, out var gameDir));
        Assert.Equal("", gameDir);
    }

    [Fact]
    public void TryUseFolder_rejects_a_path_past_the_length_limit_without_throwing()
    {
        var folder = Path.Combine(_root, new string('a', 400));
        Assert.False(GameInstall.TryUseFolder(folder, out var gameDir));
        Assert.Equal("", gameDir);
    }

    /// <summary>Ensures non-game XRS archives are rejected by folder resolution.</summary>
    [Fact]
    public void TryUseFolder_rejects_a_folder_whose_only_XRS_is_not_one_of_the_six_named_archives()
    {
        var dir = MakeFolder("Notes", withArchives: false);
        File.WriteAllBytes(Path.Combine(dir, "NOTES.XRS"), []);

        Assert.False(GameInstall.TryUseFolder(dir, out var gameDir));
        Assert.Equal("", gameDir);
    }

    /// <summary>Ensures corrupt or truncated archive files throw on open.</summary>
    [Fact]
    public void Open_throws_when_a_named_archive_exists_but_cannot_be_read()
    {
        var dir = MakeFolder("Garbage", withArchives: true); // 0-byte DIGIT0.XRS: too short for its own header

        Assert.True(GameInstall.TryUseFolder(dir, out var gameDir));
        Assert.Throws<EndOfStreamException>(() => GameInstall.Open(gameDir));
    }
}
