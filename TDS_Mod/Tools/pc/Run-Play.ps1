# Runs one vessel's play build in TDS (its Code/98_Play.cs with kit ModKitPlay; docs/reports/PLAYTEST_2026-10-26.md):
# backs up the save, enables the mod, launches the game, waits for "[<Name>] PLAY DONE", closes the game, keeps the
# Player.log and writes the PLAY lines (dumps left out), the HUD and key dumps, the mod's own lines and the captures as
# PNGs into Desktop\ClaudeProjects\handoff\play_logs. Launching TDS needs Dimitri's yes in the thread.
# Build first: Build-<Name>Mod.ps1 -Define <NAME>_PLAY,<NAME>_SHOTS,MODKIT_PLAY,MODKIT_TEST, then Tools > ModKit > Build
# Queue; the play build must be the only mod in the Mods folder (park Dimitri's play build first).
#   & .\Run-Play.ps1 -Vessel Yossele [-TimeoutMinutes 40]
param(
    [Parameter(Mandatory = $true)][string]$Vessel,
    [int]$TimeoutMinutes = 40,
    [string]$Game = "D:\Steam\steamapps\common\TDS",
    [string]$TdsMod = "C:\Users\Dimitri\TDS_Mod"
)
$ErrorActionPreference = "Stop"
$handoff = Join-Path ([Environment]::GetFolderPath("Desktop")) "ClaudeProjects\handoff"
$data = Join-Path $env:USERPROFILE "AppData\LocalLow\Blockage\TDS"
$log = Join-Path $data "Player.log"
$out = Join-Path $handoff "play_logs"
New-Item -ItemType Directory -Force -Path $out | Out-Null

$info = Join-Path $Game "Mods\$($Vessel)Mod\$($Vessel)Mod.info"
if (-not (Test-Path -LiteralPath $info)) { throw "no $info" }
$c = [IO.File]::ReadAllText($info) -replace '"_isEnabled":\s*(true|false)', '"_isEnabled": true'
[IO.File]::WriteAllText($info, $c, (New-Object System.Text.UTF8Encoding($false)))
$others = @(Get-ChildItem (Join-Path $Game "Mods") -Directory | Where-Object { $_.Name -ne "$($Vessel)Mod" })
if ($others.Count -gt 0) { throw "other mods in the Mods folder: $($others.Name -join ', ')" }

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$saveBackup = Join-Path $handoff "save_backup_$stamp"
New-Item -ItemType Directory -Force -Path $saveBackup | Out-Null
Get-ChildItem -LiteralPath $data -Force | Where-Object { $_.Name -notlike "Player*.log" } | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $saveBackup -Recurse -Force }
"save backed up to $saveBackup"

function ReadFrom($path, [ref]$from) {
    if (-not (Test-Path -LiteralPath $path)) { return "" }
    $fs = New-Object IO.FileStream($path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
    try {
        if ($fs.Length -lt $from.Value) { $from.Value = 0 }
        [void]$fs.Seek($from.Value, [IO.SeekOrigin]::Begin)
        $sr = New-Object IO.StreamReader($fs)
        $text = $sr.ReadToEnd()
        $from.Value = $fs.Position
        return $text
    } finally { $fs.Dispose() }
}

if (Get-Process TDSgame -ErrorAction SilentlyContinue) { throw "TDS is already running" }
$start = Get-Date
Start-Process "steam://rungameid/2392280"
$pos = 0; $seen = ""; $done = $false; $fresh = $false
$want = "[$Vessel] PLAY DONE"
while (((Get-Date) - $start).TotalMinutes -lt $TimeoutMinutes) {
    Start-Sleep 5
    if (-not (Test-Path -LiteralPath $log) -or (Get-Item -LiteralPath $log).LastWriteTime -lt $start) { continue }
    $fresh = $true
    $chunk = ReadFrom $log ([ref]$pos)
    $seen += $chunk
    if ($seen.Contains($want)) { $done = $true; break }
    if ($seen.Length -gt 400000) { $seen = $seen.Substring($seen.Length - 100000) }
    if (-not (Get-Process TDSgame -ErrorAction SilentlyContinue) -and ((Get-Date) - $start).TotalMinutes -gt 2) { "the game closed by itself"; break }
}
Start-Sleep 3
foreach ($p in @(Get-Process TDSgame -ErrorAction SilentlyContinue)) { [void]$p.CloseMainWindow() }
$t = 0
while ((Get-Process TDSgame -ErrorAction SilentlyContinue) -and $t -lt 60) { Start-Sleep 2; $t += 2 }
if (Get-Process TDSgame -ErrorAction SilentlyContinue) { "WARNING: TDS did not close within 60 s" }
if (-not $fresh) { "WARNING: TDS never wrote Player.log after the launch"; exit 1 }
if (-not $done) { "WARNING: no PLAY DONE within $TimeoutMinutes minutes" }

$copy = Join-Path $out "$($Vessel)_$stamp.log"
Copy-Item -LiteralPath $log -Destination $copy -Force
$lines = @(Get-Content -LiteralPath $copy | Where-Object { $_ -notmatch 'Dump\] ' })
$play = @($lines | Where-Object { $_.Contains("[$Vessel] PLAY ") -and $_ -notmatch "\] PLAY (UI|KEY|MAP|\| )" })
$file = Join-Path $out "$($Vessel)_$($stamp)_play.txt"
[IO.File]::WriteAllLines($file, $play)
$ui = @($lines | Where-Object { $_ -match "\] PLAY (UI|KEY)" })
[IO.File]::WriteAllLines((Join-Path $out "$($Vessel)_$($stamp)_ui.txt"), $ui)
$map = @($lines | Where-Object { $_ -match "\] PLAY (MAP|\| )" })
[IO.File]::WriteAllLines((Join-Path $out "$($Vessel)_$($stamp)_map.txt"), $map)
$mine = @($lines | Where-Object { $_.StartsWith("[$Vessel] ") -and -not $_.Contains(" PLAY ") })
[IO.File]::WriteAllLines((Join-Path $out "$($Vessel)_$($stamp)_mod.txt"), $mine)
$ex = @($lines | Where-Object { $_ -match 'Exception' })
"play lines: $($play.Count), UI/KEY lines: $($ui.Count), mod lines: $($mine.Count), exception lines: $($ex.Count)"
$ex | Group-Object | Sort-Object Count -Descending | Select-Object -First 10 | ForEach-Object { "  x$($_.Count) $($_.Name)" }
$shots = Join-Path $out "$($Vessel)_$($stamp)_shots"
& (Join-Path $TdsMod "ModSource\$Vessel\Tools\Decode-Dump.ps1") -Log $copy -Out $shots -Tag "$($Vessel)Dump" | Measure-Object | ForEach-Object { "decoded $($_.Count) captures into $shots" }
"summary: $file"
