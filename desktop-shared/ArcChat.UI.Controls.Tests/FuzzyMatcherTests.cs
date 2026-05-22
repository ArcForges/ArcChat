// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.UI.Controls.Search;
using FluentAssertions;
using Xunit;

namespace ArcChat.UI.Controls.Tests;

public sealed class FuzzyMatcherTests
{
    [Fact]
    public static void FuzzyMatcherPrefersExactMatchesAndBuildsSnippet()
    {
        FuzzyMatchResult exact = FuzzyMatcher.Match("alpha beta gamma".AsSpan(), "beta".AsSpan());
        FuzzyMatchResult fuzzy = FuzzyMatcher.Match("alpha beta gamma".AsSpan(), "abg".AsSpan());

        _ = exact.IsMatch.Should().BeTrue();
        _ = fuzzy.IsMatch.Should().BeTrue();
        _ = exact.Score.Should().BeGreaterThan(fuzzy.Score);
        _ = FuzzyMatcher.CreateSnippet("alpha beta gamma", exact, 2).Should().Be("...a beta g...");
    }
}
