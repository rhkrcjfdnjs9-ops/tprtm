param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sourcePath = Join-Path $ProjectRoot 'Assets\Characters\Arca\Pixel64\Source\Effects\Arca_ChainLightning_ImageGen_Source_v1.png'
$outputPath = Join-Path $ProjectRoot 'Assets\Characters\Arca\Pixel64\Resources\Characters\Arca\Effects\ChainLightningV1'
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$source = [System.Drawing.Bitmap]::FromFile($sourcePath)
try {
    for ($frame = 0; $frame -lt 8; $frame++) {
        $cellLeft = [int][Math]::Round($frame * $source.Width / 8.0)
        $cellRight = [int][Math]::Round(($frame + 1) * $source.Width / 8.0) - 1
        $minX = $cellRight
        $minY = $source.Height - 1
        $maxX = $cellLeft
        $maxY = 0
        $hasPixel = $false

        for ($y = 0; $y -lt $source.Height; $y++) {
            for ($x = $cellLeft; $x -le $cellRight; $x++) {
                if ($source.GetPixel($x, $y).A -le 12) { continue }
                $hasPixel = $true
                $minX = [Math]::Min($minX, $x)
                $minY = [Math]::Min($minY, $y)
                $maxX = [Math]::Max($maxX, $x)
                $maxY = [Math]::Max($maxY, $y)
            }
        }

        if (-not $hasPixel) { throw "Frame $frame contains no visible pixels." }
        $cropWidth = $maxX - $minX + 1
        $cropHeight = $maxY - $minY + 1
        $scale = [Math]::Min(56.0 / $cropWidth, 56.0 / $cropHeight)
        $drawWidth = [Math]::Max(1, [int][Math]::Round($cropWidth * $scale))
        $drawHeight = [Math]::Max(1, [int][Math]::Round($cropHeight * $scale))
        $drawX = [int][Math]::Floor((64 - $drawWidth) / 2.0)
        $drawY = [int][Math]::Floor((64 - $drawHeight) / 2.0)

        $frameBitmap = New-Object System.Drawing.Bitmap 64, 64, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($frameBitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                $destination = New-Object System.Drawing.Rectangle $drawX, $drawY, $drawWidth, $drawHeight
                $sourceRect = New-Object System.Drawing.Rectangle $minX, $minY, $cropWidth, $cropHeight
                $graphics.DrawImage($source, $destination, $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $graphics.Dispose()
            }
            $filename = 'Arca_ChainLightning_V1_{0:D2}.png' -f $frame
            $frameBitmap.Save((Join-Path $outputPath $filename), [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $frameBitmap.Dispose()
        }
    }
}
finally {
    $source.Dispose()
}

Write-Output "[ArcaChainLightning] Built 8 normalized 64x64 frames at $outputPath"
