# ArcChat

ArcChat is the Avalonia desktop client workspace.

## Repository layout

- `apps/desktop/ArcChat.Desktop`: Avalonia desktop application.
- `apps/desktop/ArcChat.Desktop.Tests`: desktop project tests.

## Build

```powershell
dotnet restore ArcChat.slnx
dotnet build ArcChat.slnx --configuration Release --no-restore
dotnet test ArcChat.slnx --configuration Release --no-build
```
