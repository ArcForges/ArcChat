Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$env:MSBUILDDISABLENODEREUSE = '1'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repoRoot
try {
    dotnet run --project apps/desktop/ArcChat.Desktop/ArcChat.Desktop.csproj -- @args
}
finally {
    Pop-Location
}
