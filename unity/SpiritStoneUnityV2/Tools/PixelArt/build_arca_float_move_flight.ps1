param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath
)

Add-Type -AssemblyName System.Drawing

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$outputRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/FloatMove"
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path $SourcePath).Path)
try {
    $frameCount = 6
    $cellWidth = [int]($source.Width / $frameCount)

    for ($frameIndex = 0; $frameIndex -lt $frameCount; $frameIndex++) {
        $cellLeft = $frameIndex * $cellWidth
        $cellRight = if ($frameIndex -eq $frameCount - 1) { $source.Width } else { $cellLeft + $cellWidth }
        $minX = $cellRight
        $minY = $source.Height
        $maxX = -1
        $maxY = -1

        for ($y = 0; $y -lt $source.Height; $y++) {
            for ($x = $cellLeft; $x -lt $cellRight; $x++) {
                $pixel = $source.GetPixel($x, $y)
                $isGreen = $pixel.G -gt 135 -and $pixel.G -gt ($pixel.R * 1.35) -and $pixel.G -gt ($pixel.B * 1.35)
                if ($isGreen) { continue }
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }

        if ($maxX -lt $minX -or $maxY -lt $minY) {
            throw "No character pixels found in frame $($frameIndex + 1)."
        }

        $sourceWidth = $maxX - $minX + 1
        $sourceHeight = $maxY - $minY + 1
        $scale = [Math]::Min(58.0 / $sourceWidth, 52.0 / $sourceHeight)
        $targetWidth = [Math]::Max(1, [int][Math]::Round($sourceWidth * $scale))
        $targetHeight = [Math]::Max(1, [int][Math]::Round($sourceHeight * $scale))
        $targetX = [int][Math]::Round((64 - $targetWidth) / 2.0)
        $targetY = [int][Math]::Round((64 - $targetHeight) / 2.0)

        $clean = [System.Drawing.Bitmap]::new($sourceWidth, $sourceHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            for ($y = 0; $y -lt $sourceHeight; $y++) {
                for ($x = 0; $x -lt $sourceWidth; $x++) {
                    $pixel = $source.GetPixel($minX + $x, $minY + $y)
                    $isGreen = $pixel.G -gt 135 -and $pixel.G -gt ($pixel.R * 1.35) -and $pixel.G -gt ($pixel.B * 1.35)
                    $clean.SetPixel($x, $y, $(if ($isGreen) { [System.Drawing.Color]::Transparent } else { $pixel }))
                }
            }

            $frame = [System.Drawing.Bitmap]::new(64, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try {
                $graphics = [System.Drawing.Graphics]::FromImage($frame)
                try {
                    $graphics.Clear([System.Drawing.Color]::Transparent)
                    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                    $graphics.DrawImage($clean, [System.Drawing.Rectangle]::new($targetX, $targetY, $targetWidth, $targetHeight), 0, 0, $sourceWidth, $sourceHeight, [System.Drawing.GraphicsUnit]::Pixel)
                }
                finally { $graphics.Dispose() }

                $name = "character_arca_float_move_{0:00}.png" -f ($frameIndex + 1)
                $frame.Save((Join-Path $outputRoot $name), [System.Drawing.Imaging.ImageFormat]::Png)
            }
            finally { $frame.Dispose() }
        }
        finally { $clean.Dispose() }
    }
}
finally { $source.Dispose() }

Write-Output "Built six 64x64 Arca flight frames in $outputRoot"
