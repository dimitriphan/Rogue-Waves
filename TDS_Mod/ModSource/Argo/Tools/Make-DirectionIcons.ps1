param([string]$OutDir = '')
# Draws Argo's two Direction cards' 12 x 12 icons (temporary art) in the pack's palette, as the mode part's RWDirections.Picture
# finds them: a library sprite named Direction_<Name>, any case. Until 2026-11-04 these two cards showed the crest.
# Default folder: ModSource\RogueWavesMode\Art\Temporary\game_size. The mode part's Build All Assets takes them from there.
Add-Type -AssemblyName System.Drawing
if ($OutDir -eq '') { $OutDir = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'RogueWavesMode\Art\Temporary\game_size' }
$palette = New-Object System.Collections.Hashtable    # case-sensitive, unlike @{}
foreach ($pair in @('T=3F8F96', 't=327378', 'N=2E4A62', 'L=DCEFEC', 'B=8A909C', 'b=5F6470', 'S=D8C49A', 's=A8936A')) { $palette[$pair.Substring(0, 1)] = $pair.Substring(2) }
$icons = [ordered]@{
    'Direction_Deepcurrent' = @(
        '............',
        '...TTTTT....',
        '..TLttttT...',
        '.TLt....tT..',
        '.Tt..TT..T..',
        '.Tt.TttT....',
        '.Tt.Tt.TTTT.',
        '.TLt.TTTttT.',
        '..TTt.....T.',
        '...TTTTTTTT.',
        'NNNNNNNNNNNN',
        '.NNNNNNNNNN.'
    )
    'Direction_Heavysalvo' = @(
        '.........BBB',
        '........BLBb',
        '.......BLBb.',
        '..S...BLBb..',
        '...S.BBBb...',
        '....BBBb....',
        '...bBBb.S...',
        '..sbbb...S..',
        '.ssb........',
        'ssb.........',
        's...........',
        '............'
    )
}
foreach ($name in $icons.Keys) {
    $rows = $icons[$name]
    $bmp = New-Object System.Drawing.Bitmap 12, 12, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt 12; $y++) {
        if ($rows[$y].Length -ne 12) { throw "$name row $y is $($rows[$y].Length) wide" }
        for ($x = 0; $x -lt 12; $x++) {
            $c = $rows[$y].Substring($x, 1)
            if ($c -eq '.') { $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0)); continue }
            $h = $palette[$c]
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, [Convert]::ToInt32($h.Substring(0, 2), 16), [Convert]::ToInt32($h.Substring(2, 2), 16), [Convert]::ToInt32($h.Substring(4, 2), 16)))
        }
    }
    $path = Join-Path $OutDir ($name + '.png')
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    "wrote $path"
}