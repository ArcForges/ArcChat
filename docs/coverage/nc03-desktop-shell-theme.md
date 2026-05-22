# NC03 Desktop Shell And Theme Coverage

All dotnet/MSBuild validation commands are run serially with MSBuild node reuse disabled (`MSBUILDDISABLENODEREUSE=1`; `$env:MSBUILDDISABLENODEREUSE = "1"` on Windows PowerShell).

| Traceability row | Evidence added in NC03 | Validation command | Final-gate impact |
| --- | --- | --- | --- |
| NC-UI-001 | `MainWindow` boots a two-pane shell with sidebar, splitter, placeholder detail content, and committed ArcChat screenshots under `docs/coverage/visual-baseline/arcchat/`. | `MainWindowTests.MainWindowDisplaysTwoPaneShell`; `VisualBaselineTests.Nc03VisualBaselineScreenshotsAreCommitted` | FG.03 has a runnable desktop shell baseline. |
| NC-UI-002 | `ArcChatTheme` applies light, dark, and system variants; Settings theme selection applies the requested variant immediately. | `ThemeSwitchTests.ThemeSwitchRoundTripsToAvaloniaVariant`; `SettingsViewTests.ThemeSelectionAppliesImmediately`; `ColorContrastTests.BodyTextMeetsWcagAaOnActiveThemeBackgrounds` | FG.03 theme switching and AA body contrast are locally gated. |
| NC-UI-003 | All 20 converted locale bundles load through `LocaleService`; Settings exposes a live locale selector and shell/settings strings update from the active culture. | `LocaleServiceTests.CoverageReportHasRowForEveryEnglishLocaleKey`; `LocaleServiceTests.LocaleConversionIsByteStableAcrossTwoRuns`; `SettingsViewTests.LocaleSelectionUpdatesSettingsStringsWithoutRestart`; `MainWindowTests.LocaleSelectionUpdatesSidebarStringsWithoutRestart` | FG.03 locale switching no longer requires app restart. |
| NC-UI-004 | `IAppNavigator`, destination records, history commands, and shell content host cover NextChat route names from `Path`. | `AppNavigatorTests`; `MainWindowTests.SidebarCommandDrivesShellContent`; `MainWindowTests.SplitterStateUpdatesSidebarNarrowMode` | FG.03 route parity has deterministic navigation tests. |
| NC-UI-005 | `ShortcutRegistry`, user overrides, conflict detection, and `Ctrl+K` command palette are backed by settings. | `ShortcutRegistryTests`; `MainWindowTests.MainWindowRegistersCommandPaletteShortcut` | FG.04 shortcut parity is ready for chat feature wiring. |
| NC-UI-006 | Interactive shell/settings controls expose `AutomationProperties.Name`; Tab traversal reaches visible controls; `IconButton` has an explicit focus border. | `AccessibilityBaselineTests.ShellInteractiveControlsExposeAutomationNamesAndTabTraversal`; `AccessibilityBaselineTests.SettingsInteractiveControlsExposeAutomationNamesAndTabTraversal`; `ColorContrastTests.BodyTextMeetsWcagAaOnActiveThemeBackgrounds` | FG.03 accessibility baseline is automated except optional screen-reader smoke. |
| NC-UI-007 | NC03 commits shell, narrow-sidebar, and settings-shell ArcChat visual baselines. | `ARCCHAT_UPDATE_VISUAL_BASELINE=1 dotnet test apps/desktop/ArcChat.Desktop.UiTests/ArcChat.Desktop.UiTests.csproj --filter VisualBaselineTests -m:1`; `dotnet test apps/desktop/ArcChat.Desktop.UiTests/ArcChat.Desktop.UiTests.csproj --filter VisualBaselineTests -m:1 --no-build` | FG.03 has first visual evidence for later parity comparisons. |
| NC-FEAT-024 | Settings theme/font/tight-border fields bind to the skeleton settings view; theme changes apply live and save/import/export still round-trip through `SettingsSnapshot`. | `SettingsViewTests`; `packages/local-services/ArcChat.LocalServices.Tests/Settings/SettingsRepositoryTests.cs` | FG.08 settings schema coverage has exact NC03 UI bindings for shipped fields. |
| NC-FEAT-025 | Vendored icon and locale resources are covered by `Resources/NOTICE.md`, generated icon references, and the NOTICE verification target. | `pwsh scripts/verify-notice.ps1`; `ResourceNoticeTests`; `IconResourceTests` | FG.03 and FG.09 asset/license posture is enforced for NC03 resources. |

## Screen-Reader Smoke

Screen-reader smoke is optional for the MVP baseline. The automated coverage above verifies focusable controls, names, and Tab traversal. Manual smoke can be run from a built desktop app:

| Platform | Tool | Smoke path | MVP status |
| --- | --- | --- | --- |
| Windows | Narrator | Start `dotnet run --project apps/desktop/ArcChat.Desktop`, press Tab through sidebar, back/forward, settings buttons, settings tabs, theme selector, and locale selector. Confirm spoken names match visible labels. | Documented; not required for automated NC03 gate. |
| Linux | Orca | Start the same app under a desktop session, enable Orca, then repeat the Windows path. Confirm sidebar, settings fields, and buttons are announced. | Documented; not required for automated NC03 gate. |
