// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Reflection;
using DbUp;

namespace ArcChat.LocalPersistence.Sqlite;

internal sealed class DatabaseMigrator
{
    private readonly SqliteConnectionFactory connectionFactory;

    public DatabaseMigrator(SqliteConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public Task MigrateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureDatabaseDirectoryExists(this.connectionFactory.ConnectionString);
        DbUp.Engine.DatabaseUpgradeResult result = DeployChanges.To
            .SQLiteDatabase(this.connectionFactory.ConnectionString)
            .WithScriptsEmbeddedInAssembly(
                Assembly.GetExecutingAssembly(),
                scriptName => scriptName.Contains(".Migrations.", StringComparison.Ordinal))
            .LogToNowhere()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException("SQLite migration failed.", result.Error);
        }

        return Task.CompletedTask;
    }

    private static void EnsureDatabaseDirectoryExists(string connectionString)
    {
        Microsoft.Data.Sqlite.SqliteConnectionStringBuilder builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        string? directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }
    }
}
