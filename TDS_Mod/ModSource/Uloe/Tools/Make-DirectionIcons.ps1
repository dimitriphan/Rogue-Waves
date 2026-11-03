param([string]$OutDir = '', [string]$Sheet = '')
# Draws the four Uloe Direction cards' 12 x 12 icons (temporary art) in the pack's palette, solid pixels on a clear ground, one
# object each, as the mode part's RWDirections.Picture finds them: a library sprite named Direction_<Name>, any case.
# Default folder: ModSource\RogueWavesMode\Art\Temporary\game_size. -Sheet writes an 8 x enlargement to look at.
Add-Type -AssemblyName System.Drawing
if ($OutDir -eq '') { $OutDir = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'RogueWavesMode\Art\Temporary\game_size' }

$palette = New-Object System.Collections.Hashtable    # case-sensitive, unlike @{}
foreach ($pair in @(
    'W=E9F1EE', 'w=C3D8D3',     # frosted glass
    'F=E2725B', 'f=B55240',     # the cord's orange
    'Y=E8C46C', 'y=C49C46',     # battery gold
    'T=3F8F96', 't=327378',     # battery teal
    'S=D8C49A', 's=A8936A',     # tape, sand
    'B=8A909C', 'b=5F6470',     # steel
    'N=2E4A62',                 # navy
    'G=97AFE5', 'g=4764A4',     # flash bulb's blue glass
    'L=FFFFFF',                 # highlight
    'M=DEDEDE', 'm=939292'      # smoke
)) { $palette[$pair.Substring(0, 1)] = $pair.Substring(2) }

$icons = [ordered]@{
    'Direction_Extension' = @(
        '.bB......Bb.',
        '.bBFFFFFFBb.',
        '.bBffffffBb.',
        '.bBFFFFFFBb.',
        '.bBffffffBb.',
        '.bBFFFFFFBb.',
        '.bBffffffBb.',
        '.bBFFFFFFBb.',
        '.bB.F....Bb.',
        '....F.......',
        '....fF...BB.',
        '.....fFFFBBB'
    )
    'Direction_Brightbulb' = @(
        '....WWWW....',
        '..LLWWWWww..',
        '.LLWWWWWWww.',
        '.LWWWWWWWWw.',
        '.LWWWWWWWWw.',
        '.WWbWbbWbbw.',
        '..WWWWWWww..',
        '...WWWWww...',
        '....BBBb....',
        '....bbbb....',
        '....BBBb....',
        '.....NN.....'
    )
    'Direction_Batteries' = @(
        '...b....b...',
        '.YYYYYyYYYYy',
        '.YLYYYyYLYYy',
        '.YLYYYyYLYYy',
        '.YLYYYyYLYYy',
        'SSSSSSSSSSSS',
        'SSSSSSSSSSSS',
        '.TWTTtTTWTTt',
        '.TWTTtTTWTTt',
        '.TTTTtTTTTTt',
        '.TTTTtTTTTTt',
        '.NNNNNNNNNNN'
    )
    'Direction_Flashbulb' = @(
        '.......Mm...',
        '......Mm....',
        '.......m....',
        '...GGGGGG...',
        '..GLLGGGGg..',
        '..GLGFFGGg..',
        '..GGFffFGg..',
        '..GGGFFGGg..',
        '...GGGGgg...',
        '....BBBb....',
        '....bbbb....',
        '....NNNN....'
    )
}

function Hex([string]$h) { return [System.Drawing.Color]::FromArgb(255, [Convert]::ToInt32($h.Substring(0, 2), 16), [Convert]::ToInt32($h.Substring(2, 2), 16), [Convert]::ToInt32($h.Substring(4, 2), 16)) }

$made = @()
foreach ($name in $icons.Keys) {
    $rows = $icons[$name]
    if ($rows.Count -ne 12) { throw "$name has $($rows.Count) rows" }
    $bmp = New-Object System.Drawing.Bitmap 12, 12, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt 12; $y++) {
        if ($rows[$y].Length -ne 12) { throw "$name row $y is $($rows[$y].Length) wide" }
        for ($x = 0; $x -lt 12; $x++) {
            $c = $rows[$y].Substring($x, 1)
            if ($c -eq '.') { $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0)); continue }
            if (-not $palette.ContainsKey($c)) { throw "$name uses $c" }
            $bmp.SetPixel($x, $y, (Hex $palette[$c]))
        }
    }
    $path = Join-Path $OutDir ($name + '.png')
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $made += $path
    Write-Output "wrote $path"
}

if ($Sheet -ne '') {
    $big = New-Object System.Drawing.Bitmap (4 * 104), 104
    $g = [System.Drawing.Graphics]::FromImage($big)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 40, 40, 48))
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    for ($i = 0; $i -lt $made.Count; $i++) {
        $b = New-Object System.Drawing.Bitmap $made[$i]
        $g.DrawImage($b, $i * 104 + 4, 4, 96, 96)
        $b.Dispose()
    }
    $g.Dispose()
    $big.Save($Sheet, [System.Drawing.Imaging.ImageFormat]::Png)
    $big.Dispose()
}
