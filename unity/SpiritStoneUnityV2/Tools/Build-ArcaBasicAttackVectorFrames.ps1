param()

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$inkscape = "C:\Program Files\Inkscape\bin\inkscape.exe"
$ffmpeg = "D:\Counter-Strike Online\Bin\FFmpeg.exe"
$master = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Masters/Effects/BasicAttack/Arca_BasicAttack_Projectile_Master.svg"
$sourceRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Masters/Effects/BasicAttack/Frames"
$runtimeRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Resources/Characters/Arca/Effects/BasicAttackV3/ProjectileV3"
$previewRoot = Join-Path $projectRoot "Assets/Characters/Arca/Pixel64/Previews/Effects/BasicAttackVectorV1"

foreach ($path in @($sourceRoot, $runtimeRoot, $previewRoot)) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

$masterText = Get-Content -LiteralPath $master -Raw
$frameOverlays = @(
    '<g transform="translate(16 16) scale(0.5)"><g id="motion"><path fill="#d27aff" d="M13 28h4v2h-4zM16 36h4v2h-4z"/><path fill="#ffffff" d="M48 32h2v2h-2z"/></g></g>',
    '<g transform="translate(16 16) scale(0.5)"><g id="motion"><path fill="#7619c2" d="M11 30h5v2h-5zM15 25h4v2h-4z"/><path fill="#d27aff" d="M14 36h4v2h-4z"/><path fill="#ffffff" d="M31 30h3v2h-3z"/></g></g>',
    '<g transform="translate(16 16) scale(0.5)"><g id="motion"><path fill="#d27aff" d="M12 27h5v2h-5zM10 35h6v2h-6z"/><path fill="#ffffff" d="M29 30h6v4h-6zM47 31h3v3h-3z"/></g></g>',
    '<g transform="translate(16 16) scale(0.5)"><g id="motion"><path fill="#7619c2" d="M12 29h4v2h-4zM14 38h5v2h-5z"/><path fill="#d27aff" d="M16 25h5v2h-5z"/><path fill="#ffffff" d="M32 32h4v2h-4z"/></g></g>',
    '<g transform="translate(16 16) scale(0.5)"><g id="motion"><path fill="#d27aff" d="M14 26h4v2h-4zM12 34h5v2h-5z"/><path fill="#ffffff" d="M33 31h4v3h-4z"/></g></g>',
    '<g transform="translate(16 16) scale(0.5)"><g id="motion"><path fill="#7619c2" d="M13 28h4v2h-4zM15 36h4v2h-4z"/><path fill="#d27aff" d="M18 25h3v2h-3z"/><path fill="#ffffff" d="M48 32h2v2h-2z"/></g></g>'
)

for ($index = 0; $index -lt $frameOverlays.Count; $index++) {
    $frameSvg = $masterText.Replace('</svg>', "$($frameOverlays[$index])`r`n</svg>")
    $svgPath = Join-Path $sourceRoot ("Arca_BasicAttack_Projectile_{0:D2}.svg" -f $index)
    $pngPath = Join-Path $runtimeRoot ("ProjectileV3_{0:D2}.png" -f $index)
    [System.IO.File]::WriteAllText($svgPath, $frameSvg, [System.Text.UTF8Encoding]::new($false))
    if (Test-Path -LiteralPath $pngPath) { Remove-Item -LiteralPath $pngPath -Force }
    for ($attempt = 1; $attempt -le 3 -and -not (Test-Path -LiteralPath $pngPath); $attempt++) {
        & $inkscape '--export-overwrite' '--export-area-page' '--export-type=png' "--export-filename=$pngPath" '--export-width=64' '--export-height=64' '--export-background-opacity=0' $svgPath
        if (-not (Test-Path -LiteralPath $pngPath)) { Start-Sleep -Milliseconds 2500 }
    }
    if (-not (Test-Path -LiteralPath $pngPath)) { throw "Failed to export $pngPath" }
    $cleanPath = Join-Path $runtimeRoot ("ProjectileV3_{0:D2}_Clean.png" -f $index)
    & $ffmpeg -y -v error -i $pngPath -vf "format=rgba,lut=a='if(gte(val,128),255,0)'" $cleanPath
    Move-Item -LiteralPath $cleanPath -Destination $pngPath -Force
}

$previewPattern = Join-Path $runtimeRoot "ProjectileV3_%02d.png"
$previewGif = Join-Path $previewRoot "Arca_BasicAttack_VectorV1_Preview_8x.gif"
$contactSheet = Join-Path $previewRoot "Arca_BasicAttack_VectorV1_ContactSheet.png"
& $ffmpeg -y -v error -framerate 12 -i $previewPattern -filter_complex "[0:v]scale=512:512:flags=neighbor,split[a][b];[a]palettegen=reserve_transparent=1[p];[b][p]paletteuse" -loop 0 $previewGif
& $ffmpeg -y -v error -framerate 1 -i $previewPattern -frames:v 6 -vf "scale=256:256:flags=neighbor,tile=6x1" $contactSheet

Write-Output $runtimeRoot
Write-Output $previewGif
