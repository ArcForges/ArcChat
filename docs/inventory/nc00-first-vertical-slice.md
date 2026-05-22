# NC00 First ArcChat Vertical Slice

This file freezes the first runnable C# / Avalonia rewrite slice before NC01 starts. It is intentionally small enough for the NC01-NC04 head substeps and strong enough to prove desktop, persistence, provider, agent, settings, locale, and architecture boundaries end to end.

## Included In The First Slice

| Slice item | Target path | Later step | Test owner | Acceptance evidence planned |
| --- | --- | --- | --- | --- |
| Avalonia desktop boots a two-pane window with sidebar and chat detail | `apps/desktop/ArcChat.Desktop`, `desktop-shared/ArcChat.UI.Theme`, `desktop-shared/ArcChat.UI.Controls` | NC03.00 / NC03.02 | `apps/desktop/ArcChat.Desktop.UiTests` | Headless boot/screenshot; `NC-UI-001`, `NC-UI-002` |
| Theme tokens applied to shell | `desktop-shared/ArcChat.UI.Theme` | NC03.00 | `ArcChat.UI.Theme.Tests` | token and color parity tests; `NC-UI-002` |
| Sidebar lists conversations from persistence | `packages/local-persistence/ArcChat.LocalPersistence`, `packages/local-services/ArcChat.LocalServices`, `apps/desktop/.../Conversation` | NC02.12 / NC03.04 / NC04.01 | persistence + UI tests | repository contract and chat-list UI test; `NC-FEAT-001` |
| Chat detail renders persisted user/assistant messages | same as above | NC04.02 | UI tests | message bubble render test; `NC-FEAT-002` |
| One in-memory `IChatProvider` returns streamed echo response | `packages/model-providers/ArcChat.ModelProviders.Core`, `packages/agent-core/ArcChat.Agent` | NC04.00 / NC05.00 | agent/provider unit tests | fake provider streaming test; `NC-CORE-001`, `NC-CORE-003` |
| `ArcChat.Agent` tool loop runs against echo provider with zero tools | `packages/agent-core/ArcChat.Agent`, `packages/tool-core/ArcChat.Tools` | NC04.00 / NC08.00 | agent unit tests | zero-tool loop emits complete `ChatEvent`; `NC-CORE-001`, `NC-CORE-002` |
| Settings dialog edits active model id and persists it | `apps/desktop/ArcChat.Desktop/Features/Settings`, `packages/local-services/.../Settings` | NC03.04 / NC15.00 | settings repository + UI tests | round-trip active model id; `NC-FEAT-009`, `NC-FEAT-024` |
| One locale (`en`) loads from resources | `apps/desktop/ArcChat.Desktop/Resources/Locales/en.json` | NC03.03 | locale unit test | English bundle load/fallback test; `NC-UI-003` |
| Dependency-direction and forbidden-module/reference tests are wired | `tests/ArcChat.Architecture.Tests` | NC01.03 | architecture tests | forbids server/web/API/protocol-openapi/frontend-shared and forbidden packages; `NC-FW-003`, `NC-FW-005` |

## Deferred To Later Steps

| Deferred item | Owning step | Reason |
| --- | --- | --- |
| Real model providers | NC05 / NC06 | provider SPI and offline fixture contract tests must land first |
| Masks, plugins, MCP, SD, realtime, TTS | NC07 / NC09 / NC11 / NC12 / NC13 | feature-specific stores, providers, and UI flows are too broad for first smoke |
| Artifacts | NC10 | shared artifact viewer and Markdown/WebView decision are separate |
| Sync | NC14 | requires app-state fixture and merge-then-writeback tests |
| Full export/share | NC04 / NC15 / NC16 | image capture/share needs conversation feature and packaging/deep-link decisions |
| 20-language locale loader | NC03.03 | first slice uses `en`; full coverage report follows in same shell/theme wave |
| Keychain-backed secrets | NC15.08 | provider settings need secure storage migration plan first |
| Installer packaging | NC16 | after runnable desktop and resources exist |

## Explicitly Out Of Scope

| Cut item | Decision |
| --- | --- |
| ABP server / `apps/server` | Never part of this NextChat desktop rewrite plan |
| Blazor / `apps/web` / `packages/protocol-openapi` / `frontend-shared/` | Future separate product-introduction/server plan only |
| Server-mediated desktop-provider proxy | Desktop calls providers directly through providers/net-core with keys held by `KeychainStore` |
| Tauri runtime | Replaced by Avalonia 12, `dotnet publish --self-contained`, and Velopack |
| NetMQ, broker, MessagePack IPC, native tool-host | Deferred unless NC08.F1 is explicitly approved |

## PR Size Check

The slice can fit in five or fewer early PRs:

| PR | Output |
| --- | --- |
| NC01 foundation | `.slnx`, CPM, global build props, analyzers, architecture tests |
| NC02 persistence/protocol | DTOs, SQLite schema, repository basics |
| NC03 shell/theme/settings/locale | bootable Avalonia shell, theme, English locale, settings repository |
| NC04 conversation/agent echo | chat list/detail, echo provider stream, zero-tool loop, basic message rendering |
| NC05 SPI cleanup if needed | formal `IChatProvider` registry before real providers |

No future implementer should need to guess first-slice scope: every item above names a target path, owner step, and test owner.
