// Copyright (c) ArcForges. Licensed under the MIT License.

using Dapper;
using Microsoft.Data.Sqlite;

namespace ArcChat.LocalPersistence.Sqlite;

internal sealed class SqliteConnectionFactory
{
    private readonly string connectionString;

    public SqliteConnectionFactory(string databasePath)
    {
        SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        };
        this.connectionString = builder.ToString();
    }

    public string ConnectionString => this.connectionString;

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        SqliteConnection connection = new SqliteConnection(this.connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        _ = await connection.ExecuteAsync(
            new CommandDefinition(
                "PRAGMA foreign_keys=ON;",
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return connection;
    }

    public async Task EnableWalAsync(CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await this.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        _ = await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(
                "PRAGMA journal_mode=WAL;",
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
