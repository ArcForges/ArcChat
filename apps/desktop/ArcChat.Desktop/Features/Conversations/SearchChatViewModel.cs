// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Diagnostics;
using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalPersistence.Repositories;
using ArcChat.Protocol.Chat;
using ArcChat.UI.Controls.Search;
using CommunityToolkit.Mvvm.Input;

namespace ArcChat.Desktop.Features.Conversations;

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsShouldAppearBeforeInstanceElements", Justification = "Search result factory is kept near the search helpers.")]
internal sealed class SearchChatViewModel : ViewModelBase
{
    private readonly IConversationRepository? repository;
    private readonly IAppNavigator? navigator;
    private IReadOnlyList<Conversation> conversations = Array.Empty<Conversation>();
    private string query = string.Empty;
    private string statusMessage = string.Empty;
    private TimeSpan lastSearchElapsed;

    public SearchChatViewModel()
    {
        this.conversations =
        [
            new Conversation(
                "design-search",
                "Search Preview",
                string.Empty,
                [Message.Text("design-message", MessageRole.Assistant, "Find matching chat messages quickly.", "2026-05-22")],
                new ChatStat(0, 0, 0),
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                0,
                null,
                new ArcChat.Protocol.Masks.Mask(
                    "design-mask",
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    "1f600",
                    "Default",
                    false,
                    [],
                    true,
                    ArcChat.Protocol.Providers.ModelConfig.NextChatDefault,
                    "en",
                    false,
                    [])),
        ];
        this.SearchCommand = new RelayCommand(this.Search);
        this.OpenCommand = new RelayCommand<SearchChatResult>(this.Open);
    }

    internal SearchChatViewModel(IConversationRepository repository, IAppNavigator navigator)
        : this()
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
    }

    public ObservableCollection<SearchChatResult> Results { get; } = new ObservableCollection<SearchChatResult>();

    public IRelayCommand SearchCommand { get; }

    public IRelayCommand<SearchChatResult> OpenCommand { get; }

    public string Query
    {
        get => this.query;
        set
        {
            if (this.SetProperty(ref this.query, value))
            {
                this.Search();
            }
        }
    }

    public string StatusMessage
    {
        get => this.statusMessage;
        private set => this.SetProperty(ref this.statusMessage, value);
    }

    public TimeSpan LastSearchElapsed
    {
        get => this.lastSearchElapsed;
        private set => this.SetProperty(ref this.lastSearchElapsed, value);
    }

    internal async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (this.repository is null)
        {
            this.Search();
            return;
        }

        this.conversations = await this.repository.ListAsync(cancellationToken).ConfigureAwait(true);
        this.Search();
    }

    internal void SetConversationsForTest(IReadOnlyList<Conversation> values)
    {
        this.conversations = values ?? throw new ArgumentNullException(nameof(values));
    }

    private void Search()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        this.Results.Clear();
        string searchQuery = this.Query.Trim();
        if (searchQuery.Length == 0)
        {
            stopwatch.Stop();
            this.LastSearchElapsed = stopwatch.Elapsed;
            this.StatusMessage = "Ready";
            return;
        }

        List<SearchChatResult> matches = new List<SearchChatResult>(this.FindExactMatches(searchQuery));
        if (matches.Count == 0)
        {
            matches.AddRange(this.FindFuzzyMatches(searchQuery));
        }

        foreach (SearchChatResult result in matches
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Snippet.Length)
            .Take(200))
        {
            this.Results.Add(result);
        }

        stopwatch.Stop();
        this.LastSearchElapsed = stopwatch.Elapsed;
        this.StatusMessage = this.Results.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + " matches";
    }

    private void Open(SearchChatResult? result)
    {
        if (result is null)
        {
            return;
        }

        this.navigator?.Navigate(new Chat(result.ConversationId));
    }

    private IEnumerable<SearchChatResult> FindExactMatches(string searchQuery)
    {
        return this.conversations
            .SelectMany(conversation => conversation.Messages
                .Select(message => CreateExactSearchResult(conversation, message, searchQuery)))
            .OfType<SearchChatResult>();
    }

    private IEnumerable<SearchChatResult> FindFuzzyMatches(string searchQuery)
    {
        return this.conversations
            .SelectMany(conversation => conversation.Messages
                .Select(message => CreateFuzzySearchResult(conversation, message, searchQuery)))
            .OfType<SearchChatResult>();
    }

    private static SearchChatResult CreateSearchResult(
        Conversation conversation,
        Message message,
        string text,
        FuzzyMatchResult match)
    {
        return new SearchChatResult(
            conversation.Id,
            conversation.Topic,
            message.Id,
            message.Role,
            FuzzyMatcher.CreateSnippet(text, match),
            match.Score);
    }

    private static SearchChatResult? CreateExactSearchResult(Conversation conversation, Message message, string searchQuery)
    {
        string text = MessageText.Extract(message);
        int matchStart = text.IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase);
        if (matchStart < 0)
        {
            return null;
        }

        FuzzyMatchResult match = new FuzzyMatchResult(true, 10_000 - matchStart, matchStart, searchQuery.Length);
        return CreateSearchResult(conversation, message, text, match);
    }

    private static SearchChatResult? CreateFuzzySearchResult(Conversation conversation, Message message, string searchQuery)
    {
        string text = MessageText.Extract(message);
        FuzzyMatchResult match = FuzzyMatcher.Match(text.AsSpan(), searchQuery.AsSpan());
        return match.IsMatch
            ? CreateSearchResult(conversation, message, text, match)
            : null;
    }
}
