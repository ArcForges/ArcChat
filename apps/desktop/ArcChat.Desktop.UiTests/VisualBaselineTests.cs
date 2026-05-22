// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Desktop.Navigation;
using ArcChat.Desktop.ViewModels;
using ArcChat.Desktop.Views;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using FluentAssertions;
using Xunit;

namespace ArcChat.Desktop.UiTests;

public sealed class VisualBaselineTests
{
    private const string UpdateEnvironmentVariable = "ARCCHAT_UPDATE_VISUAL_BASELINE";

    [Fact]
    public static async Task Nc03VisualBaselineScreenshotsAreCommitted()
    {
        string repositoryRoot = FindRepositoryRoot();
        string outputDirectory = Path.Combine(repositoryRoot, "docs", "coverage", "visual-baseline", "arcchat");
        string[] files =
        {
            Path.Combine(outputDirectory, "shell.verified.png"),
            Path.Combine(outputDirectory, "sidebar.verified.png"),
            Path.Combine(outputDirectory, "settings-shell.verified.png"),
        };

        bool shouldUpdate = string.Equals(Environment.GetEnvironmentVariable(UpdateEnvironmentVariable), "1", StringComparison.Ordinal)
            || files.Any(static file => !File.Exists(file) || new FileInfo(file).Length <= 1024);

        if (shouldUpdate)
        {
            using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));
            await session.Dispatch(
                () =>
                {
                    Directory.CreateDirectory(outputDirectory);
                    CaptureShell(files[0], static (_, _) => { });
                    CaptureShell(files[1], static (viewModel, _) => viewModel.SidebarPaneLength = new GridLength(ShellConstants.NarrowSidebarWidth));
                    CaptureShell(files[2], static (viewModel, _) => viewModel.NavigateCommand.Execute("settings"));
                },
                CancellationToken.None);
        }

        foreach (string file in files)
        {
            FileInfo baseline = new FileInfo(file);
            _ = baseline.Exists.Should().BeTrue(file + " must be committed as NC03 visual evidence");
            _ = baseline.Length.Should().BeGreaterThan(1024, file + " must contain a rendered PNG frame");
        }
    }

    private static void CaptureShell(string outputPath, Action<MainWindowViewModel, MainWindow> configure)
    {
        MainWindowViewModel viewModel = new MainWindowViewModel(new AppNavigator());
        MainWindow window = new MainWindow
        {
            Width = 1080,
            Height = 720,
            DataContext = viewModel,
        };

        try
        {
            window.Show();
            window.Activate();
            configure(viewModel, window);
            Dispatcher.UIThread.RunJobs();

            AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
            using Avalonia.Media.Imaging.Bitmap? frame = window.CaptureRenderedFrame();
            _ = frame.Should().NotBeNull("Avalonia.Headless should render the NC03 shell frame");
            using FileStream stream = File.Create(outputPath);
            frame!.Save(stream);
        }
        finally
        {
            window.Close();
            viewModel.Dispose();
        }
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
}
