// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace ArcChat.Architecture.Tests;

public sealed class CentralPackagingTests
{
    [Fact]
    public void ProjectFilesDoNotDeclarePackageVersions()
    {
        List<string> violations = new List<string>();
        foreach (string projectFile in RepositoryPaths.ProjectFiles)
        {
            XDocument document = XDocument.Load(projectFile);
            foreach (XElement packageReference in document.Descendants("PackageReference"))
            {
                if (packageReference.Attribute("Version") is not null || packageReference.Element("Version") is not null)
                {
                    violations.Add($"{RepositoryPaths.Relative(projectFile)}: {packageReference.Attribute("Include")?.Value}");
                }
            }
        }

        _ = violations.Should().BeEmpty("all NuGet versions must live in Directory.Packages.props");
    }
}
