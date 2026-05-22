// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.UI.Theme.Tokens;
using Avalonia.Media;
using FluentAssertions;
using Xunit;

namespace ArcChat.UI.Theme.Tests;

public sealed class TokenSnapshotTests
{
    [Fact]
    public void StaticColorTokenSnapshotStaysStable()
    {
        string[] tokens =
        [
            Token("LightWhite", ArcChatColors.LightWhite),
            Token("LightBlack", ArcChatColors.LightBlack),
            Token("LightGray", ArcChatColors.LightGray),
            Token("Primary", ArcChatColors.Primary),
            Token("LightSecond", ArcChatColors.LightSecond),
            Token("LightHover", ArcChatColors.LightHover),
            Token("DarkWhite", ArcChatColors.DarkWhite),
            Token("DarkBlack", ArcChatColors.DarkBlack),
            Token("DarkGray", ArcChatColors.DarkGray),
            Token("DarkSecond", ArcChatColors.DarkSecond),
            Token("DarkHover", ArcChatColors.DarkHover),
        ];

        _ = tokens.Should().Equal(
            "LightWhite=#FFFFFF",
            "LightBlack=#303030",
            "LightGray=#FAFAFA",
            "Primary=#1D93AB",
            "LightSecond=#E7F8FF",
            "LightHover=#F3F3F3",
            "DarkWhite=#1E1E1E",
            "DarkBlack=#BBBBBB",
            "DarkGray=#151515",
            "DarkSecond=#1B262A",
            "DarkHover=#323232");
    }

    private static string Token(string name, Color color)
    {
        return $"{name}=#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
