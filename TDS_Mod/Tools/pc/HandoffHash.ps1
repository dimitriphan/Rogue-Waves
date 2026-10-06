# Shared by the handoff scripts (dot-sourced). The hash make_handoff.py computes too: the first 16 hex digits of the
# sha256 of a text file read as UTF-8 with any BOM and every CR removed (line endings and BOMs don't count), or of the
# bytes of any other file.
$script:HandoffSha = [Security.Cryptography.SHA256]::Create()
$script:HandoffText = @(".cs", ".ps1", ".md", ".txt", ".json", ".lua", ".py", ".sh", ".html", ".csv", ".xml", ".asmdef")
function HandoffHash([string]$path) {
    if ($script:HandoffText -contains [IO.Path]::GetExtension($path).ToLower()) {
        $t = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
        $t = $t.TrimStart([char]0xFEFF).Replace("`r", "")
        $bytes = (New-Object System.Text.UTF8Encoding($false)).GetBytes($t)
    } else {
        $bytes = [IO.File]::ReadAllBytes($path)
    }
    return ([BitConverter]::ToString($script:HandoffSha.ComputeHash($bytes))).Replace("-", "").Substring(0, 16).ToLower()
}
