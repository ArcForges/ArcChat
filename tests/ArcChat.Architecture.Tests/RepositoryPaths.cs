// Copyright (c) ArcForges. Licensed under the MIT License.

namespace ArcChat.Architecture.Tests;

internal static class RepositoryPaths
{
    internal static string Root { get; } = FindRepositoryRoot(AppContext.BaseDirectory);

    internal static IReadOnlyList<string> ProjectFiles => Directory
        .EnumerateFiles(Root, "*.csproj", SearchOption.AllDirectories)
        .Where(IsRepositoryFile)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal static IReadOnlyList<string> SourceFiles => Directory
        .EnumerateFiles(Root, "*.cs", SearchOption.AllDirectories)
        .Where(IsRepositoryFile)
        .Order(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal static string Relative(string path)
    {
        return Path.GetRelativePath(Root, path).Replace('\\', '/');
    }

    internal static bool IsRepositoryFile(string path)
    {
        string relative = Relative(path);
        return !relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            && !relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            && !relative.StartsWith(".git/", StringComparison.OrdinalIgnoreCase)
            && !relative.StartsWith(".worktrees/", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot(string startDirectory)
    {
        DirectoryInfo? directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ArcChat.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate ArcChat.slnx from test output directory.");
    }
}
