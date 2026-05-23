// Copyright (c) ArcForges. Licensed under the MIT License.

using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Masks;

namespace ArcChat.Desktop.Features.Masks;

internal sealed class RecommendedMaskItem
{
    private static readonly char[] SummaryWhitespaceSeparators = new[] { ' ', '\r', '\n', '\t' };

    public RecommendedMaskItem(Mask mask)
    {
        this.Mask = mask ?? throw new ArgumentNullException(nameof(mask));
        this.Summary = CreateSummary(mask);
    }

    public Mask Mask { get; }

    public string Id => this.Mask.Id;

    public string Avatar => this.Mask.Avatar;

    public string Name => this.Mask.Name;

    public string Model => this.Mask.ModelConfig.Model;

    public string Summary { get; }

    private static string CreateSummary(Mask mask)
    {
        string text = mask.Context
            .SelectMany(static message => message.Content)
            .OfType<TextBlock>()
            .Select(static block => block.Text)
            .FirstOrDefault(static content => !string.IsNullOrWhiteSpace(content))
            ?? mask.Name;
        string[] words = text.Split(SummaryWhitespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
        string singleLine = string.Join(' ', words);
        return singleLine.Length <= 140 ? singleLine : string.Concat(singleLine.AsSpan(0, 140), "...");
    }
}
