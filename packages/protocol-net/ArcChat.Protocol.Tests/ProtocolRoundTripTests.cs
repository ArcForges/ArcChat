// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ArcChat.Protocol.Artifacts;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;
using ArcChat.Protocol.Mcp;
using ArcChat.Protocol.Plugins;
using ArcChat.Protocol.Providers;
using ArcChat.Protocol.Serialization;
using ArcChat.Protocol.Settings;
using FluentAssertions;
using Xunit;

namespace ArcChat.Protocol.Tests;

public sealed class ProtocolRoundTripTests
{
    private static readonly string[] DeferredArtifactTypeNames =
    [
        "Artifact",
        "ArtifactVersion",
        "ArtifactKind",
        "ArtifactBlock",
    ];

    [Fact]
    public void ConversationFixtureRoundTripsWithNextChatFields()
    {
        Conversation conversation = ReadFixture(
            "conversation.nextchat.json",
            ArcChatProtocolJsonContext.Default.Conversation);

        _ = conversation.Id.Should().Be("session-1");
        _ = conversation.Messages.Should().HaveCount(2);
        _ = conversation.Mask.ModelConfig.HistoryMessageCount.Should().Be(4);
        _ = conversation.Messages[1].AudioUrl.Should().Be("local://audio/assistant.wav");

        string json = JsonSerializer.Serialize(conversation, ArcChatProtocolJsonContext.Default.Conversation);
        Conversation? roundTrip = JsonSerializer.Deserialize(json, ArcChatProtocolJsonContext.Default.Conversation);

        _ = roundTrip.Should().BeEquivalentTo(conversation);
    }

    [Fact]
    public void MaskPluginMcpAndArtifactDtosRoundTrip()
    {
        Mask mask = ReadFixture("mask.nextchat.json", ArcChatProtocolJsonContext.Default.Mask);
        Plugin plugin = ReadFixture("plugin.nextchat.json", ArcChatProtocolJsonContext.Default.Plugin);
        McpConfigData mcp = ReadFixture("mcp-config.nextchat.json", ArcChatProtocolJsonContext.Default.McpConfigData);
        HtmlArtifactPreview artifact = ReadFixture("html-artifact.nextchat.json", ArcChatProtocolJsonContext.Default.HtmlArtifactPreview);
        ArcTool tool = ReadFixture("arc-tool.openapi.json", ArcChatProtocolJsonContext.Default.ArcTool);

        _ = mask.Builtin.Should().BeTrue();
        _ = plugin.AuthTokenRef.Should().Be("keychain://plugin/weather");
        _ = mcp.McpServers.Should().ContainKey("filesystem");
        _ = artifact.ContentHash.Should().Be("md5:4c4ad5");
        _ = tool.Permission.Should().Be(ToolPermissionKind.ConfirmEachCall);
    }

    [Fact]
    public void ProviderSettingsAndSyncDtosPreserveOpaqueExtraFields()
    {
        SettingsSnapshot settings = ReadFixture("settings-snapshot.nextchat.json", ArcChatProtocolJsonContext.Default.SettingsSnapshot);
        SyncSnapshot sync = ReadFixture("sync-snapshot.nextchat.json", ArcChatProtocolJsonContext.Default.SyncSnapshot);
        ProviderConfig provider = settings.Providers.ProviderConfigs.Single();

        _ = provider.Extra.Should().ContainKey("googleSafetySettings");
        _ = settings.ModelConfig.Extra.Should().ContainKey("providerSpecific");
        _ = sync.Stores.Should().ContainKeys("Chat", "Config", "Access", "Mask", "Prompt");
    }

    [Fact]
    public void MvpProtocolDoesNotExposeGenericArtifactVersionOrDiffTypes()
    {
        Type[] protocolTypes = typeof(HtmlArtifactPreview).Assembly.GetExportedTypes();

        _ = protocolTypes.Select(type => type.Name).Should().NotContain(
            DeferredArtifactTypeNames,
            "NC02 keeps generic artifact versioning out of the MVP protocol");
    }

    private static T ReadFixture<T>(string name, JsonTypeInfo<T> typeInfo)
    {
        string path = GetFixturePath(name);
        using FileStream stream = File.OpenRead(path);
        T? value = JsonSerializer.Deserialize(stream, typeInfo);
        _ = value.Should().NotBeNull();
        return value!;
    }

    private static string GetFixturePath(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (Path.IsPathRooted(name) || Path.IsPathFullyQualified(name))
        {
            throw new ArgumentException("Fixture name must be relative.", nameof(name));
        }

        string fixturesDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Resources", "Fixtures"));
        string fixturesRoot = Path.EndsInDirectorySeparator(fixturesDirectory)
            ? fixturesDirectory
            : fixturesDirectory + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(name, fixturesRoot);

        if (!path.StartsWith(fixturesRoot, StringComparison.Ordinal))
        {
            throw new ArgumentException("Fixture path must stay under the fixture directory.", nameof(name));
        }

        return path;
    }
}
