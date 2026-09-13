using System;
using System.Collections.Generic;

namespace DigItExplorer.Core.Catalog;

/// <summary>Alphanumeric natural sort comparer that orders strings containing embedded numbers logically.</summary>
public sealed class NaturalStringComparer : IComparer<string>
{
    /// <summary>The shared instance.</summary>
    public static readonly NaturalStringComparer Instance = new();

    /// <summary>Compares two strings, treating embedded digit runs as numbers.</summary>
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int ix = 0;
        int iy = 0;

        while (ix < x.Length && iy < y.Length)
        {
            char cx = x[ix];
            char cy = y[iy];

            if (char.IsDigit(cx) && char.IsDigit(cy))
            {
                int startX = ix;
                while (ix < x.Length && char.IsDigit(x[ix]))
                {
                    ix++;
                }

                int startY = iy;
                while (iy < y.Length && char.IsDigit(y[iy]))
                {
                    iy++;
                }

                ReadOnlySpan<char> spanX = x.AsSpan(startX, ix - startX);
                ReadOnlySpan<char> spanY = y.AsSpan(startY, iy - startY);

                // Trimmed so "007" and "7" compare as the same numeric value.
                spanX = TrimLeadingZeros(spanX);
                spanY = TrimLeadingZeros(spanY);

                if (spanX.Length != spanY.Length)
                {
                    return spanX.Length.CompareTo(spanY.Length);
                }

                for (int i = 0; i < spanX.Length; i++)
                {
                    if (spanX[i] != spanY[i])
                    {
                        return spanX[i].CompareTo(spanY[i]);
                    }
                }

                // "007" and "7" are numerically equal, so the digits as written settle their order.
                int origLenX = ix - startX;
                int origLenY = iy - startY;
                if (origLenX != origLenY)
                {
                    return origLenX.CompareTo(origLenY);
                }
            }
            else
            {
                char cxLower = char.ToLowerInvariant(cx);
                char cyLower = char.ToLowerInvariant(cy);

                if (cxLower != cyLower)
                {
                    return cxLower.CompareTo(cyLower);
                }

                ix++;
                iy++;
            }
        }

        return x.Length.CompareTo(y.Length);
    }

    private static ReadOnlySpan<char> TrimLeadingZeros(ReadOnlySpan<char> span)
    {
        int i = 0;
        while (i < span.Length - 1 && span[i] == '0')
        {
            i++;
        }
        return span[i..];
    }
}
