// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Localization;
using ArcChat.Desktop.ViewModels;
using ArcChat.LocalServices.Settings;
using ArcChat.Protocol.Settings;
using CommunityToolkit.Mvvm.Input;

namespace ArcChat.Desktop.Features.Settings;

public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly ISettingsRepository settingsRepository;
    private readonly ILocaleService? localeService;
    private readonly IThemeService? themeService;
    private readonly IDisposable? cultureSubscription;
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
    private string settingsTitle = "Settings";
    private string resetButtonText = "Reset";
    private string importButtonText = "Import";
    private string exportButtonText = "Export";
    private string saveButtonText = "Save";
    private string generalTabHeader = "General";
    private string appearanceTabHeader = "Appearance";
    private string localeTabHeader = "Locale";
    private string defaultModelLabel = "Default model";
    private string importJsonLabel = "Import JSON";
    private string themeLabel = "Theme";
    private string fontSizeLabel = "Font size";
    private string fontFamilyLabel = "Font family";
    private string tightBorderText = "Tight border";
    private string currentLocaleLabel = "Current locale";

    public SettingsViewModel()
        : this(new DesignSettingsRepository(), null)
    {
    }

    internal SettingsViewModel(
        ISettingsRepository settingsRepository,
        ILocaleService? localeService = null,
        IThemeService? themeService = null)
    {
        ArgumentNullException.ThrowIfNull(settingsRepository);
        this.settingsRepository = settingsRepository;
        this.localeService = localeService;
        this.themeService = themeService;
        this.snapshot = this.settingsRepository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        this.LocaleOptions = localeService?.AvailableCultures ?? new[] { "en" };
        this.CurrentLocale = localeService?.CurrentCulture ?? "en";
        this.SaveCommand = new RelayCommand(this.Save);
        this.ResetCommand = new RelayCommand(this.Reset);
        this.ImportCommand = new RelayCommand(this.Import);
        this.ExportCommand = new RelayCommand(this.Export);
        this.cultureSubscription = localeService?.Culture.Subscribe(new CultureObserver(this.OnCultureChanged));
        this.RefreshLocalizedText();
        this.ApplySnapshot(this.snapshot);
    }

    public IRelayCommand SaveCommand { get; }

    public IRelayCommand ResetCommand { get; }

    public IRelayCommand ImportCommand { get; }

    public IRelayCommand ExportCommand { get; }

    public IReadOnlyList<string> ThemeOptions { get; } = new[] { "auto", "light", "dark" };

    public IReadOnlyList<string> LocaleOptions { get; }

    public string Theme
    {
        get => this.theme;
        set
        {
            if (this.SetProperty(ref this.theme, value))
            {
                this.themeService?.Apply(value);
            }
        }
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
        set
        {
            if (this.SetProperty(ref this.currentLocale, value))
            {
                this.localeService?.SetCulture(value);
            }
        }
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

    public string SettingsTitle
    {
        get => this.settingsTitle;
        private set => this.SetProperty(ref this.settingsTitle, value);
    }

    public string ResetButtonText
    {
        get => this.resetButtonText;
        private set => this.SetProperty(ref this.resetButtonText, value);
    }

    public string ImportButtonText
    {
        get => this.importButtonText;
        private set => this.SetProperty(ref this.importButtonText, value);
    }

    public string ExportButtonText
    {
        get => this.exportButtonText;
        private set => this.SetProperty(ref this.exportButtonText, value);
    }

    public string SaveButtonText
    {
        get => this.saveButtonText;
        private set => this.SetProperty(ref this.saveButtonText, value);
    }

    public string GeneralTabHeader
    {
        get => this.generalTabHeader;
        private set => this.SetProperty(ref this.generalTabHeader, value);
    }

    public string AppearanceTabHeader
    {
        get => this.appearanceTabHeader;
        private set => this.SetProperty(ref this.appearanceTabHeader, value);
    }

    public string LocaleTabHeader
    {
        get => this.localeTabHeader;
        private set => this.SetProperty(ref this.localeTabHeader, value);
    }

    public string DefaultModelLabel
    {
        get => this.defaultModelLabel;
        private set => this.SetProperty(ref this.defaultModelLabel, value);
    }

    public string ImportJsonLabel
    {
        get => this.importJsonLabel;
        private set => this.SetProperty(ref this.importJsonLabel, value);
    }

    public string ThemeLabel
    {
        get => this.themeLabel;
        private set => this.SetProperty(ref this.themeLabel, value);
    }

    public string FontSizeLabel
    {
        get => this.fontSizeLabel;
        private set => this.SetProperty(ref this.fontSizeLabel, value);
    }

    public string FontFamilyLabel
    {
        get => this.fontFamilyLabel;
        private set => this.SetProperty(ref this.fontFamilyLabel, value);
    }

    public string TightBorderText
    {
        get => this.tightBorderText;
        private set => this.SetProperty(ref this.tightBorderText, value);
    }

    public string CurrentLocaleLabel
    {
        get => this.currentLocaleLabel;
        private set => this.SetProperty(ref this.currentLocaleLabel, value);
    }

    public void Dispose()
    {
        this.cultureSubscription?.Dispose();
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

    private void OnCultureChanged(string culture)
    {
        _ = this.SetProperty(ref this.currentLocale, culture, nameof(this.CurrentLocale));
        this.RefreshLocalizedText();
    }

    private void RefreshLocalizedText()
    {
        this.SettingsTitle = this.Translate("Settings.Title", "Settings");
        this.ResetButtonText = this.Translate("Settings.Danger.Reset.Action", "Reset");
        this.ImportButtonText = this.Translate("UI.Import", "Import");
        this.ExportButtonText = this.Translate("UI.Export", "Export");
        this.SaveButtonText = "Save";
        this.GeneralTabHeader = "General";
        this.AppearanceTabHeader = "Appearance";
        this.LocaleTabHeader = this.Translate("Settings.Lang.Name", "Locale");
        this.DefaultModelLabel = this.Translate("Settings.Model", "Default model");
        this.ImportJsonLabel = this.Translate("UI.Import", "Import") + " JSON";
        this.ThemeLabel = this.Translate("Settings.Theme", "Theme");
        this.FontSizeLabel = this.Translate("Settings.FontSize.Title", "Font size");
        this.FontFamilyLabel = this.Translate("Settings.FontFamily.Title", "Font family");
        this.TightBorderText = this.Translate("Settings.TightBorder", "Tight border");
        this.CurrentLocaleLabel = this.Translate("Settings.Lang.Name", "Current locale");
    }

    private string Translate(string key, string fallback)
    {
        return this.localeService?.Get(key) ?? fallback;
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
