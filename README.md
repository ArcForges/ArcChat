# ArcChat

ArcChat is the C# / .NET 10 / Avalonia 12 desktop rewrite of NextChat. The active implementation plan lives in `C:\MyFile\DevAll\QmlSharp\ArchitectureDesign\NextChatRewrite-CSharp`.

## Repository Layout

- `apps/desktop/ArcChat.Desktop`: Avalonia desktop executable.
- `packages/*`: protocol, transport, provider, agent, tool, persistence, and local-service foundations.
- `desktop-shared/*`: reusable Avalonia UI, Markdown, artifact-viewer, and theme projects.
- `integrations/ArcChat.Integrations.Mcp`: MCP integration foundation.
- `tests/ArcChat.Architecture.Tests`: dependency-direction, forbidden-module, and build-layout tests.

The NextChat rewrite is client-only. This repository intentionally does not contain `apps/server`, `apps/web`, `packages/server-api`, `packages/server-api-client`, `packages/protocol-openapi`, or `frontend-shared`.

## Build

```powershell
$env:MSBUILDDISABLENODEREUSE = "1"
dotnet restore ArcChat.slnx
dotnet build ArcChat.slnx -warnaserror
dotnet test ArcChat.slnx --no-build
```

## Run

```powershell
$env:MSBUILDDISABLENODEREUSE = "1"
dotnet run --project apps/desktop/ArcChat.Desktop/ArcChat.Desktop.csproj
```

## Hooks

```powershell
pwsh scripts/install-git-hooks.ps1
```

The hook verifies that staged commits are not being masked by unstaged, untracked, or ignored build inputs, then runs restore, format, build, and test before commits.
