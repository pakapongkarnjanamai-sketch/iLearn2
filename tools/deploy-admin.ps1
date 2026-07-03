#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Stamp,

    [string]$AppPoolName = 'iLearn.Admin',

    [string]$IisHost = 'AP-NTC2138-QAWB',

    [pscredential]$IisCredential,

    # Default offline strategy needs only file-write permission (no IIS admin / WinRM).
    [ValidateSet('AppOffline', 'AppPool', 'None')]
    [string]$OfflineStrategy = 'AppOffline',

    # Optional post-deploy smoke check (auto-rollback on failure). Opt-in — confirm the deploy
    # host can reach QA first. Suggested: 'https://ap-ntc2138-qawb/iLearn/admin/'
    [string]$HealthCheckUrl = '',

    [switch]$Rollback,

    [switch]$SkipPublish
)

$params = @{
    ProjectPath        = 'iLearn.Admin/iLearn.Admin.csproj'
    DeployRoot         = '\\AP-NTC2138-QAWB\wwwroot\iLearn\admin'
    DllName            = 'iLearn.Admin.dll'
    DeployFolderPrefix = '_admin_deploy_'
    PublishOutput      = 'artifacts/publish/iLearn.Admin'
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

& (Join-Path $PSScriptRoot 'deploy-side-by-side.ps1') @params -WhatIf:$WhatIfPreference