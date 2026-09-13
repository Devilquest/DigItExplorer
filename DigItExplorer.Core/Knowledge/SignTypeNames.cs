namespace DigItExplorer.Core.Knowledge;

/// <summary>Display names for world-map signpost types, matching the World Map Layers panel.</summary>
public static class SignTypeNames
{
    /// <summary>The signpost's name as the World Map Layers panel lists it.</summary>
    public static string Label(SignType type) => type switch
    {
        SignType.Level => "Level Complete",
        SignType.Checkpoint => "Check Point",
        SignType.Trace => "Traces of Dugette",
        SignType.Gate => "Connection Cave",
        SignType.Draggo => "Draggo (unused)",
        _ => type.ToString(),
    };
}
