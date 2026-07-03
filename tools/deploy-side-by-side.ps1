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

    [ValidateRange(1, 100)]
    [int]$KeepDeployments = 3,

    # How the app is taken offline during the file swap:
    #   AppOffline (default) — drop app_offline.htm at the app root. Needs ONLY file-write
    #                          permission on the share (which a deploy account already has),
    #                          so it never hits "Access is denied". ANCM drains + serves the
    #                          offline page, then restarts when the file is removed.
    #   AppPool              — Stop/Start the IIS app pool via WinRM remoting. Requires IIS
    #                          admin rights on $IisHost (use -IisCredential). Heavier (affects
    #                          every app in the pool) and the source of past Access-denied errors.
    #   None                 — no explicit drain; rely on ANCM auto-recycling when web.config
    #                          changes. Lowest privilege, but a brief window may serve mixed files.
    [ValidateSet('AppOffline', 'AppPool', 'None')]
    [string]$OfflineStrategy = 'AppOffline',

    [string]$AppPoolName,

    [string]$IisHost,

    [pscredential]$IisCredential,

    # Optional post-deploy smoke check. If set, the script polls this URL after the swap;
    # if it never responds with HTTP < 500, the web.config is auto-rolled back to the
    # previously active deployment. A 401/403 still counts as healthy (the app IS running).
    [string]$HealthCheckUrl,

    [ValidateRange(1, 50)]
    [int]$HealthCheckRetries = 5,

    [ValidateRange(1, 60)]
    [int]$HealthCheckDelaySeconds = 3,

    # Roll back to the previous side-by-side deployment (flip web.config to the most recent
    # retained folder that is not the currently active one) instead of publishing a new build.
    [switch]$Rollback,

    [switch]$SkipPublish
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

$AppOfflineHtml = @'
<!DOCTYPE html>
<html lang="th">
<head><meta charset="utf-8"><meta name="robots" content="noindex"><title>System Update</title></head>
<body style="font-family:Segoe UI,Tahoma,sans-serif;text-align:center;padding:64px 24px;color:#334155;">
  <h1 style="margin-bottom:8px;">ระบบกำลังอัปเดต</h1>
  <p>กรุณารอสักครู่แล้วลองใหม่อีกครั้ง</p>
  <p style="color:#64748b;">The system is being updated. Please try again in a moment.</p>
</body>
</html>
'@

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

function Get-AspNetCoreArguments {
    param([Parameter(Mandatory)][string]$WebConfigPath)

    [xml]$doc = Get-Content -LiteralPath $WebConfigPath
    $node = $doc.SelectSingleNode('//aspNetCore')
    if ($null -eq $node) {
        throw "aspNetCore node not found in $WebConfigPath"
    }

    return [string]$node.GetAttribute('arguments')
}

function Set-AspNetCoreArguments {
    param(
        [Parameter(Mandatory)][string]$WebConfigPath,
        [Parameter(Mandatory)][string]$Arguments
    )

    [xml]$doc = Get-Content -LiteralPath $WebConfigPath
    $node = $doc.SelectSingleNode('//aspNetCore')
    if ($null -eq $node) {
        throw "aspNetCore node not found in $WebConfigPath"
    }

    $node.SetAttribute('arguments', $Arguments)
    $doc.Save($WebConfigPath)
}

function Get-DeployFolderFromArguments {
    param([string]$Arguments)

    # arguments look like ".\_deploy_20260630120000\iLearn.API.dll"
    # Tolerate legacy double-backslash (".\\_deploy_...\\app.dll") too.
    if ([string]::IsNullOrWhiteSpace($Arguments)) { return $null }
    $trimmed = $Arguments -replace '^[.\\/]+', ''
    return ($trimmed -split '[\\/]+')[0]
}

function Invoke-AppPoolAction {
    param(
        [Parameter(Mandatory)]
        [string]$PoolName,

        [Parameter(Mandatory)]
        [string]$TargetHost,

        [ValidateSet('Stop', 'Start')]
        [string]$Action,

        [pscredential]$Credential
    )

    $remoteParams = @{
        ComputerName = $TargetHost
        ScriptBlock  = {
            param($RemotePoolName, $RemoteAction)

            Import-Module WebAdministration

            $poolState = Get-WebAppPoolState -Name $RemotePoolName -ErrorAction SilentlyContinue
            if ($null -eq $poolState) {
                throw "App pool not found: $RemotePoolName"
            }

            if ($RemoteAction -eq 'Stop') {
                if ($poolState.Value -eq 'Stopped') {
                    return "App pool already stopped: $RemotePoolName"
                }

                Stop-WebAppPool -Name $RemotePoolName
                return "Stopped app pool: $RemotePoolName"
            }

            if ($poolState.Value -eq 'Started') {
                return "App pool already started: $RemotePoolName"
            }

            Start-WebAppPool -Name $RemotePoolName
            return "Started app pool: $RemotePoolName"
        }
        ArgumentList = @($PoolName, $Action)
    }

    if ($Credential) {
        $remoteParams.Credential = $Credential
    }

    return Invoke-Command @remoteParams
}

function Test-DeploymentHealth {
    param(
        [Parameter(Mandatory)][string]$Url,
        [int]$Retries = 5,
        [int]$DelaySeconds = 3
    )

    for ($attempt = 1; $attempt -le $Retries; $attempt++) {
        try {
            # No credentials are sent: a 401/403 (auth-gated endpoint) already proves the app is
            # up, and -UseDefaultCredentials would make Invoke-WebRequest refuse plain-HTTP URLs.
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing `
                -TimeoutSec 30 -SkipHttpErrorCheck -MaximumRedirection 5
            $status = [int]$response.StatusCode

            # The app responded with anything below 500 => ANCM started it and it is serving
            # requests (401/403/404 still mean the process is up). 5xx / no response => not up.
            if ($status -lt 500) {
                Write-Host "Health check OK (HTTP $status) on attempt $attempt/$Retries" -ForegroundColor DarkGray
                return $true
            }

            Write-Host "Health check attempt $attempt/$Retries returned HTTP $status" -ForegroundColor DarkYellow
        }
        catch {
            Write-Host "Health check attempt $attempt/$Retries failed: $($_.Exception.Message)" -ForegroundColor DarkYellow
        }

        if ($attempt -lt $Retries) {
            Start-Sleep -Seconds $DelaySeconds
        }
    }

    return $false
}

# ── Validation ───────────────────────────────────────────────────────────────
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

if ($OfflineStrategy -eq 'AppPool' -and -not ($AppPoolName -and $IisHost)) {
    throw "OfflineStrategy 'AppPool' requires both -AppPoolName and -IisHost (and IIS admin rights, e.g. -IisCredential)."
}

$appOfflinePath = Join-Path $DeployRoot 'app_offline.htm'

# ── Rollback path ────────────────────────────────────────────────────────────
if ($Rollback) {
    $currentArguments = Get-AspNetCoreArguments -WebConfigPath $webConfigPath
    $currentFolder = Get-DeployFolderFromArguments -Arguments $currentArguments

    $deployDirs = @(Get-ChildItem -LiteralPath $DeployRoot -Directory -Filter "$DeployFolderPrefix*" |
        Sort-Object Name -Descending)

    $target = $deployDirs | Where-Object { $_.Name -ne $currentFolder } | Select-Object -First 1
    if ($null -eq $target) {
        throw "No previous deployment found to roll back to (current: $currentFolder)."
    }

    $rollbackArguments = ".\$($target.Name)\$DllName"

    if ($PSCmdlet.ShouldProcess($webConfigPath, "Roll back aspNetCore arguments to $rollbackArguments")) {
        Set-AspNetCoreArguments -WebConfigPath $webConfigPath -Arguments $rollbackArguments
        Write-Host "Rolled back to previous deployment: $($target.Name)" -ForegroundColor Yellow
    }

    [pscustomobject]@{
        Action             = 'Rollback'
        DeployRoot         = $DeployRoot
        PreviousActive     = $currentFolder
        RolledBackTo       = $target.Name
        WebConfigArguments = $rollbackArguments
    } | Format-List
    return
}

# ── New deployment ───────────────────────────────────────────────────────────
$deployFolderName = "$DeployFolderPrefix$Stamp"
$deployPath = Join-Path $DeployRoot $deployFolderName
if (Test-Path -LiteralPath $deployPath) {
    throw "Deploy target already exists: $deployPath"
}

$webConfigArguments = ".\$deployFolderName\$DllName"
$previousArguments = Get-AspNetCoreArguments -WebConfigPath $webConfigPath

$appOfflineCreated = $false
$broughtOnline = $false
$stoppedAppPool = $false
$startedAppPool = $false
$deploymentFailed = $false
$autoRolledBack = $false
$removedCount = 0

# ── Publish ──────────────────────────────────────────────────────────────────
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

# ── Take the app offline (drain) ─────────────────────────────────────────────
if ($OfflineStrategy -eq 'AppPool') {
    if ($PSCmdlet.ShouldProcess($AppPoolName, "Stop app pool on $IisHost")) {
        try {
            $stopResult = Invoke-AppPoolAction -PoolName $AppPoolName -TargetHost $IisHost -Action 'Stop' -Credential $IisCredential
            Write-Host $stopResult -ForegroundColor DarkGray
            $stoppedAppPool = $true
        }
        catch {
            throw "Could not stop app pool '$AppPoolName' on ${IisHost}: $($_.Exception.Message)"
        }
    }
}
elseif ($OfflineStrategy -eq 'AppOffline') {
    if ($PSCmdlet.ShouldProcess($appOfflinePath, "Create app_offline.htm (graceful drain)")) {
        Set-Content -LiteralPath $appOfflinePath -Value $AppOfflineHtml -Encoding utf8
        $appOfflineCreated = $true
        # Give ANCM a moment to notice app_offline.htm and drain in-flight requests.
        Start-Sleep -Seconds 2
    }
}

try {
    Write-Host "Copying publish output to $deployPath"
    if ($PSCmdlet.ShouldProcess($deployPath, "Copy publish output from $resolvedPublishOutput")) {
        New-Item -ItemType Directory -Path $deployPath -Force | Out-Null

        $publishEntries = Get-ChildItem -LiteralPath $resolvedPublishOutput -Force
        foreach ($publishEntry in $publishEntries) {
            Copy-Item -LiteralPath $publishEntry.FullName -Destination $deployPath -Recurse -Force
        }
    }

    # Sync root-level appsettings (ContentRoot for a side-by-side DLL resolves to the app root,
    # not the _deploy_* folder, so config + static assets must live at the root).
    # Back up the existing root config first so a clobber of manual prod overrides is recoverable.
    $configBackupDir = Join-Path $deployPath '_prev-root-config'
    $appSettingsFiles = Get-ChildItem -LiteralPath $resolvedPublishOutput -Filter 'appsettings*.json' -File -ErrorAction SilentlyContinue
    foreach ($appSettingsFile in $appSettingsFiles) {
        $rootConfigPath = Join-Path $DeployRoot $appSettingsFile.Name
        if ($PSCmdlet.ShouldProcess($rootConfigPath, "Sync config file from $($appSettingsFile.FullName)")) {
            if (Test-Path -LiteralPath $rootConfigPath) {
                New-Item -ItemType Directory -Path $configBackupDir -Force | Out-Null
                Copy-Item -LiteralPath $rootConfigPath -Destination (Join-Path $configBackupDir $appSettingsFile.Name) -Force
            }
            Copy-Item -LiteralPath $appSettingsFile.FullName -Destination $rootConfigPath -Force
        }
    }

    $publishedWwwrootPath = Join-Path $resolvedPublishOutput 'wwwroot'
    if (Test-Path -LiteralPath $publishedWwwrootPath) {
        $rootWwwrootPath = Join-Path $DeployRoot 'wwwroot'
        if ($PSCmdlet.ShouldProcess($rootWwwrootPath, "Sync static web assets from $publishedWwwrootPath")) {
            New-Item -ItemType Directory -Path $rootWwwrootPath -Force | Out-Null

            $publishedWwwrootEntries = Get-ChildItem -LiteralPath $publishedWwwrootPath -Force
            foreach ($publishedWwwrootEntry in $publishedWwwrootEntries) {
                Copy-Item -LiteralPath $publishedWwwrootEntry.FullName -Destination $rootWwwrootPath -Recurse -Force
            }
        }
    }

    # Flip the active deployment by repointing aspNetCore arguments at the new folder.
    if ($PSCmdlet.ShouldProcess($webConfigPath, "Set aspNetCore arguments to $webConfigArguments")) {
        Set-AspNetCoreArguments -WebConfigPath $webConfigPath -Arguments $webConfigArguments
    }

    # --- Cleanup old deploy folders, keeping the $KeepDeployments most recent ---
    $allDeployDirs = @(Get-ChildItem -LiteralPath $DeployRoot -Directory -Filter "$DeployFolderPrefix*" |
        Sort-Object Name -Descending)

    if ($allDeployDirs.Count -gt $KeepDeployments) {
        $staleDeployDirs = $allDeployDirs | Select-Object -Skip $KeepDeployments
        foreach ($staleDir in $staleDeployDirs) {
            if ($PSCmdlet.ShouldProcess($staleDir.FullName, "Remove stale deploy folder")) {
                try {
                    # Use cmd /c rd for UNC paths — Remove-Item hits "directory not empty"
                    # race conditions on network shares
                    & cmd /c rd /s /q $staleDir.FullName 2>&1 | Out-Null
                    if (Test-Path -LiteralPath $staleDir.FullName) {
                        # Retry once after a short pause if the first pass left remnants
                        Start-Sleep -Milliseconds 500
                        & cmd /c rd /s /q $staleDir.FullName 2>&1 | Out-Null
                    }
                    if (-not (Test-Path -LiteralPath $staleDir.FullName)) {
                        $removedCount++
                    }
                    else {
                        Write-Warning "Partially removed stale deploy folder: $($staleDir.FullName)"
                    }
                }
                catch {
                    Write-Warning "Could not remove stale deploy folder: $($staleDir.FullName) — $($_.Exception.Message)"
                }
            }
        }
        if ($removedCount -gt 0) {
            Write-Host "Cleaned up $removedCount stale deploy folder(s), kept $KeepDeployments most recent." -ForegroundColor DarkGray
        }
    }

    # Bring the app back online BEFORE the health check so the new build can answer.
    if ($appOfflineCreated -and (Test-Path -LiteralPath $appOfflinePath)) {
        if ($PSCmdlet.ShouldProcess($appOfflinePath, "Remove app_offline.htm (bring app online)")) {
            Remove-Item -LiteralPath $appOfflinePath -Force
            $broughtOnline = $true
        }
    }
    if ($stoppedAppPool) {
        if ($PSCmdlet.ShouldProcess($AppPoolName, "Start app pool on $IisHost")) {
            $startResult = Invoke-AppPoolAction -PoolName $AppPoolName -TargetHost $IisHost -Action 'Start' -Credential $IisCredential
            Write-Host $startResult -ForegroundColor DarkGray
            $startedAppPool = $true
        }
    }

    # ── Post-deploy health check (optional) → auto-rollback on failure ──
    if ($HealthCheckUrl -and -not $WhatIfPreference) {
        Write-Host "Running post-deploy health check: $HealthCheckUrl"
        if (-not (Test-DeploymentHealth -Url $HealthCheckUrl -Retries $HealthCheckRetries -DelaySeconds $HealthCheckDelaySeconds)) {
            throw "Health check failed for $HealthCheckUrl after deploy."
        }
    }
}
catch {
    $deploymentFailed = $true

    # Auto-rollback: repoint web.config at the previously active deployment.
    if (-not $WhatIfPreference -and $previousArguments -and $previousArguments -ne $webConfigArguments) {
        try {
            Set-AspNetCoreArguments -WebConfigPath $webConfigPath -Arguments $previousArguments
            $autoRolledBack = $true
            Write-Warning "Auto-rolled back web.config to previous deployment: $previousArguments"
        }
        catch {
            Write-Warning "Auto-rollback failed: $($_.Exception.Message)"
        }
    }

    throw
}
finally {
    # Invariant: never leave the site offline.
    if ($appOfflineCreated -and -not $broughtOnline -and (Test-Path -LiteralPath $appOfflinePath)) {
        Remove-Item -LiteralPath $appOfflinePath -Force -ErrorAction SilentlyContinue
        $broughtOnline = $true
    }

    # Invariant: never leave the app pool stopped.
    if ($stoppedAppPool -and -not $startedAppPool) {
        try {
            $startResult = Invoke-AppPoolAction -PoolName $AppPoolName -TargetHost $IisHost -Action 'Start' -Credential $IisCredential
            Write-Host $startResult -ForegroundColor DarkGray
            $startedAppPool = $true
        }
        catch {
            Write-Warning "Could not start app pool '$AppPoolName' on ${IisHost}: $($_.Exception.Message)"
        }
    }
}

[pscustomobject]@{
    ProjectPath        = $resolvedProjectPath
    PublishOutput      = $resolvedPublishOutput
    DeployRoot         = $DeployRoot
    DeployPath         = $deployPath
    WebConfigPath      = $webConfigPath
    WebConfigArguments = $webConfigArguments
    PreviousArguments  = $previousArguments
    Stamp              = $Stamp
    OfflineStrategy    = $OfflineStrategy
    SkippedPublish     = [bool]$SkipPublish
    StoppedAppPool     = $stoppedAppPool
    StartedAppPool     = $startedAppPool
    HealthChecked      = [bool]($HealthCheckUrl -and -not $WhatIfPreference)
    AutoRolledBack     = $autoRolledBack
    RemovedStale       = $removedCount
} | Format-List
