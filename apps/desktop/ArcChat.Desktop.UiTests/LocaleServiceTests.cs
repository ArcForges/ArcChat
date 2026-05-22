// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Diagnostics;
using System.Text.Json;
using ArcChat.Desktop.Localization;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class LocaleServiceTests
{
    [Fact]
    public void GetFallsBackFromRequestedCultureToEnglishThenMissingKey()
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> locales =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Chat.Send"] = "Send",
                    ["Settings.Title"] = "Settings",
                },
                ["fr"] = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Chat.Send"] = "Envoyer",
                },
            };

        LocaleService service = new LocaleService(locales, "fr");

        _ = service.Get("Chat.Send").Should().Be("Envoyer");
        _ = service.Get("Settings.Title").Should().Be("Settings");
#if DEBUG
        _ = service.Get("Missing.Key").Should().Be("[missing:Missing.Key]");
#else
        _ = service.Get("Missing.Key").Should().Be("Missing.Key");
#endif
    }

    [Fact]
    public void CultureObservablePublishesInitialAndChangedCulture()
    {
        Dictionary<string, IReadOnlyDictionary<string, string>> locales =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>(StringComparer.Ordinal),
                ["pt"] = new Dictionary<string, string>(StringComparer.Ordinal),
            };

        LocaleService service = new LocaleService(locales, "en");
        List<string> cultures = new List<string>();

        using IDisposable subscription = service.Culture.Subscribe(new Observer<string>(cultures.Add));
        service.SetCulture("pt-BR");

        _ = cultures.Should().Equal("en", "pt");
        _ = service.CurrentCulture.Should().Be("pt");
        _ = service.AvailableCultures.Should().Equal("en", "pt");
    }

    [Fact]
    public void CoverageReportHasRowForEveryEnglishLocaleKey()
    {
        string repositoryRoot = FindRepositoryRoot();
        Dictionary<string, string> english = ReadJsonDictionary(
            Path.Combine(repositoryRoot, "apps", "desktop", "ArcChat.Desktop", "Resources", "Locales", "en.json"));
        string report = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "coverage", "locales.md"));

        foreach (string key in english.Keys)
        {
            _ = report.Should().Contain("| `" + key + "` |", "the coverage report must include every English locale key");
        }
    }

    [Fact]
    public void LocaleConversionIsByteStableAcrossTwoRuns()
    {
        string repositoryRoot = FindRepositoryRoot();
        string tempRoot = Path.Combine(Path.GetTempPath(), "arcchat-locale-test-" + Guid.NewGuid().ToString("N"));
        string firstOutput = Path.Combine(tempRoot, "first", "locales");
        string secondOutput = Path.Combine(tempRoot, "second", "locales");
        string firstReport = Path.Combine(tempRoot, "first", "locales.md");
        string secondReport = Path.Combine(tempRoot, "second", "locales.md");

        try
        {
            RunConverter(repositoryRoot, firstOutput, firstReport);
            RunConverter(repositoryRoot, secondOutput, secondReport);

            string[] firstFiles = Directory.EnumerateFiles(firstOutput, "*.json")
                .Select(static path => Path.GetFileName(path) ?? string.Empty)
                .Order(StringComparer.Ordinal)
                .ToArray();
            string[] secondFiles = Directory.EnumerateFiles(secondOutput, "*.json")
                .Select(static path => Path.GetFileName(path) ?? string.Empty)
                .Order(StringComparer.Ordinal)
                .ToArray();

            _ = firstFiles.Should().Equal(secondFiles);
            foreach (string file in firstFiles)
            {
                byte[] firstBytes = File.ReadAllBytes(Path.Combine(firstOutput, file));
                byte[] secondBytes = File.ReadAllBytes(Path.Combine(secondOutput, file));
                _ = firstBytes.Should().Equal(secondBytes, file + " must be deterministic");
            }

            _ = File.ReadAllBytes(firstReport).Should().Equal(File.ReadAllBytes(secondReport));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static Dictionary<string, string> ReadJsonDictionary(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException("Locale JSON could not be read.");
    }

    private static void RunConverter(string repositoryRoot, string outputRoot, string reportPath)
    {
        string script = Path.Combine(repositoryRoot, "scripts", "convert-locales.ps1");
        ProcessStartInfo startInfo = new ProcessStartInfo("pwsh")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = repositoryRoot,
        };

        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(script);
        startInfo.ArgumentList.Add("-OutputRoot");
        startInfo.ArgumentList.Add(outputRoot);
        startInfo.ArgumentList.Add("-Report");
        startInfo.ArgumentList.Add("-ReportPath");
        startInfo.ArgumentList.Add(reportPath);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Locale converter process did not start.");
        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

        bool exited = process.WaitForExit(180000);
        if (!exited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }

        string standardOutput = standardOutputTask.GetAwaiter().GetResult();
        string standardError = standardErrorTask.GetAwaiter().GetResult();

        _ = exited.Should().BeTrue("locale conversion must complete");
        _ = process.ExitCode.Should().Be(0, standardOutput + standardError);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ArcChat.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
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
