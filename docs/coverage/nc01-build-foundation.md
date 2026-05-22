# NC01 Build Foundation Coverage

| Traceability row | Evidence added in NC01 | Validation command | Final-gate impact |
| --- | --- | --- | --- |
| NC-FW-001 | `ArcChat.slnx` enumerates the MVP desktop, package, desktop-shared, integration, UI test, package test, and architecture test projects. | `dotnet sln ArcChat.slnx list` | FG.01 build foundation starts from a single solution. |
| NC-FW-002 | `Directory.Packages.props` centralizes package versions and architecture tests reject per-project package versions. | `dotnet test tests/ArcChat.Architecture.Tests/ArcChat.Architecture.Tests.csproj --filter CentralPackagingTests` | FG.01 dependency drift is gated. |
| NC-FW-003 | `tests/ArcChat.Architecture.Tests` enforces dependency-direction rules with NetArchTest and source/project scans. | `dotnet test tests/ArcChat.Architecture.Tests/ArcChat.Architecture.Tests.csproj` | FG.01 later feature work inherits dependency-direction checks. |
| NC-FW-004 | `.editorconfig`, StyleCop, Roslynator, Meziantou, Public API baselines, `dotnet format`, and the forbidden-pattern target are wired. | `dotnet format ArcChat.slnx --verify-no-changes`; `dotnet build ArcChat.slnx -warnaserror` | FG.10 style, analyzer, and API-surface gates are active. |
| NC-FW-005 | Forbidden module and forbidden reference tests reject server/web/API/OpenAPI/frontend-shared modules and forbidden package families. | `dotnet test tests/ArcChat.Architecture.Tests/ArcChat.Architecture.Tests.csproj --filter Forbidden` | FG.01 no server/web/API scope enters the rewrite foundation. |
| NC-FW-006 | `docs/deviations.md` records the NC01 license-source conflict; QmlSharp remains tooling-shape-only. | PR review | FG.01/FG.09 source-input and license posture stay tied to NC00 evidence. |
| NC-FW-007 | NC01 does not add API-route replacement behavior; it keeps the forbidden server/API modules absent so NC02 and later route mapping remains desktop-native. | `pwsh scripts/verify-forbidden-patterns.ps1` | FG.01 no new unmapped server replacement surface is introduced. |
| NC-FW-008 | NC01 consumes the completed NC00 evidence package and keeps NC00 inventory docs in place. | PR review | FG.01 NC00 evidence remains the baseline for later rewrite steps. |
| NC-FW-009 | `Directory.Packages.props` follows the NC00 version plan except the documented `Avalonia.Headless.XUnit` adapter deviation; no NetMQ/MessagePack/LibGit2Sharp default pins. | `dotnet restore ArcChat.slnx` | FG.09 NuGet/license posture is documented before dependency landing. |
| NC-CI-001 | CI skeleton runs restore, format, build, and tests on `windows-2025` and `ubuntu-24.04`. | Workflow review | FG.01 CI matrix foundation exists; CI must run on the opened PR. |
