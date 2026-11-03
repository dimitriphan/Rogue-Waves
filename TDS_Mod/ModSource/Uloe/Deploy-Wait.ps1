# After Tools > Uloe > Export Mod: waits for the export to land in the game's Mods folder, then marks the mod
# enabled (the exporter writes _isEnabled: false). Optionally launches the game.
param([switch]$Launch, [int]$TimeoutSeconds = 240)
$mod = "D:\Steam\steamapps\common\TDS\Mods\UloeMod"
$info = "$mod\UloeMod.info"
$bundle = "$mod\Windows\uloemod.assets"
$start = Get-Date
$t = 0
while ($t -lt $TimeoutSeconds) {
    Start-Sleep 3; $t += 3
    if ((Test-Path $info) -and (Test-Path $bundle) -and (Get-Item $info).LastWriteTime -gt $start.AddSeconds(-90) -and (Get-Item $bundle).LastWriteTime -gt $start.AddSeconds(-90)) { break }
}
Start-Sleep 2
$c = [IO.File]::ReadAllText($info) -replace '"_isEnabled": false', '"_isEnabled": true'
[IO.File]::WriteAllText($info, $c, (New-Object System.Text.UTF8Encoding($false)))
"exported " + (Get-Item $bundle).Length + " bytes at " + (Get-Item $bundle).LastWriteTime + "; enabled"
if ($Launch) { Start-Process "steam://rungameid/2392280"; "launched" }
