<#
.SYNOPSIS
    PLAN-048: Cutover prod from QA DB to real prod DB (10.10.154.119)

.DESCRIPTION
    Interactive runbook for the DB migration cutover.
    Walks through each step with confirmations.

    Pre-requisites:
    - .bak file already restored on 10.10.154.119 (run 01/02/03 SQL scripts first)
    - Prod app pool name: iLearnService
    - Prod web root: \\ap-ntc2137-prwb\wwwroot\iLearn\Service

.NOTES
    Run from a machine that can reach both prod web server and prod DB server.
    Must run as a user with access to the prod IIS share.
#>
#requires -Version 7.0

[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ProdWebShare = '\\ap-ntc2137-prwb\wwwroot\iLearn\Service',
    [string]$ProdDbServer = '10.10.154.119',
    [string]$ProdDbName   = 'iLearnDB_New',
    [string]$AppPoolName  = 'iLearnService',
    [string]$IisHost      = 'ap-ntc2137-prwb',
    [string]$HealthUrl    = 'https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me',
    [pscredential]$IisCredential
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Step, [string]$Description)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  $Step" -ForegroundColor Cyan
    Write-Host "  $Description" -ForegroundColor DarkGray
    Write-Host "========================================" -ForegroundColor Cyan
}

function Confirm-Step {
    param([string]$Message)
    $answer = Read-Host "$Message (y/n)"
    if ($answer -ne 'y') {
        Write-Warning "Aborted by user."
        exit 1
    }
}

# ──────────────────────────────────────────────
# Step 0: Pre-flight checks
# ──────────────────────────────────────────────
Write-Step "Step 0" "Pre-flight checks"

Write-Host "Checking prod web share access..." -NoNewline
if (-not (Test-Path $ProdWebShare)) {
    Write-Error "Cannot access $ProdWebShare — check credentials/share"
}
Write-Host " OK" -ForegroundColor Green

$configPath = Join-Path $ProdWebShare 'appsettings.json'
Write-Host "Current base config connection:" -NoNewline
if (Test-Path $configPath) {
    $baseConfig = Get-Content $configPath -Raw | ConvertFrom-Json
    $currentCs = $baseConfig.ConnectionStrings.DefaultConnection
    Write-Host " $($currentCs.Substring(0, [Math]::Min(60, $currentCs.Length)))..." -ForegroundColor Yellow
} else {
    Write-Warning "appsettings.json not found at $ProdWebShare"
}

$prodConfigPath = Join-Path $ProdWebShare 'appsettings.Production.json'
Write-Host "Current Production config:" -NoNewline
if (Test-Path $prodConfigPath) {
    $prodConfig = Get-Content $prodConfigPath -Raw
    Write-Host "`n$prodConfig" -ForegroundColor Yellow
} else {
    Write-Host " (not found)" -ForegroundColor Yellow
}

Confirm-Step "Pre-flight OK? Proceed to freeze prod?"

# ──────────────────────────────────────────────
# Step 1: Freeze prod (app_offline.htm)
# ──────────────────────────────────────────────
Write-Step "Step 1" "Freeze prod — place app_offline.htm"

$appOfflinePath = Join-Path $ProdWebShare 'app_offline.htm'
if ($PSCmdlet.ShouldProcess($appOfflinePath, 'Create app_offline.htm')) {
    @'
<!DOCTYPE html>
<html>
<head><title>iLearn — Maintenance</title></head>
<body style="font-family:Segoe UI,sans-serif;text-align:center;padding:60px;">
  <h1>&#128736; ระบบกำลังบำรุงรักษา</h1>
  <p>ระบบ iLearn กำลังอัปเกรดฐานข้อมูล กรุณารอสักครู่</p>
  <p style="color:#888;">System is under maintenance. Please try again shortly.</p>
</body>
</html>
'@ | Set-Content -Path $appOfflinePath -Encoding utf8NoBOM
    Write-Host "app_offline.htm placed at $appOfflinePath" -ForegroundColor Green
}

Write-Host "`n>>> Now run the SQL backup on QA (01-backup-qa-db.sql)" -ForegroundColor Magenta
Write-Host ">>> Then restore on prod DB server (02-restore-prod-db.sql)" -ForegroundColor Magenta
Write-Host ">>> Then verify (03-verify-restored-db.sql)" -ForegroundColor Magenta
Confirm-Step "DB backup + restore + verify complete? Proceed to switch connection?"

# ──────────────────────────────────────────────
# Step 2: Update connection string on prod
# ──────────────────────────────────────────────
Write-Step "Step 2" "Update connection string in appsettings.Production.json"

Write-Host "Enter the prod DB sa password (will be embedded in config):"
$saPassword = Read-Host -AsSecureString "sa password"
$saPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($saPassword)
)

$newConnString = "Data Source=$ProdDbServer;Database=$ProdDbName;Persist Security Info=True;User ID=sa;Password=$saPasswordPlain;Trust Server Certificate=True"

# Read existing prod config
if (Test-Path $prodConfigPath) {
    $prodConfigObj = Get-Content $prodConfigPath -Raw | ConvertFrom-Json
} else {
    $prodConfigObj = [pscustomobject]@{}
}

# Add/update ConnectionStrings
if (-not $prodConfigObj.PSObject.Properties['ConnectionStrings']) {
    $prodConfigObj | Add-Member -NotePropertyName 'ConnectionStrings' -NotePropertyValue ([pscustomobject]@{
        DefaultConnection = $newConnString
    })
} else {
    $prodConfigObj.ConnectionStrings.DefaultConnection = $newConnString
}

if ($PSCmdlet.ShouldProcess($prodConfigPath, 'Write updated connection string')) {
    $prodConfigObj | ConvertTo-Json -Depth 10 | Set-Content -Path $prodConfigPath -Encoding utf8NoBOM
    Write-Host "Updated $prodConfigPath with prod DB connection" -ForegroundColor Green
    Write-Host "Connection: Data Source=$ProdDbServer;Database=$ProdDbName;..." -ForegroundColor DarkGray
}

# Clear plaintext from memory
$saPasswordPlain = $null
[System.GC]::Collect()

# ──────────────────────────────────────────────
# Step 3: Restart app pool
# ──────────────────────────────────────────────
Write-Step "Step 3" "Restart app pool $AppPoolName"

$restartCmd = "Invoke-Command -ComputerName $IisHost -ScriptBlock { Import-Module WebAdministration; Restart-WebAppPool '$AppPoolName' }"
Write-Host "Will run: $restartCmd" -ForegroundColor DarkGray

if ($IisCredential) {
    $restartCmd = "Invoke-Command -ComputerName $IisHost -Credential `$IisCredential -ScriptBlock { Import-Module WebAdministration; Restart-WebAppPool '$AppPoolName' }"
}

Confirm-Step "Restart app pool?"

try {
    if ($IisCredential) {
        Invoke-Command -ComputerName $IisHost -Credential $IisCredential -ScriptBlock {
            param($pool)
            Import-Module WebAdministration
            Restart-WebAppPool $pool
            Write-Output "App pool '$pool' restarted"
        } -ArgumentList $AppPoolName
    } else {
        Invoke-Command -ComputerName $IisHost -ScriptBlock {
            param($pool)
            Import-Module WebAdministration
            Restart-WebAppPool $pool
            Write-Output "App pool '$pool' restarted"
        } -ArgumentList $AppPoolName
    }
    Write-Host "App pool restarted successfully" -ForegroundColor Green
} catch {
    Write-Warning "Could not restart app pool remotely: $_"
    Write-Host "Please restart app pool '$AppPoolName' manually on $IisHost, then press Enter." -ForegroundColor Yellow
    Read-Host
}

# ──────────────────────────────────────────────
# Step 4: Remove app_offline.htm
# ──────────────────────────────────────────────
Write-Step "Step 4" "Remove app_offline.htm (bring app back online)"

if (Test-Path $appOfflinePath) {
    if ($PSCmdlet.ShouldProcess($appOfflinePath, 'Remove app_offline.htm')) {
        Remove-Item $appOfflinePath -Force
        Write-Host "app_offline.htm removed" -ForegroundColor Green
    }
} else {
    Write-Host "app_offline.htm already gone" -ForegroundColor DarkGray
}

# ──────────────────────────────────────────────
# Step 5: Smoke test
# ──────────────────────────────────────────────
Write-Step "Step 5" "Smoke test — verify prod connects to new DB"

Start-Sleep -Seconds 3   # Give the app pool a moment to warm up

Write-Host "Testing $HealthUrl ..."
try {
    $resp = Invoke-WebRequest -Uri $HealthUrl -UseDefaultCredentials -TimeoutSec 30 -UseBasicParsing
    Write-Host "Health check: $($resp.StatusCode) $($resp.StatusDescription)" -ForegroundColor Green
    if ($resp.StatusCode -eq 200) {
        Write-Host "Response preview: $($resp.Content.Substring(0, [Math]::Min(200, $resp.Content.Length)))" -ForegroundColor DarkGray
    }
} catch {
    Write-Warning "Health check failed: $_"
    Write-Host "Verify manually that the API is responding." -ForegroundColor Yellow
}

# ──────────────────────────────────────────────
# Done
# ──────────────────────────────────────────────
Write-Step "DONE" "PLAN-048 cutover complete"
Write-Host @"

Post-cutover checklist:
  1. Verify @@SERVERNAME returns prod DB server (run 04-post-cutover-verify.sql)
  2. Browse course catalog — Open courses visible
  3. Launch 2-3 SCORM courses — content plays (GUIDs match D:\iLearnContent)
  4. Complete a course — progress saved to prod DB (not QA)
  5. Compare response times vs before (should be faster, no QA contention)
  6. Keep QA iLearnDB_New as fallback until prod DB is stable
  7. Update connection string documentation
"@ -ForegroundColor DarkGray
