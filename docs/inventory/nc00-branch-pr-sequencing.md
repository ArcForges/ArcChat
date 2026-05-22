# NC00 Branch, PR, And Traceability Sequencing

This file freezes implementation sequencing for NC01-NC17 by reading each C# step file's recommended branch section or, where the step file has no explicit section, by deriving branch names from the step title and substep IDs in the active plan.

## PR List

| Order | Branch | Concrete output | Depends on |
| --- | --- | --- | --- |
| NC01.00-01 | `codex/nc01-01-slnx-composite-build` | solution/workspace foundation | NC00 |
| NC01.02 | `codex/nc01-02-central-package-versions` | `Directory.Packages.props`, `global.json`, build props | NC01.00 |
| NC01.03 | `codex/nc01-03-architecture-tests` | forbidden-module/reference and dependency-direction tests | NC01.00 |
| NC01.04-05 | `codex/nc01-04-format-analyzers-ci` | analyzers, format, CI skeleton | NC01.02 |
| NC02.00-05 | `codex/nc02-00-protocol-net-dtos` | protocol DTOs and fixtures | NC01 |
| NC02.06-11 | `codex/nc02-02-net-core-sse` | net-core HTTP/SSE/WS/signing/retry | NC02 protocol basics |
| NC02.12-14 | `codex/nc02-03-local-persistence` | SQLite schema/repositories/migrations | NC02 DTOs |
| NC03.00 | `codex/nc03-00-theme-tokens` | theme tokens | NC01 |
| NC03.02 | `codex/nc03-02-desktop-shell-navigation` | desktop shell and navigation | NC03.00 |
| NC03.03 | `codex/nc03-03-locales` | locale conversion and coverage | NC03.02 |
| NC03.04-07 | `codex/nc03-04-settings-accessibility` | settings repository shell, shortcuts, accessibility | NC03.02 / NC02 persistence |
| NC04.01-03 | `codex/nc04-01-conversation-core` | chat list/detail/agent echo | NC02 / NC03 |
| NC04.04 | `codex/nc04-04-markdown-renderer` | streaming Markdown renderer | NC04.01 |
| NC04.05-08 | `codex/nc04-05-export-search-attachments` | selector/export/search/attachments/commands | NC04.01 |
| NC05.00-02 | `codex/nc05-00-provider-spi` | `IChatProvider`, registry, tokenizer | NC02 net-core |
| NC05.03 | `codex/nc05-03-openai-provider` | OpenAI provider fixture/contract | NC05.00 |
| NC05.04 | `codex/nc05-04-anthropic-provider` | Anthropic provider fixture/contract | NC05.00 |
| NC05.05 | `codex/nc05-05-google-provider` | Google provider fixture/contract | NC05.00 |
| NC05.06 | `codex/nc05-06-generic-openai-provider` | custom OpenAI-compatible provider | NC05.00 |
| NC06.00-12 | `codex/nc06-<provider>-provider` | one provider per PR for Alibaba, Baidu, ByteDance, DeepSeek, GLM, iFlytek, Moonshot, SiliconFlow, Stability, Tencent, xAI, 302.AI, Azure | NC05 |
| NC07.00-03 | `codex/nc07-00-masks-prompts` | masks and prompts | NC04 |
| NC07.04-06 | `codex/nc07-04-plugin-store-ui` | plugin store and OpenAPI runtime | NC08 tool-core surface where needed |
| NC07.07 | `codex/nc07-07-model-config-dialog` | model-config dialog | NC05 |
| NC08.00 | `codex/nc08-00-tool-runtime` | in-process tool registry, permission, audit | NC02 / NC04 |
| NC08.F1 | `codex/nc08-f1-optional-ipc-decision` | optional NetMQ/MessagePack decision only if approved | explicit approval |
| NC09.00-01 | `codex/nc09-01-mcp-client-adapter` | MCP stdio client and seed config | NC08 |
| NC09.02 | `codex/nc09-02-mcp-code-block-bridge` | NextChat MCP code-block bridge | NC09.01 / NC04 |
| NC10.00-04 | `codex/nc10-00-artifact-viewer` | HTML artifact preview/share and Mermaid | NC04 markdown |
| NC11 | `codex/nc11-realtime-audio-chat` | realtime SPI, engines, transcript/audio_url | NC05 / NC04 |
| NC12 | `codex/nc12-tts-and-voiceprint` | TTS SPI/engines/playback/voice-print | NC11 audio primitives where shared |
| NC13 | `codex/nc13-stable-diffusion-panel` | SD panel and image providers | NC05 SPI split |
| NC14 | `codex/nc14-sync-webdav-upstash` | sync engine/providers/settings | NC02 persistence |
| NC15 | `codex/nc15-settings-access-update-logs-keychain` | full settings/access/update/logs/keychain/export | NC03 / NC05 / NC14 |
| NC16 | `codex/nc16-packaging-singlefile-installer` | publish, Velopack, URL scheme, NOTICE bundling | NC03 resources and NC15 settings |
| NC17 | `codex/nc17-ci-perf-release` | CI expansion, perf, package smoke, release dry-run | NC16 |

## Package Landing Order

| Package/project | Must land before |
| --- | --- |
| `packages/protocol-net/ArcChat.Protocol` | net-core, providers, agent, tools, local services, persistence |
| `packages/net-core/ArcChat.Net` | every model provider, sync providers, HTTP integrations |
| `packages/local-persistence/ArcChat.LocalPersistence` | conversation/settings/sync/mask/prompt/plugin services |
| `packages/model-providers/ArcChat.ModelProviders.Core` | all provider implementations and agent provider calls |
| `packages/tool-core/ArcChat.Tools` | plugin runtime, MCP integration, agent tool loop |
| `packages/agent-core/ArcChat.Agent` | conversation streaming UI |
| `packages/local-services/ArcChat.LocalServices` | desktop view-model feature consumption |
| `desktop-shared/ArcChat.UI.Markdown` | conversation Markdown and artifact viewer |
| `desktop-shared/ArcChat.UI.ArtifactViewer` | artifact feature |

## Rollback Rules

| Rule | Enforcement |
| --- | --- |
| Public API additions require tests in same PR | Public API analyzer + unit/contract test |
| Provider additions require offline fixture + contract test in same PR | provider contract suite |
| UI features may use placeholder providers only when visible in tests and listed in traceability | UI test names placeholder and owner |
| Manual GUI verification must be documented and later automated or accepted as P1 | coverage doc row with owner/reason |
| Generated artifacts must be reproducible | two-run determinism target/test |

## Traceability Impact

NC00 confirms existing IDs rather than adding new code IDs: `NC-FW-008` owns the evidence package; `NC-FW-009` owns license posture; `NC-FEAT-024` owns settings schema coverage; `NC-FEAT-025` owns asset/locale inventory; `NC-PROV-001` through `NC-PROV-018` own provider rows; `NC-UI-003` owns locale loader. Later PRs must replace planned evidence with exact test names and commands.
