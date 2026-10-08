# Builds Riptide's mod code: joins Code/*.cs (name order; 00_RiptideMod.cs first so its type is the entry point),
# hoists every `using` line to the top, rejects blocked namespaces, compile-checks the result against the game's own
# DLLs with the Roslyn that ships with Unity (C# 9, like the game's Roslyn 3.8). Riptide adds no art, so unlike the
# vessels it doesn't join the vessel kit in ModSource/Shared.
# Output: ModSource/Riptide/Build/RiptideMod_Code.cs, which Tools > Riptide > Build Mod Prefab embeds in the prefab.
# Run:  powershell -ExecutionPolicy Bypass -Command "& .\Build-RiptideMod.ps1 [-Define RIPTIDE_SELFTEST,RIPTIDE_SANDBOX,RIPTIDE_SHOTS]"
# -Define adds #define lines, which switch on debug-only modules (they compile to nothing in a normal build).
param(
    [string]$GameManaged = "D:\Steam\steamapps\common\TDS\TDS\DATA\Managed",
    [string]$UnityData = "C:\Program Files\Unity\Hub\Editor\2022.3.41f1\Editor\Data",
    [string[]]$Define = @()
)
$ErrorActionPreference = "Stop"
$codeDir = Join-Path $PSScriptRoot "Code"
$outDir = Join-Path $PSScriptRoot "Build"
New-Item -ItemType Directory -Force $outDir | Out-Null

$usings = New-Object System.Collections.Generic.List[string]
$bodies = New-Object System.Text.StringBuilder
$files = @(Get-ChildItem $codeDir -Filter *.cs | Sort-Object Name)
foreach ($file in $files) {
    [void]$bodies.AppendLine("// ---- " + $file.Name)
    foreach ($line in [System.IO.File]::ReadAllLines($file.FullName)) {
        if ($line -match '^\s*using\s+[\w\.]+\s*;\s*$') {
            $u = $line.Trim()
            if (-not $usings.Contains($u)) { $usings.Add($u) }
        } else {
            [void]$bodies.AppendLine($line)
        }
    }
}
$defines = ($Define | Where-Object { $_ } | ForEach-Object { "#define $_" }) -join "`n"
$code = $(if ($defines) { $defines + "`n" } else { "" }) + ($usings -join "`n") + "`n`n" + $bodies.ToString()

$codeOnly = [regex]::Replace($code, '//[^\n]*', '')
foreach ($banned in "System.IO", "System.Reflection") {
    if ($codeOnly -match [regex]::Escape($banned)) { throw "Riptide code uses $banned, which the game blocks." }
}

$out = Join-Path $outDir "RiptideMod_Code.cs"
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
