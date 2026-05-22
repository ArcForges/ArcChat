# NC02 Protocol And Shared Kernel Coverage

All dotnet/MSBuild validation commands are run serially with MSBuild node reuse disabled (`MSBUILDDISABLENODEREUSE=1`; `$env:MSBUILDDISABLENODEREUSE = "1"` on Windows PowerShell).

| Traceability row | Evidence added in NC02 | Validation command |
| --- | --- | --- |
| NC-PROTO-001 | `ArcChat.Protocol` chat DTOs, source-generated JSON context, NextChat conversation fixtures, and deterministic event replay. | `dotnet test packages/protocol-net/ArcChat.Protocol.Tests/ArcChat.Protocol.Tests.csproj -m:1 --no-restore /p:TreatWarningsAsErrors=true` |
| NC-PROTO-002 | Mask, plugin, MCP config/message/tool DTOs with fixture round-trip coverage. | `ProtocolRoundTripTests.MaskPluginMcpAndArtifactDtosRoundTrip` |
| NC-PROTO-003 | `HtmlArtifactPreview` and `ArcTool`; explicit test that generic artifact version/diff types stay out of the MVP protocol. | `ProtocolRoundTripTests.MvpProtocolDoesNotExposeGenericArtifactVersionOrDiffTypes` |
| NC-PROTO-004 | Provider/model DTOs preserve opaque provider-specific extra fields. | `ProtocolRoundTripTests.ProviderSettingsAndSyncDtosPreserveOpaqueExtraFields` |
| NC-PROTO-005 | Settings and sync snapshots preserve NextChat-compatible fields and `Extra` buckets. | `ProtocolRoundTripTests.ProviderSettingsAndSyncDtosPreserveOpaqueExtraFields` |
| NC-NET-001 | Named HttpClient profile factory and DI registration. | `NetCoreFactoryTests` |
| NC-NET-002 | PipeReader-backed SSE parser plus reconnect/Last-Event-ID source behavior. | `SseReaderTests` |
| NC-NET-003 | WebSocket session send/receive/heartbeat helpers and exponential reconnect policy. | `WebSocketSessionTests` |
| NC-NET-004 | HMAC-SHA256, Tencent TC3, Baidu IAM token, and iFlytek signed URL utilities. | `SigningTests` |
| NC-NET-005 | Retry-After parsing and awaitable token-bucket rate limiter. | `ResilienceAndErrorTests` |
| NC-NET-006 | Normalized `NetError` variants and HTTP/exception mapping. | `ResilienceAndErrorTests` |
| NC-CORE-008 | SQLite v1 migrations, WAL/shared-cache connection factory, single-writer queue, and conversation/message/settings repositories. | `dotnet test packages/local-persistence/ArcChat.LocalPersistence.Tests/ArcChat.LocalPersistence.Tests.csproj -m:1 --no-restore /p:TreatWarningsAsErrors=true` |

`NC-PROTO-SBE-*` remains deferred because NC08.F1 is not approved for the default MVP path.
