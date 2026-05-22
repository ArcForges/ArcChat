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

    internal static string FromRoot(params string[] relativeSegments)
    {
        string path = Root;
        foreach (string relativeSegment in relativeSegments)
        {
            string normalizedSegment = NormalizeRelativeSegment(relativeSegment);
            path = Path.Join(path, normalizedSegment);
        }

        return path;
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
            if (File.Exists(Path.Join(directory.FullName, "ArcChat.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate ArcChat.slnx from test output directory.");
    }

    private static string NormalizeRelativeSegment(string relativeSegment)
    {
        string normalizedSegment = relativeSegment
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (Path.IsPathRooted(normalizedSegment))
        {
            throw new InvalidOperationException($"Repository-relative path must not be rooted: {relativeSegment}");
        }

        return normalizedSegment;
    }
}
