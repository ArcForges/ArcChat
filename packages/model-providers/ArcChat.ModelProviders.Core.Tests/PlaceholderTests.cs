// Copyright (c) ArcForges. Licensed under the MIT License.

using FluentAssertions;
using Xunit;

namespace ArcChat.ModelProviders.Core.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void IsAvailableReturnsTrue()
    {
        _ = CoreMarker.IsAvailable().Should().BeTrue();
    }
}
