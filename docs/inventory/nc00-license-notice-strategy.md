# NC00 License And NOTICE Strategy

NextChat source commit: `89b8f26ff8f03a0c5b98fc3026d980721495227e`.

## NextChat MIT Posture

`C:\MyFile\DevAll\QmlSharp\NextChat\LICENSE` is MIT and contains copyright `Copyright (c) 2023-2025 NextChat`.

Reusable with attribution:

| Source kind | Source path | Target | Rule |
| --- | --- | --- | --- |
| SVG and bitmap icons | `app/icons/**/*` | `apps/desktop/ArcChat.Desktop/Resources/Icons/` | vendor files; one NOTICE row per file |
| Prompt/plugin/mask JSON seeds | `public/prompts.json`, `public/plugins.json`, `public/masks.json` | `Resources/Seed/` | vendor data; one NOTICE row per file |
| MCP default config | `app/mcp/mcp_config.default.json` | `Resources/Seed/McpConfig.default.json` | vendor data; one NOTICE row |
| Mask TypeScript seed values | `app/masks/{en,cn,tw}.ts` | `Resources/Seed/Masks/{en,cn,tw}.json` | convert values to JSON; do not copy `.ts` |
| Locale string values | `app/locales/<code>.ts` | `Resources/Locales/<code>.json` | convert values to JSON; do not copy `.ts` |

Never vendored:

| Source kind | Rule |
| --- | --- |
| `.ts`, `.tsx`, `.scss` implementation files | behavior contract only; reimplement in C# / AXAML |
| Next.js API routes | mapped to desktop-native providers/local services or deleted |
| Tauri runtime files | replaced by Avalonia + self-contained publish + Velopack |
| Browser-only public files | disposition recorded in `docs/inventory/nextchat-public.md` |

## Required `NOTICE.md` Row Shape

Every vendored or converted asset/data file under `apps/desktop/ArcChat.Desktop/Resources/` needs a row with:

| Column | Required value |
| --- | --- |
| Target path | ArcChat resource path |
| Source path | exact NextChat path |
| Upstream URL | GitHub URL at vendoring commit |
| License | MIT, or third-party license if the file carries one |
| Vendoring commit | `89b8f26ff8f03a0c5b98fc3026d980721495227e` unless updated later |
| SHA-256 | hash of ArcChat target file |
| Reason | icon, prompt seed, plugin seed, locale data, mask data, MCP default config |

The root ArcChat license remains Apache-2.0. The NOTICE file preserves NextChat MIT attribution for reused assets/data.

## Quality Source Rule

The only engineering-quality source for the rewrite is `csharp-avalonia-quality-standard.md` plus official Microsoft / Avalonia documentation referenced by that file. Local application repositories outside NextChat are not implementation, quality, test-helper, naming, or module-layout inputs. QmlSharp is tooling-shape-only for `.slnx`, `Directory.Packages.props`, `Directory.Build.*`, `global.json`, `NuGet.config`, `.editorconfig`, and `.pre-commit-config.yaml`.

## Third-Party Licenses

Third-party .NET license posture is recorded in `docs/third-party-licenses.md`. NetMQ, MessagePack-CSharp, and LibGit2Sharp are not part of the default MVP; their license rows are deferred until NC08.F1 or future Git integration approval.

## NetMQ LGPL Conditional Plan

Default MVP has no NetMQ. If NC08.F1 approves NetMQ later, NC16 must prove:

| Requirement | Evidence |
| --- | --- |
| Replaceability | package layout keeps NetMQ in a replaceable DLL/process boundary |
| No static-linking ambiguity | publish layout and trimming config documented |
| No source modification | source not vendored or modified |
| LGPL text | full LGPL text bundled under `dist/third-party-notices/` |
| Smoke | desktop works without the optional host/broker enabled |
