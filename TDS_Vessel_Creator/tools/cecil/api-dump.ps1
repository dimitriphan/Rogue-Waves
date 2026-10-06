# Usage: powershell -ExecutionPolicy Bypass -File api-dump.ps1 [-Out ..\..\reference\api_assembly_csharp.txt]
# Writes every type of the game's Assembly-CSharp.dll with its fields and method signatures, one line each:
#   TYPE public class Entity_Master_Radio : NetworkBehaviour [ns=]
#     F public NetworkVariable`1<Link_Beingref> Beingref
#     M public Void Vfxdebuffcheck(String Item, Link_Beingref On, Single Duration)
# Re-run it after a game update and diff against the old dump to see what changed.
# Needs Assembly-CSharp.dll and Trivial.Mono.Cecil.dll next to this script (see il.ps1 / docs/11_TOOLING.md).
param([string]$Out = (Join-Path $PSScriptRoot "..\..\reference\api_assembly_csharp.txt"))
$sp = $PSScriptRoot
Add-Type -Path "$sp\Trivial.Mono.Cecil.dll"
$asm = [Trivial.Mono.Cecil.AssemblyDefinition]::ReadAssembly("$sp\Assembly-CSharp.dll")
$sb = New-Object System.Text.StringBuilder
function Vis($m) {
    if ($m.IsPublic) { "public" } elseif ($m.IsFamily) { "protected" } elseif ($m.IsAssembly) { "internal" } else { "private" }
}
function TypeName($t) {
    $n = $t.Name
    if ($t -is [Trivial.Mono.Cecil.GenericInstanceType]) { $n = $t.Name + "<" + (($t.GenericArguments | ForEach-Object { TypeName $_ }) -join ",") + ">" }
    return $n
}
function Dump($t, $prefix) {
    $kind = if ($t.IsEnum) { "enum" } elseif ($t.IsInterface) { "interface" } elseif ($t.IsValueType) { "struct" } else { "class" }
    $vis = if ($t.IsPublic -or $t.IsNestedPublic) { "public" } else { "internal" }
    $base = if ($t.BaseType) { $t.BaseType.Name } else { "" }
    [void]$sb.AppendLine("TYPE $vis $kind $prefix$($t.Name) : $base [ns=$($t.Namespace)]")
    foreach ($f in $t.Fields) {
        if ($f.Name -match '^<|__') { continue }
        $st = if ($f.IsStatic) { "static " } else { "" }
        [void]$sb.AppendLine("  F $(Vis $f) $st$(TypeName $f.FieldType) $($f.Name)")
    }
    foreach ($m in $t.Methods) {
        if ($m.Name -match '^<|__' -or $m.Name -eq '.cctor') { continue }
        $st = if ($m.IsStatic) { "static " } else { "" }
        $ps = ($m.Parameters | ForEach-Object { (TypeName $_.ParameterType) + " " + $_.Name }) -join ", "
        [void]$sb.AppendLine("  M $(Vis $m) $st$(TypeName $m.ReturnType) $($m.Name)($ps)")
    }
    foreach ($n in $t.NestedTypes) {
        if ($n.Name -match '^<|d__|DisplayClass') { continue }
        Dump $n ($prefix + $t.Name + "/")
    }
}
foreach ($t in $asm.MainModule.Types) { if ($t.Name -ne '<Module>') { Dump $t "" } }
[System.IO.File]::WriteAllText($Out, $sb.ToString())
"wrote $Out ($($sb.Length) chars)"
