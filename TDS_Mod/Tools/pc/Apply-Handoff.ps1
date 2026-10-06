# Checks, then copies, a handoff's files into the TDS_Mod project. The handoff folder (a zip saved and unpacked into
# Desktop\ClaudeProjects\handoff\<name>, or what Get-Handoff.ps1 fetched there) holds README.md, HASHES.txt, an optional
# PATCHES.json and the files themselves, laid out like C:\Users\Dimitri\TDS_Mod.
#   powershell -ExecutionPolicy Bypass -Command "& '<handoff folder>\Apply-Handoff.ps1'"          (check only)
#   powershell -ExecutionPolicy Bypass -Command "& '<handoff folder>\Apply-Handoff.ps1' -Apply"   (check, back up, apply)
# Run -Apply only once Dimitri has OK'd it in the thread, in his own words.
#
# HASHES.txt, one file per line: "<base>[,<older base>...]  <new>  <path under TDS_Mod>" (hashes as HandoffHash.ps1).
#   The PC's copy must be the new one already ("up to date"), one of the bases ("will copy"), or missing where the base
#   is "new" ("will add"). A base "*" replaces whatever is there ("will replace"). A new "-" deletes the file ("will
#   delete", backed up first). Anything else is "DIFFERS": the PC has edits that aren't on GitHub, and nothing changes.
# PATCHES.json: [{ "path", "old", "new" }] line edits; each old text must appear exactly once (or the new one already).
# -Apply backs up every file it replaces or deletes under Desktop\ClaudeProjects\handoff\backup_<name>_<time>, then
# writes a fresh pc_manifest.txt (Get-PcManifest.ps1), so the next handoff starts from what the PC really has.
param([string]$Root = "C:\Users\Dimitri\TDS_Mod", [switch]$Apply)
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $here "HandoffHash.ps1")
$name = Split-Path -Leaf $here
$rows = Get-Content -LiteralPath (Join-Path $here "HASHES.txt") | Where-Object { $_ -and -not $_.StartsWith("#") }
$patches = @()
$pf = Join-Path $here "PATCHES.json"
if (Test-Path -LiteralPath $pf) { $patches = @([IO.File]::ReadAllText($pf) | ConvertFrom-Json) }
function Fit($dst, $p) {
    $text = [IO.File]::ReadAllText($dst)
    $nl = "`n"
    if ($text.Contains("`r`n")) { $nl = "`r`n" }
    return @{ Text = $text; Old = $p.old.Replace("`n", $nl); New = $p.new.Replace("`n", $nl) }
}
$bad = 0
$copy = @(); $del = @(); $todo = @()
foreach ($row in $rows) {
    $parts = $row -split "  ", 3
    $bases = $parts[0] -split ","; $new = $parts[1]; $rel = $parts[2]
    $dst = Join-Path $Root $rel
    $src = Join-Path $here $rel
    if (Test-Path -LiteralPath $dst) { $cur = HandoffHash $dst } else { $cur = "new" }
    if ($new -eq "-") {
        if ($cur -eq "new") { $state = "already gone" }
        elseif ($bases -contains $cur -or $bases -contains "*") { $state = "will delete"; $del += $rel }
        else { $state = "DIFFERS"; $bad++ }
    }
    elseif ($cur -eq $new) { $state = "up to date" }
    elseif (-not (Test-Path -LiteralPath $src)) { $state = "MISSING FROM HANDOFF"; $bad++ }
    elseif ((HandoffHash $src) -ne $new) { $state = "HANDOFF COPY DAMAGED"; $bad++ }
    elseif ($cur -eq "new" -and $bases -contains "new") { $state = "will add"; $copy += $rel }
    elseif ($cur -eq $bases[0]) { $state = "will copy"; $copy += $rel }
    elseif ($bases -contains $cur) { $state = "will copy (older copy)"; $copy += $rel }
    elseif ($bases -contains "*") { $state = "will replace"; $copy += $rel }
    else { $state = "DIFFERS"; $bad++ }
    "{0,-24} {1}" -f $state, $rel
}
foreach ($p in $patches) {
    $dst = Join-Path $Root $p.path
    if (-not (Test-Path -LiteralPath $dst)) { "{0,-24} {1}" -f "MISSING", $p.path; $bad++; continue }
    $f = Fit $dst $p
    if ($f.Text.Contains($f.New)) { $state = "patch done" }
    elseif (([regex]::Matches($f.Text, [regex]::Escape($f.Old))).Count -eq 1) { $state = "will patch"; $todo += $p }
    else { $state = "PATCH DOESN'T FIT"; $bad++ }
    "{0,-24} {1}: {2}" -f $state, $p.path, $p.old.Trim().Substring(0, [Math]::Min(50, $p.old.Trim().Length))
}
if ($bad -gt 0) {
    "$bad item(s) don't match what this handoff expects: nothing changed. Report the lines above to the cloud thread."
    exit 1
}
if (-not $Apply) {
    "Check passed: $($copy.Count) to copy, $($del.Count) to delete, $($todo.Count) patch(es). Run again with -Apply."
    exit 0
}
$backup = Join-Path ([Environment]::GetFolderPath("Desktop")) ("ClaudeProjects\handoff\backup_" + $name + "_" + (Get-Date -Format "yyyyMMdd_HHmmss"))
function Keep($rel) {
    $dst = Join-Path $Root $rel
    $b = Join-Path $backup $rel
    if ((Test-Path -LiteralPath $dst) -and -not (Test-Path -LiteralPath $b)) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $b) | Out-Null
        Copy-Item -LiteralPath $dst -Destination $b -Force
    }
}
foreach ($rel in $copy) {
    Keep $rel
    $dst = Join-Path $Root $rel
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $dst) | Out-Null
    Copy-Item -LiteralPath (Join-Path $here $rel) -Destination $dst -Force
}
foreach ($rel in $del) {
    Keep $rel
    Remove-Item -LiteralPath (Join-Path $Root $rel) -Force
}
foreach ($p in $todo) {
    Keep $p.path
    $dst = Join-Path $Root $p.path
    $bytes = [IO.File]::ReadAllBytes($dst)
    $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $f = Fit $dst $p
    [IO.File]::WriteAllText($dst, $f.Text.Replace($f.Old, $f.New), (New-Object System.Text.UTF8Encoding($bom)))
}
"Applied: $($copy.Count) copied, $($del.Count) deleted, $($todo.Count) patched. Backup: $backup"
& (Join-Path $here "Get-PcManifest.ps1") -Root $Root
