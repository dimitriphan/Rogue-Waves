# Usage: callers.ps1 -Type Bl_Address -Members Load,Unload   (lists game methods that call/read those members)
param([string]$Type, [string[]]$Members)
$sp = Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -Path "$sp\Trivial.Mono.Cecil.dll"
$asm = [Trivial.Mono.Cecil.AssemblyDefinition]::ReadAssembly("$sp\Assembly-CSharp.dll")
$out = New-Object System.Collections.Generic.List[string]
foreach ($t in $asm.MainModule.Types) {
    $all = @($t) + @($t.NestedTypes)
    foreach ($tt in $all) {
        foreach ($m in $tt.Methods) {
            if (-not $m.HasBody) { continue }
            foreach ($ins in $m.Body.Instructions) {
                $op = $ins.Operand
                if ((($op -is [Trivial.Mono.Cecil.MethodReference]) -or ($op -is [Trivial.Mono.Cecil.FieldReference])) -and $op.DeclaringType.Name -eq $Type -and ($Members -contains $op.Name)) {
                    $out.Add("$($tt.Name).$($m.Name) -> $($op.Name)")
                }
            }
        }
    }
}
$out | Sort-Object -Unique
