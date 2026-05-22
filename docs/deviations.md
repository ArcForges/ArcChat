# ArcChat Deviations

| Date | Step | Decision | Reason | Owner | Final-gate impact |
| --- | --- | --- | --- | --- | --- |
| 2026-05-22 | NC01 | Preserve the MIT root license and MIT code header. | NC00 evidence in this repository records the ArcChat root repository as MIT and the goal file makes NC00 evidence authoritative before later steps. | NC01 | FG.01/FG.09 license posture remains tied to `LICENSE` and `docs/third-party-licenses.md`; no NextChat attribution impact. |
| 2026-05-22 | NC01 | Use `Avalonia.Headless` with `HeadlessUnitTestSession` instead of pinning the Avalonia xUnit adapter package. | The adapter brings xUnit v3 extensibility; solution-wide `dotnet test` remains on the NC00 xUnit 2 / VSTest path. Avalonia documents manual headless sessions for this case. | NC01 | FG.03/FG.04 still cover desktop smoke through headless UI tests; no product behavior impact. |
