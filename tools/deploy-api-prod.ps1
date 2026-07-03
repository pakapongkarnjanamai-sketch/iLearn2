#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Stamp,

    [string]$AppPoolName = 'iLearn.Service',

    [string]$IisHost = 'ap-ntc2137-prwb',

    [pscredential]$IisCredential,

    [ValidateSet('AppOffline', 'AppPool', 'None')]
    [string]$OfflineStrategy = 'AppOffline',

    [string]$HealthCheckUrl = 'https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me',

    [switch]$Rollback,

    [switch]$SkipPublish
)

$params = @{
    ProjectPath        = 'iLearn.API/iLearn.API.csproj'
    DeployRoot         = '\\ap-ntc2137-prwb\wwwroot\iLearn\Service'
    DllName            = 'iLearn.API.dll'
    DeployFolderPrefix = '_deploy_'
    PublishOutput      = 'artifacts/publish/iLearn.API.Service.prod'
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
