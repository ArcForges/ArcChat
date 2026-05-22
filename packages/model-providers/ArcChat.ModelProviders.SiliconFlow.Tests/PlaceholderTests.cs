// Copyright (c) ArcForges. Licensed under the MIT License.

using FluentAssertions;
using Xunit;

namespace ArcChat.ModelProviders.SiliconFlow.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void IsAvailableReturnsTrue()
    {
        _ = SiliconFlowMarker.IsAvailable().Should().BeTrue();
    }
}
