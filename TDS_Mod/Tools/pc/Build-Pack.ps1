# Builds the release pack RogueWavesMod (docs/PACK.md) in two halves around one Unity menu item.
#   powershell -ExecutionPolicy Bypass -Command "& '<this folder>\Build-Pack.ps1'"        # 1. release code of every part
#   execute_menu_item("Tools/ModKit/Build Pack"), then read handoff\pack_status.txt for "PACK DONE ok"   # 2. in Unity
#   powershell -ExecutionPolicy Bypass -Command "& '<this folder>\Build-Pack.ps1' -Zip"   # 3. the zip players get
# Reads TDS_Mod/ModSource/Pack/pack.txt (version, parts). Step 1 runs ModSource/<Part>/Build-<Part>Mod.ps1 with no -Define for each
# part (a release build) and sets the built code's Version constant to the pack version, so every part reports the one
# version (docs/PACK.md section 4); the source keeps its <version>-dev. Step 3 copies TDS\Mods\RogueWavesMod (disabled,
# as the exporter writes it) and ModSource/Pack/README.txt into handoff\RogueWaves-<version>.zip.
# Before a boot check of the pack, move every dev build of a pack part (YosseleMod, ...) out of TDS\Mods: both would
# register the same vessel. Then: Run-SelfTests.ps1 -Vessels Yossele -BootCheck -Pack RogueWaves
param(
    [switch]$Zip,
    [string]$Game = "D:\Steam\steamapps\common\TDS"
)
$ErrorActionPreference = "Stop"
# TDS_Mod: two folders up from Tools\pc, or the PC's project when the script runs from a handoff folder.
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Test-Path -LiteralPath (Join-Path $root "ModSource"))) { $root = Join-Path $env:USERPROFILE "TDS_Mod" }
$handoff = Join-Path ([Environment]::GetFolderPath("Desktop")) "ClaudeProjects\handoff"
$list = Join-Path $root "ModSource\Pack\pack.txt"
if (-not (Test-Path -LiteralPath $list)) { throw "no $list" }
$version = ""; $parts = @()
foreach ($raw in [IO.File]::ReadAllLines($list)) {
    $line = $raw.Trim()
    if ($line -eq "" -or $line.StartsWith("#")) { continue }
    if ($line -match '^version\s+(\S+)$') { $version = $Matches[1] }
    elseif ($line -match '^part\s+(\S+)$') { $parts += $Matches[1] }
}
if ($version -eq "" -or $version.Contains("-") -or $parts.Count -eq 0) { throw "pack.txt needs a release version (no -dev) and at least one part" }
"pack RogueWavesMod $version`: $($parts -join ', ')"

if (-not $Zip) {
    foreach ($p in $parts) {
        $src = Join-Path $root "ModSource\$p"
        & (Join-Path $src "Build-$($p)Mod.ps1")
        $code = Join-Path $src "Build\$($p)Mod_Code.cs"
        $text = [IO.File]::ReadAllText($code)
        $rx = [regex]'const string Version = "[^"]*"'
        if ($rx.Matches($text).Count -ne 1) { throw "$code should hold exactly one 'const string Version = ...' line" }
        $text = $rx.Replace($text, "const string Version = `"$version`"")
        [IO.File]::WriteAllText($code, $text, (New-Object System.Text.UTF8Encoding($false)))
        "$p`: release code, Version set to $version"
    }
    "next: Tools > ModKit > Build Pack in Unity, then Build-Pack.ps1 -Zip"
    exit 0
}

$mod = Join-Path $Game "Mods\RogueWavesMod"
$info = Join-Path $mod "RogueWavesMod.info"
if (-not (Test-Path -LiteralPath $info)) { throw "no $($info): run Tools > ModKit > Build Pack first" }
$c = [IO.File]::ReadAllText($info)
if ($c -notmatch [regex]::Escape("`"$version`"")) { throw "$info doesn't carry version $version" }
$stage = Join-Path $handoff "pack_stage"
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null
Copy-Item -LiteralPath $mod -Destination $stage -Recurse
# A player turns the pack on in the Mods menu (README); a boot check may have left this copy enabled.
$staged = Join-Path $stage "RogueWavesMod\RogueWavesMod.info"
$c = [IO.File]::ReadAllText($staged) -replace '"_isEnabled":\s*true', '"_isEnabled": false'
[IO.File]::WriteAllText($staged, $c, (New-Object System.Text.UTF8Encoding($false)))
$readme = [IO.File]::ReadAllText((Join-Path $root "ModSource\Pack\README.txt")).Replace("{VERSION}", $version).Replace("`r`n", "`n").Replace("`n", "`r`n")
[IO.File]::WriteAllText((Join-Path $stage "README.txt"), $readme, (New-Object System.Text.UTF8Encoding($false)))
$zipFile = Join-Path $handoff "RogueWaves-$version.zip"
if (Test-Path -LiteralPath $zipFile) { Remove-Item -LiteralPath $zipFile -Force }
# Entries are written with forward slashes: PowerShell 5.1's Compress-Archive writes backslashes, which some Mac and Linux
# unzippers turn into flat files named "RogueWavesMod\Windows\...".
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::Open($zipFile, [IO.Compression.ZipArchiveMode]::Create)
try {
    $base = (Resolve-Path -LiteralPath $stage).Path.TrimEnd('\') + '\'
    foreach ($item in Get-ChildItem -LiteralPath $stage -Recurse -Force) {
        $rel = $item.FullName.Substring($base.Length).Replace('\', '/')
        if ($item.PSIsContainer) {
            if (@(Get-ChildItem -LiteralPath $item.FullName -Force).Count -eq 0) { [void]$zip.CreateEntry($rel + '/') }
        } else {
            [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $item.FullName, $rel)
        }
    }
} finally { $zip.Dispose() }
Remove-Item -LiteralPath $stage -Recurse -Force
"zip: $zipFile ($((Get-Item -LiteralPath $zipFile).Length) bytes)"
