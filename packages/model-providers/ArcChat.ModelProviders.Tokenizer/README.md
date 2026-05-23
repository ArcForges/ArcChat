# ArcChat.ModelProviders.Tokenizer

Owns the tokenizer foundation for the NextChat rewrite.

- OpenAI-family models use SharpToken with `cl100k_base` and `o200k_base`.
- Anthropic and Google Gemini models use the NextChat-compatible UTF-16 character estimator from `app/utils/token.ts`.
