// Copyright (c) ArcForges. Licensed under the MIT License.

using FluentAssertions;
using Xunit;

namespace ArcChat.UI.ArtifactViewer.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void IsAvailableReturnsTrue()
    {
        _ = ArtifactViewerMarker.IsAvailable().Should().BeTrue();
    }
}
