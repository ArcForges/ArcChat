// Copyright (c) ArcForges. Licensed under the MIT License.

using Markdig;

namespace ArcChat.UI.Markdown.Markdown;

internal static class MarkdownPipelineFactory
{
    public static MarkdownPipeline Create()
    {
        return new MarkdownPipelineBuilder()
            .UseGenericAttributes()
            .UsePipeTables()
            .UseFootnotes()
            .UseMathematics()
            .UseEmojiAndSmiley()
            .UseAutoLinks()
            .UseTaskLists()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();
    }
}
