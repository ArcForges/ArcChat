# NC05 Provider Live Demo Evidence

NC05 lands the chat-provider SPI and the four reference chat providers required before later provider waves:
OpenAI, Anthropic, Google Gemini, and GenericOpenAI-compatible endpoints.

## Offline Contract Evidence

The provider contract suites stream recorded fixtures without network access:

| Provider | Fixture | Test evidence |
| --- | --- | --- |
| OpenAI | `packages/model-providers/ArcChat.ModelProviders.OpenAi.Tests/Resources/openai-stream-tools-vision.ndjson` | `OpenAiProviderTests.StreamsOpenAiFixtureWithVisionToolsAndReasoning` |
| Anthropic | `packages/model-providers/ArcChat.ModelProviders.Anthropic.Tests/Resources/anthropic-messages-tools-vision.ndjson` | `AnthropicProviderTests.StreamsAnthropicFixtureWithVisionToolsAndThinking` |
| Google | `packages/model-providers/ArcChat.ModelProviders.Google.Tests/Resources/google-stream-tools-vision.ndjson` | `GoogleProviderTests.StreamsGoogleFixtureWithVisionToolsAndSafetySettings` |
| GenericOpenAI | `packages/model-providers/ArcChat.ModelProviders.GenericOpenAi.Tests/Resources/vllm-stream-tools-vision.ndjson`; `packages/model-providers/ArcChat.ModelProviders.GenericOpenAi.Tests/Resources/lmstudio-chat-stream.ndjson` | `GenericOpenAiProviderTests.StreamsVllmCompatibleFixtureWithConfiguredEndpointVisionAndTools`; `GenericOpenAiProviderTests.StreamsLmStudioFixtureAgainstBaseUriWithoutVersionSegment` |

## Desktop Selection Evidence

`SettingsDefaults.Create()` now seeds selectable provider configs for OpenAI, Anthropic, Google, and GenericOpenAI. `ArcChat.Desktop` registers all four providers with the streaming HTTP profile, so `AgentRuntime` resolves the selected `ModelConfig.ProviderName` directly from the desktop composition root.

Automated coverage:

```text
dotnet test apps/desktop/ArcChat.Desktop.UiTests/ArcChat.Desktop.UiTests.csproj -m:1 /p:TreatWarningsAsErrors=true
```

Result: passed, 46 tests. `NewChatViewModelTests.NewChatProviderPickerIncludesReferenceProviders` verifies the provider picker contains OpenAI, Anthropic, Google, and GenericOpenAI with at least one selectable model each.

## Live Demo Procedure

Live provider streaming is enabled through provider-specific environment variables until NC15 moves API keys into the OS keychain:

```powershell
$env:OPENAI_API_KEY = "<developer key>"
$env:ANTHROPIC_API_KEY = "<developer key>"
$env:GOOGLE_API_KEY = "<developer key>"
$env:GENERIC_OPENAI_BASE_URL = "http://localhost:8000"
$env:GENERIC_OPENAI_API_KEY = "<optional endpoint token>"
$env:MSBUILDDISABLENODEREUSE = "1"
dotnet run --project apps/desktop/ArcChat.Desktop/ArcChat.Desktop.csproj
```

Manual path: New Chat -> provider picker -> choose OpenAI, Anthropic, Google, or GenericOpenAI -> send a prompt -> verify streamed assistant response.

Live execution status for this automated run: not executed because no developer provider key was supplied in the environment. The desktop wiring and stream behavior are covered by the offline contract and UI tests above.
