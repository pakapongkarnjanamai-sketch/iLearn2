#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Stamp,

    [string]$AppPoolName = 'iLearn.User',

    [string]$IisHost = 'ap-ntc2137-prwb',

    [pscredential]$IisCredential,

    [ValidateSet('AppOffline', 'AppPool', 'None')]
    [string]$OfflineStrategy = 'AppOffline',

    [string]$HealthCheckUrl = 'https://ap-ntc2137-prwb/iLearn/',

    [switch]$Rollback,

    [switch]$SkipPublish
)

$params = @{
    ProjectPath        = 'iLearn.User/iLearn.User.csproj'
    DeployRoot         = '\\ap-ntc2137-prwb\wwwroot\iLearn'
    DllName            = 'iLearn.User.dll'
    DeployFolderPrefix = '_user_deploy_'
    PublishOutput      = 'artifacts/publish/iLearn.User.prod'
    Configuration      = $Configuration
    OfflineStrategy    = $OfflineStrategy
    SkipPublish        = $SkipPublish
}

if ($PSBoundParameters.ContainsKey('Stamp')) {
    $params.Stamp = $Stamp
}
if ($AppPoolName) {
    $params.AppPoolName = $AppPoolName
    $params.IisHost = $IisHost
}
if ($IisCredential) {
    $params.IisCredential = $IisCredential
}
if ($HealthCheckUrl) {
    $params.HealthCheckUrl = $HealthCheckUrl
}
if ($Rollback) {
    $params.Rollback = $true
}

function Sync-RootAppleTouchIcon {
    param([Parameter(Mandatory)][string]$DeployRoot)

    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    $iconSource = Join-Path $repoRoot 'iLearn.User\wwwroot\apple-touch-icon.png'
    if (-not (Test-Path -LiteralPath $iconSource)) {
        throw "apple-touch-icon source not found: $iconSource"
    }

    $siteRoot = Split-Path -Path $DeployRoot -Parent
    foreach ($fileName in @('apple-touch-icon.png', 'apple-touch-icon-precomposed.png')) {
        $targetPath = Join-Path $siteRoot $fileName
        if ($PSCmdlet.ShouldProcess($targetPath, 'Sync iOS home-screen icon fallback')) {
            Copy-Item -LiteralPath $iconSource -Destination $targetPath -Force
        }
    }

    Write-Host "Synced root iOS icon fallbacks to $siteRoot" -ForegroundColor DarkGray
}

& (Join-Path $PSScriptRoot 'deploy-side-by-side.ps1') @params -WhatIf:$WhatIfPreference

if (-not $Rollback) {
    Sync-RootAppleTouchIcon -DeployRoot $params.DeployRoot
}
