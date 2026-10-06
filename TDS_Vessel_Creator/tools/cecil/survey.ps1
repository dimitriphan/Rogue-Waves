# Usage: survey.ps1 -Match 'Boutman|Fie' [-Out file]
# For every game type whose name matches, prints base type, fields, and per method: string literals, called engine
# members (Helper_*, Radio, Game_*, Entity_Master fields, NetworkVariable access...), compactly.
param([string]$Match, [string]$Out = "")
$sp = Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -Path "$sp\Trivial.Mono.Cecil.dll"
$asm = [Trivial.Mono.Cecil.AssemblyDefinition]::ReadAssembly("$sp\Assembly-CSharp.dll")
$sb = New-Object System.Text.StringBuilder
function MethodSummary($m, $indent) {
    if (-not $m.HasBody) { return }
    $strs = New-Object System.Collections.Generic.List[string]
    $calls = New-Object System.Collections.Generic.List[string]
    foreach ($ins in $m.Body.Instructions) {
        $op = $ins.Operand
        if ($op -is [string]) { if (-not $strs.Contains($op)) { $strs.Add($op) } }
        elseif ($op -is [Trivial.Mono.Cecil.MethodReference] -or $op -is [Trivial.Mono.Cecil.FieldReference]) {
            $dt = $op.DeclaringType.Name
            if ($dt -match '^(Helper_|Entity_Master|Game_|Tds_|Bl_Address|Link_|Blm_|Bla_|Macro_|Entity_Ai_Base|Entity_Command_Base|Entity_Vis_Base|Mode_)' -or $dt -match 'Radio') {
                if ($op.Name -match '^(get_|set_)?(Value)$' ) { continue }
                $c = "$dt.$($op.Name)"
                if (-not $calls.Contains($c)) { $calls.Add($c) }
            }
        }
    }
    $line = "$indent$($m.Name)(" + (($m.Parameters | ForEach-Object { $_.ParameterType.Name }) -join ',') + ")"
    if ($strs.Count) { $line += "  STR: " + (($strs | ForEach-Object { '"' + $_ + '"' }) -join ' ') }
    if ($calls.Count) { $line += "  USES: " + ($calls -join ' ') }
    [void]$sb.AppendLine($line)
}
function TypeSummary($t, $indent) {
    [void]$sb.AppendLine("$indent== $($t.Name) : $($t.BaseType.Name)")
    $f = @($t.Fields | Where-Object { $_.Name -notmatch '^<' } | ForEach-Object { ($_.FieldType.Name -replace '`1','') + " " + $_.Name })
    if ($f.Count) { [void]$sb.AppendLine("$indent   F: " + ($f -join '; ')) }
    foreach ($m in $t.Methods) { if ($m.Name -notmatch '^(\.ctor|\.cctor|__)') { MethodSummary $m "$indent   " } }
    foreach ($n in $t.NestedTypes) {
        if ($n.Name -match 'd__|DisplayClass|<') {
            foreach ($m in $n.Methods) { if ($m.Name -eq 'MoveNext' -or $m.Name -match '^<') { MethodSummary $m "$indent   [$($n.Name)] " } }
        }
    }
}
foreach ($t in ($asm.MainModule.Types | Sort-Object Name)) {
    if ($t.Name -match $Match) { TypeSummary $t "" }
}
if ($Out) { [System.IO.File]::WriteAllText($Out, $sb.ToString()); "wrote $Out ($($sb.Length) chars)" } else { $sb.ToString() }
