# Runs the mode's two-player round on one PC (RogueWavesMode -Define ROGUEWAVESMODE_SELFTEST,ROGUEWAVESMODE_TWOPC, 93_TwoPlayers.cs):
# a host and a real second player, two copies of the game joined over the game's own UnityTransport on the loopback address.
#   powershell -Command "& '<this folder>\Run-TwoPlayers.ps1'"
# Launching TDS needs Dimitri's yes in the thread. Before the first launch it backs up the save folder and the game's registry
# settings; both copies run windowed at 1280x720, so the settings go back at the end. The second copy never writes the save.
# Commands the host's log can give this script, by a line "TEST DRIVER <word>":
#   KILL      ends the second copy as Task Manager would (a friend's crash)
#   START <phase>  starts the second copy (again), its log named after the phase
param(
    [int]$TimeoutMinutes = 25,
    [string]$Game = "D:\Steam\steamapps\common\TDS\TDS\TDSgame.exe",
    [string]$SecondCopy = "D:\TDS_second\TDS",
    [string]$Part = "RogueWavesMode",
    [string]$Extra = "",
    [switch]$KeepSave,
    [int]$Width = 1280,
    [int]$Height = 720
)
$ErrorActionPreference = "Stop"
$Root = Split-Path (Split-Path $Game)   # Steam starts the game here; it finds its Mods folder from it
$handoff = Join-Path ([Environment]::GetFolderPath("Desktop")) "ClaudeProjects\handoff"
$data = Join-Path $env:USERPROFILE "AppData\LocalLow\Blockage\TDS"
$out = Join-Path $handoff "twoplayer_logs"
New-Item -ItemType Directory -Force -Path $out | Out-Null
if (Get-Process TDSgame -ErrorAction SilentlyContinue) { throw "TDS is already running: it must be closed before a test (it may be Dimitri playing)" }

# The exporter leaves a new build disabled.
$info = "D:\Steam\steamapps\common\TDS\Mods\$($Part)Mod\$($Part)Mod.info"
$infoBefore = [IO.File]::ReadAllText($info)   # put back at the end: a later Run-SelfTests turns on only what it is told to, and leaves the rest
$c = $infoBefore -replace '"_isEnabled":\s*(true|false)', '"_isEnabled": true'
[IO.File]::WriteAllText($info, $c, (New-Object Text.UTF8Encoding($false)))

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$saveBackup = Join-Path $handoff "save_backup_$stamp"
New-Item -ItemType Directory -Force -Path $saveBackup | Out-Null
Get-ChildItem -LiteralPath $data -Force | Where-Object { $_.Name -notlike "Player*.log" } | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $saveBackup -Recurse -Force }
$reg = Join-Path $saveBackup "tds_registry.reg"
cmd /c "reg export HKCU\Software\Blockage\TDS `"$reg`" /y >nul 2>&1"
"save and settings backed up to $saveBackup"

# The game's boot.config says single-instance, so a second launch of the same folder quits at once. The second player runs from a
# copy of the game's folder with that one line gone; the Steam install is never touched, and its working folder finds the same Mods.
if (-not (Test-Path -LiteralPath (Join-Path $SecondCopy "TDSgame.exe"))) {
    "copying the game to $SecondCopy (about 1.2 GB)"
    New-Item -ItemType Directory -Force -Path $SecondCopy | Out-Null
    cmd /c "robocopy `"$(Split-Path $Game)`" `"$SecondCopy`" /E /NFL /NDL /NJH /NJS /NP /R:1 /W:1 >nul"
    $bc = Join-Path $SecondCopy "DATA\boot.config"
    [IO.File]::WriteAllText($bc, ((Get-Content -LiteralPath $bc | Where-Object { $_ -notmatch '^single-instance=' }) -join "`n"), (New-Object Text.UTF8Encoding($false)))
}

$hostLog = Join-Path $out "host_$stamp.log"
$secondLog = Join-Path $out "second_$stamp.log"
$winH = "-screen-fullscreen 0 -screen-width $Width -screen-height $Height -logFile `"$hostLog`" $Extra"
$winS = "-screen-fullscreen 0 -screen-width 1280 -screen-height 720 -rwclient $Extra"
$hostProc = Start-Process -FilePath $Game -ArgumentList $winH -WorkingDirectory $Root -PassThru
"host started, pid $($hostProc.Id)"
$secondProc = $null

function ReadNew($path, [ref]$from) {
    if (-not (Test-Path -LiteralPath $path)) { return "" }
    $fs = New-Object IO.FileStream($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    try {
        if ($fs.Length -lt $from.Value) { $from.Value = 0 }
        [void]$fs.Seek($from.Value, [IO.SeekOrigin]::Begin)
        $sr = New-Object IO.StreamReader($fs)
        $text = $sr.ReadToEnd()
        $from.Value = $fs.Position
        return $text
    } finally { $fs.Dispose() }
}

# Each launch of the second copy has its own log, and a phase word saying why it was started.
function StartSecond($phase) {
    $script:secondCount++
    $script:secondLog = Join-Path $out "second_$($stamp)_$($script:secondCount)_$phase.log"
    $script:posS = 0L
    $more = if ($phase -eq "nopack") { "-rwnopack" } elseif ($phase) { "-rwphase $phase" } else { "" }
    $p = Start-Process -FilePath (Join-Path $SecondCopy "TDSgame.exe") -ArgumentList "$winS -logFile `"$($script:secondLog)`" $more" -WorkingDirectory $Root -PassThru
    "second player started, pid $($p.Id), phase '$phase'"
    $script:secondProc = $p
    $script:secondDone = $false
}

$pre = "[$Part] TEST "
$start = Get-Date
$posH = 0L; $hostDone = $false; $secondDone = $false; $posS = 0L; $secondLog = ""
$secondCount = 0
while (((Get-Date) - $start).TotalMinutes -lt $TimeoutMinutes) {
    Start-Sleep 2
    $new = ReadNew $hostLog ([ref]$posH)
    foreach ($l in ($new -split "`n")) {
        if ($l.Contains($pre + "WAITING for the second player") -and -not $secondProc) { StartSecond "" }
        if ($l.Contains($pre + "DRIVER KILL") -and $secondProc -and -not $secondProc.HasExited) {
            # As Task Manager ends it: a crash, for the host and the rejoin.
            Stop-Process -Id $secondProc.Id -Force
            "second player killed"
        }
        if ($l -match [regex]::Escape($pre + "DRIVER START") + ' (\w+)') { StartSecond $Matches[1] }
        if ($l.Contains($pre + "DONE ")) { $hostDone = $true }
        if ($l.Contains($pre + "PASS ") -or $l.Contains($pre + "FAIL ") -or $l.Contains($pre + "SETUP ")) { "H " + $l.Substring($l.IndexOf($pre) + $pre.Length).Trim() }
    }
    $new = if ($secondLog) { ReadNew $secondLog ([ref]$posS) } else { "" }
    foreach ($l in ($new -split "`n")) {
        if ($l.Contains($pre + "DONE ")) { $secondDone = $true }
        if ($l.Contains($pre + "PASS ") -or $l.Contains($pre + "FAIL ") -or $l.Contains($pre + "SETUP ")) { "S " + $l.Substring($l.IndexOf($pre) + $pre.Length).Trim() }
    }
    if ($hostDone) { break }
    if ($hostProc.HasExited) { "host exited"; break }
}
"loop ended after " + [int]((Get-Date) - $start).TotalSeconds + " s: host done $hostDone, second done $secondDone"
Start-Sleep 3
foreach ($p in @(Get-Process TDSgame -ErrorAction SilentlyContinue)) { [void]$p.CloseMainWindow() }
$t = 0
while ((Get-Process TDSgame -ErrorAction SilentlyContinue) -and $t -lt 40) { Start-Sleep 2; $t += 2 }
foreach ($p in @(Get-Process TDSgame -ErrorAction SilentlyContinue)) { Stop-Process -Id $p.Id -Force }
Start-Sleep 2

[IO.File]::WriteAllText($info, $infoBefore, (New-Object Text.UTF8Encoding($false)))

# Settings back: a windowed launch writes its size into the registry. The save goes back unless told otherwise.
cmd /c "reg import `"$reg`" >nul 2>&1"
if (-not $KeepSave) {
    Get-ChildItem -LiteralPath $saveBackup -Force | Where-Object { $_.Name -ne "tds_registry.reg" } | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $data -Recurse -Force }
    "save and settings restored"
}

foreach ($file in @(Get-ChildItem -LiteralPath $out -Filter "*_$stamp*.log")) {
    $lines = @(Get-Content -LiteralPath $file.FullName | Where-Object { $_ -notmatch 'Dump\] ' })
    $tests = @($lines | Where-Object { $_.Contains($pre) })
    $pass = @($tests | Where-Object { $_.Contains($pre + "PASS ") -and $_ -notmatch 'TEST PASS (K0|CH) ' }).Count
    $fail = @($tests | Where-Object { $_.Contains($pre + "FAIL ") })
    $setup = @($tests | Where-Object { $_.Contains($pre + "SETUP ") })
    $ex = @($lines | Where-Object { $_ -match 'Exception' })
    "$($file.Name): pass $pass, fail $($fail.Count), setup $($setup.Count), exception lines $($ex.Count)"
    $fail | ForEach-Object { "  " + $_ }
    $setup | ForEach-Object { "  " + $_ }
}
