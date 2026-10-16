# Runs vessel self-tests on the PC, one vessel per game launch, and writes a short summary of each run, so a Remote
# Control session relays a few lines instead of reading a 40 MB Player.log.
#   powershell -ExecutionPolicy Bypass -Command "& '<this folder>\Run-SelfTests.ps1' -Vessels Argo,Crackpot"
#   powershell -ExecutionPolicy Bypass -Command "& '<this folder>\Run-SelfTests.ps1' -Vessels Argo,Crackpot -BootCheck"
# Launching TDS needs Dimitri's yes in the thread. Before the first launch the script backs up his save folder
# (%USERPROFILE%\AppData\LocalLow\Blockage\TDS, logs left out) into the handoff folder.
#
# Self-test mode: each vessel's self-test build must already be exported (Build-<Name>Mod.ps1 -Define <NAME>_SELFTEST,
# then Tools > ModKit > Build Queue). For each vessel in turn: only its mod among -Vessels is enabled, TDS is launched,
# the script waits for "[<Name>] TEST DONE", closes the game, copies Player.log to handoff\selftest_logs and writes
# <Name>_<time>_summary.txt (counts, every FAIL and SETUP line, skipped checks, exceptions) and <Name>_<time>_map.txt
# (the map snapshot for TDS_Mod/Tools/cloud-check/map_view.py). The self-test build is left installed but disabled:
# the release build replaces it afterwards.
# -BootCheck: the release builds of -Vessels are enabled, TDS is launched, the script waits until each logs
# "[<Name>] icons:" and reports every vessel mod's ready/icons lines and the exception count.
param(
    [Parameter(Mandatory = $true)][string[]]$Vessels,
    [switch]$BootCheck,
    [int]$TimeoutMinutes = 12,
    [string]$Game = "D:\Steam\steamapps\common\TDS"
)
$ErrorActionPreference = "Stop"
$handoff = Join-Path ([Environment]::GetFolderPath("Desktop")) "ClaudeProjects\handoff"
$data = Join-Path $env:USERPROFILE "AppData\LocalLow\Blockage\TDS"
$log = Join-Path $data "Player.log"
$out = Join-Path $handoff "selftest_logs"
New-Item -ItemType Directory -Force -Path $out | Out-Null

function SetEnabled($name, [bool]$on) {
    $info = Join-Path $Game "Mods\$($name)Mod\$($name)Mod.info"
    if (-not (Test-Path -LiteralPath $info)) { throw "no $($info): export the mod first" }
    $c = [IO.File]::ReadAllText($info)
    $c = $c -replace '"_isEnabled":\s*(true|false)', ('"_isEnabled": ' + $(if ($on) { "true" } else { "false" }))
    [IO.File]::WriteAllText($info, $c, (New-Object System.Text.UTF8Encoding($false)))
}

# Reads what the game has added to Player.log since 'from' (the game keeps the file open, so share it).
function ReadFrom($path, [ref]$from) {
    if (-not (Test-Path -LiteralPath $path)) { return "" }
    $fs = New-Object IO.FileStream($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    try {
        if ($fs.Length -lt $from.Value) { $from.Value = 0 }
        [void]$fs.Seek($from.Value, [IO.SeekOrigin]::Begin)
        $sr = New-Object IO.StreamReader($fs)
        $text = $sr.ReadToEnd()
        $from.Value = $fs.Position   # what was read, not the length now: lines written meanwhile are read next time
        return $text
    } finally { $fs.Dispose() }
}

function Launch($until, $what) {
    if (Get-Process TDSgame -ErrorAction SilentlyContinue) { throw "TDS is already running: it must be closed before a test (it may be Dimitri playing)" }
    $start = Get-Date
    $script:LogFresh = $false
    Start-Process "steam://rungameid/2392280"
    $pos = 0; $seen = ""; $done = $false
    while (((Get-Date) - $start).TotalMinutes -lt $TimeoutMinutes) {
        Start-Sleep 5
        if (-not (Test-Path -LiteralPath $log) -or (Get-Item -LiteralPath $log).LastWriteTime -lt $start) { continue }
        $script:LogFresh = $true
        $seen += ReadFrom $log ([ref]$pos)
        if (& $until $seen) { $done = $true; break }
        if ($seen.Length -gt 200000) { $seen = $seen.Substring($seen.Length - 100000) }
    }
    Start-Sleep 3
    foreach ($p in @(Get-Process TDSgame -ErrorAction SilentlyContinue)) { [void]$p.CloseMainWindow() }
    $t = 0
    while ((Get-Process TDSgame -ErrorAction SilentlyContinue) -and $t -lt 60) { Start-Sleep 2; $t += 2 }
    # Warnings go to the host: a bare string would join the function's output, so "$ok = Launch ..." would get an
    # array (always true) instead of $done, and the warning would never print.
    if (Get-Process TDSgame -ErrorAction SilentlyContinue) { Write-Host "WARNING: TDS did not close within 60 s; it is still running" }
    if (-not $done) { Write-Host "WARNING: no $what within $TimeoutMinutes minutes" }
    # Argo round 2 (2026-10-08): a Steam Cloud dialog held the launch, the game never started, and the old Player.log
    # was summarized as if it were this run's. Nothing is summarized from a log the game did not write this launch.
    if (-not $script:LogFresh) { Write-Host "WARNING: TDS never wrote Player.log after the launch (a Steam dialog waiting? check Steam's console_log); nothing from this launch to summarize" }
    return $done
}

# The save, once per call, before the first launch.
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$saveBackup = Join-Path $handoff "save_backup_$stamp"
if (Test-Path -LiteralPath $data) {
    New-Item -ItemType Directory -Force -Path $saveBackup | Out-Null
    Get-ChildItem -LiteralPath $data -Force | Where-Object { $_.Name -notlike "Player*.log" } | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $saveBackup -Recurse -Force }
    "save backed up to $saveBackup"
}

if ($BootCheck) {
    foreach ($v in $Vessels) { SetEnabled $v $true }
    $ok = Launch { param($s) $all = $true; foreach ($v in $Vessels) { if ($s -notmatch [regex]::Escape("[$v] icons:")) { $all = $false } }; $all } "icons line from every vessel"
    if (-not $script:LogFresh) { "BOOT CHECK $($stamp): the game never started; nothing summarized"; exit 1 }
    $copy = Join-Path $out "boot_$stamp.log"
    Copy-Item -LiteralPath $log -Destination $copy -Force
    $lines = Get-Content -LiteralPath $copy | Where-Object { $_ -notmatch 'Dump\] ' }
    $sum = New-Object System.Collections.Generic.List[string]
    $sum.Add("BOOT CHECK $stamp of " + ($Vessels -join ", ") + ": " + $(if ($ok) { "every vessel logged its icons" } else { "timed out" }))
    foreach ($l in $lines) { if ($l -match '^\[\w+\] (loaded|ready|icons:)') { $sum.Add($l) } }
    $ex = @($lines | Where-Object { $_ -match 'Exception' })
    $sum.Add("exception lines: " + $ex.Count)
    $ex | Select-Object -First 8 | ForEach-Object { $sum.Add("  " + $_) }
    $file = Join-Path $out "boot_$($stamp)_summary.txt"
    [IO.File]::WriteAllLines($file, $sum.ToArray())
    $sum
    exit 0
}

foreach ($v in $Vessels) {
    foreach ($w in $Vessels) { SetEnabled $w ($w -eq $v) }
    $ok = Launch { param($s) $s -match [regex]::Escape("[$v] TEST DONE") } "TEST DONE"
    $t = Get-Date -Format "yyyyMMdd_HHmmss"
    if (-not $script:LogFresh) {
        SetEnabled $v $false
        "SELF-TEST $v $($t): the game never started; nothing summarized (the old Player.log is not this run's)"
        ""
        continue
    }
    $copy = Join-Path $out "$($v)_$t.log"
    Copy-Item -LiteralPath $log -Destination $copy -Force
    SetEnabled $v $false
    $mine = @(Get-Content -LiteralPath $copy | Where-Object { $_ -notmatch 'Dump\] ' })
    $pre = "[$v] TEST "
    $tests = @($mine | Where-Object { $_.Contains($pre) })
    $sum = New-Object System.Collections.Generic.List[string]
    $pass = @($tests | Where-Object { $_.Contains($pre + "PASS ") }).Count
    $fail = @($tests | Where-Object { $_.Contains($pre + "FAIL ") })
    $setup = @($tests | Where-Object { $_.Contains($pre + "SETUP ") })
    $sum.Add("SELF-TEST $v $t" + $(if ($ok) { "" } else { " (no TEST DONE: timed out or the test stopped)" }) + ": pass $pass, fail $($fail.Count), setup $($setup.Count)")
    foreach ($l in $tests) { if ($l -match 'TEST (DONE|map:|picked|ready)') { $sum.Add($l) } }
    foreach ($l in $fail) { $sum.Add($l) }
    foreach ($l in $setup) { $sum.Add($l) }
    foreach ($l in $tests) { if ($l -match 'skipped|driver error|giving up|no free tile|cannot spawn') { $sum.Add($l) } }
    $ex = @($mine | Where-Object { $_ -match 'Exception' })
    $sum.Add("exception lines: " + $ex.Count + " (docs/08 section 6 lists the game's own)")
    $ex | Select-Object -First 8 | ForEach-Object { $sum.Add("  " + $_) }
    $file = Join-Path $out "$($v)_$($t)_summary.txt"
    [IO.File]::WriteAllLines($file, $sum.ToArray())
    $map = @($tests | Where-Object { $_.Contains($pre + "MAP ") })
    if ($map.Count -gt 0) { [IO.File]::WriteAllLines((Join-Path $out "$($v)_$($t)_map.txt"), $map) }
    $sum
    ""
}
"logs and summaries in $out; the self-test builds are installed but disabled until the release replaces them"
