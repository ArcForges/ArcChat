// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace ArcChat.Desktop.Features.Conversations;

internal static class NextChatVisionModelGate
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    private static readonly VisionPattern[] VisionPatterns =
    {
        new VisionPattern("vision"),
        new VisionPattern("gpt-4o"),
        new VisionPattern(@"gpt-4\.1"),
        new VisionPattern(@"claude.*[34]"),
        new VisionPattern(@"gemini-1\.5"),
        new VisionPattern("gemini-exp"),
        new VisionPattern(@"gemini-2\.[05]"),
        new VisionPattern("learnlm"),
        new VisionPattern("qwen-vl"),
        new VisionPattern("qwen2-vl"),
        new VisionPattern(@"gpt-4-turbo(?!.*preview)"),
        new VisionPattern(@"^dall-e-3$"),
        new VisionPattern("glm-4v"),
        new VisionPattern("vl", RegexOptions.IgnoreCase),
        new VisionPattern("o3"),
        new VisionPattern("o4-mini"),
        new VisionPattern("grok-4", RegexOptions.IgnoreCase),
        new VisionPattern("gpt-5"),
    };

    private static readonly VisionPattern[] ExcludedVisionPatterns =
    {
        new VisionPattern("claude-3-5-haiku-20241022"),
    };

    internal static bool IsVisionModel(string? model, IEnumerable<string>? configuredVisionModels = null)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        if (configuredVisionModels is not null && configuredVisionModels.Contains(model, StringComparer.Ordinal))
        {
            return true;
        }

        return !ExcludedVisionPatterns.Any(pattern => pattern.IsMatch(model))
            && VisionPatterns.Any(pattern => pattern.IsMatch(model));
    }

    private readonly struct VisionPattern
    {
        private readonly string pattern;
        private readonly RegexOptions options;

        public VisionPattern(string pattern, RegexOptions options = RegexOptions.None)
        {
            this.pattern = pattern;
            this.options = options;
        }

        public bool IsMatch(string input)
        {
            return Regex.IsMatch(input, this.pattern, this.options | RegexOptions.CultureInvariant, RegexTimeout);
        }
    }
}
