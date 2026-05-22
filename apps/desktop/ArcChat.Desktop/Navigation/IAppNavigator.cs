// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Navigation;

internal interface IAppNavigator
{
    IObservable<Destination> CurrentDestination { get; }

    Destination Current { get; }

    void Navigate(Destination destination);

    bool Back();

    bool Forward();
}
