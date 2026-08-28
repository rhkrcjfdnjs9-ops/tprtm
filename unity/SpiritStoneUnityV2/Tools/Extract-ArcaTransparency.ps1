param(
    [string]$InputPath = "D:\UnityProjects\SpiritStoneUnityV2\Assets\Characters\Arca\References\Arca_Master_ApprovedDraft.png",
    [string]$OutputPath = "D:\UnityProjects\SpiritStoneUnityV2\Assets\Characters\Arca\Source\Arca_Master_Transparent.png"
)

Add-Type -AssemblyName System.Drawing

$source = [System.Drawing.Bitmap]::new($InputPath)
$output = [System.Drawing.Bitmap]::new($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$width = $source.Width
$height = $source.Height
$candidate = [bool[]]::new($width * $height)
$background = [bool[]]::new($width * $height)
$queue = [System.Collections.Generic.Queue[int]]::new()

for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $color = $source.GetPixel($x, $y)
        $max = [Math]::Max($color.R, [Math]::Max($color.G, $color.B))
        $min = [Math]::Min($color.R, [Math]::Min($color.G, $color.B))
        $spread = $max - $min
        $candidate[$y * $width + $x] = $min -ge 220 -and $spread -le 12
    }
}

for ($x = 0; $x -lt $width; $x++) {
    foreach ($index in @($x, (($height - 1) * $width + $x))) {
        if ($candidate[$index] -and -not $background[$index]) { $background[$index] = $true; $queue.Enqueue($index) }
    }
}
for ($y = 0; $y -lt $height; $y++) {
    foreach ($index in @(($y * $width), ($y * $width + $width - 1))) {
        if ($candidate[$index] -and -not $background[$index]) { $background[$index] = $true; $queue.Enqueue($index) }
    }
}

while ($queue.Count -gt 0) {
    $index = $queue.Dequeue()
    $x = $index % $width
    $y = [int][Math]::Floor($index / $width)
    if ($x -gt 0) { $next = $index - 1; if ($candidate[$next] -and -not $background[$next]) { $background[$next] = $true; $queue.Enqueue($next) } }
    if ($x -lt $width - 1) { $next = $index + 1; if ($candidate[$next] -and -not $background[$next]) { $background[$next] = $true; $queue.Enqueue($next) } }
    if ($y -gt 0) { $next = $index - $width; if ($candidate[$next] -and -not $background[$next]) { $background[$next] = $true; $queue.Enqueue($next) } }
    if ($y -lt $height - 1) { $next = $index + $width; if ($candidate[$next] -and -not $background[$next]) { $background[$next] = $true; $queue.Enqueue($next) } }
}

for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $color = $source.GetPixel($x, $y)
        $alpha = if ($background[$y * $width + $x]) { 0 } else { 255 }
        $output.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, $color.R, $color.G, $color.B))
    }
}

$destinationFolder = Split-Path -Parent $OutputPath
[System.IO.Directory]::CreateDirectory($destinationFolder) | Out-Null
$output.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$source.Dispose()
$output.Dispose()
