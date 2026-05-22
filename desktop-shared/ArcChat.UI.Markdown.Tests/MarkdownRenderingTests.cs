// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.UI.Markdown.Markdown.Renderer;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAssertions;
using Xunit;

namespace ArcChat.UI.Markdown.Tests;

public sealed class MarkdownRenderingTests
{
    [Fact]
    public static void StreamingDeltaFixtureRerendersMarkdownSnapshots()
    {
        TextSource source = new TextSource();
        MarkdownView view = new MarkdownView
        {
            Source = source,
            IsStreaming = true,
        };

        string[] deltas = File.ReadAllLines(Path.Join(AppContext.BaseDirectory, "Fixtures", "streaming-deltas.txt"));
        List<string> snapshots = new List<string>();
        foreach (string delta in deltas)
        {
            source.Publish(delta.Replace("\\n", Environment.NewLine, StringComparison.Ordinal));
            snapshots.Add(Snapshot(view.Content as Control));
        }

        _ = snapshots.Should().Equal(
            "StackPanel[SelectableTextBlock]",
            "StackPanel[SelectableTextBlock,CodeBlock:True:csharp]",
            "StackPanel[SelectableTextBlock,CodeBlock:False:csharp]",
            "StackPanel[SelectableTextBlock,MathBlock:True]",
            "StackPanel[SelectableTextBlock,MathBlock:False]");
    }

    [Fact]
    public static void PartialFenceRendersPlaceholderUntilFenceCloses()
    {
        MarkdownAvaloniaRenderer renderer = new MarkdownAvaloniaRenderer();

        Control partial = renderer.Render("before\n```json\n{\"a\":", true);
        Control complete = renderer.Render("before\n```json\n{\"a\": 1}\n```", true);

        _ = partial.GetVisualDescendants().OfType<CodeBlock>().Single().IsPlaceholder.Should().BeTrue();
        _ = complete.GetVisualDescendants().OfType<CodeBlock>().Single().IsPlaceholder.Should().BeFalse();
    }

    [Fact]
    public static void PartialMathRendersPlaceholderUntilDelimiterCloses()
    {
        MarkdownAvaloniaRenderer renderer = new MarkdownAvaloniaRenderer();

        Control partial = renderer.Render("before\n$$\nx^2", true);
        Control complete = renderer.Render("before\n$$\nx^2\n$$", true);

        _ = partial.GetVisualDescendants().OfType<MathBlock>().Single().IsPlaceholder.Should().BeTrue();
        _ = complete.GetVisualDescendants().OfType<MathBlock>().Single().IsPlaceholder.Should().BeFalse();
    }

    [Fact]
    public static void PartialMermaidRendersDefaultPlaceholder()
    {
        MarkdownAvaloniaRenderer renderer = new MarkdownAvaloniaRenderer();

        Control partial = renderer.Render("```mermaid\ngraph TD", true);
        Control complete = renderer.Render("```mermaid\ngraph TD\nA-->B\n```", true);

        _ = partial.GetVisualDescendants().OfType<MermaidPlaceholder>().Single().IsPartial.Should().BeTrue();
        _ = complete.GetVisualDescendants().OfType<MermaidPlaceholder>().Single().IsPartial.Should().BeFalse();
        _ = complete.GetVisualDescendants().OfType<MermaidPlaceholder>().Single().SourceHash.Should().HaveLength(64);
    }

    [Fact]
    public static void GfmTableAndSoftBreaksRenderAsAvaloniaControls()
    {
        MarkdownAvaloniaRenderer renderer = new MarkdownAvaloniaRenderer();

        Control rendered = renderer.Render("a\nb\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n- [x] done", false);

        _ = rendered.GetVisualDescendants().OfType<ItemsControl>().Should().NotBeEmpty();
        _ = rendered.GetVisualDescendants().OfType<SelectableTextBlock>().First().Text.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public static async Task MarkdownVisualBaselinePngIsCommitted()
    {
        string repositoryRoot = FindRepositoryRoot();
        string outputPath = Path.Join(repositoryRoot, "docs", "coverage", "visual-baseline", "arcchat", "markdown.verified.png");
        bool shouldUpdate = !File.Exists(outputPath) || new FileInfo(outputPath).Length <= 1024;
        if (shouldUpdate)
        {
            using HeadlessUnitTestSession session = HeadlessUnitTestSession.StartNew(typeof(MarkdownTestAppBuilder));
            await session.Dispatch(
                () =>
                {
                    string? outputDirectory = Path.GetDirectoryName(outputPath);
                    if (outputDirectory is null)
                    {
                        throw new InvalidOperationException("Markdown baseline output directory could not be resolved.");
                    }

                    Directory.CreateDirectory(outputDirectory);
                    MarkdownView view = new MarkdownView
                    {
                        Text = "## Markdown\nLine one\nline two\n\n```csharp\npublic string Echo() => \"ok\";\n```\n\n$$\nx^2 + y^2\n$$\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n```mermaid\ngraph TD\nA-->B\n```",
                    };
                    Window window = new Window
                    {
                        Width = 720,
                        Height = 560,
                        Content = view,
                    };

                    try
                    {
                        window.Show();
                        Dispatcher.UIThread.RunJobs();
                        AvaloniaHeadlessPlatform.ForceRenderTimerTick(1);
                        using Avalonia.Media.Imaging.Bitmap? frame = window.CaptureRenderedFrame();
                        _ = frame.Should().NotBeNull("MarkdownView should render a headless PNG baseline");
                        using FileStream stream = File.Create(outputPath);
                        frame!.Save(stream);
                    }
                    finally
                    {
                        window.Close();
                    }
                },
                CancellationToken.None).ConfigureAwait(true);
        }

        FileInfo baseline = new FileInfo(outputPath);
        _ = baseline.Exists.Should().BeTrue("NC04.04 requires committed Markdown visual evidence");
        _ = baseline.Length.Should().BeGreaterThan(1024);
    }

    private static string Snapshot(Control? control)
    {
        if (control is StackPanel panel)
        {
            return "StackPanel[" + string.Join(",", panel.Children.OfType<Control>().Select(SnapshotNode)) + "]";
        }

        return SnapshotNode(control);
    }

    private static string SnapshotNode(Control? control)
    {
        return control switch
        {
            CodeBlock code => "CodeBlock:" + code.IsPlaceholder + ":" + code.Language,
            MathBlock math => "MathBlock:" + math.IsPlaceholder,
            MermaidPlaceholder mermaid => "MermaidPlaceholder:" + mermaid.IsPartial,
            SelectableTextBlock => "SelectableTextBlock",
            TextBlock => "TextBlock",
            ItemsControl => "ItemsControl",
            Border border => SnapshotNode(border.Child),
            StackPanel panel => "StackPanel[" + string.Join(",", panel.Children.OfType<Control>().Select(SnapshotNode)) + "]",
            null => "null",
            _ => control.GetType().Name,
        };
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Join(directory.FullName, "ArcChat.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }

    private sealed class TextSource : IObservable<string>
    {
        private readonly List<IObserver<string>> observers = new List<IObserver<string>>();

        public IDisposable Subscribe(IObserver<string> observer)
        {
            this.observers.Add(observer);
            return new Subscription(this.observers, observer);
        }

        public void Publish(string text)
        {
            foreach (IObserver<string> observer in this.observers.ToArray())
            {
                observer.OnNext(text);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly List<IObserver<string>> observers;
            private readonly IObserver<string> observer;

            public Subscription(List<IObserver<string>> observers, IObserver<string> observer)
            {
                this.observers = observers;
                this.observer = observer;
            }

            public void Dispose()
            {
                _ = this.observers.Remove(this.observer);
            }
        }
    }
}
