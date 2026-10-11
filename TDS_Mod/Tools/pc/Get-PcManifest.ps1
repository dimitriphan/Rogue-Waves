# Writes the PC's file manifest: a short hash of every source file in the mod project, so a cloud thread knows exactly
# what the PC has and packs only what differs (make_handoff.py --manifest). Read-only: it changes nothing in TDS_Mod.
#   powershell -ExecutionPolicy Bypass -Command "& '<this folder>\Get-PcManifest.ps1'"
# Covers ModSource\** (not Build, Research, Previews or Aseprite\generated) and Assets\<X>\Editor\*.cs. Text files are
# hashed as UTF-8 with any BOM and every CR removed, so line endings don't count; other files as their bytes.
# Output: Desktop\ClaudeProjects\handoff\pc_manifest.txt ("<16 hex>  <path under TDS_Mod>" per line). Paste it to the
# cloud thread.
param([string]$Root = "C:\Users\Dimitri\TDS_Mod", [string]$Out = "")
. (Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "HandoffHash.ps1")
if (-not $Out) { $Out = Join-Path ([Environment]::GetFolderPath("Desktop")) "ClaudeProjects\handoff\pc_manifest.txt" }
$full = (Resolve-Path -LiteralPath $Root).Path.TrimEnd("\")
$skip = '\\(Build|Research|Previews|bin|obj)\\|\\Aseprite\\generated\\'
$files = @()
$files += Get-ChildItem -LiteralPath (Join-Path $full "ModSource") -Recurse -File | Where-Object { $_.FullName.Substring($full.Length) -notmatch $skip }
foreach ($d in Get-ChildItem -LiteralPath (Join-Path $full "Assets") -Directory) {
    $ed = Join-Path $d.FullName "Editor"
    if (Test-Path -LiteralPath $ed) { $files += Get-ChildItem -LiteralPath $ed -Filter *.cs -File }
}
$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("# PC manifest of $full, " + (Get-Date -Format "yyyy-MM-dd HH:mm") + ", " + $files.Count + " files")
foreach ($f in ($files | Sort-Object FullName)) {
    $lines.Add((HandoffHash $f.FullName) + "  " + $f.FullName.Substring($full.Length + 1))
}
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Out) | Out-Null
[IO.File]::WriteAllLines($Out, $lines.ToArray(), (New-Object System.Text.UTF8Encoding($false)))
"wrote $Out ($($files.Count) files)"
