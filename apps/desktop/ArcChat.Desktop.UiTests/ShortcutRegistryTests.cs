// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Features.Shell;
using ArcChat.Desktop.Shortcuts;
using ArcChat.LocalServices.Settings;
using ArcChat.Protocol.Settings;
using Avalonia.Input;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class ShortcutRegistryTests
{
    [Fact]
    public static void GestureParserParsesNextChatGestures()
    {
        KeyGesture newChat = ShortcutGestureParser.Parse("Ctrl+Shift+O");
        KeyGesture focusInput = ShortcutGestureParser.Parse("Shift+Esc");
        KeyGesture copyLastCode = ShortcutGestureParser.Parse("Ctrl+Shift+;");
        KeyGesture showShortcuts = ShortcutGestureParser.Parse("Ctrl+/");

        _ = newChat.Key.Should().Be(Key.O);
        _ = newChat.KeyModifiers.Should().Be(KeyModifiers.Control | KeyModifiers.Shift);
        _ = focusInput.Key.Should().Be(Key.Escape);
        _ = focusInput.KeyModifiers.Should().Be(KeyModifiers.Shift);
        _ = copyLastCode.Key.Should().Be(Key.OemSemicolon);
        _ = copyLastCode.KeyModifiers.Should().Be(KeyModifiers.Control | KeyModifiers.Shift);
        _ = showShortcuts.Key.Should().Be(Key.OemQuestion);
        _ = showShortcuts.KeyModifiers.Should().Be(KeyModifiers.Control);
    }

    [Fact]
    public static void DefaultGesturesMatchNextChatHotkeys()
    {
        ShortcutRegistry registry = new ShortcutRegistry();

        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatNew, GestureText = "Ctrl+Shift+O" });
        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatFocusInput, GestureText = "Shift+Esc" });
        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatCopyLastCode, GestureText = "Ctrl+Shift+;" });
        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatCopyLastMessage, GestureText = "Ctrl+Shift+C" });
        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatShowShortcuts, GestureText = "Ctrl+/" });
        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatClearContext, GestureText = "Ctrl+Shift+Backspace" });
        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatPrevious, GestureText = "Ctrl+ArrowUp" });
        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatPrevious, GestureText = "Alt+ArrowUp" });
        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatNext, GestureText = "Ctrl+ArrowDown" });
        _ = registry.Bindings.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatNext, GestureText = "Alt+ArrowDown" });
    }

    [Fact]
    public static void RegistryDetectsGestureConflicts()
    {
        ShortcutRegistry registry = new ShortcutRegistry();

        registry.Register("test.unique", ShortcutGestureParser.Parse("Ctrl+Alt+P"));
        Action registerConflict = () => registry.Register("test.other", ShortcutGestureParser.Parse("Ctrl+Alt+P"));

        ShortcutConflictException exception = registerConflict.Should().Throw<ShortcutConflictException>().Which;
        _ = exception.Action.Should().Be("test.other");
        _ = exception.ExistingAction.Should().Be("test.unique");
        _ = exception.Gesture.Should().Be("Ctrl+Alt+P");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "xUnit test methods do not require context capture.")]
    public static async Task OverridePersistsInSettingsSnapshot()
    {
        InMemorySettingsRepository settingsRepository = new InMemorySettingsRepository(SettingsDefaults.Create());
        ShortcutRegistry registry = new ShortcutRegistry(settingsRepository);

        await registry.OverrideAsync(
            ShortcutDefaults.ChatNew,
            ShortcutGestureParser.Parse("Ctrl+Alt+N"),
            CancellationToken.None);

        _ = settingsRepository.Snapshot.Shortcuts.Should().NotBeNull();
        _ = settingsRepository.Snapshot.Shortcuts!.Overrides.Should()
            .Contain(ShortcutDefaults.ChatNew, "Ctrl+Alt+N");
        _ = registry.Bindings.Where(static binding => string.Equals(binding.Action, ShortcutDefaults.ChatNew, StringComparison.Ordinal))
            .Should().ContainSingle()
            .Which.GestureText.Should().Be("Ctrl+Alt+N");
    }

    [Fact]
    public static void CommandPaletteListsRegisteredActionsAndGestures()
    {
        CommandPaletteViewModel viewModel = new CommandPaletteViewModel(new ShortcutRegistry());

        viewModel.OpenCommand.Execute(null);

        _ = viewModel.IsOpen.Should().BeTrue();
        _ = viewModel.Items.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.CommandPaletteOpen, GestureText = "Ctrl+K" });
        _ = viewModel.Items.Should().ContainEquivalentOf(
            new { Action = ShortcutDefaults.ChatNew, GestureText = "Ctrl+Shift+O" });
    }

    private sealed class InMemorySettingsRepository : ISettingsRepository
    {
        public InMemorySettingsRepository(SettingsSnapshot snapshot)
        {
            this.Snapshot = snapshot;
        }

        public SettingsSnapshot Snapshot { get; private set; }

        public Task<SettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.Snapshot);
        }

        public Task SaveAsync(SettingsSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            this.Snapshot = snapshot;
            return Task.CompletedTask;
        }

        public IObservable<T> Observe<T>(KeyExpression<T> keyExpression)
        {
            return new EmptyObservable<T>();
        }
    }

    private sealed class EmptyObservable<T> : IObservable<T>
    {
        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnCompleted();
            return EmptySubscription.Instance;
        }
    }

    private sealed class EmptySubscription : IDisposable
    {
        public static readonly EmptySubscription Instance = new EmptySubscription();

        public void Dispose()
        {
        }
    }
}
