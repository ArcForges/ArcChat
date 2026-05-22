param(
    [string]$NextChatRoot = "C:\MyFile\DevAll\QmlSharp\NextChat",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

function Normalize-NextChatPath {
    param([string]$Path)
    return $Path.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
}

function Expand-BracePath {
    param([string]$Path)

    if ($Path -match "\{([^{}]+)\}") {
        $prefix = $Path.Substring(0, $Path.IndexOf("{"))
        $suffix = $Path.Substring($Path.IndexOf("}") + 1)
        return ($Matches[1].Split(",") | ForEach-Object { "$prefix$_$suffix" })
    }

    return @($Path)
}

function Assert-ExistsOrPattern {
    param([string]$RelativePath)

    if ($RelativePath.Contains("<")) {
        return
    }

    foreach ($expanded in (Expand-BracePath $RelativePath)) {
        if ($expanded.Contains("*")) {
            if ($expanded.Contains("**/*")) {
                $prefix = $expanded.Substring(0, $expanded.IndexOf("**/*"))
                $prefixPath = Join-Path $NextChatRoot (Normalize-NextChatPath $prefix)
                if (-not (Test-Path -LiteralPath $prefixPath)) {
                    throw "Pattern prefix does not exist in NextChat: $expanded"
                }

                $matches = Get-ChildItem -LiteralPath $prefixPath -Recurse -File
                if (-not $matches) {
                    throw "Pattern has no file matches in NextChat: $expanded"
                }

                continue
            }

            $matches = Get-ChildItem -LiteralPath $NextChatRoot -Recurse -File |
                Where-Object {
                    $relative = $_.FullName.Substring($NextChatRoot.Length + 1)
                    $relative = $relative.Replace([System.IO.Path]::DirectorySeparatorChar, "/")
                    $relative -like $expanded
                }

            if (-not $matches) {
                throw "Pattern has no matches in NextChat: $expanded"
            }

            continue
        }

        $fullPath = Join-Path $NextChatRoot (Normalize-NextChatPath $expanded)
        if (-not (Test-Path -LiteralPath $fullPath)) {
            throw "Missing NextChat source path cited by NC00 inventory: $expanded"
        }
    }
}

if (-not (Test-Path -LiteralPath $NextChatRoot)) {
    throw "NextChat root not found: $NextChatRoot"
}

$inventoryFiles = @(
    "docs/inventory/nextchat-features.md",
    "docs/inventory/nextchat-public.md",
    "docs/coverage/settings-schema.md",
    "docs/inventory/nc00-license-notice-strategy.md"
)

foreach ($file in $inventoryFiles) {
    $fullPath = Join-Path $RepoRoot (Normalize-NextChatPath $file)
    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "Required NC00 inventory file missing: $file"
    }
}

$sourcePathPattern = '`((app|public|src-tauri)/[^`]+)`'
$seen = [System.Collections.Generic.HashSet[string]]::new()

foreach ($file in $inventoryFiles) {
    $content = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot (Normalize-NextChatPath $file))
    foreach ($match in [regex]::Matches($content, $sourcePathPattern)) {
        $path = $match.Groups[1].Value
        if ($path.Contains("Resources/")) {
            continue
        }

        if ($seen.Add($path)) {
            Assert-ExistsOrPattern $path
        }
    }
}

$apiFiles = Get-ChildItem -LiteralPath (Join-Path $NextChatRoot "app\api") -Recurse -File |
    Where-Object { $_.Extension -in ".ts", ".tsx" }
if ($apiFiles.Count -ne 24) {
    throw "Expected 24 NextChat app/api files, found $($apiFiles.Count)."
}

$localeFiles = Get-ChildItem -LiteralPath (Join-Path $NextChatRoot "app\locales") -File -Filter "*.ts" |
    Where-Object { $_.Name -ne "index.ts" }
if ($localeFiles.Count -ne 20) {
    throw "Expected 20 locale bundles excluding index.ts, found $($localeFiles.Count)."
}

$featureInventory = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot "docs\inventory\nextchat-features.md")
$packageJson = Get-Content -Raw -LiteralPath (Join-Path $NextChatRoot "package.json") | ConvertFrom-Json
$missingDeps = @()
foreach ($dependency in $packageJson.dependencies.PSObject.Properties.Name) {
    $dependencyToken = '`' + $dependency + '`'
    if ($featureInventory -notmatch [regex]::Escape($dependencyToken)) {
        $missingDeps += $dependency
    }
}

if ($missingDeps.Count -gt 0) {
    throw "Runtime dependencies missing from NC00 inventory: $($missingDeps -join ', ')"
}

$forbiddenPaths = @(
    "apps\server",
    "apps\web",
    "packages\server-api",
    "packages\server-api-client",
    "packages\protocol-openapi",
    "frontend-shared"
)

foreach ($path in $forbiddenPaths) {
    $fullPath = Join-Path $RepoRoot $path
    if (Test-Path -LiteralPath $fullPath) {
        throw "Forbidden path exists in ArcChat for this rewrite: $path"
    }
}

$settingsSchema = Get-Content -Raw -LiteralPath (Join-Path $RepoRoot "docs\coverage\settings-schema.md")
$requiredSettingsFields = @(
    "openaiApiKey",
    "azureApiVersion",
    "modelConfig.compressMessageLengthThreshold",
    "realtimeConfig.azure.deployment",
    "webdav.password",
    "upstash.apiKey",
    "mcpServers"
)

foreach ($field in $requiredSettingsFields) {
    if ($settingsSchema -notmatch [regex]::Escape($field)) {
        throw "Required settings field missing from settings schema inventory: $field"
    }
}

Write-Host "NC00 inventory verification passed."
Write-Host "Checked cited NextChat paths: $($seen.Count)"
Write-Host "Checked API files: $($apiFiles.Count)"
Write-Host "Checked locale bundles: $($localeFiles.Count)"
Write-Host "Checked runtime dependencies: $($packageJson.dependencies.PSObject.Properties.Name.Count)"
