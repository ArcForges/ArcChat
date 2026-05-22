// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json;
using ArcChat.UI.Theme.Tokens;
using Avalonia.Media;
using FluentAssertions;
using Xunit;

namespace ArcChat.UI.Theme.Tests;

public sealed class ColorParityTests
{
    [Fact]
    public void ColorTokensMatchNextChatManifestWithinOneSrgbUnit()
    {
        Dictionary<string, Dictionary<string, string>> manifest = ReadManifest();

        AssertNear(manifest["light"]["white"], ArcChatColors.LightWhite);
        AssertNear(manifest["light"]["black"], ArcChatColors.LightBlack);
        AssertNear(manifest["light"]["gray"], ArcChatColors.LightGray);
        AssertNear(manifest["light"]["primary"], ArcChatColors.Primary);
        AssertNear(manifest["light"]["second"], ArcChatColors.LightSecond);
        AssertNear(manifest["light"]["hover-color"], ArcChatColors.LightHover);

        AssertNear(manifest["dark"]["white"], ArcChatColors.DarkWhite);
        AssertNear(manifest["dark"]["black"], ArcChatColors.DarkBlack);
        AssertNear(manifest["dark"]["gray"], ArcChatColors.DarkGray);
        AssertNear(manifest["dark"]["primary"], ArcChatColors.Primary);
        AssertNear(manifest["dark"]["second"], ArcChatColors.DarkSecond);
        AssertNear(manifest["dark"]["hover-color"], ArcChatColors.DarkHover);
    }

    private static Dictionary<string, Dictionary<string, string>> ReadManifest()
    {
        string path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "ArcChat.UI.Theme",
            "Tokens",
            "nextchat-colors.json"));
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
            ?? throw new InvalidOperationException("Could not deserialize NextChat color manifest.");
    }

    private static void AssertNear(string expectedHex, Color actual)
    {
        Color expected = Color.Parse(expectedHex);
        _ = Math.Abs(expected.R - actual.R).Should().BeLessThanOrEqualTo(1);
        _ = Math.Abs(expected.G - actual.G).Should().BeLessThanOrEqualTo(1);
        _ = Math.Abs(expected.B - actual.B).Should().BeLessThanOrEqualTo(1);
    }
}
