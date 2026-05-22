// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ArcChat.Protocol.Serialization;

namespace ArcChat.LocalPersistence.Repositories;

internal static class StorageJson
{
    public static ArcChatProtocolJsonContext Context => ArcChatProtocolJsonContext.Default;

    public static string Serialize<T>(T value, JsonTypeInfo<T> jsonTypeInfo)
    {
        return JsonSerializer.Serialize(value, jsonTypeInfo);
    }

    public static T Deserialize<T>(string json, JsonTypeInfo<T> jsonTypeInfo)
    {
        T? value = JsonSerializer.Deserialize(json, jsonTypeInfo);
        return value ?? throw new InvalidOperationException("Stored JSON value could not be deserialized.");
    }
}
