// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Desktop.Localization;

internal interface ILocaleService
{
    IObservable<string> Culture { get; }

    string CurrentCulture { get; }

    string Get(string key);

    void SetCulture(string culture);
}
