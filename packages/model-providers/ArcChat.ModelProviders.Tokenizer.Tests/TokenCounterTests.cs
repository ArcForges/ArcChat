// Copyright (c) ArcForges. Licensed under the MIT License.

using System.Collections.Immutable;
using ArcChat.Protocol.Chat;
using ArcChat.Protocol.Providers;
using FluentAssertions;
using SharpToken;
using Xunit;

namespace ArcChat.ModelProviders.Tokenizer.Tests;

public sealed class TokenCounterTests
{
    private const double NextChatReferenceEstimate = 87.75;

    [Theory]
    [InlineData("gpt-4-turbo", "cl100k_base")]
    [InlineData("gpt-4o-mini", "o200k_base")]
    public void OpenAiModelsUseSharpTokenEncoding(string modelId, string encodingName)
    {
        TokenCounter counter = new TokenCounter();
        ImmutableArray<Message> messages = CreateReferenceMessages();
        ModelDescriptor model = CreateModel(modelId, "openai");
        GptEncoding encoding = GptEncoding.GetEncoding(encodingName);
        int expected = messages.Sum(message => encoding.CountTokens(GetText(message)));

        int actual = counter.Count(messages, model);

        _ = actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("claude-3-5-sonnet-latest", "anthropic")]
    [InlineData("gemini-2.0-flash", "google")]
    public void NonOpenAiReferenceModelsUseNextChatCharacterEstimator(string modelId, string providerId)
    {
        TokenCounter counter = new TokenCounter();
        ImmutableArray<Message> messages = CreateReferenceMessages();
        ModelDescriptor model = CreateModel(modelId, providerId);

        int actual = counter.Count(messages, model);

        _ = actual.Should().BeInRange(
            LowerToleranceBound(NextChatReferenceEstimate),
            UpperToleranceBound(NextChatReferenceEstimate));
    }

    [Fact]
    public void CounterUsesFirstTextBlockLikeNextChat()
    {
        TokenCounter counter = new TokenCounter();
        Message message = new Message(
            "multi",
            MessageRole.User,
            ImmutableArray.Create<ContentBlock>(new TextBlock("Short"), new TextBlock("This second block is ignored.")),
            "0",
            Tools: ImmutableArray<ChatMessageTool>.Empty);
        ModelDescriptor model = CreateModel("claude-3-5-sonnet-latest", "anthropic");

        int actual = counter.Count(new[] { message }, model);

        _ = actual.Should().Be(2);
    }

    private static ImmutableArray<Message> CreateReferenceMessages()
    {
        return ImmutableArray.Create(
            Message.Text("m1", MessageRole.User, "Hello, world!", "0"),
            Message.Text("m2", MessageRole.Assistant, "Explain C# records in two bullets.", "0"),
            Message.Text("m3", MessageRole.System, "Use JSON: {\"ok\": true, \"count\": 3}.", "0"),
            Message.Text("m4", MessageRole.User, "你好，世界", "0"),
            Message.Text("m5", MessageRole.Assistant, "Emoji 🙂 count as unicode.", "0"),
            Message.Text("m6", MessageRole.User, "Line one\nLine two\nLine three.", "0"),
            Message.Text("m7", MessageRole.Assistant, "Math: f(x)=x^2 + 2x + 1.", "0"),
            Message.Text("m8", MessageRole.User, "Tabs\tand spaces    together.", "0"),
            Message.Text("m9", MessageRole.Assistant, "Mixed café déjà vu and 東京.", "0"),
            Message.Text("m10", MessageRole.User, "Short", "0"));
    }

    private static ModelDescriptor CreateModel(string id, string providerId)
    {
        return new ModelDescriptor(
            id,
            id,
            providerId,
            true,
            0,
            ImmutableArray<ProviderCapability>.Empty);
    }

    private static string GetText(Message message)
    {
        return message.Content.OfType<TextBlock>().Single().Text;
    }

    private static int LowerToleranceBound(double expected)
    {
        return (int)Math.Floor(expected * 0.95);
    }

    private static int UpperToleranceBound(double expected)
    {
        return (int)Math.Ceiling(expected * 1.05);
    }
}
