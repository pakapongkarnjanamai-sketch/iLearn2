#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Stamp,

    [string]$AppPoolName = 'iLearn.Admin',

    [string]$IisHost = 'ap-ntc2137-prwb',

    [pscredential]$IisCredential,

    [ValidateSet('AppOffline', 'AppPool', 'None')]
    [string]$OfflineStrategy = 'AppOffline',

    [string]$HealthCheckUrl = 'https://ap-ntc2137-prwb/iLearn/admin/',

    [switch]$Rollback,

    [switch]$SkipPublish
)

$params = @{
    ProjectPath        = 'iLearn.Admin/iLearn.Admin.csproj'
    DeployRoot         = '\\ap-ntc2137-prwb\wwwroot\iLearn\admin'
    DllName            = 'iLearn.Admin.dll'
    DeployFolderPrefix = '_admin_deploy_'
    PublishOutput      = 'artifacts/publish/iLearn.Admin.prod'
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
