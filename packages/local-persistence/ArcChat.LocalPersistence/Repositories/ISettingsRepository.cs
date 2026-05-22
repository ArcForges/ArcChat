// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Settings;

namespace ArcChat.LocalPersistence.Repositories;

/// <summary>
/// Stores typed settings and opaque key-value settings.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Inserts or updates the typed settings snapshot.
    /// </summary>
    Task UpsertSnapshotAsync(SettingsSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the typed settings snapshot.
    /// </summary>
    Task<SettingsSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates an opaque settings value.
    /// </summary>
    Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an opaque settings value.
    /// </summary>
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);
}
