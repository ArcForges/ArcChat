// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Text.Json;

namespace ArcChat.Desktop.Localization;

internal sealed class LocaleService : ILocaleService
{
    internal const string EnglishCulture = "en";

    private static readonly Dictionary<string, string> CultureAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["zh"] = "cn",
            ["zh-CN"] = "cn",
            ["zh-Hans"] = "cn",
            ["zh-TW"] = "tw",
            ["zh-Hant"] = "tw",
        };

    private readonly ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> locales;
    private readonly ObservableCulture culture;

    public LocaleService(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> locales,
        string culture)
    {
        ArgumentNullException.ThrowIfNull(locales);

        this.locales = CopyLocales(locales);
        if (!this.locales.ContainsKey(EnglishCulture))
        {
            throw new InvalidOperationException("The English locale is required for fallback.");
        }

        this.CurrentCulture = this.ResolveCulture(culture);
        this.culture = new ObservableCulture(this.CurrentCulture);
    }

    public IObservable<string> Culture => this.culture;

    public string CurrentCulture { get; private set; }

    public static LocaleService FromDirectory(string localesDirectory, string culture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localesDirectory);
        if (!Directory.Exists(localesDirectory))
        {
            throw new DirectoryNotFoundException(localesDirectory);
        }

        Dictionary<string, IReadOnlyDictionary<string, string>> loadedLocales =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in Directory.EnumerateFiles(localesDirectory, "*.json").Order(StringComparer.Ordinal))
        {
            using FileStream stream = File.OpenRead(file);
            Dictionary<string, string>? values = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
            if (values is null)
            {
                throw new InvalidOperationException($"Locale file '{file}' is empty.");
            }

            loadedLocales[Path.GetFileNameWithoutExtension(file)] =
                new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(values, StringComparer.Ordinal));
        }

        return new LocaleService(loadedLocales, culture);
    }

    public string Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (this.TryGet(this.CurrentCulture, key, out string? value) && value is not null)
        {
            return value;
        }

        if (this.TryGet(EnglishCulture, key, out value) && value is not null)
        {
            return value;
        }

#if DEBUG
        return "[missing:" + key + "]";
#else
        return key;
#endif
    }

    public void SetCulture(string culture)
    {
        string resolvedCulture = this.ResolveCulture(culture);
        if (StringComparer.OrdinalIgnoreCase.Equals(this.CurrentCulture, resolvedCulture))
        {
            return;
        }

        this.CurrentCulture = resolvedCulture;
        this.culture.Publish(resolvedCulture);
    }

    private static ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CopyLocales(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> locales)
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> copy =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, IReadOnlyDictionary<string, string>> locale in locales)
        {
            copy[locale.Key] = new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(locale.Value, StringComparer.Ordinal));
        }

        return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(copy);
    }

    private string ResolveCulture(string culture)
    {
        string requested = string.IsNullOrWhiteSpace(culture) ? EnglishCulture : culture.Trim();
        if (CultureAliases.TryGetValue(requested, out string? alias) && this.locales.ContainsKey(alias))
        {
            return alias;
        }

        if (this.locales.ContainsKey(requested))
        {
            return requested;
        }

        int separatorIndex = requested.IndexOf('-', StringComparison.Ordinal);
        if (separatorIndex > 0)
        {
            string neutralCulture = requested[..separatorIndex];
            if (this.locales.ContainsKey(neutralCulture))
            {
                return neutralCulture;
            }
        }

        return EnglishCulture;
    }

    private bool TryGet(string culture, string key, out string? value)
    {
        value = null;
        return this.locales.TryGetValue(culture, out IReadOnlyDictionary<string, string>? values)
            && values.TryGetValue(key, out value);
    }

    private sealed class ObservableCulture : IObservable<string>
    {
        private readonly List<IObserver<string>> observers = new List<IObserver<string>>();

        public ObservableCulture(string initialCulture)
        {
            this.Value = initialCulture;
        }

        public string Value { get; private set; }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);
            this.observers.Add(observer);
            observer.OnNext(this.Value);
            return new Subscription(this.observers, observer);
        }

        public void Publish(string value)
        {
            this.Value = value;
            foreach (IObserver<string> observer in this.observers.ToArray())
            {
                observer.OnNext(value);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly List<IObserver<string>> observers;
            private readonly IObserver<string> observer;

            public Subscription(List<IObserver<string>> observers, IObserver<string> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                _ = this.observers.Remove(this.observer);
            }
        }
    }
}
