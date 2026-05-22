param(
    [string]$Source = "C:\MyFile\DevAll\QmlSharp\NextChat\app\styles\globals.scss",
    [string]$Output = "desktop-shared\ArcChat.UI.Theme\Tokens\nextchat-colors.json"
)

$ErrorActionPreference = "Stop"

function Convert-ScssColor {
    param([string]$Value)

    $trimmed = $Value.Trim().TrimEnd(";")
    if ($trimmed -eq "white") {
        return "#FFFFFF"
    }

    if ($trimmed -match "^#(?<hex>[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$") {
        $hex = $Matches.hex.ToUpperInvariant()
        if ($hex.Length -eq 3) {
            return "#" + ($hex[0] * 2) + ($hex[1] * 2) + ($hex[2] * 2)
        }
        return "#" + $hex
    }

    if ($trimmed -match "^rgb\((?<parts>[^)]+)\)$") {
        $parts = $Matches.parts -split "[,\s]+" | Where-Object { $_ -ne "" }
        if ($parts.Count -ge 3) {
            return "#{0:X2}{1:X2}{2:X2}" -f [int]$parts[0], [int]$parts[1], [int]$parts[2]
        }
    }

    return $null
}

function Read-MixinColors {
    param(
        [string[]]$Lines,
        [string]$MixinName
    )

    $inside = $false
    $values = [ordered]@{}
    foreach ($line in $Lines) {
        if ($line -match "^\s*@mixin\s+$MixinName\s+\{") {
            $inside = $true
            continue
        }

        if ($inside -and $line -match "^\s*\}") {
            break
        }

        if ($inside -and $line -match "^\s*--(?<name>white|black|gray|primary|second|hover-color):\s*(?<value>.+)$") {
            $color = Convert-ScssColor $Matches.value
            if ($null -ne $color) {
                $values[$Matches.name] = $color
            }
        }
    }

    return $values
}

$lines = Get-Content -LiteralPath $Source
$manifest = [ordered]@{
    light = Read-MixinColors -Lines $lines -MixinName "light"
    dark = Read-MixinColors -Lines $lines -MixinName "dark"
}

$json = $manifest | ConvertTo-Json -Depth 4
$target = if ([System.IO.Path]::IsPathRooted($Output)) { $Output } else { Join-Path (Get-Location) $Output }
$directory = Split-Path -Parent $target
if (-not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory | Out-Null
}

Set-Content -LiteralPath $target -Value ($json + [Environment]::NewLine) -Encoding utf8NoBOM
