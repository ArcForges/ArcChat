// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Threading.Channels;
using Microsoft.Data.Sqlite;

namespace ArcChat.LocalPersistence.Sqlite;

internal sealed class SqliteWriteQueue : IAsyncDisposable
{
    private readonly SqliteConnectionFactory connectionFactory;
    private readonly Channel<WriteWork> channel = Channel.CreateUnbounded<WriteWork>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
    private readonly Task pump;

    public SqliteWriteQueue(SqliteConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
        this.pump = Task.Run(this.RunAsync);
    }

    public async Task EnqueueAsync(
        Func<SqliteConnection, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        WriteWork work = new WriteWork(action, cancellationToken);
        await this.channel.Writer.WriteAsync(work, cancellationToken).ConfigureAwait(false);
        await work.Completion.Task.ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        _ = this.channel.Writer.TryComplete();
        await this.shutdown.CancelAsync().ConfigureAwait(false);
        try
        {
            await this.pump.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        this.shutdown.Dispose();
    }

    private async Task RunAsync()
    {
        await foreach (WriteWork work in this.channel.Reader.ReadAllAsync(this.shutdown.Token).ConfigureAwait(false))
        {
            try
            {
                work.CancellationToken.ThrowIfCancellationRequested();
                await using SqliteConnection connection = await this.connectionFactory.OpenConnectionAsync(work.CancellationToken).ConfigureAwait(false);
                await work.Action(connection, work.CancellationToken).ConfigureAwait(false);
                _ = work.Completion.TrySetResult();
            }
            catch (OperationCanceledException exception)
            {
                _ = work.Completion.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                _ = work.Completion.TrySetException(exception);
            }
        }
    }

    private sealed class WriteWork
    {
        public WriteWork(Func<SqliteConnection, CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            this.Action = action;
            this.CancellationToken = cancellationToken;
        }

        public Func<SqliteConnection, CancellationToken, Task> Action { get; }

        public CancellationToken CancellationToken { get; }

        public TaskCompletionSource Completion { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
