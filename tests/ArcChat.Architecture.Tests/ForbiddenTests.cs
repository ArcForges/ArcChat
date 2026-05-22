// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace ArcChat.Architecture.Tests;

public sealed class ForbiddenTests
{
    private static readonly string[] ForbiddenDirectories =
    [
        "apps/server",
        "apps/web",
        "packages/server-api",
        "packages/server-api-client",
        "packages/protocol-openapi",
        "frontend-shared",
        "infra",
        "native/tool-host-net",
        "native/broker",
        "packages/ipc-core",
        "packages/protocol-messagepack",
        "packages/artifact-core",
        "packages/knowledge-core",
    ];

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Volo.Abp.",
        "Microsoft.AspNetCore.",
        "Microsoft.Maui.",
        "Microsoft.AspNetCore.Components.",
        "NetMQ",
        "MessagePack",
    ];

    [Fact]
    public void ForbiddenDirectoriesDoNotExist()
    {
        List<string> existing = ForbiddenDirectories
            .Select(directory => RepositoryPaths.FromRoot(directory))
            .Where(Directory.Exists)
            .Select(RepositoryPaths.Relative)
            .ToList();

        _ = existing.Should().BeEmpty();
    }

    [Fact]
    public void ProjectFilesDoNotReferenceForbiddenPackages()
    {
        List<string> violations = new List<string>();
        foreach (string projectFile in RepositoryPaths.ProjectFiles)
        {
            XDocument document = XDocument.Load(projectFile);
            IEnumerable<string> references = document.Descendants()
                .Where(element => element.Name.LocalName is "PackageReference" or "Reference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>();

            violations.AddRange(references
                .Where(IsForbiddenReference)
                .Select(reference => $"{RepositoryPaths.Relative(projectFile)}: {reference}"));
        }

        _ = violations.Should().BeEmpty();
    }

    [Fact]
    public void FixtureRejectsForbiddenValuesAndAcceptsAllowedValues()
    {
        _ = IsForbiddenDirectory("apps/server").Should().BeTrue();
        _ = IsForbiddenDirectory("apps/desktop").Should().BeFalse();
        _ = IsForbiddenReference("Volo.Abp.AspNetCore").Should().BeTrue();
        _ = IsForbiddenReference("Avalonia").Should().BeFalse();
    }

    private static bool IsForbiddenDirectory(string relativePath)
    {
        return ForbiddenDirectories.Contains(relativePath.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsForbiddenReference(string reference)
    {
        return ForbiddenReferencePrefixes.Any(prefix => reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
