// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Navigation;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class AppNavigatorTests
{
    [Fact]
    public static void NavigatorPublishesCurrentDestinationAndHistory()
    {
        AppNavigator navigator = new AppNavigator();
        List<Destination> observed = new List<Destination>();
        using IDisposable subscription = navigator.CurrentDestination.Subscribe(new DestinationObserver(observed.Add));

        navigator.Navigate(new NewChat());
        navigator.Navigate(new Settings(SettingsSection.Providers));

        _ = navigator.Back().Should().BeTrue();
        _ = navigator.Current.Should().BeOfType<NewChat>();
        _ = navigator.Forward().Should().BeTrue();
        _ = navigator.Current.Should().BeOfType<Settings>();
        _ = navigator.Forward().Should().BeFalse();

        _ = observed.Select(destination => destination.Id)
            .Should()
            .Equal("home", "new-chat", "settings", "new-chat", "settings");
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
