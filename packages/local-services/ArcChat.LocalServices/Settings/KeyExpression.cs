// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Settings;

namespace ArcChat.LocalServices.Settings;

/// <summary>
/// A named projection over a settings snapshot.
/// </summary>
public sealed record KeyExpression<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyExpression{T}"/> class.
    /// </summary>
    public KeyExpression(string key, Func<SettingsSnapshot, T> selector)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(selector);
        this.Key = key;
        this.Selector = selector;
    }

    /// <summary>
    /// Gets the stable settings key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the value projection.
    /// </summary>
    public Func<SettingsSnapshot, T> Selector { get; }

    /// <summary>
    /// Evaluates the expression against a settings snapshot.
    /// </summary>
    public T Evaluate(SettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return this.Selector(snapshot);
    }
}
