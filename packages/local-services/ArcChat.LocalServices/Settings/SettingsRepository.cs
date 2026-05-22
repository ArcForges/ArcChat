// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Settings;
using PersistenceSettingsRepository = ArcChat.LocalPersistence.Repositories.ISettingsRepository;

namespace ArcChat.LocalServices.Settings;

/// <summary>
/// Observable settings repository backed by local persistence.
/// </summary>
public sealed class SettingsRepository : ISettingsRepository
{
    private readonly PersistenceSettingsRepository persistenceRepository;
    private readonly List<Action<SettingsSnapshot>> observers = new List<Action<SettingsSnapshot>>();
    private SettingsSnapshot currentSnapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepository"/> class.
    /// </summary>
    public SettingsRepository(PersistenceSettingsRepository persistenceRepository)
    {
        ArgumentNullException.ThrowIfNull(persistenceRepository);
        this.persistenceRepository = persistenceRepository;
        this.currentSnapshot = SettingsDefaults.Create();
    }

    /// <inheritdoc />
    public async Task<SettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        SettingsSnapshot? snapshot = await this.persistenceRepository.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        this.currentSnapshot = snapshot ?? SettingsDefaults.Create();
        this.Publish();
        return this.currentSnapshot;
    }

    /// <inheritdoc />
    public async Task SaveAsync(SettingsSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        await this.persistenceRepository.UpsertSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
        this.currentSnapshot = snapshot;
        this.Publish();
    }

    /// <inheritdoc />
    public IObservable<T> Observe<T>(KeyExpression<T> keyExpression)
    {
        ArgumentNullException.ThrowIfNull(keyExpression);
        return new SettingsObservable<T>(this, keyExpression);
    }

    private Subscription Subscribe<T>(KeyExpression<T> keyExpression, IObserver<T> observer)
    {
        Action<SettingsSnapshot> settingObserver = snapshot => observer.OnNext(keyExpression.Evaluate(snapshot));
        this.observers.Add(settingObserver);
        settingObserver(this.currentSnapshot);
        return new Subscription(this.observers, settingObserver);
    }

    private void Publish()
    {
        foreach (Action<SettingsSnapshot> observer in this.observers.ToArray())
        {
            observer(this.currentSnapshot);
        }
    }

    private sealed class SettingsObservable<T> : IObservable<T>
    {
        private readonly SettingsRepository owner;
        private readonly KeyExpression<T> keyExpression;

        public SettingsObservable(SettingsRepository owner, KeyExpression<T> keyExpression)
        {
            this.owner = owner;
            this.keyExpression = keyExpression;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            return this.owner.Subscribe(this.keyExpression, observer);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly List<Action<SettingsSnapshot>> observers;
        private readonly Action<SettingsSnapshot> observer;

        public Subscription(List<Action<SettingsSnapshot>> observers, Action<SettingsSnapshot> observer)
        {
            this.observers = observers;
            this.observer = observer;
        }

        public void Dispose()
        {
            _ = this.observers.Remove(this.observer);
        }
    }
}
