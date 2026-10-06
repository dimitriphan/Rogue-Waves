# Usage: powershell -File il.ps1 -Type Name -Method Name [-Max 400] [-From 0]
# Prints the IL of a game method in a compact readable form (for reverse-engineering the TDS API).
param([string]$Type, [string]$Method, [int]$Max = 400, [int]$From = 0)
$sp = Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -Path "$sp\Trivial.Mono.Cecil.dll"
$asm = [Trivial.Mono.Cecil.AssemblyDefinition]::ReadAssembly("$sp\Assembly-CSharp.dll")
function FindType($name) {
    if ($name.Contains("/")) { $pp = $name.Split("/"); foreach ($t in $asm.MainModule.Types) { if ($t.Name -eq $pp[0]) { foreach ($n in $t.NestedTypes) { if ($n.Name -eq $pp[1]) { return $n } } } } }
    foreach ($t in $asm.MainModule.Types) {
        if ($t.Name -eq $name) { return $t }
        foreach ($n in $t.NestedTypes) { if (($t.Name + '/' + $n.Name) -eq $name -or $n.Name -eq $name) { return $n } }
    }
    return $null
}
$t = FindType $Type
if ($t -eq $null) { "type not found"; exit }
foreach ($m in $t.Methods) {
    if ($m.Name -ne $Method -or -not $m.HasBody) { continue }
    "=== $Type.$Method(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name + ' ' + $_.Name }) -join ', ') + ")"
    $i = 0
    foreach ($ins in $m.Body.Instructions) {
        if ($i -ge $From -and $i -lt $From + $Max) {
            $op = $ins.Operand
            if ($op -is [Trivial.Mono.Cecil.GenericInstanceMethod]) { $s = $op.DeclaringType.Name + '::' + $op.Name + '<' + (($op.GenericArguments | ForEach-Object Name) -join ',') + '>' }
            elseif ($op -is [Trivial.Mono.Cecil.MethodReference]) { $s = $op.DeclaringType.Name + '::' + $op.Name }
            elseif ($op -is [Trivial.Mono.Cecil.FieldReference]) { $s = $op.DeclaringType.Name + '::' + $op.Name }
            elseif ($op -is [Trivial.Mono.Cecil.Cil.Instruction]) { $s = 'IL_' + $op.Offset.ToString('x4') }
            elseif ($op -is [string]) { $s = '"' + $op + '"' }
            else { $s = "$op" }
            '{0:x4} {1} {2}' -f $ins.Offset, $ins.OpCode.Name, $s
        }
        $i++
    }
}
