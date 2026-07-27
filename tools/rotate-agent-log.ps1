<#
.SYNOPSIS
    หมุน DOC/AGENT_LOG.md — เก็บ entry ล่าสุด N รายการไว้ในไฟล์หลัก ที่เหลือย้ายเข้า DOC/archive/AGENT_LOG-<YYYY-MM>.md

.DESCRIPTION
    AGENT_LOG.md เป็นไฟล์ที่ agent ทุกตัวต้องอ่านก่อนเริ่มงาน (กติกาใน CLAUDE.md)
    ถ้าปล่อยให้โตไม่จำกัด agent จะเสีย context window ไปกับ log เก่าที่ไม่เกี่ยวกับงาน
    (เคยโตถึง 869 KB / 474 entries ก่อน rotate ครั้งแรก 2026-07-27)

    สคริปต์นี้:
      - แยก header (ทุกบรรทัดก่อน entry แรก) ออกมาคงไว้ในไฟล์หลักเสมอ
      - นับ entry จากบรรทัด `## [` โดย **ข้ามบรรทัดที่อยู่ใน code fence** (``` ) — ในไฟล์มี template
        `## [YYYY-MM-DD HH:mm]` อยู่ใน fence ซึ่งไม่ใช่ entry จริง
      - entry ที่เกิน -KeepEntries จะถูก prepend เข้าไฟล์ archive ของเดือนตัวเอง (ใหม่สุดอยู่บนสุดเหมือนกัน)

.PARAMETER KeepEntries
    จำนวน entry ล่าสุดที่คงไว้ใน DOC/AGENT_LOG.md (ค่าเริ่มต้น 30 — กติกาให้อ่าน 10 entry ล่าสุด จึงเผื่อไว้ 3 เท่า)

.PARAMETER WhatIf
    แสดงผลว่าจะย้ายอะไรบ้างโดยไม่เขียนไฟล์จริง

.EXAMPLE
    pwsh tools/rotate-agent-log.ps1 -WhatIf
    pwsh tools/rotate-agent-log.ps1 -KeepEntries 30
#>
[CmdletBinding()]
param(
    [int]$KeepEntries = 30,
    [string]$LogPath = "DOC/AGENT_LOG.md",
    [string]$ArchiveDir = "DOC/archive",
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'

# ทำงานจาก repo root เสมอ (สคริปต์อยู่ใน tools/)
$repoRoot = Split-Path -Parent $PSScriptRoot
$logFull = Join-Path $repoRoot $LogPath
$archiveFull = Join-Path $repoRoot $ArchiveDir

if (-not (Test-Path $logFull)) { throw "ไม่พบไฟล์ log: $logFull" }

$lines = [System.IO.File]::ReadAllLines($logFull, [System.Text.UTF8Encoding]::new($false))

# ── หาบรรทัดเริ่ม entry (ข้าม code fence) ────────────────────────────────────
$entryStarts = [System.Collections.Generic.List[int]]::new()
$inFence = $false
for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match '^\s*```') { $inFence = -not $inFence; continue }
    if (-not $inFence -and $line -match '^## \[') { $entryStarts.Add($i) }
}

if ($entryStarts.Count -eq 0) { throw "ไม่พบ entry (`## [`) ใน $LogPath" }

Write-Host "พบ $($entryStarts.Count) entries ใน $LogPath ($([math]::Round((Get-Item $logFull).Length / 1KB)) KB)"

if ($entryStarts.Count -le $KeepEntries) {
    Write-Host "entry ไม่เกิน $KeepEntries — ไม่ต้อง rotate" -ForegroundColor Green
    return
}

# ── แบ่ง header / entries ────────────────────────────────────────────────────
$header = $lines[0..($entryStarts[0] - 1)]

# entry ที่ index i กินตั้งแต่ entryStarts[i] ถึงก่อน entryStarts[i+1] (ตัวสุดท้ายถึงจบไฟล์)
function Get-EntryLines([int]$idx) {
    $start = $entryStarts[$idx]
    $end = if ($idx + 1 -lt $entryStarts.Count) { $entryStarts[$idx + 1] - 1 } else { $lines.Count - 1 }
    return $lines[$start..$end]
}

$kept = [System.Collections.Generic.List[string]]::new()
for ($i = 0; $i -lt $KeepEntries; $i++) { $kept.AddRange([string[]](Get-EntryLines $i)) }

# ── จัดกลุ่ม entry ที่จะ archive ตามเดือน (คงลำดับใหม่→เก่า) ────────────────
$byMonth = [ordered]@{}
for ($i = $KeepEntries; $i -lt $entryStarts.Count; $i++) {
    $entry = [string[]](Get-EntryLines $i)
    $month = if ($entry[0] -match '^## \[(\d{4})-(\d{2})') { "$($Matches[1])-$($Matches[2])" } else { 'unknown' }
    if (-not $byMonth.Contains($month)) { $byMonth[$month] = [System.Collections.Generic.List[string]]::new() }
    $byMonth[$month].AddRange($entry)
}

Write-Host "เก็บไว้ในไฟล์หลัก: $KeepEntries entries · ย้ายเข้า archive: $($entryStarts.Count - $KeepEntries) entries"
foreach ($m in $byMonth.Keys) {
    $count = ($byMonth[$m] | Where-Object { $_ -match '^## \[' }).Count
    Write-Host "  → $ArchiveDir/AGENT_LOG-$m.md  (+$count entries)"
}

if ($WhatIf) { Write-Host "`n-WhatIf: ไม่ได้เขียนไฟล์" -ForegroundColor Yellow; return }

# ── เขียน archive (prepend ให้ใหม่สุดอยู่บนสุด) ──────────────────────────────
if (-not (Test-Path $archiveFull)) { New-Item -ItemType Directory -Path $archiveFull | Out-Null }
$utf8 = [System.Text.UTF8Encoding]::new($false)

foreach ($m in $byMonth.Keys) {
    $target = Join-Path $archiveFull "AGENT_LOG-$m.md"
    $body = $byMonth[$m]

    if (Test-Path $target) {
        $existing = [System.IO.File]::ReadAllLines($target, $utf8)
        # ข้าม header เดิม (ทุกบรรทัดก่อน entry แรก) แล้วต่อ body ใหม่ไว้บนสุด
        $firstEntry = 0
        for ($i = 0; $i -lt $existing.Count; $i++) { if ($existing[$i] -match '^## \[') { $firstEntry = $i; break } }
        $oldHeader = if ($firstEntry -gt 0) { $existing[0..($firstEntry - 1)] } else { @() }
        $oldBody = $existing[$firstEntry..($existing.Count - 1)]
        $out = @($oldHeader) + @($body) + @($oldBody)
    }
    else {
        $archHeader = @(
            "# Agent Work Log — archive $m",
            "",
            "entry เก่าที่ถูกหมุนออกจาก [../AGENT_LOG.md](../AGENT_LOG.md) โดย ``tools/rotate-agent-log.ps1``",
            "**อ่านอย่างเดียว** — ห้ามเขียน entry ใหม่ที่นี่ ให้เขียนบนสุดของ ``DOC/AGENT_LOG.md`` เสมอ",
            "",
            "---",
            ""
        )
        $out = @($archHeader) + @($body)
    }
    [System.IO.File]::WriteAllLines($target, [string[]]$out, $utf8)
}

# ── เขียนไฟล์หลักใหม่ ────────────────────────────────────────────────────────
$final = @($header) + @($kept)
[System.IO.File]::WriteAllLines($logFull, [string[]]$final, $utf8)

$newSize = [math]::Round((Get-Item $logFull).Length / 1KB)
Write-Host "`nเสร็จ — $LogPath เหลือ $newSize KB" -ForegroundColor Green
