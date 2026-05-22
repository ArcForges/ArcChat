# NC00 Environment And Toolchain Matrix

This matrix locks the NC01 package and toolchain plan. Values were checked from the local SDK, the existing ArcChat package plan, Microsoft .NET documentation, and NuGet package metadata on May 22, 2026.

## SDK And Language

| Item | Locked value | Evidence / source |
| --- | --- | --- |
| .NET SDK | `10.0.300` | `dotnet --version` in the NC00 worktree |
| Runtime | `net10.0`; local runtime `Microsoft.NETCore.App 10.0.8` | `dotnet --list-runtimes` |
| SDK roll-forward policy | `latestPatch` for NC01 `global.json` plan | NC00 step requirement; NC01 aligned ArcChat `global.json` to this value |
| C# language version | `latest` unless Avalonia tooling requires `preview` | `csharp-avalonia-quality-standard.md` |
| CI runners | `windows-2025`, `ubuntu-24.04`, `macos-14` | Current required PR CI matrix |
| MSBuild node reuse | disabled before dotnet/build/test/format commands | `arcchat.md` workflow rule |

Microsoft documents .NET 10 as LTS supported until November 2028 and documents SDK feature bands and Visual Studio 18.0+ requirements for `net10.0`: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support and https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs.

## NuGet Version Plan For NC01

| Package / API | Locked or planned version | Notes |
| --- | ---: | --- |
| `Avalonia` | 12.0.3 | current NuGet latest stable |
| `Avalonia.Desktop` | 12.0.3 | current NuGet latest stable |
| `Avalonia.Themes.Fluent` | 12.0.3 | current NuGet latest stable |
| `Avalonia.Markup.Xaml.Loader` | 12.0.3 | current NuGet latest stable |
| `Avalonia.Headless` | 12.0.3 | current NuGet latest stable |
| `Avalonia.Svg.Skia` | 11.3.0 | current NuGet latest stable; NC03 must verify Avalonia 12 compatibility before use |
| `CommunityToolkit.Mvvm` | 8.4.2 | current NuGet latest stable |
| `Microsoft.Extensions.Hosting` | 10.0.8 | current NuGet latest stable |
| `Microsoft.Extensions.DependencyInjection` | 10.0.8 | current NuGet latest stable |
| `Microsoft.Extensions.Logging` | 10.0.8 | current NuGet latest stable |
| `Microsoft.Extensions.Options` | 10.0.8 | current NuGet latest stable |
| `Microsoft.Extensions.Configuration` | 10.0.8 | current NuGet latest stable |
| `System.IO.Pipelines` | 10.0.8 | package pin only if needed outside shared framework |
| `System.Net.Http` | BCL/shared framework | do not pin legacy NuGet `4.3.4` in net10 projects unless a package explicitly requires it |
| `System.Net.WebSockets.Client` | BCL/shared framework | do not pin legacy NuGet `4.3.2` in net10 projects unless a package explicitly requires it |
| `System.Threading.Channels` | 10.0.8 | current NuGet latest stable |
| `Polly.Core` | 8.6.6 | current NuGet latest stable |
| `Microsoft.Extensions.Http.Resilience` | 10.6.0 | current NuGet latest stable |
| `Microsoft.Data.Sqlite` | 10.0.8 | current NuGet latest stable |
| `Dapper.AOT` | 1.0.52 | current NuGet latest stable |
| `DbUp` | 5.0.41 | current NuGet latest stable |
| `Microsoft.Agents.AI` | 1.6.2 | NuGet flat-container latest stable; verify package page/listed status before NC04/NC05 |
| `Markdig` | 1.2.0 | current NuGet latest stable |
| `ColorCode.Core` | 2.0.15 | current NuGet latest stable |
| `Magick.NET-Q8-AnyCPU` | 14.13.1 | current NuGet latest stable |
| `StyleCop.Analyzers` | 1.1.118 | current NuGet latest stable |
| `Roslynator.Analyzers` | 4.15.0 | current NuGet latest stable |
| `Meziantou.Analyzer` | 3.0.91 | current NuGet latest stable |
| `NetArchTest.Rules` | 1.3.2 | current NuGet latest stable |
| `xunit` | 2.9.3 | current NuGet latest stable |
| `FluentAssertions` | 8.10.0 | current NuGet latest stable |
| `NSubstitute` | 5.3.0 | current NuGet latest stable |
| `Verify.Xunit` | 31.12.5 | current NuGet latest stable |
| `Verify.Avalonia` | 1.4.0 | current NuGet latest stable |
| `NetMQ`, `AsyncIO`, `MessagePack`, `LibGit2Sharp` | not pinned for default MVP | document only if NC08.F1 or future Git integration is approved |

NuGet package metadata was checked with the NuGet V3 flat-container API described at https://learn.microsoft.com/en-us/nuget/api/package-base-address-resource.

## Allowed Runtime Features

| Area | Allowed feature |
| --- | --- |
| C# / .NET | file-scoped types, primary constructors, collection expressions, `field` keyword where supported, ref-readonly parameters, allows-ref-struct constraints, `Lock`, Native AOT, `System.Text.Json` source generators |
| Concurrency | `System.Threading.Channels`; use `PriorityChannel` only after confirming availability in the pinned SDK/API |
| AI/tooling | `Microsoft.Extensions.AI.AIFunctionFactory` where compatible with the pinned Agent Framework package |
| Packaging | self-contained publish; NativeAOT only for future native/Qt interop path, not default NC01 |

## Offline Test Policy

| Area | Offline policy |
| --- | --- |
| Model providers | recorded HTTP/SSE/WS fixtures; no real provider calls in CI |
| MCP | local echo server fixture |
| Sync | in-process WebDAV and Upstash mocks |
| UI | Avalonia.Headless with deterministic dispatcher; no `Thread.Sleep` |
| Generated assets | two-run determinism checks for locale/icon/schema generators |

## Performance Baselines To Record

| Metric | Baseline target | Measurement owner |
| --- | ---: | --- |
| Cold start | under 2.5s on dev laptop | NC17 |
| Message append latency | under 16ms p95 at 5k-message conversation | NC17 |
| Streaming render | 60fps at 100 tokens/s | NC17 |
| Working set | under 600MB for 50 conversations x 1k messages | NC17 |
| Package size | under 250MB default, under 400MB WebView variant | NC16 |

## NC01 Verification Requirement

NC01 makes `Directory.Packages.props` match the rows above unless a deviation is recorded, and `global.json` is aligned to the locked `latestPatch` roll-forward policy. Later dependency changes must update this matrix or document changed latest-version evidence.
