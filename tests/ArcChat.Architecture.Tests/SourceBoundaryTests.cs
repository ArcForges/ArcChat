// Copyright (c) ArcForges. Licensed under the MIT License.

using FluentAssertions;
using Xunit;

namespace ArcChat.Architecture.Tests;

public sealed class SourceBoundaryTests
{
    private static readonly string[] AllowedUsingPrefixes =
    {
        "ArcChat.",
        "Avalonia",
        "CommunityToolkit.",
        "Dapper",
        "DbUp",
        "FluentAssertions",
        "Microsoft.CodeAnalysis",
        "Microsoft.Data.Sqlite",
        "Microsoft.Extensions.",
        "NetArchTest.",
        "NSubstitute",
        "System",
        "VerifyXunit",
        "Xunit",
    };

    [Fact]
    public void SourceUsingDirectivesComeFromAllowedInputs()
    {
        List<string> violations = new List<string>();
        foreach (string sourceFile in RepositoryPaths.SourceFiles)
        {
            foreach (string trimmed in File.ReadLines(sourceFile).Select(line => line.Trim()))
            {
                if (!trimmed.StartsWith("using ", StringComparison.Ordinal)
                    || trimmed.StartsWith("using static ", StringComparison.Ordinal)
                    || !trimmed.EndsWith(';'))
                {
                    continue;
                }

                string namespaceName = trimmed[6..].TrimEnd(';');
                if (namespaceName.Contains(' ', StringComparison.Ordinal) || namespaceName.Contains('=', StringComparison.Ordinal))
                {
                    continue;
                }

                if (!AllowedUsingPrefixes.Any(prefix => namespaceName.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    violations.Add($"{RepositoryPaths.Relative(sourceFile)}: {namespaceName}");
                }
            }
        }

        _ = violations.Should().BeEmpty();
    }

    [Fact]
    public void RawHttpAndWebSocketTypesOnlyAppearInNetCoreOrTests()
    {
        string[] forbiddenTokens = new[] { "System.Net.Http.HttpClient", "System.Net.WebSockets.ClientWebSocket" };
        List<string> violations = new List<string>();
        foreach (string sourceFile in RepositoryPaths.SourceFiles)
        {
            string relative = RepositoryPaths.Relative(sourceFile);
            bool allowed = relative.StartsWith("packages/net-core/", StringComparison.OrdinalIgnoreCase)
                || relative.Contains(".Tests/", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("tests/", StringComparison.OrdinalIgnoreCase);

            if (allowed)
            {
                continue;
            }

            string content = File.ReadAllText(sourceFile);
            violations.AddRange(forbiddenTokens
                .Where(token => content.Contains(token, StringComparison.Ordinal))
                .Select(token => $"{relative}: {token}"));
        }

        _ = violations.Should().BeEmpty();
    }

    [Fact]
    public void MergeBlockerTodosDoNotAppearOutsideDocs()
    {
        List<string> violations = RepositoryPaths.SourceFiles
            .Where(path => !RepositoryPaths.Relative(path).StartsWith("docs/", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("// TODO(" + "merge-blocker)", StringComparison.Ordinal))
            .Select(RepositoryPaths.Relative)
            .ToList();

        _ = violations.Should().BeEmpty();
    }
}
