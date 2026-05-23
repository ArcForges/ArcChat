// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.ModelProviders.Core;

/// <summary>
/// Stable chat provider id used to resolve NextChat provider names.
/// </summary>
public readonly record struct ProviderId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderId"/> struct.
    /// </summary>
    /// <param name="value">Provider id value, for example <c>OpenAI</c> or <c>Anthropic</c>.</param>
    public ProviderId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        this.Value = value;
    }

    /// <summary>
    /// Gets the provider id value.
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// Returns the provider id value.
    /// </summary>
    /// <returns>The provider id value.</returns>
    public override string ToString()
    {
        return this.Value;
    }
}
