#requires -Version 7.0

[CmdletBinding()]
param()

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repoRoot

$publishPath = Join-Path $repoRoot 'artifacts/publish/iLearn.Admin.prod'
$deployRoot = '\\ap-ntc2137-prwb\wwwroot\iLearn\admin'
$stamp = Get-Date -Format 'yyyyMMddHHmmss'
$deployFolder = "_admin_deploy_$stamp"
$deployPath = Join-Path $deployRoot $deployFolder
$webConfigPath = Join-Path $deployRoot 'web.config'
$appOfflinePath = Join-Path $deployRoot 'app_offline.htm'

if (-not (Test-Path -LiteralPath $publishPath)) {
    Write-Host "Publishing iLearn.Admin to $publishPath"
    & dotnet publish 'iLearn.Admin/iLearn.Admin.csproj' -c Release -o $publishPath
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet publish failed for iLearn.Admin'
    }
}

Set-Content -LiteralPath $appOfflinePath -Value '<html><body>System updating...</body></html>' -Encoding utf8

try {
    New-Item -ItemType Directory -Path $deployPath -Force | Out-Null

    & robocopy $publishPath $deployPath /E /R:2 /W:2 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy app payload failed with exit code $LASTEXITCODE"
    }

    Get-ChildItem -LiteralPath $publishPath -Filter 'appsettings*.json' -File -ErrorAction SilentlyContinue |
        ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $deployRoot $_.Name) -Force
        }

    $publishedWwwroot = Join-Path $publishPath 'wwwroot'
    if (Test-Path -LiteralPath $publishedWwwroot) {
        $rootWwwroot = Join-Path $deployRoot 'wwwroot'
        New-Item -ItemType Directory -Path $rootWwwroot -Force | Out-Null

        & robocopy $publishedWwwroot $rootWwwroot /E /R:2 /W:2 /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) {
            throw "robocopy wwwroot sync failed with exit code $LASTEXITCODE"
        }
    }

    [xml]$doc = Get-Content -LiteralPath $webConfigPath
    $aspNetCoreNode = $doc.SelectSingleNode('//aspNetCore')
    if ($null -eq $aspNetCoreNode) {
        throw "aspNetCore node not found in $webConfigPath"
    }

    $aspNetCoreNode.SetAttribute('arguments', ".\\$deployFolder\\iLearn.Admin.dll")
    $doc.Save($webConfigPath)
}
finally {
    if (Test-Path -LiteralPath $appOfflinePath) {
        Remove-Item -LiteralPath $appOfflinePath -Force
    }
}

$activeLine = (Get-Content -LiteralPath $webConfigPath | Select-String 'aspNetCore processPath').Line

[pscustomobject]@{
    DeployRoot   = $deployRoot
    DeployPath   = $deployPath
    ActiveLine   = $activeLine
    DeployFolder = $deployFolder
} | Format-List
