<#
PowerShell script: move-admin-styles.ps1
Scans Razor views under iLearn.Admin\Views for `@section Styles { ... }` blocks,
extracts CSS (removes surrounding <style> tags if present), appends to
iLearn.Admin/wwwroot/css/admin-site.css with a source comment, backs up
original .cshtml files and removes the section blocks.

Usage:
  powershell -ExecutionPolicy Bypass -File tools\move-admin-styles.ps1
#>

# Resolve paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$viewsPath = Join-Path $repoRoot 'iLearn.Admin\Views'
$cssFile = Join-Path $repoRoot 'iLearn.Admin\wwwroot\css\admin-site.css'

if (-not (Test-Path $viewsPath)) {
    Write-Error "Views path not found: $viewsPath"
    exit 1
}

if (-not (Test-Path $cssFile)) {
    Write-Host "CSS file not found, creating: $cssFile"
    New-Item -ItemType File -Path $cssFile -Force | Out-Null
}

$files = Get-ChildItem -Path $viewsPath -Recurse -Include *.cshtml -File
if ($files.Count -eq 0) {
    Write-Host "No .cshtml files found under $viewsPath"
    exit 0
}

# Regex to find @section Styles { ... }
$regex = [regex]::new('(?is)@section\s+Styles\s*\{(.*?)\}', [System.Text.RegularExpressions.RegexOptions]::Singleline)

$foundAny = $false
$appendCount = 0

foreach ($file in $files) {
    $content = Get-Content -Raw -LiteralPath $file.FullName
    $matches = $regex.Matches($content)
    if ($matches.Count -eq 0) { continue }

    $foundAny = $true
    # Backup original file
    $bakPath = "$($file.FullName).bak"
    Copy-Item -LiteralPath $file.FullName -Destination $bakPath -Force

    # Work with a mutable string: remove matches from end to start
    $newContent = $content
    # We'll collect css pieces to append
    $cssPieces = @()

    # Iterate matches in reverse index order to remove without shifting indices
    $matchesSorted = $matches | Sort-Object { $_.Index } -Descending
    foreach ($m in $matchesSorted) {
        $inner = $m.Groups[1].Value.Trim()
        # If inner contains <style> tag, strip it
        if ($inner -match '(?is)<style\b[^>]*>(.*?)</style>') {
            $innerCss = [regex]::Replace($inner, '(?is)<style\b[^>]*>(.*?)</style>', '$1')
        } else {
            $innerCss = $inner
        }

        if ($innerCss.Trim()) {
            $cssHeader = "\n/* MOVED from: $($file.FullName) - at $(Get-Date -Format o) */\n"
            $cssPieces += ($cssHeader + $innerCss.Trim() + "`n")
            $appendCount++
        }

        # remove the matched block
        $newContent = $newContent.Remove($m.Index, $m.Length)
    }

    # Write updated view file (trim trailing whitespace)
    Set-Content -LiteralPath $file.FullName -Value $newContent -Encoding UTF8

    # Append collected css to cssFile
    if ($cssPieces.Count -gt 0) {
        Add-Content -LiteralPath $cssFile -Value $cssPieces -Encoding UTF8
        Write-Host "Moved $($cssPieces.Count) style block(s) from $($file.FullName) -> $cssFile"
    }
}

if (-not $foundAny) {
    Write-Host "No @section Styles blocks found in iLearn.Admin/Views"
} else {
    Write-Host "Completed. Appended $appendCount style block(s) to $cssFile"
}
