// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Windows.Input;
using ArcChat.Desktop.Features.Settings;
using ArcChat.Desktop.Features.Shell;
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
    private readonly SettingsViewModel settingsViewModel;
    private readonly IDisposable destinationSubscription;
    private Destination currentDestination;
    private ViewModelBase currentContent;
    private GridLength sidebarPaneLength = new GridLength(300);
    private bool isSidebarNarrow;

    public MainWindowViewModel()
        : this(new AppNavigator(), new SettingsViewModel())
    {
    }

    internal MainWindowViewModel(IAppNavigator navigator)
        : this(navigator, new SettingsViewModel())
    {
    }

    internal MainWindowViewModel(IAppNavigator navigator, SettingsViewModel settingsViewModel)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        ArgumentNullException.ThrowIfNull(settingsViewModel);
        this.navigator = navigator;
        this.settingsViewModel = settingsViewModel;
        this.currentDestination = navigator.Current;
        this.currentContent = this.CreateContent(navigator.Current);
        this.isSidebarNarrow = IsNarrow(this.sidebarPaneLength);
        this.NavigationItems = new SidebarItem[]
        {
            new SidebarItem("home", "Home", "H"),
            new SidebarItem("new-chat", "New Chat", "+", true),
            new SidebarItem("search-chat", "Search Chat", "S"),
            new SidebarItem("masks", "Masks", "M"),
            new SidebarItem("plugins", "Plugins", "P"),
            new SidebarItem("artifacts", "Artifacts", "A"),
            new SidebarItem("settings", "Settings", "?"),
            new SidebarItem("auth", "Auth", "@"),
            new SidebarItem("sd", "Stable Diffusion", "I"),
            new SidebarItem("sd-new", "New Image", "N"),
            new SidebarItem("mcp-market", "MCP Market", "C"),
        };
        this.NavigateCommand = new RelayCommand<string?>(this.Navigate);
        this.BackCommand = new RelayCommand(() => _ = this.navigator.Back());
        this.ForwardCommand = new RelayCommand(() => _ = this.navigator.Forward());
        this.destinationSubscription = navigator.CurrentDestination.Subscribe(new DestinationObserver(this.OnDestinationChanged));
    }

    public IReadOnlyList<SidebarItem> NavigationItems { get; }

    public ICommand NavigateCommand { get; }

    public ICommand BackCommand { get; }

    public ICommand ForwardCommand { get; }

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

    public string CurrentDestinationTitle => this.CurrentDestination.Title;

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
    }

    private static bool IsNarrow(GridLength paneLength)
    {
        return paneLength.Value <= ShellConstants.NarrowSidebarWidth;
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
        return destination is SettingsDestination
            ? this.settingsViewModel
            : new DestinationPlaceholderViewModel(destination.Title, destination.Id);
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
}
