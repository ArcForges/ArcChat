// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Settings;

namespace ArcChat.LocalServices.Settings;

/// <summary>
/// UI-facing settings repository with typed load, save, and observation.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Loads the persisted settings snapshot, or the NextChat-compatible default snapshot when none exists.
    /// </summary>
    Task<SettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the complete settings snapshot.
    /// </summary>
    Task SaveAsync(SettingsSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Observes a single projected settings value.
    /// </summary>
    IObservable<T> Observe<T>(KeyExpression<T> keyExpression);
}
