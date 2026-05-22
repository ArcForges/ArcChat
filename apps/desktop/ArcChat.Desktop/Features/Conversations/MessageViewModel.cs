// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using ArcChat.Desktop.ViewModels;
using ArcChat.Protocol.Chat;

namespace ArcChat.Desktop.Features.Conversations;

internal sealed class MessageViewModel : ViewModelBase
{
    private readonly string date;
    private readonly MessageTextObservable textStream;
    private string text;
    private bool isStreaming;
    private bool isError;
    private bool isEditing;
    private string draftText;

    public MessageViewModel(
        string id,
        MessageRole role,
        string text,
        string date,
        bool isStreaming = false,
        bool isError = false,
        string? branchOfMessageId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        this.Id = id;
        this.Role = role;
        this.text = text;
        this.date = date;
        this.isStreaming = isStreaming;
        this.isError = isError;
        this.BranchOfMessageId = branchOfMessageId;
        this.draftText = text;
        this.textStream = new MessageTextObservable(() => this.Text);
    }

    public string Id { get; }

    public MessageRole Role { get; }

    public string RoleName => this.Role.ToString();

    public bool IsUser => this.Role == MessageRole.User;

    public bool IsAssistant => this.Role == MessageRole.Assistant;

    public string Date => this.date;

    public string? BranchOfMessageId { get; }

    public bool HasBranch => !string.IsNullOrWhiteSpace(this.BranchOfMessageId);

    public IObservable<string> TextStream => this.textStream;

    public string Text
    {
        get => this.text;
        set
        {
            if (this.SetProperty(ref this.text, value))
            {
                this.textStream.Publish(value);
            }
        }
    }

    public bool IsStreaming
    {
        get => this.isStreaming;
        set => this.SetProperty(ref this.isStreaming, value);
    }

    public bool IsError
    {
        get => this.isError;
        set => this.SetProperty(ref this.isError, value);
    }

    public bool IsEditing
    {
        get => this.isEditing;
        set => this.SetProperty(ref this.isEditing, value);
    }

    public string DraftText
    {
        get => this.draftText;
        set => this.SetProperty(ref this.draftText, value);
    }

    public static MessageViewModel FromMessage(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new MessageViewModel(
            message.Id,
            message.Role,
            ExtractText(message),
            message.Date,
            message.Streaming,
            message.IsError);
    }

    public static string ExtractText(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return string.Concat(message.Content.OfType<TextBlock>().Select(block => block.Text));
    }

    public void AppendDelta(string delta)
    {
        this.Text += delta;
    }

    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    public Message ToMessage()
    {
        return Message.Text(this.Id, this.Role, this.Text, this.Date) with
        {
            Streaming = this.IsStreaming,
            IsError = this.IsError,
        };
    }

    private sealed class MessageTextObservable : IObservable<string>
    {
        private readonly Func<string> currentText;
        private readonly List<IObserver<string>> observers = new List<IObserver<string>>();

        public MessageTextObservable(Func<string> currentText)
        {
            this.currentText = currentText;
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            this.observers.Add(observer);
            observer.OnNext(this.currentText());
            return new Subscription(this.observers, observer);
        }

        public void Publish(string value)
        {
            foreach (IObserver<string> observer in this.observers.ToArray())
            {
                observer.OnNext(value);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly List<IObserver<string>> observers;
            private IObserver<string>? observer;

            public Subscription(List<IObserver<string>> observers, IObserver<string> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                if (this.observer is { } value)
                {
                    _ = this.observers.Remove(value);
                    this.observer = null;
                }
            }
        }
    }
}
