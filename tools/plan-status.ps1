<#
.SYNOPSIS
    สรุปสถานะแผนทั้งหมดใน DOC/PLANS/ — หาเลขแผนถัดไป, งานที่รอรีวิว, และไฟล์ที่ header ผิด format

.DESCRIPTION
    แก้ปัญหาที่ status ของแผนตอบคำถามพื้นฐานไม่ได้ เพราะเขียนกันคนละ format:
      `- **Status:** DONE` / `- **Status**: DONE` / `- **สถานะ:** DONE` / `Status: VERIFIED` / ไม่มีเลย
    และหลายไฟล์ยัดคำอธิบายยาว ๆ ต่อท้ายค่า status (`VERIFIED ✅ (Claude review 2026-06-16: ...)`)
    ทำให้ `grep Status` ได้ค่าที่ไม่ซ้ำกันเป็นสิบ ๆ แบบ

    parser นี้ **ทนทุก format ที่มีอยู่จริง** (ไม่ต้องไล่แก้ไฟล์เก่า) โดย:
      - อ่าน 15 บรรทัดแรกของแต่ละแผน หา key `Status` หรือ `สถานะ`
      - ตัดคำอธิบายท้ายค่าออก เหลือเฉพาะ state
      - ถ้าเป็น chain (`DONE → VERIFIED`) ใช้ state **สุดท้าย** เป็นสถานะจริง

.PARAMETER Debt
    แสดงเฉพาะ "หนี้รีวิว" — แผนที่ implement เสร็จ (DONE/REVIEWED) แต่ยังไม่ถึง VERIFIED

.PARAMETER Next
    พิมพ์เลขแผนถัดไปที่ว่าง (ตามกติกาจองเลขใน DOC/PLANS/README.md) แล้วจบ

.EXAMPLE
    pwsh tools/plan-status.ps1
    pwsh tools/plan-status.ps1 -Debt
    pwsh tools/plan-status.ps1 -Next
#>
[CmdletBinding()]
param(
    [switch]$Debt,
    [switch]$Next,
    [string]$PlansDir = "DOC/PLANS"
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$plansFull = Join-Path $repoRoot $PlansDir
if (-not (Test-Path $plansFull)) { throw "ไม่พบโฟลเดอร์แผน: $plansFull" }

$files = Get-ChildItem -Path $plansFull -Filter 'PLAN-*.md' | Sort-Object Name

# ── เลขแผนถัดไป ──────────────────────────────────────────────────────────────
$numbers = $files | ForEach-Object { if ($_.Name -match '^PLAN-(\d+)') { [int]$Matches[1] } } | Sort-Object -Unique
$nextNumber = [int](($numbers | Measure-Object -Maximum).Maximum) + 1

if ($Next) {
    Write-Output ('PLAN-{0:D3}' -f $nextNumber)
    return
}

# ── parse status ─────────────────────────────────────────────────────────────
# เรียงตามลำดับ lifecycle — ใช้ตัดสินว่า chain `DONE → VERIFIED` จบที่ไหน
$knownStates = @('DRAFT', 'READY', 'IN PROGRESS', 'IN-PROGRESS', 'DONE', 'REVIEWED',
    'VERIFIED', 'DEPLOYED', 'SUPERSEDED', 'REFERENCE', 'ASSESSMENT', 'DECIDED', 'ACTIVE')

function Resolve-Status([string]$raw) {
    if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
    # ตัด markdown/emoji/วงเล็บอธิบายออก (raw อาจมี `**` ค้างจาก `- **Status:** DONE` → ต้อง Trim ด้วย)
    $v = ($raw -replace '\*\*', '' -replace '[✅❌⚠️]', '').Trim()
    # chain: เอา segment สุดท้ายที่เป็น state ที่รู้จัก — @() กัน -split คืนค่าเดี่ยวแล้วถูก unwrap เป็น string
    # (ถ้าเป็น string การ index จะได้ char ทีละตัว ไม่ใช่ segment)
    $segments = @($v -split '→|->' | ForEach-Object { $_.Trim() })
    for ($i = $segments.Count - 1; $i -ge 0; $i--) {
        foreach ($state in $knownStates) {
            if ($segments[$i] -match "^$([regex]::Escape($state))\b") { return $state.ToUpper() }
        }
    }
    # ไม่ match chain — ลองจับ state แรกที่โผล่ในข้อความ
    foreach ($state in $knownStates) {
        if ($v -match "^$([regex]::Escape($state))\b") { return $state.ToUpper() }
    }
    return 'OTHER'
}

$rows = foreach ($f in $files) {
    $head = Get-Content -LiteralPath $f.FullName -TotalCount 15 -Encoding utf8
    $raw = $null
    foreach ($line in $head) {
        if ($line -match '^\s*[-*]?\s*\*{0,2}\s*(Status|สถานะ)\s*\*{0,2}\s*[:：]\s*(.+)$') {
            $raw = $Matches[2].Trim(); break
        }
    }
    [pscustomobject]@{
        Plan   = $f.BaseName
        Status = if ($null -eq $raw) { 'MISSING' } else { Resolve-Status $raw }
        Raw    = $raw
    }
}

# ── หนี้รีวิว: implement เสร็จแต่ยังไม่ VERIFIED ─────────────────────────────
$debtRows = $rows | Where-Object { $_.Status -in @('DONE', 'REVIEWED', 'DEPLOYED') }

if ($Debt) {
    if ($debtRows.Count -eq 0) { Write-Host "ไม่มีหนี้รีวิว — แผนที่ implement แล้วถึง VERIFIED ครบ" -ForegroundColor Green; return }
    Write-Host "หนี้รีวิว ($($debtRows.Count) แผน) — implement แล้วแต่ยังไม่ VERIFIED:`n" -ForegroundColor Yellow
    $debtRows | Sort-Object Plan | Format-Table @{L = 'Plan'; E = { $_.Plan }; Width = 62 }, Status -AutoSize
    return
}

# ── สรุปรวม ──────────────────────────────────────────────────────────────────
Write-Host "DOC/PLANS — $($files.Count) แผน · เลขถัดไปที่ว่าง: PLAN-$('{0:D3}' -f $nextNumber)`n"

Write-Host "สถานะ:" -ForegroundColor Cyan
$rows | Group-Object Status | Sort-Object Count -Descending |
    Format-Table @{L = 'Status'; E = { $_.Name }; Width = 14 }, Count -AutoSize

if ($debtRows.Count -gt 0) {
    Write-Host "หนี้รีวิว: $($debtRows.Count) แผน (ดูรายชื่อ: -Debt)" -ForegroundColor Yellow
}

$missing = $rows | Where-Object { $_.Status -in @('MISSING', 'OTHER') }
if ($missing.Count -gt 0) {
    Write-Host "`nheader อ่านสถานะไม่ได้ ($($missing.Count) ไฟล์) — แผนใหม่ให้ใช้ format ``- **Status:** READY``:" -ForegroundColor DarkYellow
    $missing | ForEach-Object { Write-Host ("  {0,-62} {1}" -f $_.Plan, $_.Status) }
}
