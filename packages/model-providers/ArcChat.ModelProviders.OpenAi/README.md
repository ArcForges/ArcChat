# ArcChat.ModelProviders.OpenAi

Owns the OpenAI chat-completions provider for the NextChat rewrite.

- Streams `v1/chat/completions` SSE through `ServerSentEventReader`.
- Maps o-series and GPT-5 system instructions to `developer` messages.
- Sends `max_completion_tokens` for o-series and GPT-5 models.
- Supports chat vision payloads, function tools, reasoning deltas, and normalized chat errors.
