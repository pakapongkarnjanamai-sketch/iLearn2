#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Stamp,

    [string]$AppPoolName = 'iLearnNew.User',

    [string]$IisHost = 'AP-NTC2138-QAWB',

    [pscredential]$IisCredential,

    [switch]$SkipPublish
)

$params = @{
    ProjectPath        = 'iLearn.User/iLearn.User.csproj'
    DeployRoot         = '\\10.10.143.39\wwwroot\iLearnNew'
    DllName            = 'iLearn.User.dll'
    DeployFolderPrefix = '_user_deploy_'
    PublishOutput      = 'artifacts/publish/iLearn.User'
    Configuration      = $Configuration
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

& (Join-Path $PSScriptRoot 'deploy-side-by-side.ps1') @params -WhatIf:$WhatIfPreference