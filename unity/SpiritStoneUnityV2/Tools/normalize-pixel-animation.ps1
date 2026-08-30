param(
    [Parameter(Mandatory = $true)]
    [string]$InputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$SpriteSheetPath,

    [Parameter(Mandatory = $true)]
    [string]$PaletteImagePath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$resolvedInput = (Resolve-Path -LiteralPath $InputDirectory).Path
$resolvedPalette = (Resolve-Path -LiteralPath $PaletteImagePath).Path
$outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
$sheetPath = [System.IO.Path]::GetFullPath($SpriteSheetPath)
[System.IO.Directory]::CreateDirectory($outputPath) | Out-Null
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($sheetPath)) | Out-Null

$paletteBitmap = [System.Drawing.Bitmap]::FromFile($resolvedPalette)
$palette = [System.Collections.Generic.List[System.Drawing.Color]]::new()
$seen = [System.Collections.Generic.HashSet[string]]::new()
for ($y = 0; $y -lt $paletteBitmap.Height; $y++) {
    for ($x = 0; $x -lt $paletteBitmap.Width; $x++) {
        $color = $paletteBitmap.GetPixel($x, $y)
        if ($color.A -lt 128) { continue }
        $key = '{0},{1},{2}' -f $color.R, $color.G, $color.B
        if ($seen.Add($key)) { $palette.Add($color) }
    }
}
$paletteBitmap.Dispose()
if ($palette.Count -eq 0) { throw 'Palette image contains no opaque colors.' }

function Find-NearestPaletteColor([System.Drawing.Color]$source, $colors) {
    $best = $colors[0]
    $bestDistance = [double]::MaxValue
    foreach ($candidate in $colors) {
        $dr = [double]$source.R - $candidate.R
        $dg = [double]$source.G - $candidate.G
        $db = [double]$source.B - $candidate.B
        $distance = ($dr * $dr) + ($dg * $dg) + ($db * $db)
        if ($distance -lt $bestDistance) {
            $bestDistance = $distance
            $best = $candidate
        }
    }
    return [System.Drawing.Color]::FromArgb(255, $best.R, $best.G, $best.B)
}

$files = @(Get-ChildItem -LiteralPath $resolvedInput -Filter '*.png' | Sort-Object Name)
if ($files.Count -eq 0) { throw 'No PNG frames found.' }

$normalized = [System.Collections.Generic.List[System.Drawing.Bitmap]]::new()
foreach ($file in $files) {
    $source = [System.Drawing.Bitmap]::FromFile($file.FullName)
    $frame = [System.Drawing.Bitmap]::new(64, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($frame)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $graphics.DrawImage($source, 0, 0, 64, 64)
    $graphics.Dispose()
    $source.Dispose()

    for ($y = 0; $y -lt 64; $y++) {
        for ($x = 0; $x -lt 64; $x++) {
            $color = $frame.GetPixel($x, $y)
            if ($color.A -lt 128) {
                $frame.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            }
            else {
                $frame.SetPixel($x, $y, (Find-NearestPaletteColor $color $palette))
            }
        }
    }

    $frameOutput = Join-Path $outputPath $file.Name
    $frame.Save($frameOutput, [System.Drawing.Imaging.ImageFormat]::Png)
    $normalized.Add($frame)
}

$sheet = [System.Drawing.Bitmap]::new(64 * $normalized.Count, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$sheetGraphics = [System.Drawing.Graphics]::FromImage($sheet)
$sheetGraphics.Clear([System.Drawing.Color]::Transparent)
for ($index = 0; $index -lt $normalized.Count; $index++) {
    $sheetGraphics.DrawImageUnscaled($normalized[$index], $index * 64, 0)
}
$sheetGraphics.Dispose()
$sheet.Save($sheetPath, [System.Drawing.Imaging.ImageFormat]::Png)
$sheet.Dispose()
foreach ($frame in $normalized) { $frame.Dispose() }

Write-Output ('FRAME_COUNT={0}' -f $files.Count)
Write-Output ('PALETTE_COUNT={0}' -f $palette.Count)
Write-Output ('SPRITE_SHEET={0}' -f $sheetPath)
