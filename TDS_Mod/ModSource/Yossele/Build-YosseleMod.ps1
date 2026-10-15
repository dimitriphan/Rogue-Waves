# Builds Yossele's mod code: joins Code/*.cs (name order; 00_YosseleMod.cs first so its type is the entry point) and
# the shared vessel-mod kit in ModSource/Shared/*.cs (the "ModKit" type prefix becomes "YosseleKit" in every file, so
# no two mods ever define the same type), hoists every `using` line to the top, rejects blocked namespaces, compile-checks
# the result against the game's own DLLs with the Roslyn that ships with Unity (C# 9, like the game's Roslyn 3.8).
# Output: ModSource/Yossele/Build/YosseleMod_Code.cs, which Tools > Yossele > Build Mod Prefab embeds in the prefab.
# Run:  powershell -ExecutionPolicy Bypass -Command "& .\Build-YosseleMod.ps1 [-Define YOSSELE_SELFTEST,YOSSELE_SHOTS]"
# -Define adds #define lines, which switch on debug-only modules (they compile to nothing in a normal build).
param(
    [string]$GameManaged = "D:\Steam\steamapps\common\TDS\TDS\DATA\Managed",
    [string]$UnityData = "C:\Program Files\Unity\Hub\Editor\2022.3.41f1\Editor\Data",
    [string[]]$Define = @(),
    [string]$Prefix = "Yossele"
)
$ErrorActionPreference = "Stop"
$codeDir = Join-Path $PSScriptRoot "Code"
$sharedDir = Join-Path (Split-Path $PSScriptRoot -Parent) "Shared"
$outDir = Join-Path $PSScriptRoot "Build"
New-Item -ItemType Directory -Force $outDir | Out-Null

$usings = New-Object System.Collections.Generic.List[string]
$bodies = New-Object System.Text.StringBuilder
$files = @(Get-ChildItem $codeDir -Filter *.cs | Sort-Object Name)
if (Test-Path $sharedDir) { $files += @(Get-ChildItem $sharedDir -Filter *.cs | Sort-Object Name) }
foreach ($file in $files) {
    [void]$bodies.AppendLine("// ---- " + $file.Name)
    foreach ($line in [System.IO.File]::ReadAllLines($file.FullName)) {
        $line = $line.Replace("ModKit", $Prefix + "Kit")
        if ($line -match '^\s*using\s+[\w\.]+\s*;\s*$') {
            $u = $line.Trim()
            if (-not $usings.Contains($u)) { $usings.Add($u) }
        } else {
            [void]$bodies.AppendLine($line)
        }
    }
}
# A <NAME>_SELFTEST define also switches on the shared kit's self-test helpers (ModKitTest, kit 1.5).
if (@($Define | Where-Object { $_ -match "_SELFTEST" }).Count -gt 0) { $Define = @($Define) + "MODKIT_TEST" }
$defines = ($Define | Where-Object { $_ } | ForEach-Object { "#define $_" }) -join "`n"
$code = $(if ($defines) { $defines + "`n" } else { "" }) + ($usings -join "`n") + "`n`n" + $bodies.ToString()

$codeOnly = [regex]::Replace($code, '//[^\n]*', '')
foreach ($banned in "System.IO", "System.Reflection") {
    if ($codeOnly -match [regex]::Escape($banned)) { throw "Yossele code uses $banned, which the game blocks." }
}

$out = Join-Path $outDir "YosseleMod_Code.cs"
[System.IO.File]::WriteAllText($out, $code.Replace("`r`n", "`n"), (New-Object System.Text.UTF8Encoding($false)))

$rsp = Join-Path $outDir "check.rsp"
$lines = @("-nologo", "-nostdlib+", "-target:library", "-langversion:9", "-nowarn:CS0618,CS0162,CS0414,CS0649",
           "-out:`"$(Join-Path $outDir 'check.dll')`"")
# Exactly the assemblies the game's RoslynCSharp settings reference (read from resources.assets), plus mscorlib.
# Anything else (System.Core: HashSet/Linq; UnityEngine.JSONSerializeModule: JsonUtility...) fails in game.
$gameRefs = "mscorlib", "Assembly-CSharp", "Cinemachine", "netstandard", "UniTask", "Unity.Addressables", "Unity.Collections",
    "Unity.InputSystem", "Unity.Netcode.Components", "Unity.Netcode.Runtime", "Unity.Networking.Transport", "Unity.TextMeshPro",
    "UnityEngine.AIModule", "UnityEngine.AnimationModule", "UnityEngine.AssetBundleModule", "UnityEngine.AudioModule",
    "UnityEngine.CoreModule", "UnityEngine", "UnityEngine.InputLegacyModule", "UnityEngine.ParticleSystemModule",
    "UnityEngine.Physics2DModule", "UnityEngine.PhysicsModule", "UnityEngine.UI", "UnityEngine.UIModule"
foreach ($r in $gameRefs) {
    $dll = Join-Path $GameManaged "$r.dll"
    if (-not (Test-Path $dll)) { throw "missing reference $dll" }
    $lines += "-reference:`"$dll`""
}
$lines += "`"$out`""
[System.IO.File]::WriteAllLines($rsp, $lines)
& "$UnityData\NetCoreRuntime\dotnet.exe" "$UnityData\DotNetSdkRoslyn\csc.dll" "@$rsp"
if ($LASTEXITCODE -ne 0) { throw "Compile check failed." }
Write-Output "Compile check passed: $out ($((Get-Item $out).Length) bytes)"
