using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Knowledge.Animations;

/// <summary>Failure reason for animation sheet resolution.</summary>
public enum AnimationSheetFailure
{
    /// <summary>Resolution succeeded.</summary>
    None,

    /// <summary>Category has no cataloged sheet grid.</summary>
    NoGrid,

    /// <summary>Required sheet or palette file is missing.</summary>
    MissingFiles,
}

/// <summary>Resolved animation assets including decoded pages, palette, frame rects, and grid metadata.</summary>
/// <param name="Pages">Decoded sheet pages in file order.</param>
/// <param name="Palette">VGA palette used for rendering.</param>
/// <param name="Rects">Explicit frame crops, or null for uniform grid sets.</param>
/// <param name="Grid">Uniform grid dimensions for cell slicing.</param>
/// <param name="SheetFile">Source sprite sheet filename.</param>
/// <param name="PaletteFile">Source palette filename, or null if using embedded palette.</param>
public sealed record ResolvedAnimationSheet(IReadOnlyList<byte[]> Pages, VgaPalette Palette,
    IReadOnlyDictionary<int, FrameRect>? Rects, EnemyGrid Grid, string SheetFile, string? PaletteFile);

/// <summary>Resolves and decodes sprite sheet and palette files for character animation sets.</summary>
public static class AnimationSheetResolver
{
    /// <summary>Resolves and decodes animation sheet assets for a given character set and skin suffix.</summary>
    public static ResolvedAnimationSheet? Resolve(CharacterAnimSet set, string suffix,
        Func<string, byte[]?> read, out AnimationSheetFailure failure)
    {
        failure = AnimationSheetFailure.None;
        EnemyGrid? enemyGrid = null;
        (byte[]? Bytes, string? Name) sheet;
        (byte[]? Bytes, string? Name) pal;

        // The first candidate name this copy of the game ships, kept alongside its bytes: which one that was is the
        // only record of where the art actually came from, and nothing downstream can work it out again.
        (byte[]? Bytes, string? Name) ReadFirst(params string[] names)
        {
            foreach (var name in names)
                if (read(name) is { } bytes) return (bytes, name);
            return (null, null);
        }

        if (set.SheetLetter is char letter)
        {
            // Player costume sheet (CharacterAnimSet.SheetLetter): filename/palette come from
            // PlayerCostumes by digit, not the enemy branch's {grid.Sheet}{suffix} + SuffixPal lookup.
            var costume = PlayerCostumes.All.First(c => c.Code == suffix[0]);
            sheet = ReadFirst($"DUG{costume.Code}{letter}.SPF");
            pal = ReadFirst(costume.Palette);
        }
        else if (set.FixedSheet is not null)
        {
            sheet = ReadFirst($"{set.FixedSheet}.SPF", $"{set.FixedSheet}.MPF");
            pal = set.UseEmbeddedPalette
                ? (null, null)
                : ReadFirst(set.FixedPalette ?? SkinCatalog.SuffixPal[""]);
        }
        else if (set.WorldSheet is { } worldSheet)
        {
            // World-suffixed sheet with no EnemyGrids entry (CharacterAnimSet.WorldSheet): the filename
            // comes from the set's own format string, the palette from the usual per-suffix lookup.
            string stem = string.Format(worldSheet.SheetFormat, suffix);
            sheet = ReadFirst($"{stem}.SPF", $"{stem}.MPF");
            pal = ReadFirst(set.FixedPalette ?? SkinCatalog.SuffixPal[suffix]);
        }
        else if (set.Category is not byte cat || !EnemyGrids.TryGet(cat, out var catalogedGrid))
        {
            failure = AnimationSheetFailure.NoGrid;
            return null;
        }
        else
        {
            enemyGrid = catalogedGrid;
            sheet = ReadFirst($"{catalogedGrid.Sheet}{suffix}.SPF", $"{catalogedGrid.Sheet}{suffix}.MPF");
            pal = ReadFirst(set.FixedPalette ?? SkinCatalog.SuffixPal[suffix]);
        }

        if (sheet.Bytes is null || (!set.UseEmbeddedPalette && (pal.Bytes is null || pal.Bytes.Length < 768)))
        {
            failure = AnimationSheetFailure.MissingFiles;
            return null;
        }

        var sheetImage = SheetImage.Read(sheet.Bytes);
        var pages = sheetImage.Frames;
        var palette = set.UseEmbeddedPalette ? sheetImage.Palette : VgaPalette.From6Bit(pal.Bytes!.AsSpan(0, 768));
        var rects = SkinCatalog.RectsOf(set, pages);
        return new ResolvedAnimationSheet(pages, palette, rects, enemyGrid ?? SkinCatalog.FixedGrid(set, rects),
            sheet.Name!, set.UseEmbeddedPalette ? null : pal.Name);
    }
}
