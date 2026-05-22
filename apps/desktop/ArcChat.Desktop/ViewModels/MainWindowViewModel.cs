// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Navigation;

namespace ArcChat.Desktop.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel()
        : this(new AppNavigator())
    {
    }

    internal MainWindowViewModel(IAppNavigator navigator)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        this.CurrentDestination = navigator.CurrentDestination;
    }

    public string CurrentDestination { get; }
}
