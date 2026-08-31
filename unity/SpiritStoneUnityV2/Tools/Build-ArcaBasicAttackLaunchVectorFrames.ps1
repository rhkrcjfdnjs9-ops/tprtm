param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$inkscape = "C:\Program Files\Inkscape\bin\inkscape.exe"
$ffmpeg = "D:\Counter-Strike Online\Bin\FFmpeg.exe"
$sourceRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Masters/Effects/BasicAttack/LaunchFrames"
$runtimeRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/Effects/BasicAttackV3/LaunchFlashV3"
$previewRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Previews/Effects/BasicAttackLaunchVectorV1"
foreach ($path in @($sourceRoot, $runtimeRoot, $previewRoot)) { New-Item -ItemType Directory -Path $path -Force | Out-Null }

$header = '<?xml version="1.0" encoding="UTF-8"?><svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64" shape-rendering="crispEdges">'
$footer = '</svg>'
$bodies = @(
    '<g fill="#32084f"><path d="M29 29h6v6h-6z"/></g><g fill="#7619c2"><path d="M30 30h4v4h-4z"/></g><g fill="#ffffff"><path d="M31 31h2v2h-2z"/></g>',
    '<g fill="#32084f"><path d="M27 26h10v3h4v7h-4v3H27v-3h-4v-7h4z"/></g><g fill="#7619c2"><path d="M29 27h6v3h4v4h-4v3h-6v-3h-4v-4h4z"/></g><g fill="#d27aff"><path d="M29 29h6v6h-6z"/></g><g fill="#ffffff"><path d="M31 30h3v4h-3z"/></g>',
    '<g fill="#32084f"><path d="M29 20h6v7h5v3h9v5h-9v3h-5v7h-6v-7h-5v-3h-9v-5h9v-3h5zM20 23h3v5h-3zm23 15h3v4h-3zM18 39h5v3h-5z"/></g><g fill="#7619c2"><path d="M30 23h4v6h5v2h7v3h-7v2h-5v6h-4v-6h-5v-2h-7v-3h7v-2h5z"/></g><g fill="#d27aff"><path d="M30 27h4v4h8v3h-8v4h-4v-4h-7v-3h7z"/></g><g fill="#ffffff"><path d="M30 29h4v2h5v3h-5v3h-4v-3h-5v-3h5z"/></g>',
    '<g fill="#32084f"><path d="M29 27h6v3h14v5H35v3h-6zM21 25h4v3h-4zm2 13h4v3h-4z"/></g><g fill="#7619c2"><path d="M30 29h5v2h11v3H35v2h-5z"/></g><g fill="#d27aff"><path d="M31 30h5v1h8v2h-8v2h-5z"/></g><g fill="#ffffff"><path d="M32 31h10v2H32z"/></g>',
    '<g fill="#32084f"><path d="M35 30h8v4h-8zM27 25h3v3h-3zm-5 12h4v3h-4z"/></g><g fill="#7619c2"><path d="M36 31h6v2h-6zM28 26h2v2h-2z"/></g><g fill="#d27aff"><path d="M23 37h2v2h-2z"/></g><g fill="#ffffff"><path d="M39 31h2v2h-2z"/></g>'
)

$warmupSvg = Join-Path $sourceRoot "Inkscape_Warmup.svg"
$warmupPng = Join-Path $sourceRoot "Inkscape_Warmup.png"
[System.IO.File]::WriteAllText($warmupSvg, "$header$($bodies[0])$footer", [System.Text.UTF8Encoding]::new($false))
if (Test-Path -LiteralPath $warmupPng) { Remove-Item -LiteralPath $warmupPng -Force }
& $inkscape '--export-area-page' '--export-type=png' "--export-filename=$warmupPng" '--export-width=64' '--export-height=64' '--export-background-opacity=0' $warmupSvg

for ($index = 0; $index -lt $bodies.Count; $index++) {
    $svgPath = Join-Path $sourceRoot ("Arca_BasicAttack_Launch_{0:D2}.svg" -f $index)
    $pngPath = Join-Path $runtimeRoot ("LaunchFlashV3_{0:D2}.png" -f $index)
    [System.IO.File]::WriteAllText($svgPath, "$header$($bodies[$index])$footer", [System.Text.UTF8Encoding]::new($false))
    if (Test-Path -LiteralPath $pngPath) { Remove-Item -LiteralPath $pngPath -Force }
    & $inkscape '--export-area-page' '--export-type=png' "--export-filename=$pngPath" '--export-width=64' '--export-height=64' '--export-background-opacity=0' $svgPath
    Start-Sleep -Milliseconds 2500
    if (-not (Test-Path -LiteralPath $pngPath)) {
        & $inkscape '--export-area-page' '--export-type=png' "--export-filename=$pngPath" '--export-width=64' '--export-height=64' '--export-background-opacity=0' $svgPath
        Start-Sleep -Milliseconds 2500
    }
    if (-not (Test-Path -LiteralPath $pngPath)) { throw "Failed to export $pngPath" }
}

$pattern = Join-Path $runtimeRoot "LaunchFlashV3_%02d.png"
& $ffmpeg -y -v error -framerate 12 -i $pattern -filter_complex "[0:v]scale=512:512:flags=neighbor,split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse" -loop 0 (Join-Path $previewRoot "Arca_BasicAttack_Launch_VectorV1_Preview_8x.gif")
& $ffmpeg -y -v error -framerate 1 -i $pattern -frames:v 5 -vf "scale=256:256:flags=neighbor,tile=5x1" (Join-Path $previewRoot "Arca_BasicAttack_Launch_VectorV1_ContactSheet.png")
Write-Output $runtimeRoot
