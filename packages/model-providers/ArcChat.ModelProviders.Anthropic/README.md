# ArcChat.ModelProviders.Anthropic

Owns the Anthropic Messages API provider for the NextChat rewrite.

- Streams `v1/messages` SSE through `ServerSentEventReader`.
- Sends system prompts through the top-level Anthropic `system` field.
- Supports `tool_use` / `tool_result` content blocks, vision image sources, thinking deltas, and normalized chat errors.
- Adds explicit `cache_control` breakpoints for prompt caching.
