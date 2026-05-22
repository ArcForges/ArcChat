// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.LocalPersistence.Repositories;
using ArcChat.LocalPersistence.Sqlite;

namespace ArcChat.LocalPersistence;

/// <summary>
/// ArcChat local SQLite database composition root.
/// </summary>
public sealed class ArcChatDatabase : IAsyncDisposable
{
    private readonly SqliteConnectionFactory connectionFactory;
    private readonly SqliteWriteQueue writeQueue;
    private readonly DatabaseMigrator migrator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArcChatDatabase"/> class.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    public ArcChatDatabase(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        this.connectionFactory = new SqliteConnectionFactory(databasePath);
        this.writeQueue = new SqliteWriteQueue(this.connectionFactory);
        this.migrator = new DatabaseMigrator(this.connectionFactory);
        this.Conversations = new ConversationRepository(this.connectionFactory, this.writeQueue);
        this.Messages = new MessageRepository(this.connectionFactory, this.writeQueue);
        this.Settings = new SettingsRepository(this.connectionFactory, this.writeQueue);
        this.JsonTables = new LocalJsonTableStore(this.connectionFactory, this.writeQueue);
    }

    /// <summary>
    /// Gets the conversation repository.
    /// </summary>
    public IConversationRepository Conversations { get; }

    /// <summary>
    /// Gets the message repository.
    /// </summary>
    public IMessageRepository Messages { get; }

    /// <summary>
    /// Gets the settings repository.
    /// </summary>
    public ISettingsRepository Settings { get; }

    internal LocalJsonTableStore JsonTables { get; }

    /// <summary>
    /// Runs migrations and enables SQLite pragmas required by ArcChat.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await this.migrator.MigrateAsync(cancellationToken).ConfigureAwait(false);
        await this.connectionFactory.EnableWalAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await this.writeQueue.DisposeAsync().ConfigureAwait(false);
    }
}
