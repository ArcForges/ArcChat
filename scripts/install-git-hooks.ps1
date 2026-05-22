Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Push-Location $repoRoot
try {
    python -m pip install --upgrade pre-commit
    pre-commit install
}
finally {
    Pop-Location
}
