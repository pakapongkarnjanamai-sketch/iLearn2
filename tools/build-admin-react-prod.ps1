#requires -Version 7.0

[CmdletBinding()]
param(
    [switch]$SkipLint,

    [switch]$SkipBuild,

    [switch]$SkipDistWebConfigPatch
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$reactRoot = Join-Path $repoRoot 'iLearn.Admin.React'
$distPath = Join-Path $reactRoot 'dist'
$prodWebConfig = Join-Path $reactRoot 'public/web.config.prod'
$distWebConfig = Join-Path $distPath 'web.config'

if (-not (Test-Path -LiteralPath $reactRoot)) {
    throw "React project root not found: $reactRoot"
}

Push-Location $reactRoot
try {
    if (-not $SkipLint) {
        Write-Host 'Running npm run lint (prod prep)'
        & npm run lint
        if ($LASTEXITCODE -ne 0) {
            throw 'npm run lint failed for iLearn.Admin.React'
        }
    }

    if (-not $SkipBuild) {
        Write-Host 'Running npm run build (uses .env.production)'
        & npm run build
        if ($LASTEXITCODE -ne 0) {
            throw 'npm run build failed for iLearn.Admin.React'
        }
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $distPath)) {
    throw "Build output folder not found: $distPath"
}

if (-not $SkipDistWebConfigPatch) {
    if (-not (Test-Path -LiteralPath $prodWebConfig)) {
        throw "Production web.config template not found: $prodWebConfig"
    }

    Copy-Item -LiteralPath $prodWebConfig -Destination $distWebConfig -Force
}

[pscustomobject]@{
    ReactRoot          = $reactRoot
    DistPath           = $distPath
    DistWebConfig      = $distWebConfig
    PatchedDistWebConf = [bool](-not $SkipDistWebConfigPatch)
    RanLint            = [bool](-not $SkipLint)
    RanBuild           = [bool](-not $SkipBuild)
} | Format-List
