// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.UI.Theme.Tokens;
using FluentAssertions;
using Xunit;

namespace ArcChat.UI.Theme.Tests;

public sealed class ColorContrastTests
{
    [Fact]
    public void BodyTextMeetsWcagAaOnActiveThemeBackgrounds()
    {
        _ = ColorContrastCalculator.ContrastRatio(ArcChatColors.LightBlack, ArcChatColors.LightGray)
            .Should().BeGreaterThanOrEqualTo(ColorContrastCalculator.NormalTextAaRatio);
        _ = ColorContrastCalculator.ContrastRatio(ArcChatColors.DarkBlack, ArcChatColors.DarkGray)
            .Should().BeGreaterThanOrEqualTo(ColorContrastCalculator.NormalTextAaRatio);
        _ = ColorContrastCalculator.ContrastRatio(ArcChatColors.LightBlack, ArcChatColors.LightGray)
            .Should().BeGreaterThanOrEqualTo(
                ColorContrastCalculator.NormalTextAaRatio,
                "the system/default theme starts from the light NextChat token pair");
    }
}
