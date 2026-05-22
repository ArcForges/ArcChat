// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace ArcChat.Architecture.Tests;

public sealed class SolutionLayoutTests
{
    [Fact]
    public void SolutionEnumeratesEveryPlannedProject()
    {
        string[] expectedProjects = ReadPlannedProjects();
        XDocument solution = XDocument.Load(Path.Combine(RepositoryPaths.Root, "ArcChat.slnx"));
        string[] actualProjects = solution
            .Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value.Replace('\\', '/'))
            .Where(path => path is not null)
            .Cast<string>()
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _ = actualProjects.Should().Equal(expectedProjects);
    }

    [Fact]
    public void EveryProjectExistsAndHasSource()
    {
        foreach (string project in ReadPlannedProjects())
        {
            string projectPath = Path.Combine(RepositoryPaths.Root, project.Replace('/', Path.DirectorySeparatorChar));
            string projectDirectory = Path.GetDirectoryName(projectPath)
                ?? throw new InvalidOperationException($"Could not resolve project directory for {project}.");

            _ = File.Exists(projectPath).Should().BeTrue(project);
            _ = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(RepositoryPaths.IsRepositoryFile)
                .Should().NotBeEmpty(project);
        }
    }

    private static string[] ReadPlannedProjects()
    {
        return File.ReadAllLines(Path.Combine(RepositoryPaths.Root, "tests", "ArcChat.Architecture.Tests", "Resources", "planned-projects.txt"))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
