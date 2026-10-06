# Usage: strings.ps1 -Want 'Setup','Gameloading' [-Like]   (lists game methods that load those string literals)
param([string[]]$Want, [switch]$Like)
$sp = Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -Path "$sp\Trivial.Mono.Cecil.dll"
$asm = [Trivial.Mono.Cecil.AssemblyDefinition]::ReadAssembly("$sp\Assembly-CSharp.dll")
$out = New-Object System.Collections.Generic.List[string]
function Walk($t) {
    foreach ($m in $t.Methods) {
        if (-not $m.HasBody) { continue }
        foreach ($ins in $m.Body.Instructions) {
            $op = $ins.Operand
            if ($op -is [string]) {
                foreach ($w in $Want) {
                    if (($Like -and $op -like "*$w*") -or ($op -eq $w)) { $out.Add("$($t.FullName).$($m.Name) : '$op'") }
                }
            }
        }
    }
    foreach ($n in $t.NestedTypes) { Walk $n }
}
foreach ($t in $asm.MainModule.Types) { Walk $t }
$out | Sort-Object -Unique
