# Scaffolds a new TDS vessel mod from the reference vessel in ..\template (Mummer 1.0.0, 72/72 in its in-game self-test).
#
#   powershell -ExecutionPolicy Bypass -File tools\New-Vessel.ps1 -Name Bramble
#
# Creates, in the Unity mod project:
#   ModSource\<Name>\Code\*.cs              runtime code (joined and compile-checked by Build-<Name>Mod.ps1)
#   ModSource\<Name>\Build-<Name>Mod.ps1    build + compile check;  Deploy-Wait.ps1;  Tools\Decode-Dump.ps1
#   ModSource\<Name>\DESIGN.md              the design form to fill in first (docs\02_DESIGN_RULES.md)
#   Assets\<Name>Mod\Editor\*.cs            asset generator + Tools/<Name>/... menu (Build All Assets, Build Mod Prefab, Export...)
#   ModSource\Shared\*.cs                   the shared kit (TDS_Vessel_Creator\kit\README.md), only if the project has none yet
#
# Every identifier is renamed: Mummer -> <Name>, mummer -> <name>, MUMMER -> <NAME>, and the reference vessel's unit
# (Understudy, whose ids carry no vessel name) -> <Name>unit, so nothing the new vessel registers collides with Mummer
# or any other mod (the game keeps one global name table: Bl_Address.Groupsname). The result compiles and plays exactly
# like Mummer under the new name; docs\04_BUILD_PIPELINE.md says what to replace, in which order.
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$ProjectRoot = "C:\Users\Dimitri\TDS_Mod",
    [string]$GameRoot = "D:\Steam\steamapps\common\TDS",
    [string]$Template = "Mummer",
    [string]$TemplateUnit = "Understudy"
)
$ErrorActionPreference = "Stop"
$resource = Split-Path $PSScriptRoot -Parent
$src = Join-Path $resource "template\$Template"
$kit = Join-Path $resource "kit\Shared"

# ---------------------------------------------------------------- checks
if ($Name -cnotmatch '^[A-Z][a-z]{2,14}$') {
    throw "Name must be one capitalised word of 3-15 letters, lower case after the first (like the game's Smacky, Boutman, Buster): '$Name'"
}
$taken = @("Smacky", "Snosno", "Boutman", "Fie", "Buster", "Smocky", $Template)
foreach ($t in $taken) { if ($Name -eq $t) { throw "'$Name' is a shipped or reference vessel name" } }
if ($Name.ToLower().Contains($Template.ToLower()) -or $Template.ToLower().Contains($Name.ToLower())) {
    throw "'$Name' overlaps the template name '$Template'; pick a name that neither contains it nor is contained in it"
}
$modSource = Join-Path $ProjectRoot "ModSource\$Name"
$assets = Join-Path $ProjectRoot "Assets\${Name}Mod"
$installed = Join-Path $GameRoot "Mods\${Name}Mod"
foreach ($p in $modSource, $assets) { if (Test-Path $p) { throw "already exists: $p" } }
if (Test-Path $installed) { Write-Warning "a mod named ${Name}Mod is already installed at $installed" }
if (-not (Test-Path (Join-Path $ProjectRoot "Assets"))) { throw "not a Unity project: $ProjectRoot" }
if (-not (Test-Path $src)) { throw "template missing: $src" }

$lower = $Name.ToLower(); $upper = $Name.ToUpper()
$unit = $Name + "unit"; $units = $Name + "units"
$tl = $Template.ToLower(); $tu = $Template.ToUpper()
$ul = $TemplateUnit.ToLower()
# Order matters: the plural first ("Understudies" does not contain "Understudy"). A list of pairs, not a hashtable:
# PowerShell hashtables (and [ordered]) ignore key case, which merged Mummer/MUMMER/mummer into one key.
$pairs = @(
    ,@(($TemplateUnit.Substring(0, $TemplateUnit.Length - 1) + "ies"), $units)
    ,@($TemplateUnit, $unit)
    ,@(($ul.Substring(0, $ul.Length - 1) + "ies"), $units.ToLower())
    ,@($ul, $unit.ToLower())
    ,@($tu, $upper)
    ,@($Template, $Name)
    ,@($tl, $lower)
)

function Rename([string]$text) {
    foreach ($p in $pairs) { $text = $text.Replace($p[0], $p[1]) }   # String.Replace is case-sensitive
    return $text
}
$utf8 = New-Object System.Text.UTF8Encoding($false)
function CopyRenamed([string]$from, [string]$toDir) {
    New-Item -ItemType Directory -Force $toDir | Out-Null
    $target = Join-Path $toDir (Rename (Split-Path $from -Leaf))
    $text = [System.IO.File]::ReadAllText($from, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($target, (Rename $text), $utf8)
    return $target
}

# ---------------------------------------------------------------- copy
$written = New-Object System.Collections.Generic.List[string]
foreach ($f in Get-ChildItem (Join-Path $src "Code") -Filter *.cs) { $written.Add((CopyRenamed $f.FullName (Join-Path $modSource "Code"))) }
foreach ($f in Get-ChildItem (Join-Path $src "Editor") -Filter *.cs) { $written.Add((CopyRenamed $f.FullName (Join-Path $assets "Editor"))) }
foreach ($f in Get-ChildItem $src -Filter *.ps1) { $written.Add((CopyRenamed $f.FullName $modSource)) }
foreach ($f in Get-ChildItem (Join-Path $src "Tools") -Filter *.ps1) { $written.Add((CopyRenamed $f.FullName (Join-Path $modSource "Tools"))) }

$design = Join-Path $resource "template\DESIGN_TEMPLATE.md"
if (Test-Path $design) {
    $d = [System.IO.File]::ReadAllText($design, [System.Text.Encoding]::UTF8).Replace("{{NAME}}", $Name).Replace("{{DATE}}", (Get-Date -Format "yyyy-MM-dd"))
    [System.IO.File]::WriteAllText((Join-Path $modSource "DESIGN.md"), $d, $utf8)
    $written.Add((Join-Path $modSource "DESIGN.md"))
}

# ---------------------------------------------------------------- shared kit
$shared = Join-Path $ProjectRoot "ModSource\Shared"
if (-not (Test-Path $shared)) {
    New-Item -ItemType Directory -Force $shared | Out-Null
    Copy-Item (Join-Path $kit "*.cs") $shared
    $written.Add("$shared (kit copied)")
} else {
    foreach ($f in Get-ChildItem $kit -Filter *.cs) {
        $there = Join-Path $shared $f.Name
        if (-not (Test-Path $there)) { Copy-Item $f.FullName $there; $written.Add("$there (added)"); continue }
        if ((Get-FileHash $there).Hash -ne (Get-FileHash $f.FullName).Hash) {
            Write-Warning "ModSource\Shared\$($f.Name) differs from the resource kit. Keep the newer one (read both headers); every mod in the project compiles the same Shared folder."
        }
    }
}

# ---------------------------------------------------------------- leftovers check
$left = @()
foreach ($p in $written) {
    if ($p -match '\(' -or $p.EndsWith("DESIGN.md")) { continue }   # the design form names the template as an example
    $hits = Select-String -Path $p -Pattern $Template, $TemplateUnit -SimpleMatch -CaseSensitive
    if ($hits) { $left += $hits }
}
if ($left.Count -gt 0) { Write-Warning ("template names left behind: " + (($left | ForEach-Object { $_.Filename + ":" + $_.LineNumber } | Select-Object -First 10) -join ", ")) }

"Scaffolded $Name from $Template ($($written.Count) files)."
"  code:   $modSource"
"  editor: $assets\Editor"
""
"Next (docs\04_BUILD_PIPELINE.md):"
"  1. Fill in $modSource\DESIGN.md (docs\02_DESIGN_RULES.md) before touching code."
"  2. powershell -ExecutionPolicy Bypass -Command `"& '$modSource\Build-${Name}Mod.ps1'`"   (must pass as scaffolded)"
"  3. Let Unity compile, then Tools > $Name > Build All Assets (the art is still $Template's: redraw it)."
"  4. Replace the mechanics module by module; keep the generic modules (docs\05_CODE_COOKBOOK.md)."
