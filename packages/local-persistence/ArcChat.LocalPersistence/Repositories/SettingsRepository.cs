// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.LocalPersistence.Sqlite;
using ArcChat.Protocol.Settings;

namespace ArcChat.LocalPersistence.Repositories;

internal sealed class SettingsRepository : ISettingsRepository
{
    private const string SnapshotKey = "settings.snapshot";
    private readonly LocalJsonTableStore tableStore;

    public SettingsRepository(SqliteConnectionFactory connectionFactory, SqliteWriteQueue writeQueue)
    {
        this.tableStore = new LocalJsonTableStore(connectionFactory, writeQueue);
    }

    public async Task UpsertSnapshotAsync(SettingsSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        string json = StorageJson.Serialize(snapshot, StorageJson.Context.SettingsSnapshot);
        await this.tableStore.UpsertAsync(PersistenceTable.Setting, SnapshotKey, json, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SettingsSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        string? json = await this.tableStore.GetAsync(PersistenceTable.Setting, SnapshotKey, cancellationToken).ConfigureAwait(false);
        return json is null ? null : StorageJson.Deserialize(json, StorageJson.Context.SettingsSnapshot);
    }

    public async Task UpsertValueAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        await this.tableStore.UpsertAsync(PersistenceTable.Setting, key, value, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return await this.tableStore.GetAsync(PersistenceTable.Setting, key, cancellationToken).ConfigureAwait(false);
    }
}
