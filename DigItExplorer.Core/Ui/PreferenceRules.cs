namespace DigItExplorer.Core.Ui;

/// <summary>Validation and clamping rules for persisted user preferences.</summary>
public static class PreferenceRules
{
    /// <summary>Clamps a volume level value to the valid range [0.0, 1.0].</summary>
    public static double ClampVolume(double volume) => double.IsFinite(volume) ? Math.Clamp(volume, 0.0, 1.0) : 1.0;

    /// <summary>Clamps last audible unmute volume level to the range (0.0, 1.0].</summary>
    public static double ClampLastAudibleVolume(double volume)
        => double.IsFinite(volume) && volume > 0 ? Math.Min(volume, 1.0) : 1.0;

    /// <summary>Validates export magnification scale factor (accepts 1, 2, or 4; defaults to 1).</summary>
    public static int ClampExportScale(int scale) => scale is 1 or 2 or 4 ? scale : 1;

    /// <summary>Parses a string into an enum value with fallback on failure.</summary>
    /// <typeparam name="TEnum">Target enum type.</typeparam>
    /// <param name="raw">Raw string name.</param>
    /// <param name="fallback">Default fallback value.</param>
    /// <returns>Parsed enum value or fallback.</returns>
    public static TEnum ParseEnumOrDefault<TEnum>(string? raw, TEnum fallback) where TEnum : struct, Enum
        => Enum.TryParse(raw, ignoreCase: true, out TEnum value) && Enum.IsDefined(value) ? value : fallback;
}
