param()

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$inkscape = "C:\Program Files\Inkscape\bin\inkscape.exe"
$ffmpeg = "D:\Counter-Strike Online\Bin\FFmpeg.exe"
$sourceRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Masters/Effects/BasicAttack/ImpactFrames"
$runtimeRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/Effects/BasicAttackV3/ImpactV3"
$previewRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Previews/Effects/BasicAttackImpactVectorV1"
foreach ($path in @($sourceRoot, $runtimeRoot, $previewRoot)) { New-Item -ItemType Directory -Path $path -Force | Out-Null }

$header = '<?xml version="1.0" encoding="UTF-8"?><svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64" shape-rendering="crispEdges">'
$footer = '</svg>'
$bodies = @(
    '<g fill="#32084f"><path d="M28 29h8v6h-8zM25 31h3v2h-3z"/></g><g fill="#7619c2"><path d="M29 30h6v4h-6z"/></g><g fill="#ffffff"><path d="M31 31h3v2h-3z"/></g>',
    '<g fill="#32084f"><path d="M29 24h6v5h5v6h-5v5h-6v-5h-5v-6h5z"/></g><g fill="#7619c2"><path d="M30 26h4v5h4v3h-4v4h-4v-4h-4v-3h4z"/></g><g fill="#d27aff"><path d="M30 29h4v6h-4zM28 31h8v2h-8z"/></g><g fill="#ffffff"><path d="M31 30h2v4h-2z"/></g>',
    '<g fill="#32084f"><path d="M29 19h6v8h4v2h7v6h-7v3h-4v8h-6v-8h-4v-3h-7v-6h7v-2h4zM22 22h4v4h-4zm16 16h4v4h-4zM21 39h4v4h-4zm18-18h4v4h-4z"/></g><g fill="#7619c2"><path d="M30 22h4v7h4v2h6v3h-6v2h-4v7h-4v-7h-4v-2h-6v-3h6v-2h4z"/></g><g fill="#d27aff"><path d="M30 26h4v5h6v3h-6v5h-4v-5h-6v-3h6z"/></g><g fill="#ffffff"><path d="M30 29h4v2h4v3h-4v3h-4v-3h-4v-3h4z"/></g>',
    '<g fill="#32084f"><path d="M20 23h4v4h-4zm20 2h5v4h-5zM19 38h5v4h-5zm19 2h4v4h-4zM28 28h8v8h-8z"/></g><g fill="#7619c2"><path d="M21 24h2v2h-2zm20 2h3v2h-3zM20 39h3v2h-3zm19 2h2v2h-2zM29 29h6v6h-6z"/></g><g fill="#d27aff"><path d="M30 30h4v4h-4z"/></g><g fill="#ffffff"><path d="M31 31h2v2h-2z"/></g>',
    '<g fill="#32084f"><path d="M25 27h3v3h-3zm12 1h3v3h-3zM23 37h3v3h-3zm13 2h3v3h-3zM30 31h4v4h-4z"/></g><g fill="#7619c2"><path d="M26 28h2v2h-2zm12 1h2v2h-2zM24 38h2v2h-2zm13 2h2v2h-2z"/></g><g fill="#d27aff"><path d="M31 32h2v2h-2z"/></g>'
)

for ($index = 0; $index -lt $bodies.Count; $index++) {
    $svgPath = Join-Path $sourceRoot ("Arca_BasicAttack_Impact_{0:D2}.svg" -f $index)
    $pngPath = Join-Path $runtimeRoot ("ImpactV3_{0:D2}.png" -f $index)
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

$pattern = Join-Path $runtimeRoot "ImpactV3_%02d.png"
& $ffmpeg -y -v error -framerate 12 -i $pattern -filter_complex "[0:v]scale=512:512:flags=neighbor,split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse" -loop 0 (Join-Path $previewRoot "Arca_BasicAttack_Impact_VectorV1_Preview_8x.gif")
& $ffmpeg -y -v error -framerate 1 -i $pattern -frames:v 5 -vf "scale=256:256:flags=neighbor,tile=5x1" (Join-Path $previewRoot "Arca_BasicAttack_Impact_VectorV1_ContactSheet.png")
Write-Output $runtimeRoot
