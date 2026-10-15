# Turns "[ArgoDump] <name> <w>x<h> <part>/<parts> <base64 RGBA>" lines from Player.log (written by the
# ARGO_SHOTS build, Code/97_Shot.cs) back into PNG files. Rows arrive bottom first (Unity order).
# Run:  powershell -ExecutionPolicy Bypass -File Tools\Decode-Dump.ps1 [-Log <Player.log>] [-Out <folder>]
param(
    [string]$Log = "$env:USERPROFILE\AppData\LocalLow\Blockage\TDS\Player.log",
    [string]$Out = (Join-Path (Split-Path $PSScriptRoot -Parent) "Research\dump")
)
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Out | Out-Null
$parts = @{}
$sizes = @{}
foreach ($line in [IO.File]::ReadLines($Log)) {
    if (-not $line.StartsWith("[ArgoDump] ")) { continue }
    $f = $line.Split(' ', 5)
    if ($f.Count -lt 5 -or $f[1] -eq 'INFO' -or $f[3] -notmatch '^(\d+)/(\d+)$') { continue }
    $name = $f[1]
    if (-not $parts.ContainsKey($name)) { $parts[$name] = @{}; $sizes[$name] = $f[2] }
    $parts[$name][[int]$Matches[1]] = $f[4]
}
foreach ($name in $parts.Keys) {
    $wh = $sizes[$name].Split('x'); $w = [int]$wh[0]; $h = [int]$wh[1]
    $sb = New-Object Text.StringBuilder
    foreach ($i in ($parts[$name].Keys | Sort-Object)) { [void]$sb.Append($parts[$name][$i]) }
    $bytes = [Convert]::FromBase64String($sb.ToString())
    $bmp = New-Object Drawing.Bitmap $w, $h, ([Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $data = $bmp.LockBits((New-Object Drawing.Rectangle 0, 0, $w, $h), 'WriteOnly', $bmp.PixelFormat)
    $row = New-Object byte[] ($w * 4)
    for ($y = 0; $y -lt $h; $y++) {
        $src = ($h - 1 - $y) * $w * 4
        for ($x = 0; $x -lt $w; $x++) {
            $s = $src + $x * 4
            $row[$x * 4] = $bytes[$s + 2]; $row[$x * 4 + 1] = $bytes[$s + 1]; $row[$x * 4 + 2] = $bytes[$s]; $row[$x * 4 + 3] = $bytes[$s + 3]
        }
        [Runtime.InteropServices.Marshal]::Copy($row, 0, [IntPtr]($data.Scan0.ToInt64() + $y * $data.Stride), $w * 4)
    }
    $bmp.UnlockBits($data)
    $file = Join-Path $Out ($name + ".png")
    $bmp.Save($file, [Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    "$name ${w}x$h -> $file"
}

