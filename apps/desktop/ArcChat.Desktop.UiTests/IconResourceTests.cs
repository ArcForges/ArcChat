// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Reflection;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class IconResourceTests
{
    [Fact]
    public static async Task IconReferencesLoadFromAxaml()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(
            () =>
            {
                Uri iconReferences = new Uri("avares://ArcChat.Desktop.UiTests/Resources/IconReferences.axaml");
                Action load = () => _ = AvaloniaXamlLoader.Load(iconReferences, iconReferences);

                _ = load.Should().NotThrow();
            },
            CancellationToken.None);
    }

    [Fact]
    public static async Task GeneratedIconUrisResolveAvaloniaResources()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(
            () =>
            {
                Type iconsType = typeof(App).Assembly.GetType("ArcChat.Desktop.Resources.Icons", throwOnError: true)
                    ?? throw new InvalidOperationException("Generated icon type was not found.");
                PropertyInfo[] iconProperties = iconsType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                    .Where(static property => property.PropertyType == typeof(string))
                    .OrderBy(static property => property.Name, StringComparer.Ordinal)
                    .ToArray();

                _ = iconProperties.Should().HaveCount(89);
                foreach (PropertyInfo property in iconProperties)
                {
                    string uri = property.GetValue(null).Should().BeOfType<string>().Subject;
                    using Stream stream = AssetLoader.Open(new Uri(uri));
                    _ = stream.CanRead.Should().BeTrue(property.Name);
                }
            },
            CancellationToken.None);
    }
}
