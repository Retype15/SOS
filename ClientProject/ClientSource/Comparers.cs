// Copyright (c) 2026 Retype15
// This file is licensed under the GNU GPLv3.
// See the LICENSE file in the project root for details.

#pragma warning disable IDE0130
#pragma warning disable IDE0290

using System.Diagnostics.CodeAnalysis;

namespace SOS.Comparers
{
    public sealed class NaturalStringComparer : IComparer<string?>, IEqualityComparer<string?>
    {
        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            ReadOnlySpan<char> spanX = x.AsSpan();
            ReadOnlySpan<char> spanY = y.AsSpan();

            int ix = 0, iy = 0;

            while (ix < spanX.Length && iy < spanY.Length)
            {
                char cx = spanX[ix];
                char cy = spanY[iy];

                if (char.IsDigit(cx) && char.IsDigit(cy))
                {
                    int startX = ix;
                    while (ix < spanX.Length && char.IsDigit(spanX[ix])) ix++;

                    int startY = iy;
                    while (iy < spanY.Length && char.IsDigit(spanY[iy])) iy++;

                    ReadOnlySpan<char> numSpanX = spanX[startX..ix];
                    ReadOnlySpan<char> numSpanY = spanY[startY..iy];

                    ReadOnlySpan<char> trimmedX = numSpanX.TrimStart('0');
                    ReadOnlySpan<char> trimmedY = numSpanY.TrimStart('0');

                    if (trimmedX.Length != trimmedY.Length)
                        return trimmedX.Length.CompareTo(trimmedY.Length);

                    int numCompare = trimmedX.SequenceCompareTo(trimmedY);
                    if (numCompare != 0)
                        return numCompare;

                    if (numSpanX.Length != numSpanY.Length)
                        return numSpanX.Length.CompareTo(numSpanY.Length);
                }
                else
                {
                    int charCompare = char.ToLowerInvariant(cx).CompareTo(char.ToLowerInvariant(cy));
                    if (charCompare != 0)
                        return charCompare;

                    ix++;
                    iy++;
                }
            }

            return spanX.Length.CompareTo(spanY.Length);
        }

        public bool Equals(string? x, string? y) => Compare(x, y) == 0;

        public int GetHashCode([DisallowNull] string? obj)
        {
            if (obj is null) return 0;
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
        }
    }
}