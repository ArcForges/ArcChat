Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$matches = Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Include *.cs,*.csproj,*.props,*.targets,*.axaml,*.ps1,*.yml,*.yaml |
    Where-Object {
        $path = $_.FullName
        $path -notmatch '\\(bin|obj|\.git|\.worktrees)\\' -and $path -notmatch '\\docs\\'
    } |
    Select-String -Pattern '// TODO\(merge-blocker\)'

if ($matches) {
    $messages = $matches | ForEach-Object { "$($_.Path):$($_.LineNumber): $($_.Line)" }
    throw "Forbidden merge-blocker TODO found:`n$($messages -join "`n")"
}
