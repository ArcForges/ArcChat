// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Shortcuts;
using ArcChat.Desktop.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace ArcChat.Desktop.Features.Shell;

public sealed class CommandPaletteViewModel : ViewModelBase
{
    private readonly IShortcutRegistry shortcutRegistry;
    private bool isOpen;

    public CommandPaletteViewModel()
        : this(new ShortcutRegistry())
    {
    }

    internal CommandPaletteViewModel(IShortcutRegistry shortcutRegistry)
    {
        ArgumentNullException.ThrowIfNull(shortcutRegistry);
        this.shortcutRegistry = shortcutRegistry;
        this.Items = Array.Empty<CommandPaletteItem>();
        this.OpenCommand = new RelayCommand(this.Open);
        this.CloseCommand = new RelayCommand(this.Close);
    }

    public IRelayCommand OpenCommand { get; }

    public IRelayCommand CloseCommand { get; }

    public IReadOnlyList<CommandPaletteItem> Items { get; private set; }

    public bool IsOpen
    {
        get => this.isOpen;
        private set => this.SetProperty(ref this.isOpen, value);
    }

    private void Open()
    {
        this.Items = this.shortcutRegistry.Bindings
            .Select(static binding => new CommandPaletteItem(binding.Action, binding.Title, binding.GestureText))
            .ToArray();
        this.OnPropertyChanged(nameof(this.Items));
        this.IsOpen = true;
    }

    private void Close()
    {
        this.IsOpen = false;
    }
}
