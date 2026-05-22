// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Net.Factory;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ArcChat.Net.Tests;

public sealed class NetCoreFactoryTests
{
    [Fact]
    public async Task FactoryReturnsCachedNamedClientAndDisposesIt()
    {
        ServiceProvider services = new ServiceCollection()
            .AddArcChatNetCore()
            .BuildServiceProvider();

        INetCoreFactory factory = services.GetRequiredService<INetCoreFactory>();
        HttpClient first = factory.GetClient(NetClientProfileNames.Default);
        HttpClient second = factory.GetClient(NetClientProfileNames.Default);

        _ = ReferenceEquals(first, second).Should().BeTrue();
        _ = factory.GetProfile(NetClientProfileNames.Streaming).CompletionOption.Should().Be(HttpCompletionOption.ResponseHeadersRead);

        await factory.DisposeAsync();
        Action useDisposed = () => first.CancelPendingRequests();
        _ = useDisposed.Should().Throw<ObjectDisposedException>();

        await services.DisposeAsync();
    }

    [Fact]
    public void ConcurrentGetsReturnOneCachedClient()
    {
        ServiceProvider services = new ServiceCollection()
            .AddArcChatNetCore()
            .BuildServiceProvider();

        INetCoreFactory factory = services.GetRequiredService<INetCoreFactory>();
        HttpClient[] clients = Enumerable.Range(0, 16)
            .AsParallel()
            .Select(_ => factory.GetClient(NetClientProfileNames.SigningTencent))
            .ToArray();

        _ = clients.Distinct(ReferenceEqualityComparer.Instance).Should().HaveCount(1);
    }
}
