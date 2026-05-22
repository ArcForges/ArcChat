# Visual Baseline Evidence

NC03 commits ArcChat desktop screenshots for the first runnable shell:

| File | State | Capture owner |
| --- | --- | --- |
| `arcchat/shell.verified.png` | Default shell with sidebar and placeholder detail pane. | `VisualBaselineTests.Nc03VisualBaselineScreenshotsAreCommitted` |
| `arcchat/sidebar.verified.png` | Shell with the sidebar collapsed to the NC03 narrow width. | `VisualBaselineTests.Nc03VisualBaselineScreenshotsAreCommitted` |
| `arcchat/settings-shell.verified.png` | Shell navigated to the settings skeleton. | `VisualBaselineTests.Nc03VisualBaselineScreenshotsAreCommitted` |

Regenerate ArcChat baselines from the repository root:

```powershell
$env:MSBUILDDISABLENODEREUSE = "1"
$env:ARCCHAT_UPDATE_VISUAL_BASELINE = "1"
dotnet test apps/desktop/ArcChat.Desktop.UiTests/ArcChat.Desktop.UiTests.csproj --filter VisualBaselineTests -m:1
Remove-Item Env:\ARCCHAT_UPDATE_VISUAL_BASELINE
```

Normal validation asserts the committed PNG files exist and contain rendered frames.
