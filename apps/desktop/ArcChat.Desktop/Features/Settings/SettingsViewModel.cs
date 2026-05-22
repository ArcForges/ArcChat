// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Localization;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalServices.Settings;
using ArcChat.Protocol.Settings;
using CommunityToolkit.Mvvm.Input;

namespace ArcChat.Desktop.Features.Settings;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsRepository settingsRepository;
    private SettingsSnapshot snapshot;
    private string theme = "auto";
    private int fontSize = 14;
    private string fontFamily = string.Empty;
    private bool tightBorder;
    private string currentLocale = "en";
    private string defaultModel = "gpt-4o-mini";
    private string exportedJson = string.Empty;
    private string importJson = string.Empty;
    private string statusMessage = string.Empty;

    public SettingsViewModel()
        : this(new DesignSettingsRepository(), null)
    {
    }

    internal SettingsViewModel(ISettingsRepository settingsRepository, ILocaleService? localeService = null)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        this.settingsRepository = settingsRepository;
        this.snapshot = this.settingsRepository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        this.CurrentLocale = localeService?.CurrentCulture ?? "en";
        this.SaveCommand = new RelayCommand(this.Save);
        this.ResetCommand = new RelayCommand(this.Reset);
        this.ImportCommand = new RelayCommand(this.Import);
        this.ExportCommand = new RelayCommand(this.Export);
        this.ApplySnapshot(this.snapshot);
    }

    public IRelayCommand SaveCommand { get; }

    public IRelayCommand ResetCommand { get; }

    public IRelayCommand ImportCommand { get; }

    public IRelayCommand ExportCommand { get; }

    public IReadOnlyList<string> ThemeOptions { get; } = new[] { "auto", "light", "dark" };

    public string Theme
    {
        get => this.theme;
        set => this.SetProperty(ref this.theme, value);
    }

    public int FontSize
    {
        get => this.fontSize;
        set => this.SetProperty(ref this.fontSize, value);
    }

    public string FontFamily
    {
        get => this.fontFamily;
        set => this.SetProperty(ref this.fontFamily, value);
    }

    public bool TightBorder
    {
        get => this.tightBorder;
        set => this.SetProperty(ref this.tightBorder, value);
    }

    public string CurrentLocale
    {
        get => this.currentLocale;
        private set => this.SetProperty(ref this.currentLocale, value);
    }

    public string DefaultModel
    {
        get => this.defaultModel;
        private set => this.SetProperty(ref this.defaultModel, value);
    }

    public string ExportedJson
    {
        get => this.exportedJson;
        private set => this.SetProperty(ref this.exportedJson, value);
    }

    public string ImportJson
    {
        get => this.importJson;
        set => this.SetProperty(ref this.importJson, value);
    }

    public string StatusMessage
    {
        get => this.statusMessage;
        private set => this.SetProperty(ref this.statusMessage, value);
    }

    internal async Task<SettingsSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        SettingsSnapshot loaded = await this.settingsRepository.LoadAsync(cancellationToken).ConfigureAwait(false);
        this.ApplySnapshot(loaded);
        this.StatusMessage = "Loaded";
        return loaded;
    }

    private void Save()
    {
        SettingsSnapshot updated = this.CreateSnapshotFromFields();
        this.settingsRepository.SaveAsync(updated, CancellationToken.None).GetAwaiter().GetResult();
        this.ApplySnapshot(updated);
        this.StatusMessage = "Saved";
    }

    private void Reset()
    {
        this.ApplySnapshot(SettingsDefaults.Create());
        this.StatusMessage = "Reset pending save";
    }

    private void Export()
    {
        this.ExportedJson = SettingsDocumentSerializer.Export(this.CreateSnapshotFromFields());
        this.StatusMessage = "Exported";
    }

    private void Import()
    {
        string json = string.IsNullOrWhiteSpace(this.ImportJson) ? this.ExportedJson : this.ImportJson;
        if (string.IsNullOrWhiteSpace(json))
        {
            this.StatusMessage = "Nothing to import";
            return;
        }

        SettingsSnapshot imported = SettingsDocumentSerializer.Import(json);
        this.settingsRepository.SaveAsync(imported, CancellationToken.None).GetAwaiter().GetResult();
        this.ApplySnapshot(imported);
        this.StatusMessage = "Imported";
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "Record with-expressions use member assignment syntax.")]
    private SettingsSnapshot CreateSnapshotFromFields()
    {
        return this.snapshot with
        {
            LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Ui = this.snapshot.Ui with
            {
                Theme = this.Theme,
                FontSize = this.FontSize,
                FontFamily = this.FontFamily,
                TightBorder = this.TightBorder,
            },
        };
    }

    private void ApplySnapshot(SettingsSnapshot nextSnapshot)
    {
        this.snapshot = nextSnapshot;
        this.Theme = nextSnapshot.Ui.Theme;
        this.FontSize = nextSnapshot.Ui.FontSize;
        this.FontFamily = nextSnapshot.Ui.FontFamily;
        this.TightBorder = nextSnapshot.Ui.TightBorder;
        this.DefaultModel = nextSnapshot.Providers.DefaultModel;
    }

    private sealed class DesignSettingsRepository : ISettingsRepository
    {
        private SettingsSnapshot snapshot = SettingsDefaults.Create();

        public Task<SettingsSnapshot> LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.snapshot);
        }

        public Task SaveAsync(SettingsSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(snapshot);
            this.snapshot = snapshot;
            return Task.CompletedTask;
        }

        public IObservable<T> Observe<T>(KeyExpression<T> keyExpression)
        {
            ArgumentNullException.ThrowIfNull(keyExpression);
            return new DesignObservable<T>(keyExpression.Evaluate(this.snapshot));
        }
    }

    private sealed class DesignObservable<T> : IObservable<T>
    {
        private readonly T value;

        public DesignObservable(T value)
        {
            this.value = value;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            observer.OnNext(this.value);
            return new DesignSubscription();
        }
    }

    private sealed class DesignSubscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
