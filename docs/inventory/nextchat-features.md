# NC00 NextChat Feature Inventory

Source commit: `C:\MyFile\DevAll\QmlSharp\NextChat` at `89b8f26ff8f03a0c5b98fc3026d980721495227e`.

This inventory is source evidence for Step NC00 only. It records source paths, target ArcChat projects, replacement strategy, and later owning step. NextChat TypeScript, TSX, and SCSS are behavior contracts only; none are vendored or transpiled.

## Feature Rows

| Feature | NextChat source path(s) | Runtime deps observed | Core/P1 | Target ArcChat project path | Replacement strategy | Acceptance summary | Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Shell, home, sidebar, chat list | `app/components/home.tsx`, `app/components/sidebar.tsx`, `app/components/chat-list.tsx`, `app/components/home.module.scss`, `app/components/chat.module.scss`, `app/constant.ts` | `react`, `react-router-dom`, `@hello-pangea/dnd`, `clsx`, `next` | Core | `apps/desktop/ArcChat.Desktop/Shell`, `apps/desktop/ArcChat.Desktop/Features/Conversation`, `desktop-shared/ArcChat.UI.Controls` | Avalonia two-pane shell, virtualized conversation list, native drag-drop reorder, route service replacing React Router. | Empty/populated/active/pinned/reordered/sidebar states render in Avalonia screenshots and UI tests. | NC03.02 / NC04.01 |
| Navigation routes | `app/constant.ts`, `app/page.tsx`, `app/command.ts` | `react-router-dom` | Core | `apps/desktop/ArcChat.Desktop/Navigation` | Replace hash routes with `AppNavigator` destinations for home, chat, settings, new-chat, masks, plugins, auth, sd, sd-new, artifacts, search-chat, and mcp-market. | Navigation tests prove every `Path` destination reaches the expected Avalonia view. | NC03.02 |
| Chat detail, composer, streaming, abort, retry, edit, branch | `app/components/chat.tsx`, `app/store/chat.ts`, `app/client/controller.ts`, `app/utils/chat.ts`, `app/utils/token.ts`, `app/command.ts` | `lodash-es`, `markdown-to-txt`, `use-debounce`, `nanoid`, `@fortaine/fetch-event-source` | Core | `apps/desktop/ArcChat.Desktop/Features/Conversation`, `packages/agent-core/ArcChat.Agent`, `packages/local-services/ArcChat.LocalServices` | MVVM view-models over `ConversationService`; streaming events from `ArcChat.Agent`; colon commands parse to commands. | UI tests cover stream/abort/retry/edit/fork/delete/clear and parser tests cover `:new`, `:newm`, `:prev`, `:next`, `:clear`, `:fork`, `:del`. | NC04.02 / NC04.03 / NC04.08 |
| Markdown rendering | `app/components/markdown.tsx`, `app/styles/markdown.scss`, `app/styles/highlight.scss` | `react-markdown`, `remark-gfm`, `remark-breaks`, `remark-math`, `rehype-katex`, `rehype-highlight`, `mermaid`, `use-debounce`, `clsx` | Core | `desktop-shared/ArcChat.UI.Markdown`, `desktop-shared/ArcChat.UI.ArtifactViewer` | Markdig pipeline plus Avalonia renderer; ColorCode for code; typed math fallback; Mermaid placeholder/default or WebView variant only in shared viewer/markdown modules. | Replay-delta snapshot tests cover partial code, math, tables, Mermaid, links, copy-block. | NC04.04 / NC10 |
| Message selector, exporter, ShareGPT share | `app/components/message-selector.tsx`, `app/components/exporter.tsx`, `app/client/api.ts`, `app/utils.ts` | `html-to-image`, `react`, `clsx`, `next` | Core | `apps/desktop/ArcChat.Desktop/Features/Conversation` | Avalonia selection dialog, JSON/Markdown exporters, `RenderTargetBitmap` image capture, ShareGPT-compatible request shape. | Golden format tests and UI tests prove select/export/share flow. | NC04.05 / NC15.09 / NC16.04 |
| Search chat | `app/components/search-chat.tsx`, `app/store/chat.ts`, `app/utils/chat.ts` | `react`, `react-router-dom` | Core | `apps/desktop/ArcChat.Desktop/Features/Conversation/Search` | SQLite-backed search through `ConversationService`; no browser routing. | Search UI returns matching messages and navigates to conversation. | NC04.05 |
| New chat and mask splash | `app/components/new-chat.tsx`, `app/store/mask.ts`, `app/masks/index.ts`, `app/masks/{en,cn,tw}.ts` | `react`, `react-router-dom`, `clsx`, `nanoid` | Core | `apps/desktop/ArcChat.Desktop/Features/Masks`, `Resources/Seed/Masks` | Convert mask seeds to JSON, render mask picker, persist "do not show again". | New-chat flow can start blank or from a selected builtin mask. | NC04.07 / NC07.00 |
| Mask CRUD, import, export, reorder | `app/components/mask.tsx`, `app/components/mask.module.scss`, `app/store/mask.ts`, `app/masks/*` | `@hello-pangea/dnd`, `react`, `react-router-dom`, `clsx`, `nanoid` | Core | `apps/desktop/ArcChat.Desktop/Features/Masks`, `packages/local-services/ArcChat.LocalServices` | Mask records in SQLite/settings repository; Avalonia editor and import/export. | Builtin/user masks round-trip with NextChat-compatible JSON. | NC07.01 / NC07.02 |
| Prompt store and slash picker | `app/store/prompt.ts`, `public/prompts.json`, `app/components/settings.tsx`, `app/components/chat.tsx` | `fuse.js`, `nanoid` | Core | `packages/local-services/ArcChat.LocalServices/Prompts`, `apps/desktop/ArcChat.Desktop/Resources/Seed/Prompts.json` | Prompt seed is vendored as data; small C# fuzzy matcher replaces Fuse.js. | Slash picker finds builtin and user prompts within the performance target. | NC07.03 |
| Plugin store and OpenAPI plugin calls | `app/components/plugin.tsx`, `app/store/plugin.ts`, `public/plugins.json`, `app/api/proxy.ts` | `openapi-client-axios`, `axios`, `zod`, `nanoid`, `use-debounce`, `clsx` | Core | `apps/desktop/ArcChat.Desktop/Features/Plugins`, `packages/tool-core/ArcChat.Tools` | `Microsoft.OpenApi.Readers` plus `OpenApiPluginInvoker`; no ArcChat proxy server. | Plugin install/enable/call test uses a mock HTTP server and permission gate. | NC07.04 / NC07.05 / NC07.06 |
| MCP market, stdio client, prompt bridge | `app/components/mcp-market.tsx`, `app/mcp/client.ts`, `app/mcp/actions.ts`, `app/mcp/types.ts`, `app/mcp/utils.ts`, `app/mcp/mcp_config.default.json`, `app/store/chat.ts`, `app/constant.ts` | `@modelcontextprotocol/sdk`, `zod`, `clsx`, `react-router-dom` | Core | `apps/desktop/ArcChat.Desktop/Features/Mcp`, `integrations/ArcChat.Integrations.Mcp`, `Resources/Seed/McpConfig.default.json` | C# stdio MCP client; preserve `json:mcp:<clientId>` and `json:mcp-response` code-block bridge. | Echo MCP fixture starts, lists tools, executes, and injects response blocks. | NC09.00 / NC09.02 |
| Artifacts viewer and local share | `app/components/artifacts.tsx`, `app/api/artifacts/route.ts`, `app/components/markdown.tsx` | `nanoid`, `spark-md5`, `react`, `next` | Core | `desktop-shared/ArcChat.UI.ArtifactViewer`, `packages/local-services/ArcChat.LocalServices/Artifacts` | Local HTML artifact preview/share store; no Cloudflare/server artifact route in MVP. | HTML preview/share round-trip and Mermaid/default placeholder tests pass. | NC10 |
| Settings | `app/components/settings.tsx`, `app/components/settings.module.scss`, `app/store/config.ts`, `app/store/access.ts`, `app/store/sync.ts`, `app/store/update.ts` | `react`, `react-router-dom`, `nanoid`, `next` | Core | `apps/desktop/ArcChat.Desktop/Features/Settings`, `packages/local-services/ArcChat.LocalServices/Settings` | AXAML settings dialog over typed settings records and repository; secrets migrate to keychain refs in NC15. | Every field listed in `docs/coverage/settings-schema.md` round-trips or has a cut reason. | NC03 / NC15 |
| Model config dialog | `app/components/model-config.tsx`, `app/store/config.ts`, `app/utils/model.ts`, `app/constant.ts` | `lodash-es` | Core | `apps/desktop/ArcChat.Desktop/Features/Settings` | Per-conversation model override editor bound to typed `ModelConfig`. | Field-level binding tests cover model, provider, temperature, top-p, penalties, max tokens, image settings. | NC07.07 |
| Auth/access-control | `app/components/auth.tsx`, `app/store/access.ts`, `app/api/auth.ts`, `app/utils/auth-settings-events.ts` | `spark-md5`, `@next/third-parties`, `react-router-dom`, `clsx` | Core | `apps/desktop/ArcChat.Desktop/Features/Auth`, `packages/local-services/ArcChat.LocalServices/Access` | Normal desktop direct-provider mode bypasses server gate; external proxy mode keeps access-code behavior. | Access-code gate and bypass state tests pass; server config route is absent. | NC15.05 |
| Sync: WebDAV, Upstash, import/export | `app/store/sync.ts`, `app/utils/sync.ts`, `app/utils/cloud/index.ts`, `app/utils/cloud/webdav.ts`, `app/utils/cloud/upstash.ts`, `app/api/webdav/[...path]/route.ts`, `app/api/upstash/[action]/[...key]/route.ts` | `idb-keyval`, `zustand`, `react` | Core | `packages/local-services/ArcChat.LocalServices/Sync`, `packages/local-persistence/ArcChat.LocalPersistence`, `apps/desktop/ArcChat.Desktop/Features/Sync` | Direct WebDAV/Upstash clients with NextChat-compatible merge-then-writeback snapshot semantics. | Mock WebDAV/Upstash round-trips a NextChat export fixture. | NC14 |
| Update notifier and usage query | `app/store/update.ts`, `app/components/settings.tsx`, `app/api/openai.ts`, `app/constant.ts` | `next`, `spark-md5` | Core | `apps/desktop/ArcChat.Desktop/Features/Settings`, `packages/local-services/ArcChat.LocalServices/Update` | Passive update repository and banner; usage query through provider/service, no Next.js route. | Forced-out-of-date scenario renders banner; skip/check state persists. | NC15.06 |
| TTS config and playback | `app/components/tts-config.tsx`, `app/components/tts.module.scss`, `app/utils/ms_edge_tts.ts`, `app/store/config.ts`, `app/store/access.ts` | `axios`, `rt-client` | Core | `apps/desktop/ArcChat.Desktop/Features/Tts`, `packages/local-services/ArcChat.LocalServices/Tts` | `ITtsEngine` split from chat provider; Edge/OpenAI/Azure engines; playback through platform audio services. | Fixture smoke plays/stops sample and settings round-trip. | NC12 |
| Realtime chat | `app/components/realtime-chat/realtime-chat.tsx`, `app/components/realtime-chat/realtime-config.tsx`, `app/lib/audio.ts`, `app/store/config.ts`, `app/store/chat.ts` | `rt-client`, `react`, `clsx` | P1 extension for MVP parity | `apps/desktop/ArcChat.Desktop/Features/Realtime`, provider `IRealtimeEngine` projects | `ClientWebSocket` realtime engine plus platform PCM capture/playback; WebRTC deferred. | Mocked realtime WS stores transcript and `audio_url` metadata. | NC11 |
| Voice print | `app/components/voice-print/voice-print.tsx`, `app/components/voice-print/voice-print.module.scss` | `react` | P1 extension for MVP parity | `apps/desktop/ArcChat.Desktop/Features/Tts`, `Features/Realtime` | Avalonia waveform control fed by audio amplitude samples. | Waveform snapshot is deterministic under headless tests. | NC11 / NC12 |
| Stable Diffusion panel | `app/components/sd/sd.tsx`, `app/components/sd/sd-panel.tsx`, `app/components/sd/sd-sidebar.tsx`, `app/store/sd.ts`, `app/api/stability.ts`, `app/typing.ts` | `react`, `react-router-dom`, `clsx`, `next`, `nanoid` | P1 extension for MVP parity | `apps/desktop/ArcChat.Desktop/Features/Sd`, `packages/model-providers/ArcChat.ModelProviders.Stability` | `IImageGenProvider` for Stability/OpenAI/SD-WebUI-compatible providers; local gallery/history. | Mock image HTTP fixture produces history item and drag-into-conversation works. | NC13 |
| Image attach, paste, HEIC, clipboard | `app/components/chat.tsx`, `app/utils/chat.ts`, `app/typing.ts` | `heic2any`, `@fortaine/fetch-event-source`, `nanoid` | Core | `apps/desktop/ArcChat.Desktop/Features/Conversation`, platform services | File picker/clipboard services plus `Magick.NET-Q8-AnyCPU` HEIC conversion; max image and vision gating preserved. | Attachment fixture suite covers max 3 images, HEIC, compression/cache fallback. | NC04.06 |
| Emoji picker and avatars | `app/components/emoji.tsx`, `app/store/config.ts`, `app/store/mask.ts` | `emoji-picker-react` | Core | `desktop-shared/ArcChat.UI.Controls/Emoji` | Custom Avalonia emoji picker using Unicode CLDR data. | Picker selects avatar and persists setting/mask avatar. | NC04 / NC07 |
| Cloud icons and connection state | `app/icons/cloud-success.svg`, `app/icons/cloud-fail.svg`, `app/icons/connection.svg`, `app/components/settings.tsx`, `app/store/sync.ts` | none beyond settings deps | Core | `apps/desktop/ArcChat.Desktop/Resources/Icons`, `Features/Sync` | Vendor icons with NOTICE; sync view state icons from typed sync status. | Sync success/failure states have light/dark visual captures. | NC03.06 / NC14 |
| Locale loader | `app/locales/*.ts`, `app/locales/index.ts` | none; TypeScript source converted, not vendored | Core | `apps/desktop/ArcChat.Desktop/Resources/Locales`, `scripts/convert-locales.ps1` | Convert 20 bundles to JSON; `index.ts` is a locale selector/barrel and is dropped. | Locale coverage report proves fallback to English for missing keys. | NC03.03 |
| Icon and seed assets | `app/icons/**/*`, `public/prompts.json`, `public/plugins.json`, `public/masks.json`, `app/masks/{en,cn,tw}.ts`, `app/mcp/mcp_config.default.json` | asset/data only | Core | `apps/desktop/ArcChat.Desktop/Resources/{Icons,Seed,Locales}`, `Resources/NOTICE.md` | Vendor SVG/PNG/JSON data only; convert TS mask/locale data to JSON; add NOTICE row for each copied file. | NOTICE integrity target hashes every vendored file. | NC03.06 / NC07 / NC09 |
| Next.js API route deletion/replacement | `app/api/**/*`, `app/client/api.ts`, `app/client/platforms/*` | `next`, `spark-md5` | Core | `packages/net-core`, `packages/model-providers/*`, `packages/local-services/*` | Every route maps to direct provider/local service or deletion; no ArcChat-owned HTTP proxy. | Forbidden-module/reference tests pass and no route remains unmapped. | NC00.00 / NC02 / NC05-NC15 |
| Tauri packaging replacement | `src-tauri/**/*`, `public/*`, `app/utils.ts`, `app/store/update.ts` | `@tauri-apps/api` dev/runtime package, `@tauri-apps/cli` dev package | Core packaging | `apps/desktop/ArcChat.Desktop`, `Resources/Velopack`, `scripts/package-desktop.ps1` | Avalonia desktop plus `dotnet publish --self-contained` and Velopack; no Tauri runtime. | Package smoke produces installer and ships NOTICE. | NC16 |

## Provider Rows

| Provider | Provider id | Endpoint/path evidence | Streaming style | Vision | Tools/function calls | System prompt / max-token behavior | Target | Owner |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| OpenAI | `openai` | `app/client/platforms/openai.ts`, `app/api/openai.ts`, `OpenaiPath.ChatPath`, `OpenaiPath.ImagePath` | OpenAI-compatible SSE via `streamWithThink` | Yes via `isVisionModel` and `image_url` | Yes, `tool_calls` | o-series/GPT-5 use `max_completion_tokens`; vision max at least 4000; system/developer role quirks preserved. | `packages/model-providers/ArcChat.ModelProviders.OpenAi` | NC05.03 / NC13.03 |
| Azure OpenAI | `azure` | `app/client/platforms/openai.ts`, `app/api/azure.ts`, `Azure.ChatPath(deployName, apiVersion)` | OpenAI-compatible SSE | Same as deployment | Same as OpenAI where supported | Deployment path and `api-version`; same token rules as OpenAI. | `packages/model-providers/ArcChat.ModelProviders.Azure` | NC06.12 |
| Anthropic | `anthropic` | `app/client/platforms/anthropic.ts`, `app/api/anthropic.ts`, `Anthropic.ChatPath` | Anthropic SSE | Yes, multimodal preprocessing | Yes, `tool_use` / `tool_result` | System prompt separated; `max_tokens` required; alternating role quirk. | `packages/model-providers/ArcChat.ModelProviders.Anthropic` | NC05.04 |
| Google Gemini | `google` | `app/client/platforms/google.ts`, `app/api/google.ts`, `Google.ChatPath(model)` | `streamGenerateContent` | Yes, inline image parts | Yes, function declarations/calls | System messages converted to Gemini-compatible roles; `maxOutputTokens`. | `packages/model-providers/ArcChat.ModelProviders.Google` | NC05.05 |
| Alibaba Qwen | `alibaba` | `app/client/platforms/alibaba.ts`, `app/api/alibaba.ts`, `Alibaba.ChatPath(model)` | DashScope incremental stream | Yes for `vl` / `omni` names | Yes where DashScope tools are passed | Text vs multimodal path selected by model name. | `packages/model-providers/ArcChat.ModelProviders.Alibaba` | NC06.00 |
| Baidu ERNIE | `baidu` | `app/client/platforms/baidu.ts`, `app/api/baidu.ts`, `app/utils/baidu.ts`, `Baidu.ChatPath(model)` | Chunked/event stream | Not explicit in current source | Not explicit in current source | IAM token and endpoint slug remaps. | `packages/model-providers/ArcChat.ModelProviders.Baidu` | NC06.01 |
| ByteDance Doubao | `bytedance` | `app/client/platforms/bytedance.ts`, `app/api/bytedance.ts`, `ByteDance.ChatPath` | OpenAI-compatible SSE | Uses preprocessing path; no dedicated source row | OpenAI-compatible where passed | Ark endpoint and model casing. | `packages/model-providers/ArcChat.ModelProviders.ByteDance` | NC06.02 |
| DeepSeek | `deepseek` | `app/client/platforms/deepseek.ts`, `app/api/deepseek.ts`, `DeepSeek.ChatPath` | OpenAI-compatible SSE with reasoning | Not explicit in current source | OpenAI-compatible where passed | First non-system message must be user; `reasoning_content` surfaced; max token intentionally not sent. | `packages/model-providers/ArcChat.ModelProviders.DeepSeek` | NC06.03 |
| ChatGLM | `chatglm` | `app/client/platforms/glm.ts`, `app/api/glm.ts`, `ChatGLM.ChatPath/ImagePath/VideoPath` | SSE | Yes for `glm-4v*` names | Yes where `tool_calls` mapped | JWT/API-key behavior; image/video paths recorded but split by SPI. | `packages/model-providers/ArcChat.ModelProviders.Glm` | NC06.04 |
| iFlytek Spark | `iflytek` | `app/client/platforms/iflytek.ts`, `app/api/iflytek.ts`, `app/utils/hmac.ts`, `Iflytek.ChatPath` | Fetch-event-source/WebSocket-style handling | Not explicit in current source | Not explicit in current source | HMAC signed URL/API secret behavior. | `packages/model-providers/ArcChat.ModelProviders.Iflytek` | NC06.05 |
| Moonshot | `moonshot` | `app/client/platforms/moonshot.ts`, `app/api/moonshot.ts`, `Moonshot.ChatPath` | OpenAI-compatible SSE | Yes for vision preview names | Yes, OpenAI-compatible `tool_calls` | Max token not sent. | `packages/model-providers/ArcChat.ModelProviders.Moonshot` | NC06.06 |
| SiliconFlow | `siliconflow` | `app/client/platforms/siliconflow.ts`, `app/api/siliconflow.ts`, `SiliconFlow.ChatPath/ListModelPath` | OpenAI-compatible SSE with reasoning | Global vision detection | Yes, OpenAI-compatible `tool_calls` | Reasoning and custom model-list behavior; max token not sent. | `packages/model-providers/ArcChat.ModelProviders.SiliconFlow` | NC06.07 |
| Stability | `stability` | `app/api/stability.ts`, `app/store/sd.ts`, `app/components/sd/*`, `Stability.GeneratePath` | HTTP image generation | Image output, not chat vision | No chat tools | Image-generation SPI only. | `packages/model-providers/ArcChat.ModelProviders.Stability` | NC06.08 / NC13 |
| Tencent Hunyuan | `tencent` | `app/client/platforms/tencent.ts`, `app/api/tencent/route.ts`, `app/utils/tencent.ts` | Provider stream flag | Global vision detection for Hunyuan vision | Limited to provider source | System role must be first; TC3 signing. | `packages/model-providers/ArcChat.ModelProviders.Tencent` | NC06.09 |
| xAI | `xai` | `app/client/platforms/xai.ts`, `app/api/xai.ts`, `XAI.ChatPath` | OpenAI-compatible SSE | Yes for Grok vision names | Yes, OpenAI-compatible `tool_calls` | Own base URL/model list; OpenAI-compatible behavior. | `packages/model-providers/ArcChat.ModelProviders.Xai` | NC06.10 |
| 302.AI | `ai302` | `app/client/platforms/ai302.ts`, `app/api/302ai.ts`, `AI302.ChatPath/EmbeddingsPath/ListModelPath` | OpenAI-compatible SSE with reasoning | Global vision detection | Yes, OpenAI-compatible `tool_calls` | Aggregator headers and model listing; max token not sent. | `packages/model-providers/ArcChat.ModelProviders.Ai302` | NC06.11 |
| Generic OpenAI-compatible | `custom-openai` | `app/client/api.ts`, `app/utils/model.ts`, `app/store/config.ts`, `app/store/access.ts` | OpenAI-compatible SSE | By custom model name regex | By endpoint support | Custom model merge/sort and endpoint availability. | `packages/model-providers/ArcChat.ModelProviders.GenericOpenAi` | NC05.06 |

## DEFAULT_MODELS Counts

Extracted with `tsx` from `app/constant.ts`: 239 default model rows total. Provider counts: OpenAI 38, Azure 38, Google 20, Anthropic 16, Baidu 11, ByteDance 6, Alibaba 10, Tencent 7, Moonshot 10, Iflytek 5, XAI 25, ChatGLM 14, DeepSeek 3, SiliconFlow 14, 302.AI 22. Stability is image-provider only in the default model table.

## Locale Rows

`index.ts` is not a locale bundle. Completeness is a key-path comparison against `en.ts` using TypeScript AST inspection; function-valued leaves count as keys.

| Locale | Source | Bytes | Key count | Completeness vs `en.ts` | Target | Owner |
| --- | --- | ---: | ---: | ---: | --- | --- |
| `ar` | `app/locales/ar.ts` | 23503 | 307 | 64.8% | `Resources/Locales/ar.json` | NC03.03 |
| `bn` | `app/locales/bn.ts` | 33747 | 307 | 64.8% | `Resources/Locales/bn.json` | NC03.03 |
| `cn` | `app/locales/cn.ts` | 25729 | 474 | 100.0% | `Resources/Locales/cn.json` | NC03.03 |
| `cs` | `app/locales/cs.ts` | 20075 | 307 | 64.8% | `Resources/Locales/cs.json` | NC03.03 |
| `da` | `app/locales/da.ts` | 24224 | 469 | 98.9% | `Resources/Locales/da.json` | NC03.03 |
| `de` | `app/locales/de.ts` | 21464 | 307 | 64.8% | `Resources/Locales/de.json` | NC03.03 |
| `en` | `app/locales/en.ts` | 25390 | 474 | 100.0% | `Resources/Locales/en.json` | NC03.03 |
| `es` | `app/locales/es.ts` | 21115 | 307 | 64.8% | `Resources/Locales/es.json` | NC03.03 |
| `fr` | `app/locales/fr.ts` | 21610 | 307 | 64.8% | `Resources/Locales/fr.json` | NC03.03 |
| `id` | `app/locales/id.ts` | 19553 | 307 | 64.8% | `Resources/Locales/id.json` | NC03.03 |
| `it` | `app/locales/it.ts` | 20995 | 307 | 64.8% | `Resources/Locales/it.json` | NC03.03 |
| `jp` | `app/locales/jp.ts` | 22273 | 308 | 65.0% | `Resources/Locales/jp.json` | NC03.03 |
| `ko` | `app/locales/ko.ts` | 28400 | 470 | 99.2% | `Resources/Locales/ko.json` | NC03.03 |
| `no` | `app/locales/no.ts` | 19808 | 307 | 64.8% | `Resources/Locales/no.json` | NC03.03 |
| `pt` | `app/locales/pt.ts` | 17356 | 279 | 58.9% | `Resources/Locales/pt.json` | NC03.03 |
| `ru` | `app/locales/ru.ts` | 27390 | 307 | 64.8% | `Resources/Locales/ru.json` | NC03.03 |
| `sk` | `app/locales/sk.ts` | 17987 | 286 | 60.3% | `Resources/Locales/sk.json` | NC03.03 |
| `tr` | `app/locales/tr.ts` | 20525 | 307 | 64.8% | `Resources/Locales/tr.json` | NC03.03 |
| `tw` | `app/locales/tw.ts` | 17945 | 294 | 62.0% | `Resources/Locales/tw.json` | NC03.03 |
| `vi` | `app/locales/vi.ts` | 22387 | 307 | 64.8% | `Resources/Locales/vi.json` | NC03.03 |

## Runtime Dependency Coverage

| Dependency | Covered by inventory row(s) or skipped reason |
| --- | --- |
| `@fortaine/fetch-event-source` | Chat streaming, provider platform files, net-core SSE replacement. |
| `@hello-pangea/dnd` | Chat list and mask drag-drop; replaced by Avalonia native drag-drop. |
| `@modelcontextprotocol/sdk` | MCP market/client; replaced by C# stdio MCP client. |
| `@next/third-parties` | Auth/settings analytics helper; skipped for MVP behavior, no analytics dependency. |
| `@svgr/webpack` | Icon pipeline; replaced by Avalonia resource/source-generator plan. |
| `@vercel/analytics` | Browser analytics only; skipped for desktop MVP. |
| `@vercel/speed-insights` | Browser analytics only; skipped for desktop MVP. |
| `axios` | Plugin proxy and Edge TTS helper; replaced by `HttpClient` and provider/service abstractions. |
| `clsx` | UI class concatenation; replaced by Avalonia classes/styles. |
| `emoji-picker-react` | Emoji picker; replaced by custom Avalonia control. |
| `fuse.js` | Prompt fuzzy search; replaced by small C# fuzzy matcher. |
| `heic2any` | HEIC conversion; replaced by `Magick.NET-Q8-AnyCPU`. |
| `html-to-image` | Export image; replaced by Avalonia `RenderTargetBitmap`. |
| `idb-keyval` | Browser persistence; replaced by SQLite/Dapper.AOT repositories. |
| `lodash-es` | Utility functions in model/settings/provider code; replaced by LINQ/local helpers. |
| `markdown-to-txt` | Export/plain text; replaced by Markdig text renderer. |
| `mermaid` | Mermaid diagrams; replaced by artifact/markdown viewer WebView variant or placeholder. |
| `nanoid` | IDs; replaced by `Guid.CreateVersion7()` plus NanoID-shaped helper where export compatibility needs it. |
| `next` | App/router/API/browser shell; replaced by Avalonia desktop and deleted API routes. |
| `node-fetch` | HTTP helper; replaced by `HttpClient`. |
| `openapi-client-axios` | Plugin OpenAPI runtime; replaced by `Microsoft.OpenApi.Readers` invoker. |
| `react` | UI framework; replaced by Avalonia/AXAML/MVVM. |
| `react-dom` | UI renderer; replaced by Avalonia desktop lifetime. |
| `react-markdown` | Markdown rendering; replaced by Markdig/Avalonia renderer. |
| `react-router-dom` | Routes; replaced by `AppNavigator`. |
| `rehype-highlight` | Code highlighting; replaced by ColorCode. |
| `rehype-katex` | Math rendering; replaced by typed fallback/WebView KaTeX variant. |
| `remark-breaks` | Markdown breaks; Markdig pipeline option. |
| `remark-gfm` | GFM tables/task/list behavior; Markdig extensions. |
| `remark-math` | Math parsing; Markdig extension/custom block parser. |
| `rt-client` | Realtime audio; replaced by `IRealtimeEngine` using WebSocket/audio platform services. |
| `sass` | SCSS build; replaced by Avalonia theme `ResourceDictionary`. |
| `spark-md5` | Auth/artifact hashing; replaced by `System.Security.Cryptography.MD5`. |
| `use-debounce` | UI debounce; replaced by dispatcher-aware debouncer or Rx throttle. |
| `zod` | MCP/runtime schemas; replaced by `System.Text.Json`, validation, and typed records. |
| `zustand` | Stores; replaced by `CommunityToolkit.Mvvm`, repositories, and services. |

## Support Matrix Consistency

- API routes: `no-server-api-replacement-matrix.md` already owns all 24 files under `app/api/**/*`; the verifier checks every listed route path still exists in NextChat.
- Visual states: `visual-parity-baseline.md` covers every visible row above; UI rows reference NC03-NC15 owners.
- Providers: `provider-parity-matrix.md` covers the provider table above plus `DEFAULT_MODELS` and TTS defaults.
- Settings: `settings-schema-migration-matrix.md` delegates field-level rows to `docs/coverage/settings-schema.md`, created in this PR.
- Assets/locales: `asset-locale-inventory.md` is reflected by the asset and locale rows above plus `docs/inventory/nextchat-public.md`.
- Traceability: existing `NC-FW-008`, `NC-FW-009`, `NC-FEAT-*`, `NC-PROV-*`, and `NC-UI-003` rows own this inventory; NC00 evidence is recorded here before NC01 begins.
