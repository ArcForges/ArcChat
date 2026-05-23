// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Artifacts;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Chat-only provider request mapped from NextChat chat options.
/// </summary>
public sealed record ChatRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatRequest"/> class.
    /// </summary>
    /// <param name="history">Ordered conversation history to send to the provider.</param>
    /// <param name="config">Selected model configuration.</param>
    /// <param name="tools">Tools exposed to the provider for this request.</param>
    /// <param name="extra">Provider-specific metadata and ArcChat stream ids.</param>
    public ChatRequest(
        ImmutableArray<Message> history,
        ModelConfig config,
        ImmutableArray<ArcTool> tools,
        ProviderExtra extra)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(extra);

        this.History = history;
        this.Config = config;
        this.Tools = tools;
        this.Extra = extra;
    }

    /// <summary>
    /// Gets the ordered conversation history to send to the provider.
    /// </summary>
    public ImmutableArray<Message> History { get; init; }

    /// <summary>
    /// Gets the selected model configuration.
    /// </summary>
    public ModelConfig Config { get; init; }

    /// <summary>
    /// Gets the tools exposed to the provider for this request.
    /// </summary>
    public ImmutableArray<ArcTool> Tools { get; init; }

    /// <summary>
    /// Gets provider-specific metadata and ArcChat stream ids.
    /// </summary>
    public ProviderExtra Extra { get; init; }
}
