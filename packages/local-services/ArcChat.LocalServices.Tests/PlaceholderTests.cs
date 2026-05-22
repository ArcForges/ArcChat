// Copyright (c) ArcForges. Licensed under the MIT License.

using FluentAssertions;
using Xunit;

namespace ArcChat.LocalServices.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void IsAvailableReturnsTrue()
    {
        _ = LocalServicesMarker.IsAvailable().Should().BeTrue();
    }
}
