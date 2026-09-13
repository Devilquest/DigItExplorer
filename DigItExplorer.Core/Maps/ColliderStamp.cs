using DigItExplorer.Core.Formats;

namespace DigItExplorer.Core.Maps;

/// <summary>Resolved collider footprint cell and layer metadata ready to stamp onto a collision plane.</summary>
/// <param name="LayerKey">Entity category and optional type key associated with this collider.</param>
/// <param name="X">Horizontal canvas anchor coordinate.</param>
/// <param name="Y">Vertical canvas anchor coordinate.</param>
/// <param name="Cell">Sprite cell containing material code bytes.</param>
/// <param name="Mirrored">Flag indicating whether cell is horizontally flipped.</param>
/// <param name="TransparentCode">Optional material code treated as transparent pass-through.</param>
internal readonly record struct ColliderStamp((byte Category, ushort? Type) LayerKey, int X, int Y,
    SpriteCell Cell, bool Mirrored, byte? TransparentCode = null);
