// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.ViewModels;
using ArcChat.Desktop.Views;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class MainWindowTests
{
    [Fact]
    public static async Task MainWindowDisplaysArcChatHeader()
    {
        using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
        await session.Dispatch(
            () =>
            {
                MainWindow window = new MainWindow()
                {
                    DataContext = new MainWindowViewModel(),
                };

                try
                {
                    window.Show();
                    Dispatcher.UIThread.RunJobs();

                    TextBlock? textBlock = window.FindControl<TextBlock>("HeaderText");
                    _ = textBlock.Should().NotBeNull();
                    _ = textBlock!.Text.Should().Be("ArcChat");
                }
                finally
                {
                    window.Close();
                }
            },
            CancellationToken.None);
    }
}
