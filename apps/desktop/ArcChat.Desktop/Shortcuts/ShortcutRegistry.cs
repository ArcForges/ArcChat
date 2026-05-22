// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.LocalServices.Settings;
using ArcChat.Protocol.Settings;
using Avalonia.Input;

namespace ArcChat.Desktop.Shortcuts;

internal sealed class ShortcutRegistry : IShortcutRegistry
{
    private readonly ISettingsRepository? settingsRepository;
    private readonly Dictionary<string, List<ShortcutBinding>> bindingsByAction = new Dictionary<string, List<ShortcutBinding>>(StringComparer.Ordinal);
    private readonly Dictionary<string, ShortcutBinding> bindingsByGesture = new Dictionary<string, ShortcutBinding>(StringComparer.OrdinalIgnoreCase);
    private SettingsSnapshot snapshot;

    public ShortcutRegistry()
    {
        this.snapshot = SettingsDefaults.Create();
        this.Rebuild(this.snapshot);
    }

    public ShortcutRegistry(ISettingsRepository settingsRepository)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        this.settingsRepository = settingsRepository;
        this.snapshot = settingsRepository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        this.Rebuild(this.snapshot);
    }

    public IReadOnlyList<ShortcutBinding> Bindings
    {
        get
        {
            return this.bindingsByAction.Values
                .SelectMany(static bindings => bindings)
                .OrderBy(static binding => binding.Title, StringComparer.Ordinal)
                .ThenBy(static binding => binding.GestureText, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public void Register(string action, KeyGesture gesture)
    {
        this.RegisterCore(action, action, gesture);
    }

    public async Task OverrideAsync(string action, KeyGesture gesture, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (!this.bindingsByAction.ContainsKey(action))
        {
            throw new KeyNotFoundException("Shortcut action '" + action + "' is not registered.");
        }

        this.EnsureGestureAvailable(action, gesture);
        ImmutableDictionary<string, string> overrides = (this.snapshot.Shortcuts?.Overrides ?? ImmutableDictionary<string, string>.Empty)
            .SetItem(action, ShortcutGestureParser.Format(gesture));
        SettingsSnapshot updated = new SettingsSnapshot(
            this.snapshot.LastUpdate,
            this.snapshot.Ui,
            this.snapshot.Conversation,
            this.snapshot.Providers,
            this.snapshot.ModelConfig,
            this.snapshot.Tts,
            this.snapshot.Realtime,
            new ShortcutSettings(overrides),
            this.snapshot.Extra);

        if (this.settingsRepository is not null)
        {
            await this.settingsRepository.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        }

        this.Rebuild(updated);
    }

    private void Rebuild(SettingsSnapshot nextSnapshot)
    {
        this.snapshot = nextSnapshot;
        this.bindingsByAction.Clear();
        this.bindingsByGesture.Clear();
        HashSet<string> overriddenActions = new HashSet<string>(StringComparer.Ordinal);
        ImmutableDictionary<string, string> overrides = nextSnapshot.Shortcuts?.Overrides ?? ImmutableDictionary<string, string>.Empty;

        foreach (ShortcutDefinition definition in ShortcutDefaults.All)
        {
            KeyGesture gesture = definition.Gesture;
            if (overrides.TryGetValue(definition.Action, out string? overrideText))
            {
                if (!overriddenActions.Add(definition.Action))
                {
                    continue;
                }

                gesture = ShortcutGestureParser.Parse(overrideText);
            }

            this.RegisterCore(definition.Action, definition.Title, gesture);
        }
    }

    private void RegisterCore(string action, string title, KeyGesture gesture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        this.EnsureGestureAvailable(action, gesture);
        string gestureText = ShortcutGestureParser.Format(gesture);
        ShortcutBinding binding = new ShortcutBinding(action, title, gesture, gestureText);
        if (!this.bindingsByAction.TryGetValue(action, out List<ShortcutBinding>? bindings))
        {
            bindings = new List<ShortcutBinding>();
            this.bindingsByAction.Add(action, bindings);
        }

        if (bindings.Any(existing => string.Equals(existing.GestureText, gestureText, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        bindings.Add(binding);
        this.bindingsByGesture.Add(gestureText, binding);
    }

    private void EnsureGestureAvailable(string action, KeyGesture gesture)
    {
        string gestureText = ShortcutGestureParser.Format(gesture);
        if (this.bindingsByGesture.TryGetValue(gestureText, out ShortcutBinding? existing)
            && !string.Equals(existing.Action, action, StringComparison.Ordinal))
        {
            throw new ShortcutConflictException(action, existing.Action, gestureText);
        }
    }
}
