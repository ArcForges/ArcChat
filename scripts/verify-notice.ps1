param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$ResourceRoot = (Join-Path $RepositoryRoot "apps/desktop/ArcChat.Desktop/Resources"),
    [string]$NoticePath = (Join-Path $ResourceRoot "NOTICE.md")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $NoticePath -PathType Leaf)) {
    throw "NOTICE file does not exist: $NoticePath"
}

function Convert-ToRepositoryPath {
    param([string]$Path)

    return [System.IO.Path]::GetRelativePath($RepositoryRoot, $Path).Replace("\", "/")
}

function Get-NoticeRows {
    param([string]$Path)

    $rows = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        if (-not $line.StartsWith("| apps/desktop/ArcChat.Desktop/Resources/", [StringComparison]::Ordinal)) {
            continue
        }

        $columns = $line.Trim("|").Split("|") | ForEach-Object { $_.Trim() }
        if ($columns.Count -lt 7) {
            throw "Malformed NOTICE row: $line"
        }

        $rows[$columns[0]] = [PSCustomObject]@{
            TargetPath = $columns[0]
            SourcePath = $columns[1]
            UpstreamUrl = $columns[2]
            License = $columns[3]
            VendoringCommit = $columns[4]
            Sha256 = $columns[5].ToLowerInvariant()
            Reason = $columns[6]
        }
    }

    return $rows
}

$noticeRows = Get-NoticeRows -Path $NoticePath
$resources = Get-ChildItem -LiteralPath $ResourceRoot -Recurse -File |
    Where-Object { -not [string]::Equals($_.FullName, $NoticePath, [StringComparison]::OrdinalIgnoreCase) } |
    Sort-Object FullName

foreach ($resource in $resources) {
    $targetPath = Convert-ToRepositoryPath -Path $resource.FullName
    if (-not $noticeRows.ContainsKey($targetPath)) {
        throw "NOTICE is missing row for $targetPath"
    }

    $actualHash = (Get-FileHash -LiteralPath $resource.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $expectedHash = $noticeRows[$targetPath].Sha256
    if (-not [string]::Equals($actualHash, $expectedHash, [StringComparison]::OrdinalIgnoreCase)) {
        throw "NOTICE hash mismatch for $targetPath. Expected $expectedHash but found $actualHash."
    }
}

foreach ($targetPath in $noticeRows.Keys) {
    $absolutePath = Join-Path $RepositoryRoot $targetPath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        throw "NOTICE row references missing resource $targetPath"
    }
}

Write-Host "NOTICE covers $($resources.Count) desktop resource files."
