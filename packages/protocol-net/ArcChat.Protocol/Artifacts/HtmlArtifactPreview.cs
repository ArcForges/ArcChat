// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Protocol.Artifacts;

/// <summary>
/// NextChat HTML artifact preview from app/components/artifacts.tsx.
/// </summary>
public sealed record HtmlArtifactPreview(
    string Id,
    string SourceConversationId,
    string SourceMessageId,
    string Html,
    string ContentHash,
    DateTimeOffset CreatedAt);
