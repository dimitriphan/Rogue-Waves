# Fetches a handoff from GitHub, so nobody has to save a zip by hand. Downloads branch <Ref> of the repository as one
# zip, then lays out handoff <Name> in Desktop\ClaudeProjects\handoff\<Name> exactly like a saved and unpacked handoff
# zip: README.md, HASHES.txt, PATCHES.json (from TDS_Mod\Handoff\<Name> on that branch), the handoff scripts, and every
# file HASHES.txt lists. Nothing in TDS_Mod changes; Apply-Handoff.ps1 does that afterwards, once Dimitri OKs it.
#   powershell -ExecutionPolicy Bypass -Command "& '<this folder>\Get-Handoff.ps1' -Ref claude/some-branch -Name Argo_round10"
# Needs a fine-grained GitHub token that reaches only dimitriphan/ModProjects with Contents: Read-only, which Dimitri
# saves in %USERPROFILE%\.modprojects_token (TDS_Mod\Tools\pc\README.md). The script never prints or copies the token.
param(
    [Parameter(Mandatory = $true)][string]$Ref,
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$Repo = "dimitriphan/ModProjects",
    [string]$TokenFile = (Join-Path $env:USERPROFILE ".modprojects_token")
)
$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (-not (Test-Path -LiteralPath $TokenFile)) { throw "No token at $TokenFile. See TDS_Mod\Tools\pc\README.md." }
$token = ([IO.File]::ReadAllText($TokenFile)).Trim()
$headers = @{ Authorization = "Bearer $token"; Accept = "application/vnd.github+json"; "User-Agent" = "ModProjects-Get-Handoff"; "X-GitHub-Api-Version" = "2022-11-28" }
$sha = (Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/commits/$Ref" -Headers $headers).sha
$short = $sha.Substring(0, 7)
$zip = Join-Path $env:TEMP "modprojects_$short.zip"
Invoke-WebRequest -Uri "https://api.github.com/repos/$Repo/zipball/$sha" -Headers $headers -OutFile $zip -UseBasicParsing

$dest = Join-Path ([Environment]::GetFolderPath("Desktop")) "ClaudeProjects\handoff\$Name"
if (Test-Path -LiteralPath $dest) {
    $aside = $dest + "_old_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    Rename-Item -LiteralPath $dest -NewName (Split-Path -Leaf $aside)
    "an earlier copy of $Name was renamed to $(Split-Path -Leaf $aside)"
}
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $root = $archive.Entries[0].FullName.Split("/")[0] + "/"
    $byName = @{}
    foreach ($e in $archive.Entries) { $byName[$e.FullName] = $e }
    function Take($entryName, $target) {
        $e = $byName[$entryName]
        if ($e -eq $null) { return $false }
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $target, $true)
        return $true
    }
    $hdir = $root + "TDS_Mod/Handoff/$Name/"
    $got = 0
    foreach ($e in $archive.Entries) {
        if ($e.FullName.StartsWith($hdir) -and $e.Name) { [void](Take $e.FullName (Join-Path $dest $e.FullName.Substring($hdir.Length))); $got++ }
        elseif ($e.FullName.StartsWith($root + "TDS_Mod/Tools/pc/") -and $e.Name.EndsWith(".ps1")) { [void](Take $e.FullName (Join-Path $dest $e.Name)) }
    }
    if ($got -eq 0) { throw "branch $Ref ($short) has no TDS_Mod/Handoff/$Name" }
    . (Join-Path $dest "HandoffHash.ps1")
    $files = 0; $bad = 0
    foreach ($row in (Get-Content -LiteralPath (Join-Path $dest "HASHES.txt") | Where-Object { $_ -and -not $_.StartsWith("#") })) {
        $parts = $row -split "  ", 3
        $new = $parts[1]; $rel = $parts[2]
        if ($new -eq "-") { continue }
        $target = Join-Path $dest $rel
        if (-not (Take ($root + "TDS_Mod/" + $rel.Replace("\", "/")) $target)) { "MISSING ON BRANCH  $rel"; $bad++; continue }
        if ((HandoffHash $target) -ne $new) { "HASH MISMATCH      $rel"; $bad++ }
        $files++
    }
}
finally { $archive.Dispose(); Remove-Item -LiteralPath $zip -Force }
if ($bad -gt 0) { "fetched $Name from $Ref at $short with $bad problem(s): report them to the cloud thread"; exit 1 }
"fetched $Name from $Ref at $($short): $files file(s), every hash matches. Next: Apply-Handoff.ps1 in $dest (check only)."
