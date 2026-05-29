#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Stamp,

    [string]$AppPoolName = 'iLearnNew.Service',

    [string]$IisHost = 'AP-NTC2138-QAWB',

    [pscredential]$IisCredential,

    [switch]$SkipPublish
)

$params = @{
    ProjectPath        = 'iLearn.API/iLearn.API.csproj'
    DeployRoot         = '\\10.10.143.39\wwwroot\iLearnNew\Service'
    DllName            = 'iLearn.API.dll'
    DeployFolderPrefix = '_deploy_'
    PublishOutput      = 'artifacts/publish/iLearn.API.Service'
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