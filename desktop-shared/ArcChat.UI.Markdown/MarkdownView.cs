// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.UI.Markdown.Markdown.Renderer;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ArcChat.UI.Markdown;

/// <summary>
/// Streaming Markdown surface for assistant messages.
/// </summary>
public sealed class MarkdownView : ContentControl
{
    /// <summary>Defines the <see cref="Source"/> property.</summary>
    public static readonly StyledProperty<IObservable<string>?> SourceProperty =
        AvaloniaProperty.Register<MarkdownView, IObservable<string>?>(nameof(Source));

    /// <summary>Defines the <see cref="Text"/> property.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MarkdownView, string?>(nameof(Text));

    /// <summary>Defines the <see cref="IsStreaming"/> property.</summary>
    public static readonly StyledProperty<bool> IsStreamingProperty =
        AvaloniaProperty.Register<MarkdownView, bool>(nameof(IsStreaming));

    private readonly MarkdownAvaloniaRenderer renderer = new MarkdownAvaloniaRenderer();
    private IDisposable? subscription;
    private string currentText = string.Empty;
    private bool sourceCompleted;

    /// <summary>Gets or sets the observable text stream.</summary>
    public IObservable<string>? Source
    {
        get => this.GetValue(SourceProperty);
        set => this.SetValue(SourceProperty, value);
    }

    /// <summary>Gets or sets static Markdown text.</summary>
    public string? Text
    {
        get => this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether the source is still streaming.</summary>
    public bool IsStreaming
    {
        get => this.GetValue(IsStreamingProperty);
        set => this.SetValue(IsStreamingProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ArgumentNullException.ThrowIfNull(change);
        base.OnPropertyChanged(change);
        if (change.Property == SourceProperty)
        {
            this.SubscribeToSource(change.GetNewValue<IObservable<string>?>());
            return;
        }

        if (change.Property == TextProperty)
        {
            this.UpdateText(change.GetNewValue<string?>() ?? string.Empty);
            return;
        }

        if (change.Property == IsStreamingProperty)
        {
            this.Render();
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        this.subscription?.Dispose();
        this.subscription = null;
    }

    private void SubscribeToSource(IObservable<string>? source)
    {
        this.subscription?.Dispose();
        this.subscription = null;
        this.sourceCompleted = false;
        if (source is null)
        {
            this.UpdateText(this.Text ?? string.Empty);
            return;
        }

        this.subscription = source.Subscribe(new StreamObserver(this));
    }

    private void UpdateText(string value)
    {
        this.currentText = value;
        this.Render();
    }

    private void Render()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            this.Content = this.renderer.Render(this.currentText, this.IsStreaming && !this.sourceCompleted);
            return;
        }

        Dispatcher.UIThread.Post(() => this.Content = this.renderer.Render(this.currentText, this.IsStreaming && !this.sourceCompleted));
    }

    private sealed class StreamObserver : IObserver<string>
    {
        private readonly MarkdownView owner;

        public StreamObserver(MarkdownView owner)
        {
            this.owner = owner;
        }

        public void OnCompleted()
        {
            this.owner.sourceCompleted = true;
            this.owner.Render();
        }

        public void OnError(Exception error)
        {
            ArgumentNullException.ThrowIfNull(error);
            this.owner.UpdateText(error.Message);
        }

        public void OnNext(string value)
        {
            this.owner.UpdateText(value);
        }
    }
}
