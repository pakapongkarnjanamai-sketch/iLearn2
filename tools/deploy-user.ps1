#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Stamp,

    [string]$AppPoolName = 'iLearn.User',

    [string]$IisHost = 'AP-NTC2138-QAWB',

    [pscredential]$IisCredential,

    # Default offline strategy needs only file-write permission (no IIS admin / WinRM).
    [ValidateSet('AppOffline', 'AppPool', 'None')]
    [string]$OfflineStrategy = 'AppOffline',

    # Optional post-deploy smoke check (auto-rollback on failure). Opt-in — confirm the deploy
    # host can reach QA first. Suggested: 'https://ap-ntc2138-qawb/iLearn/'
    [string]$HealthCheckUrl = '',

    [switch]$Rollback,

    [switch]$SkipPublish
)

$params = @{
    ProjectPath        = 'iLearn.User/iLearn.User.csproj'
    DeployRoot         = '\\AP-NTC2138-QAWB\wwwroot\iLearn'
    DllName            = 'iLearn.User.dll'
    DeployFolderPrefix = '_user_deploy_'
    PublishOutput      = 'artifacts/publish/iLearn.User'
    Configuration      = $Configuration
    OfflineStrategy    = $OfflineStrategy
    SkipPublish        = $SkipPublish
    # QA: never sync appsettings.Production.json to the server (it belongs only on PROD)
    ExcludeConfigFiles = @('appsettings.Production.json')
    # QA: pin environment so appsettings.Production.json can't be loaded even if it lands here
    SetEnvironmentName = 'Staging'
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