// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.LocalPersistence;
using ArcChat.LocalServices.Settings;
using ArcChat.Protocol.Settings;
using FluentAssertions;
using Xunit;

namespace ArcChat.LocalServices.Tests.Settings;

public sealed class SettingsRepositoryTests
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "xUnit test methods do not require context capture.")]
    public static async Task LoadSaveLoadRoundTripsSettingsSnapshot()
    {
        string databasePath = CreateDatabasePath();
        await using ArcChatDatabase database = new ArcChatDatabase(databasePath);
        await database.InitializeAsync(CancellationToken.None);
        SettingsRepository repository = new SettingsRepository(database.Settings);
        SettingsSnapshot snapshot = SettingsDefaults.Create() with
        {
            Ui = SettingsDefaults.Create().Ui with
            {
                Theme = "dark",
                FontSize = 18,
            },
        };

        await repository.SaveAsync(snapshot, CancellationToken.None);

        SettingsRepository reloadedRepository = new SettingsRepository(database.Settings);
        SettingsSnapshot loaded = await reloadedRepository.LoadAsync(CancellationToken.None);

        _ = loaded.Should().BeEquivalentTo(snapshot);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "xUnit test methods do not require context capture.")]
    public static async Task ObservePublishesInitialAndSavedProjectedValues()
    {
        string databasePath = CreateDatabasePath();
        await using ArcChatDatabase database = new ArcChatDatabase(databasePath);
        await database.InitializeAsync(CancellationToken.None);
        SettingsRepository repository = new SettingsRepository(database.Settings);
        List<string> values = new List<string>();
        using IDisposable subscription = repository.Observe(SettingsKeys.Theme).Subscribe(new Observer<string>(values.Add));
        SettingsSnapshot snapshot = SettingsDefaults.Create() with
        {
            Ui = SettingsDefaults.Create().Ui with
            {
                Theme = "light",
            },
        };

        await repository.SaveAsync(snapshot, CancellationToken.None);

        _ = values.Should().Equal("auto", "light");
    }

    [Fact]
    public static void ExportImportRoundTripsSettingsSnapshot()
    {
        SettingsSnapshot snapshot = SettingsDefaults.Create() with
        {
            Ui = SettingsDefaults.Create().Ui with
            {
                FontFamily = "Inter",
                Theme = "dark",
            },
        };

        string exported = SettingsDocumentSerializer.Export(snapshot);
        SettingsSnapshot imported = SettingsDocumentSerializer.Import(exported);

        _ = imported.Should().BeEquivalentTo(snapshot);
    }

    private static string CreateDatabasePath()
    {
        string directory = Path.Join(Path.GetTempPath(), "ArcChat.LocalServices.Tests");
        Directory.CreateDirectory(directory);
        return Path.Join(directory, Guid.NewGuid().ToString("N") + ".db");
    }

    private sealed class Observer<T> : IObserver<T>
    {
        private readonly Action<T> onNext;

        public Observer(Action<T> onNext)
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

        public void OnNext(T value)
        {
            this.onNext(value);
        }
    }
}
