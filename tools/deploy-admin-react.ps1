#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$DeployRoot = '\\AP-NTC2138-QAWB\wwwroot\iLearn\admin-react',

    [string]$DistPath = 'iLearn.Admin.React/dist',

    [switch]$SkipBuild,

    [switch]$SkipLint
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$resolvedDistPath = if ([System.IO.Path]::IsPathRooted($DistPath)) {
    [System.IO.Path]::GetFullPath($DistPath)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $DistPath))
}

if (-not $SkipBuild) {
    & (Join-Path $PSScriptRoot 'build-admin-react-prod.ps1') -SkipLint:$SkipLint
}

if (-not (Test-Path -LiteralPath $resolvedDistPath)) {
    throw "Dist path not found: $resolvedDistPath"
}

if ($PSCmdlet.ShouldProcess($DeployRoot, 'Ensure deploy root exists')) {
    New-Item -ItemType Directory -Path $DeployRoot -Force | Out-Null
}

$copySucceeded = $false
if ($PSCmdlet.ShouldProcess($DeployRoot, "Copy static files from $resolvedDistPath")) {
    $robocopyArgs = @(
        $resolvedDistPath,
        $DeployRoot,
        '/E',
        '/R:2',
        '/W:2',
        '/NFL',
        '/NDL',
        '/NJH',
        '/NJS',
        '/NP'
    )

    & robocopy @robocopyArgs | Out-Null
    $robocopyExitCode = $LASTEXITCODE

    if ($robocopyExitCode -ge 8) {
        throw "robocopy failed with exit code $robocopyExitCode"
    }

    $copySucceeded = $true
}

[pscustomobject]@{
    DistPath         = $resolvedDistPath
    DeployRoot       = $DeployRoot
    SkipBuild        = [bool]$SkipBuild
    CopySucceeded    = $copySucceeded
    RobocopyExitCode = $LASTEXITCODE
} | Format-List
