// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Windows.Input;
using ArcChat.Desktop.Features.Conversations;
using ArcChat.Desktop.Features.Masks;
using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Features.Shell;
using ArcChat.Desktop.Localization;
using ArcChat.Desktop.Navigation;
using ArcChat.UI.Controls;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using SettingsDestination = ArcChat.Desktop.Navigation.Settings;

namespace ArcChat.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private static readonly Dictionary<string, Destination> DestinationsById =
        new Dictionary<string, Destination>(StringComparer.Ordinal)
        {
            ["home"] = new Home(),
            ["new-chat"] = new NewChat(),
            ["search-chat"] = new SearchChat(),
            ["masks"] = new Masks(),
            ["plugins"] = new Plugins(),
            ["artifacts"] = new Artifacts(),
            ["settings"] = new SettingsDestination(),
            ["auth"] = new Auth(),
            ["sd"] = new Sd(),
            ["sd-new"] = new SdNew(),
            ["mcp-market"] = new McpMarket(),
        };

    private readonly IAppNavigator navigator;
    private readonly ConversationListViewModel conversationListViewModel;
    private readonly SettingsViewModel settingsViewModel;
    private readonly Func<string, ChatDetailViewModel> chatDetailFactory;
    private readonly Func<SearchChatViewModel> searchChatFactory;
    private readonly Func<NewChatViewModel> newChatFactory;
    private readonly ILocaleService? localeService;
    private readonly IDisposable destinationSubscription;
    private readonly IDisposable? cultureSubscription;
    private Destination currentDestination;
    private ViewModelBase currentContent;
    private GridLength sidebarPaneLength = new GridLength(300);
    private bool isSidebarNarrow;
    private IReadOnlyList<SidebarItem> navigationItems = Array.Empty<SidebarItem>();

    public MainWindowViewModel()
        : this(new AppNavigator(), new ConversationListViewModel(), new SettingsViewModel(), new CommandPaletteViewModel())
    {
    }

    internal MainWindowViewModel(IAppNavigator navigator)
        : this(navigator, new ConversationListViewModel(), new SettingsViewModel(), new CommandPaletteViewModel(), null, DefaultChatDetailFactory)
    {
    }

    internal MainWindowViewModel(
        IAppNavigator navigator,
        ConversationListViewModel conversationListViewModel,
        SettingsViewModel settingsViewModel,
        CommandPaletteViewModel commandPalette,
        ILocaleService? localeService = null,
        Func<string, ChatDetailViewModel>? chatDetailFactory = null,
        Func<SearchChatViewModel>? searchChatFactory = null,
        Func<NewChatViewModel>? newChatFactory = null)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(conversationListViewModel);
        ArgumentNullException.ThrowIfNull(settingsViewModel);
        ArgumentNullException.ThrowIfNull(commandPalette);
        this.navigator = navigator;
        this.conversationListViewModel = conversationListViewModel;
        this.settingsViewModel = settingsViewModel;
        this.chatDetailFactory = chatDetailFactory ?? DefaultChatDetailFactory;
        this.searchChatFactory = searchChatFactory ?? DefaultSearchChatFactory;
        this.newChatFactory = newChatFactory ?? DefaultNewChatFactory;
        this.CommandPalette = commandPalette;
        this.currentDestination = navigator.Current;
        this.currentContent = this.CreateContent(navigator.Current);
        this.isSidebarNarrow = IsNarrow(this.sidebarPaneLength);
        this.localeService = localeService;
        this.NavigateCommand = new RelayCommand<string?>(this.Navigate);
        this.BackCommand = new RelayCommand(() => _ = this.navigator.Back());
        this.ForwardCommand = new RelayCommand(() => _ = this.navigator.Forward());
        this.destinationSubscription = navigator.CurrentDestination.Subscribe(new DestinationObserver(this.OnDestinationChanged));
        this.cultureSubscription = localeService?.Culture.Subscribe(new CultureObserver(_ => this.RefreshLocalizedShell()));
        this.RefreshLocalizedShell();
    }

    public IReadOnlyList<SidebarItem> NavigationItems
    {
        get => this.navigationItems;
        private set => this.SetProperty(ref this.navigationItems, value);
    }

    public ICommand NavigateCommand { get; }

    public ICommand BackCommand { get; }

    public ICommand ForwardCommand { get; }

    public CommandPaletteViewModel CommandPalette { get; }

    public Destination CurrentDestination
    {
        get => this.currentDestination;
        private set
        {
            if (this.SetProperty(ref this.currentDestination, value))
            {
                this.OnPropertyChanged(nameof(this.CurrentDestinationTitle));
            }
        }
    }

    public string CurrentDestinationTitle => this.TranslateDestination(this.CurrentDestination);

    public ViewModelBase CurrentContent
    {
        get => this.currentContent;
        private set => this.SetProperty(ref this.currentContent, value);
    }

    public GridLength SidebarPaneLength
    {
        get => this.sidebarPaneLength;
        set
        {
            if (this.SetProperty(ref this.sidebarPaneLength, value))
            {
                this.IsSidebarNarrow = IsNarrow(value);
            }
        }
    }

    public bool IsSidebarNarrow
    {
        get => this.isSidebarNarrow;
        private set => this.SetProperty(ref this.isSidebarNarrow, value);
    }

    public void Dispose()
    {
        this.destinationSubscription.Dispose();
        this.cultureSubscription?.Dispose();
        this.settingsViewModel.Dispose();
    }

    private static bool IsNarrow(GridLength paneLength)
    {
        return paneLength.Value <= ShellConstants.NarrowSidebarWidth;
    }

    private static ChatDetailViewModel DefaultChatDetailFactory(string conversationId)
    {
        return new ChatDetailViewModel(conversationId);
    }

    private static SearchChatViewModel DefaultSearchChatFactory()
    {
        return new SearchChatViewModel();
    }

    private static NewChatViewModel DefaultNewChatFactory()
    {
        return new NewChatViewModel();
    }

    private void Navigate(string? destinationId)
    {
        if (destinationId is null || !DestinationsById.TryGetValue(destinationId, out Destination? destination))
        {
            return;
        }

        this.navigator.Navigate(destination);
    }

    private void OnDestinationChanged(Destination destination)
    {
        this.CurrentDestination = destination;
        this.CurrentContent = this.CreateContent(destination);
    }

    private ViewModelBase CreateContent(Destination destination)
    {
        return destination switch
        {
            Home => this.conversationListViewModel,
            NewChat => this.newChatFactory(),
            Chat chat => this.chatDetailFactory(chat.ConversationId),
            SearchChat => this.searchChatFactory(),
            SettingsDestination => this.settingsViewModel,
            _ => new DestinationPlaceholderViewModel(this.TranslateDestination(destination), destination.Id),
        };
    }

    private void RefreshLocalizedShell()
    {
        this.NavigationItems = new SidebarItem[]
        {
            new SidebarItem("home", this.Translate("Home.Title", "Home"), "H"),
            new SidebarItem("new-chat", this.Translate("Home.NewChat", "New Chat"), "+", true),
            new SidebarItem("search-chat", this.Translate("SearchChat.Name", "Search Chat"), "S"),
            new SidebarItem("masks", this.Translate("Chat.InputActions.Masks", "Masks"), "M"),
            new SidebarItem("plugins", this.Translate("Plugin.Page.Title", "Plugins"), "P"),
            new SidebarItem("artifacts", this.Translate("Export.Artifacts.Title", "Artifacts"), "A"),
            new SidebarItem("settings", this.Translate("Settings.Title", "Settings"), "?"),
            new SidebarItem("auth", this.Translate("Auth.Title", "Auth"), "@"),
            new SidebarItem("sd", this.Translate("Sd.Status.Name", "Stable Diffusion"), "I"),
            new SidebarItem("sd-new", this.Translate("Chat.InputActions.UploadImage", "New Image"), "N"),
            new SidebarItem("mcp-market", this.Translate("Mcp.Name", "MCP Market"), "C"),
        };
        this.OnPropertyChanged(nameof(this.CurrentDestinationTitle));
        if (this.CurrentDestination is not SettingsDestination and not Home)
        {
            this.CurrentContent = this.CreateContent(this.CurrentDestination);
        }
    }

    private string TranslateDestination(Destination destination)
    {
        SidebarItem? item = this.NavigationItems.FirstOrDefault(
            sidebarItem => string.Equals(sidebarItem.Id, destination.Id, StringComparison.Ordinal));
        return item?.Title ?? destination.Title;
    }

    private string Translate(string key, string fallback)
    {
        return this.localeService?.Get(key) ?? fallback;
    }

    private sealed class DestinationObserver : IObserver<Destination>
    {
        private readonly Action<Destination> onNext;

        public DestinationObserver(Action<Destination> onNext)
        {
            this.onNext = onNext;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            ArgumentNullException.ThrowIfNull(error);
        }

        public void OnNext(Destination value)
        {
            this.onNext(value);
        }
    }

    private sealed class CultureObserver : IObserver<string>
    {
        private readonly Action<string> onNext;

        public CultureObserver(Action<string> onNext)
        {
            this.onNext = onNext;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            throw error;
        }

        public void OnNext(string value)
        {
            this.onNext(value);
        }
    }
}
