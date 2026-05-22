Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$env:MSBUILDDISABLENODEREUSE = '1'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repoRoot
try {
    dotnet restore ArcChat.slnx
    dotnet format ArcChat.slnx --verify-no-changes --no-restore --verbosity minimal
    dotnet build ArcChat.slnx -warnaserror --no-restore
    dotnet test ArcChat.slnx --no-build --logger trx
}
finally {
    Pop-Location
}
