// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace ArcChat.Desktop.Features.Conversations;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(ShareGptRequest))]
[JsonSerializable(typeof(ShareGptResponse))]
internal partial class ConversationExportJsonContext : JsonSerializerContext
{
}
