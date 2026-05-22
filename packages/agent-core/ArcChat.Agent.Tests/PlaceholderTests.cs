// Copyright (c) ArcForges. Licensed under the MIT License.

using FluentAssertions;
using Xunit;

namespace ArcChat.Agent.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void IsAvailableReturnsTrue()
    {
        _ = AgentMarker.IsAvailable().Should().BeTrue();
    }
}
