// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Security.Cryptography;
using FluentAssertions;
using Xunit;

namespace ArcChat.Architecture.Tests;

public sealed class ResourceNoticeTests
{
    [Fact]
    public void DesktopResourceNoticeCoversEveryResource()
    {
        string resourceRoot = RepositoryPaths.FromRoot("apps", "desktop", "ArcChat.Desktop", "Resources");
        string noticePath = Path.Join(resourceRoot, "NOTICE.md");
        Dictionary<string, NoticeRow> rows = ReadNoticeRows(noticePath);
        string[] resources = Directory.EnumerateFiles(resourceRoot, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(path, noticePath, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (string resource in resources)
        {
            string targetPath = RepositoryPaths.Relative(resource);
            _ = rows.Should().ContainKey(targetPath);
            _ = rows[targetPath].Sha256.Should().Be(ComputeSha256(resource), targetPath);
        }

        foreach (string targetPath in rows.Keys)
        {
            _ = File.Exists(RepositoryPaths.FromRoot(targetPath)).Should().BeTrue(targetPath);
        }

        _ = rows.Should().HaveCount(resources.Length);
    }

    private static Dictionary<string, NoticeRow> ReadNoticeRows(string noticePath)
    {
        Dictionary<string, NoticeRow> rows = new Dictionary<string, NoticeRow>(StringComparer.Ordinal);
        foreach (string line in File.ReadLines(noticePath))
        {
            if (!line.StartsWith("| apps/desktop/ArcChat.Desktop/Resources/", StringComparison.Ordinal))
            {
                continue;
            }

            string[] columns = line.Trim('|').Split('|').Select(static column => column.Trim()).ToArray();
            if (columns.Length < 7)
            {
                throw new InvalidOperationException("Malformed NOTICE row: " + line);
            }

            rows.Add(columns[0], new NoticeRow(columns[0], columns[5]));
        }

        return rows;
    }

    private static string ComputeSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private sealed class NoticeRow
    {
        public NoticeRow(string targetPath, string sha256)
        {
            this.TargetPath = targetPath;
            this.Sha256 = sha256;
        }

        public string TargetPath { get; }

        public string Sha256 { get; }
    }
}
