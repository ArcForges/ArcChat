Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return $Path.Replace('\', '/')
}

function Test-IsOutputPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $repoPath = Get-RepoPath $Path
    return $repoPath -match '(^|/)(bin|obj|\.vs|out|dist|TestResults)(/|$)' -or $repoPath -match '^artifacts/'
}

function Test-IsBuildInput {
    param([Parameter(Mandatory = $true)][string]$Path)

    $extension = [System.IO.Path]::GetExtension($Path)
    return @(
        '.axaml',
        '.cs',
        '.csproj',
        '.editorconfig',
        '.globalconfig',
        '.json',
        '.jsonc',
        '.props',
        '.resx',
        '.ruleset',
        '.sln',
        '.slnx',
        '.targets',
        '.xml'
    ) -contains $extension
}

function Stop-WithPathList {
    param(
        [Parameter(Mandatory = $true)][string]$Message,
        [Parameter(Mandatory = $true)][string[]]$Paths
    )

    Write-Host $Message
    $Paths | ForEach-Object { Write-Host "  $_" }
    exit 1
}

$repoRoot = (git rev-parse --show-toplevel).Trim()
Push-Location $repoRoot
try {
    $env:MSBUILDDISABLENODEREUSE = '1'
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_NOLOGO = '1'
    $env:CI = 'true'

    $unstagedTracked = @(git diff --name-only)
    if ($unstagedTracked.Count -gt 0) {
        Stop-WithPathList `
            -Message 'Pre-commit validation requires tracked changes to be staged so the checked tree matches the commit.' `
            -Paths $unstagedTracked
    }

    $untrackedBuildInputs = @(git ls-files --others --exclude-standard |
        Where-Object { (Test-IsBuildInput $_) -and -not (Test-IsOutputPath $_) })
    if ($untrackedBuildInputs.Count -gt 0) {
        Stop-WithPathList `
            -Message 'Pre-commit validation found untracked build inputs. Stage them or remove them before committing.' `
            -Paths $untrackedBuildInputs
    }

    $ignoredBuildInputs = @(git ls-files --others --ignored --exclude-standard |
        Where-Object { (Test-IsBuildInput $_) -and -not (Test-IsOutputPath $_) })
    if ($ignoredBuildInputs.Count -gt 0) {
        Stop-WithPathList `
            -Message 'Pre-commit validation found ignored build inputs that could mask CI failures.' `
            -Paths $ignoredBuildInputs
    }

    dotnet restore ArcChat.slnx
    dotnet build tools/ArcChat.IconCodegen/ArcChat.IconCodegen.csproj --no-restore
    dotnet format ArcChat.slnx --verify-no-changes --no-restore --verbosity minimal
    dotnet build ArcChat.slnx -warnaserror --no-restore -m:1
    dotnet test ArcChat.slnx --no-build --logger trx
}
finally {
    Pop-Location
}
