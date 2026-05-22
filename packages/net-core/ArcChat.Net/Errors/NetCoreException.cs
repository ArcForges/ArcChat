// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Net.Errors;

/// <summary>
/// Exception carrying a normalized network error payload.
/// </summary>
public sealed class NetCoreException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetCoreException"/> class.
    /// </summary>
    public NetCoreException()
        : this(new UnknownNetError("A network error occurred."))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetCoreException"/> class.
    /// </summary>
    public NetCoreException(string message)
        : this(new UnknownNetError(message))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetCoreException"/> class.
    /// </summary>
    public NetCoreException(string message, Exception innerException)
        : this(new UnknownNetError(message), innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetCoreException"/> class.
    /// </summary>
    public NetCoreException(NetError error)
        : this(error, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetCoreException"/> class.
    /// </summary>
    public NetCoreException(NetError error, Exception? innerException)
        : base((error ?? throw new ArgumentNullException(nameof(error))).Message, innerException)
    {
        this.Error = error;
    }

    /// <summary>
    /// Gets the normalized error payload.
    /// </summary>
    public NetError Error { get; }
}
