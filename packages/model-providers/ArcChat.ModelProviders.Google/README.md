# ArcChat.ModelProviders.Google

Owns the Google Gemini `streamGenerateContent` provider for the NextChat rewrite.

- Streams `v1beta/models/{model}:streamGenerateContent?alt=sse` through `ServerSentEventReader`.
- Maps system prompts to `systemInstruction` and Gemini chat roles to `user` / `model`.
- Supports inline image parts, function declarations / calls, function responses, safety settings, and normalized chat errors.
