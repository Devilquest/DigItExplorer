using DigItExplorer.Core.Formats;
using DigItExplorer.Core.Knowledge;

namespace DigItExplorer.Core.Maps;

/// <summary>Resolves moving-platform and drain collider footprints and applies them to collision planes.</summary>
internal static class ColliderStamper
{
    private const byte MvlSeparator = 255;

    /// <summary>Resolves collider footprint stamps for moving platform entity records.</summary>
    /// <param name="records">Level DLF entity records.</param>
    /// <param name="woMvl">Raw bytes of the world moving platform sheet.</param>
    /// <returns>List of resolved collider stamps.</returns>
    public static IReadOnlyList<ColliderStamp> BuildMovingPlatformStamps(IReadOnlyList<DlfRecord> records, byte[] woMvl)
    {
        if (!records.Any(r => r.Category == 0x04)) return [];

        var frame = SheetImage.Read(woMvl).Frames[0];
        var cells = SpriteSheetSlicer.Slice(frame, MvlSeparator);
        var colliders = cells.Select(SpriteSheetSlicer.BottomHalf).ToArray();

        var stamps = new List<ColliderStamp>();
        foreach (var rec in records)
        {
            if (rec.Category != 0x04 || rec.Type >= colliders.Length) continue;
            stamps.Add(new ColliderStamp(EntityCategories.KeyOf(rec), rec.X, rec.Y, colliders[rec.Type],
                Mirrored: false));
        }
        return stamps;
    }

    // Collider template rectangle on WO_DRAIN (top-left cell of 2x2 grid).
    private static readonly (int X1, int Y1, int X2, int Y2) DrainColliderRect = (1, 1, 89, 50);

    // Border marker code on WO_DRAIN collider template, treated as transparent pass-through.
    private const byte DrainSpriteBorder = 255;

    /// <summary>Resolves collider footprint stamps for drain entity records.</summary>
    /// <param name="records">Level DLF entity records.</param>
    /// <param name="woDrain">Raw bytes of the WO_DRAIN sheet.</param>
    /// <returns>List of resolved collider stamps with transparent borders.</returns>
    public static IReadOnlyList<ColliderStamp> BuildDrainStamps(IReadOnlyList<DlfRecord> records, byte[] woDrain)
        => DrainStamps(records, woDrain, borderIsTransparent: true);

    private static IReadOnlyList<ColliderStamp> DrainStamps(IReadOnlyList<DlfRecord> records, byte[] woDrain,
        bool borderIsTransparent)
    {
        var drainRecords = records.Where(r => r.Category == 0x5B).ToList();
        if (drainRecords.Count == 0) return [];

        var frame = SheetImage.Read(woDrain).Frames[0];
        var (x1, y1, x2, y2) = DrainColliderRect;
        var template = SpriteSheetSlicer.RectCell(frame, x1, y1, x2, y2);
        var pixels = new byte[template.Pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            byte code = template.Pixels[i];
            bool keep = code == 64 || (borderIsTransparent && code == DrainSpriteBorder);
            pixels[i] = keep ? code : (byte)15;
        }
        var collider = template with { Pixels = pixels };
        byte? transparent = borderIsTransparent ? DrainSpriteBorder : null;

        return [.. drainRecords.Select(rec => new ColliderStamp(EntityCategories.KeyOf(rec), rec.X, rec.Y,
            collider, rec.P0 == 0xFFFF, transparent))];
    }

    /// <summary>Applies a sequence of collider stamps onto a raw material code buffer in place.</summary>
    public static void Apply(byte[] codes, int width, int height, IEnumerable<ColliderStamp> stamps)
    {
        foreach (var stamp in stamps)
            StampCell(codes, width, height, stamp);
    }

    /// <summary>Resolves and applies moving-platform footprints onto a CollisionImage in place.</summary>
    public static void StampMovingPlatforms(CollisionImage collision, IReadOnlyList<DlfRecord> records, byte[] woMvl)
        => Apply(collision.MaterialCodes, collision.Width, collision.Height,
            BuildMovingPlatformStamps(records, woMvl));

    /// <summary>Resolves and applies drain footprints onto a CollisionImage in place.</summary>
    public static void StampDrains(CollisionImage collision, IReadOnlyList<DlfRecord> records, byte[] woDrain)
        => Apply(collision.MaterialCodes, collision.Width, collision.Height,
            DrainStamps(records, woDrain, borderIsTransparent: false));

    /// <summary>Overlays a single collider cell onto the material codes buffer with clipping and mirroring.</summary>
    private static void StampCell(byte[] codes, int canvasW, int canvasH, ColliderStamp stamp)
    {
        foreach (var (dst, code) in new ClippedBlit(stamp.Cell, stamp.X, stamp.Y, canvasW, canvasH, stamp.Mirrored))
        {
            if (code == stamp.TransparentCode) continue;
            codes[dst] = code;
        }
    }
}
