#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Stamp,

    [switch]$SkipPublish
)

$params = @{
    ProjectPath        = 'iLearn.Admin/iLearn.Admin.csproj'
    DeployRoot         = '\\10.10.143.39\wwwroot\iLearnNew\admin'
    DllName            = 'iLearn.Admin.dll'
    DeployFolderPrefix = '_admin_deploy_'
    PublishOutput      = 'artifacts/publish/iLearn.Admin'
    Configuration      = $Configuration
    SkipPublish        = $SkipPublish
}

if ($PSBoundParameters.ContainsKey('Stamp')) {
    $params.Stamp = $Stamp
}

& (Join-Path $PSScriptRoot 'deploy-side-by-side.ps1') @params -WhatIf:$WhatIfPreference