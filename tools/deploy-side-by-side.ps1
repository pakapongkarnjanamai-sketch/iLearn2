#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$ProjectPath,

    [Parameter(Mandatory)]
    [string]$DeployRoot,

    [Parameter(Mandatory)]
    [string]$DllName,

    [Parameter(Mandatory)]
    [string]$DeployFolderPrefix,

    [string]$PublishOutput,

    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Stamp = (Get-Date -Format 'yyyyMMddHHmmss'),

    [switch]$SkipPublish
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Resolve-RepoPath {
    param(
        [Parameter(Mandatory)]
        [string]$Path,

        [Parameter(Mandatory)]
        [string]$BasePath
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

$resolvedProjectPath = Resolve-RepoPath -Path $ProjectPath -BasePath $repoRoot
if (-not (Test-Path -LiteralPath $resolvedProjectPath)) {
    throw "Project file not found: $resolvedProjectPath"
}

if ([string]::IsNullOrWhiteSpace($PublishOutput)) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($resolvedProjectPath)
    $PublishOutput = "artifacts/publish/$projectName"
}

$resolvedPublishOutput = Resolve-RepoPath -Path $PublishOutput -BasePath $repoRoot

if (-not (Test-Path -LiteralPath $DeployRoot)) {
    throw "Deploy root not found: $DeployRoot"
}

$webConfigPath = Join-Path $DeployRoot 'web.config'
if (-not (Test-Path -LiteralPath $webConfigPath)) {
    throw "web.config not found: $webConfigPath"
}

$deployFolderName = "$DeployFolderPrefix$Stamp"
$deployPath = Join-Path $DeployRoot $deployFolderName
if (Test-Path -LiteralPath $deployPath) {
    throw "Deploy target already exists: $deployPath"
}

$webConfigArguments = ".\\$deployFolderName\\$DllName"

if (-not $SkipPublish) {
    if ($PSCmdlet.ShouldProcess($resolvedPublishOutput, "Refresh publish output for $resolvedProjectPath")) {
        if (Test-Path -LiteralPath $resolvedPublishOutput) {
            Remove-Item -LiteralPath $resolvedPublishOutput -Recurse -Force
        }

        New-Item -ItemType Directory -Path $resolvedPublishOutput -Force | Out-Null

        Write-Host "Publishing $resolvedProjectPath"
        & dotnet publish $resolvedProjectPath -c $Configuration -o $resolvedPublishOutput
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed for $resolvedProjectPath"
        }
    }
}

if (-not (Test-Path -LiteralPath $resolvedPublishOutput)) {
    if ($WhatIfPreference) {
        Write-Warning "Publish output not found during WhatIf dry-run: $resolvedPublishOutput"
    } else {
        throw "Publish output not found: $resolvedPublishOutput"
    }
}

Write-Host "Copying publish output to $deployPath"
if ($PSCmdlet.ShouldProcess($deployPath, "Copy publish output from $resolvedPublishOutput")) {
    New-Item -ItemType Directory -Path $deployPath -Force | Out-Null
    Copy-Item -Path (Join-Path $resolvedPublishOutput '*') -Destination $deployPath -Recurse -Force
}

$appSettingsFiles = Get-ChildItem -LiteralPath $resolvedPublishOutput -Filter 'appsettings*.json' -File -ErrorAction SilentlyContinue
foreach ($appSettingsFile in $appSettingsFiles) {
    $rootConfigPath = Join-Path $DeployRoot $appSettingsFile.Name
    if ($PSCmdlet.ShouldProcess($rootConfigPath, "Sync config file from $($appSettingsFile.FullName)")) {
        Copy-Item -LiteralPath $appSettingsFile.FullName -Destination $rootConfigPath -Force
    }
}

[xml]$webConfig = Get-Content -LiteralPath $webConfigPath
$aspNetCoreNode = $webConfig.SelectSingleNode('//aspNetCore')
if ($null -eq $aspNetCoreNode) {
    throw "aspNetCore node not found in $webConfigPath"
}

if ($PSCmdlet.ShouldProcess($webConfigPath, "Set aspNetCore arguments to $webConfigArguments")) {
    $aspNetCoreNode.SetAttribute('arguments', $webConfigArguments)
    $webConfig.Save($webConfigPath)
}

[pscustomobject]@{
    ProjectPath        = $resolvedProjectPath
    PublishOutput      = $resolvedPublishOutput
    DeployRoot         = $DeployRoot
    DeployPath         = $deployPath
    WebConfigPath      = $webConfigPath
    WebConfigArguments = $webConfigArguments
    Stamp              = $Stamp
    SkippedPublish     = [bool]$SkipPublish
} | Format-List