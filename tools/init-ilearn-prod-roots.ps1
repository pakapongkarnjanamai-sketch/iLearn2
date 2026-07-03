#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ProdRoot = '\\ap-ntc2137-prwb\wwwroot\iLearn',

    [string]$QaRoot = '\\10.10.143.39\wwwroot\iLearnNew'
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$reactProdWebConfig = Join-Path $repoRoot 'iLearn.Admin.React/public/web.config.prod'

if (-not (Test-Path -LiteralPath $reactProdWebConfig)) {
    throw "Missing React production web.config template: $reactProdWebConfig"
}

$targets = @(
    [pscustomobject]@{
        Name            = 'Service'
        Path            = Join-Path $ProdRoot 'Service'
        WebConfigSource = Join-Path $QaRoot 'Service/web.config'
    },
    [pscustomobject]@{
        Name            = 'admin'
        Path            = Join-Path $ProdRoot 'admin'
        WebConfigSource = Join-Path $QaRoot 'admin/web.config'
    },
    [pscustomobject]@{
        Name            = 'student'
        Path            = Join-Path $ProdRoot 'student'
        WebConfigSource = Join-Path $QaRoot 'web.config'
    },
    [pscustomobject]@{
        Name            = 'admin-react'
        Path            = Join-Path $ProdRoot 'admin-react'
        WebConfigSource = $reactProdWebConfig
    }
)

$results = @()

foreach ($target in $targets) {
    if ($PSCmdlet.ShouldProcess($target.Path, 'Ensure directory exists')) {
        New-Item -ItemType Directory -Path $target.Path -Force | Out-Null
    }

    $targetWebConfig = Join-Path $target.Path 'web.config'
    $seededWebConfig = $false

    if (-not (Test-Path -LiteralPath $targetWebConfig)) {
        if (-not (Test-Path -LiteralPath $target.WebConfigSource)) {
            throw "Seed web.config source not found for $($target.Name): $($target.WebConfigSource)"
        }

        if ($PSCmdlet.ShouldProcess($targetWebConfig, "Seed web.config from $($target.WebConfigSource)")) {
            Copy-Item -LiteralPath $target.WebConfigSource -Destination $targetWebConfig -Force
            $seededWebConfig = $true
        }
    }

    $results += [pscustomobject]@{
        Name            = $target.Name
        Path            = $target.Path
        WebConfig       = $targetWebConfig
        WebConfigExists = [bool](Test-Path -LiteralPath $targetWebConfig)
        SeededWebConfig = $seededWebConfig
    }
}

$results | Format-Table -AutoSize
