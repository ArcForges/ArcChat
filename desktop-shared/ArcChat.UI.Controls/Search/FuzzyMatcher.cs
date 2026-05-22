// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.UI.Controls.Search;

/// <summary>
/// Allocation-light fuzzy matcher for global chat search.
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// Finds an ordinal-ignore-case exact or subsequence match.
    /// </summary>
    /// <param name="candidate">Candidate text to search.</param>
    /// <param name="query">User search text.</param>
    /// <returns>Match metadata suitable for sorting and snippet creation.</returns>
    public static FuzzyMatchResult Match(ReadOnlySpan<char> candidate, ReadOnlySpan<char> query)
    {
        query = Trim(query);
        if (query.IsEmpty)
        {
            return new FuzzyMatchResult(false, 0, 0, 0);
        }

        int exactIndex = IndexOf(candidate, query);
        if (exactIndex >= 0)
        {
            return new FuzzyMatchResult(true, 10_000 - exactIndex, exactIndex, query.Length);
        }

        int queryIndex = 0;
        int start = -1;
        int last = -1;
        int gaps = 0;
        for (int candidateIndex = 0; candidateIndex < candidate.Length && queryIndex < query.Length; candidateIndex++)
        {
            if (!EqualsIgnoreCase(candidate[candidateIndex], query[queryIndex]))
            {
                continue;
            }

            if (start < 0)
            {
                start = candidateIndex;
            }
            else if (last >= 0)
            {
                gaps += candidateIndex - last - 1;
            }

            last = candidateIndex;
            queryIndex++;
        }

        if (queryIndex != query.Length || start < 0 || last < start)
        {
            return new FuzzyMatchResult(false, 0, 0, 0);
        }

        int spanLength = last - start + 1;
        int score = 5_000 - gaps - start;
        return new FuzzyMatchResult(true, score, start, spanLength);
    }

    /// <summary>
    /// Creates a compact snippet around a match.
    /// </summary>
    public static string CreateSnippet(string candidate, FuzzyMatchResult match, int contextCharacters = 35)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (!match.IsMatch)
        {
            int maxSnippetLength = contextCharacters * 2;
            return candidate.Length <= contextCharacters * 2
                ? candidate
                : candidate[..maxSnippetLength].Trim() + "...";
        }

        int start = Math.Max(0, match.Start - contextCharacters);
        int end = Math.Min(candidate.Length, match.Start + match.Length + contextCharacters);
        string prefix = start > 0 ? "..." : string.Empty;
        string suffix = end < candidate.Length ? "..." : string.Empty;
        return prefix + candidate[start..end].Trim() + suffix;
    }

    private static ReadOnlySpan<char> Trim(ReadOnlySpan<char> value)
    {
        int start = 0;
        int end = value.Length - 1;
        while (start <= end && char.IsWhiteSpace(value[start]))
        {
            start++;
        }

        while (end >= start && char.IsWhiteSpace(value[end]))
        {
            end--;
        }

        int valueEnd = end + 1;
        return start > end ? ReadOnlySpan<char>.Empty : value[start..valueEnd];
    }

    private static int IndexOf(ReadOnlySpan<char> candidate, ReadOnlySpan<char> query)
    {
        if (query.Length > candidate.Length)
        {
            return -1;
        }

        int limit = candidate.Length - query.Length;
        for (int index = 0; index <= limit; index++)
        {
            int queryEnd = index + query.Length;
            if (candidate[index..queryEnd].Equals(query, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool EqualsIgnoreCase(char left, char right)
    {
        return char.ToUpperInvariant(left) == char.ToUpperInvariant(right);
    }
}
