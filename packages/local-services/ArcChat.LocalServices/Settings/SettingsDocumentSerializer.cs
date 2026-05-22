// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json;
using ArcChat.Protocol.Serialization;
using ArcChat.Protocol.Settings;

namespace ArcChat.LocalServices.Settings;

/// <summary>
/// Imports and exports complete settings snapshots as deterministic JSON.
/// </summary>
public static class SettingsDocumentSerializer
{
    /// <summary>
    /// Serializes a settings snapshot.
    /// </summary>
    public static string Export(SettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, ArcChatProtocolJsonContext.Default.SettingsSnapshot);
    }

    /// <summary>
    /// Deserializes a settings snapshot.
    /// </summary>
    public static SettingsSnapshot Import(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        SettingsSnapshot? snapshot = JsonSerializer.Deserialize(json, ArcChatProtocolJsonContext.Default.SettingsSnapshot);
        return snapshot ?? throw new InvalidOperationException("Settings JSON did not contain a settings snapshot.");
    }
}
