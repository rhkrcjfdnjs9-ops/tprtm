param(
    [Parameter(Mandatory = $true)] [string]$FloatMoveSource,
    [Parameter(Mandatory = $true)] [string]$AttackSource,
    [Parameter(Mandatory = $true)] [string]$HitSource,
    [Parameter(Mandatory = $true)] [string]$DeathSource
)

Add-Type -AssemblyName System.Drawing

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$arcaRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Production"
$masterRoot = Join-Path $arcaRoot "Masters"
$sourceRoot = Join-Path $masterRoot "GeneratedSources"
New-Item -ItemType Directory -Force -Path $masterRoot, $sourceRoot | Out-Null

$idleSource = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/character_arca_idle_01_v3.png"
Copy-Item -LiteralPath $idleSource -Destination (Join-Path $arcaRoot "Arca_Master.png") -Force
Copy-Item -LiteralPath $idleSource -Destination (Join-Path $masterRoot "Arca_Idle_Master.png") -Force

function Convert-ToMaster {
    param([string]$SourcePath, [string]$OutputName)

    $resolvedSource = (Resolve-Path $SourcePath).Path
    Copy-Item -LiteralPath $resolvedSource -Destination (Join-Path $sourceRoot ($OutputName -replace '\.png$', '_Source.png')) -Force
    $source = [System.Drawing.Bitmap]::FromFile($resolvedSource)
    try {
        $minX = $source.Width
        $minY = $source.Height
        $maxX = -1
        $maxY = -1
        for ($y = 0; $y -lt $source.Height; $y++) {
            for ($x = 0; $x -lt $source.Width; $x++) {
                $pixel = $source.GetPixel($x, $y)
                $isGreen = $pixel.G -gt 125 -and $pixel.G -gt ($pixel.R * 1.3) -and $pixel.G -gt ($pixel.B * 1.3)
                $isVisible = $pixel.A -gt 24 -and -not $isGreen
                if (-not $isVisible) { continue }
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
        if ($maxX -lt $minX -or $maxY -lt $minY) { throw "No visible character found in $SourcePath" }

        $sourceWidth = $maxX - $minX + 1
        $sourceHeight = $maxY - $minY + 1
        $scale = [Math]::Min(58.0 / $sourceWidth, 56.0 / $sourceHeight)
        $targetWidth = [Math]::Max(1, [int][Math]::Round($sourceWidth * $scale))
        $targetHeight = [Math]::Max(1, [int][Math]::Round($sourceHeight * $scale))
        $targetX = [int][Math]::Round((64 - $targetWidth) / 2.0)
        $targetY = [int][Math]::Round((64 - $targetHeight) / 2.0)

        $clean = [System.Drawing.Bitmap]::new($sourceWidth, $sourceHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            for ($y = 0; $y -lt $sourceHeight; $y++) {
                for ($x = 0; $x -lt $sourceWidth; $x++) {
                    $pixel = $source.GetPixel($minX + $x, $minY + $y)
                    $isGreen = $pixel.G -gt 125 -and $pixel.G -gt ($pixel.R * 1.3) -and $pixel.G -gt ($pixel.B * 1.3)
                    $clean.SetPixel($x, $y, $(if ($isGreen -or $pixel.A -le 24) { [System.Drawing.Color]::Transparent } else { $pixel }))
                }
            }

            $output = [System.Drawing.Bitmap]::new(64, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try {
                $graphics = [System.Drawing.Graphics]::FromImage($output)
                try {
                    $graphics.Clear([System.Drawing.Color]::Transparent)
                    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                    $graphics.DrawImage($clean, [System.Drawing.Rectangle]::new($targetX, $targetY, $targetWidth, $targetHeight), 0, 0, $sourceWidth, $sourceHeight, [System.Drawing.GraphicsUnit]::Pixel)
                }
                finally { $graphics.Dispose() }
                $output.Save((Join-Path $masterRoot $OutputName), [System.Drawing.Imaging.ImageFormat]::Png)
            }
            finally { $output.Dispose() }
        }
        finally { $clean.Dispose() }
    }
    finally { $source.Dispose() }
}

Convert-ToMaster $FloatMoveSource "Arca_FloatMove_Master.png"
Convert-ToMaster $AttackSource "Arca_Attack_Master.png"
Convert-ToMaster $HitSource "Arca_Hit_Master.png"
Convert-ToMaster $DeathSource "Arca_Death_Master.png"

Write-Output "Created Arca_Master.png and five motion masters under $arcaRoot"
