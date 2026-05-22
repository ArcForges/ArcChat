param(
    [switch]$Report,
    [string]$NextChatRoot,
    [string]$OutputRoot,
    [string]$ReportPath
)

$ErrorActionPreference = "Stop"

$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $RepositoryRoot "apps\desktop\ArcChat.Desktop\Resources\Locales"
}

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $RepositoryRoot "docs\coverage\locales.md"
}

function Resolve-NextChatRoot {
    param([string]$ExplicitRoot)

    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ExplicitRoot)) {
        $candidates.Add($ExplicitRoot)
    }

    if (-not [string]::IsNullOrWhiteSpace($env:NEXTCHAT_ROOT)) {
        $candidates.Add($env:NEXTCHAT_ROOT)
    }

    $candidates.Add((Join-Path $RepositoryRoot "..\NextChat"))
    $candidates.Add((Join-Path $RepositoryRoot "..\..\..\NextChat"))

    foreach ($candidate in $candidates) {
        $resolved = $null
        try {
            $resolved = (Resolve-Path $candidate -ErrorAction Stop).Path
        } catch {
            continue
        }

        if (Test-Path (Join-Path $resolved "app\locales\en.ts")) {
            return $resolved
        }
    }

    throw "Unable to locate NextChat root. Pass -NextChatRoot or set NEXTCHAT_ROOT."
}

function Write-Utf8NoBom {
    param(
        [string]$Path,
        [string]$Content
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Force $directory | Out-Null
    }

    $encoding = [System.Text.UTF8Encoding]::new($false)
    [System.IO.File]::WriteAllText($Path, $Content, $encoding)
}

function Read-LocaleJson {
    param([string]$Path)

    return Get-Content -Raw $Path | ConvertFrom-Json -AsHashtable
}

function Test-LocalesCurrent {
    param(
        [System.IO.FileInfo[]]$LocaleFiles,
        [string]$TargetRoot,
        [string]$ParserPath,
        [string]$ScriptPath
    )

    if (-not (Test-Path $TargetRoot)) {
        return $false
    }

    $inputFiles = @($LocaleFiles | ForEach-Object { $_.FullName }) + @($ParserPath, $ScriptPath)
    $newestInput = @($inputFiles | ForEach-Object { (Get-Item $_).LastWriteTimeUtc } | Sort-Object -Descending)[0]

    foreach ($file in $LocaleFiles) {
        $targetPath = Join-Path $TargetRoot ($file.BaseName + ".json")
        if (-not (Test-Path $targetPath)) {
            return $false
        }

        if ((Get-Item $targetPath).LastWriteTimeUtc -lt $newestInput) {
            return $false
        }
    }

    return $true
}

function Invoke-LocaleParser {
    param(
        [string]$ParserPath,
        [string]$LocaleFile
    )

    $nodeResult = Invoke-ProcessWithTimeout -FileName "node" -Arguments @($ParserPath, $LocaleFile)
    if ($nodeResult.ExitCode -eq 0) {
        return $nodeResult.StandardOutput
    }

    $npxCommand = if ($IsWindows) { "npx.cmd" } else { "npx" }
    $standardOutput = ""
    $standardError = $nodeResult.StandardError

    foreach ($attempt in 1..3) {
        $result = Invoke-ProcessWithTimeout -FileName $npxCommand -Arguments @("--yes", "tsx", $ParserPath, $LocaleFile)
        $standardOutput = $result.StandardOutput
        $standardError = $result.StandardError

        if ($result.ExitCode -eq 0) {
            return $standardOutput
        }

        if ($attempt -lt 3) {
            Start-Sleep -Milliseconds (250 * $attempt)
        }
    }

    throw "Locale parser failed for $LocaleFile.`n$standardError`n$standardOutput"
}

function Invoke-ProcessWithTimeout {
    param(
        [string]$FileName,
        [string[]]$Arguments,
        [int]$TimeoutMilliseconds = 60000
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $FileName
    $startInfo.RedirectStandardError = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.UseShellExecute = $false

    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "Unable to start $FileName."
    }

    try {
        $standardOutputTask = $process.StandardOutput.ReadToEndAsync()
        $standardErrorTask = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit($TimeoutMilliseconds)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw "$FileName timed out after $TimeoutMilliseconds ms."
        }

        return [PSCustomObject]@{
            ExitCode = $process.ExitCode
            StandardOutput = $standardOutputTask.GetAwaiter().GetResult()
            StandardError = $standardErrorTask.GetAwaiter().GetResult()
        }
    } finally {
        $process.Dispose()
    }
}

function New-CoverageReport {
    param(
        [hashtable]$Locales,
        [string[]]$LocaleCodes,
        [string]$SourceDirectory
    )

    $english = $Locales["en"]
    if ($null -eq $english) {
        throw "English locale was not generated."
    }

    $englishKeys = @($english.Keys | Sort-Object)
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Locale Coverage")
    $lines.Add("")
    $lines.Add("Source: ``$SourceDirectory``")
    $lines.Add("")
    $lines.Add("| Locale | Keys | Missing English Keys |")
    $lines.Add("| --- | ---: | ---: |")

    foreach ($code in $LocaleCodes) {
        $locale = $Locales[$code]
        $missing = @($englishKeys | Where-Object { -not $locale.ContainsKey($_) }).Count
        $lines.Add("| ``$code`` | $($locale.Count) | $missing |")
    }

    $lines.Add("")
    $lines.Add("| Key | " + (($LocaleCodes | ForEach-Object { "``$_``" }) -join " | ") + " |")
    $lines.Add("| --- | " + (($LocaleCodes | ForEach-Object { "---" }) -join " | ") + " |")

    foreach ($key in $englishKeys) {
        $cells = foreach ($code in $LocaleCodes) {
            if ($Locales[$code].ContainsKey($key)) {
                "ok"
            } else {
                "fallback"
            }
        }

        $lines.Add("| ``$key`` | " + ($cells -join " | ") + " |")
    }

    return ($lines -join "`n") + "`n"
}

$nextChat = Resolve-NextChatRoot -ExplicitRoot $NextChatRoot
$sourceDirectory = Join-Path $nextChat "app\locales"
$parser = Join-Path $PSScriptRoot "parse-locale.ts"
$localeFiles = @(Get-ChildItem -Path $sourceDirectory -Filter "*.ts" | Where-Object { $_.Name -ne "index.ts" } | Sort-Object Name)

if ($localeFiles.Count -eq 0) {
    throw "No locale files found in $sourceDirectory."
}

New-Item -ItemType Directory -Force $OutputRoot | Out-Null

if (-not $Report -and (Test-LocalesCurrent -LocaleFiles $localeFiles -TargetRoot $OutputRoot -ParserPath $parser -ScriptPath $PSCommandPath)) {
    Write-Host "Locale resources already current at $OutputRoot."
    exit 0
}

foreach ($file in $localeFiles) {
    $json = (Invoke-LocaleParser -ParserPath $parser -LocaleFile $file.FullName).TrimEnd() + "`n"
    $null = $json | ConvertFrom-Json -AsHashtable
    $targetPath = Join-Path $OutputRoot ($file.BaseName + ".json")
    Write-Utf8NoBom -Path $targetPath -Content $json
}

if ($Report) {
    $locales = @{}
    $localeCodes = @()
    foreach ($file in @(Get-ChildItem -Path $OutputRoot -Filter "*.json" | Sort-Object Name)) {
        $localeCodes += $file.BaseName
        $locales[$file.BaseName] = Read-LocaleJson -Path $file.FullName
    }

    $coverage = New-CoverageReport -Locales $locales -LocaleCodes $localeCodes -SourceDirectory "NextChat/app/locales"
    Write-Utf8NoBom -Path $ReportPath -Content $coverage
}

Write-Host "Converted $($localeFiles.Count) locale files to $OutputRoot."
