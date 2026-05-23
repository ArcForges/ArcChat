// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.UI.Controls.Search;

/// <summary>
/// Lightweight fuzzy-match result used by conversation search.
/// </summary>
public readonly record struct FuzzyMatchResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FuzzyMatchResult"/> struct.
    /// </summary>
    public FuzzyMatchResult(bool isMatch, int score, int start, int length)
    {
        this.IsMatch = isMatch;
        this.Score = score;
        this.Start = start;
        this.Length = length;
    }

    /// <summary>Gets a value indicating whether the candidate matched the query.</summary>
    public bool IsMatch { get; }

    /// <summary>Gets a sort score; higher values are better matches.</summary>
    public int Score { get; }

    /// <summary>Gets the start index of the first matched character.</summary>
    public int Start { get; }

    /// <summary>Gets the length of the candidate span containing the match.</summary>
    public int Length { get; }
}
