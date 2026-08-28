[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$projectName = Split-Path -Leaf $projectRoot

Write-Host "[UnityVerify] Project: $projectName"

$forbiddenPhysics = & rg -n --glob '*.cs' '\b(Rigidbody|Collider|Physics)\b' (Join-Path $projectRoot 'Assets') 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Error "[UnityVerify] 3D physics API reference detected:`n$forbiddenPhysics"
}

$perFrameLookups = & rg -n --glob '*.cs' '(Update|LateUpdate|FixedUpdate)\s*\([^)]*\)[\s\S]{0,500}(GetComponent|GameObject\.Find|Camera\.main)' (Join-Path $projectRoot 'Assets') 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Warning "[UnityVerify] Review possible per-frame component lookup:`n$perFrameLookups"
}

$projectFiles = Get-ChildItem -LiteralPath $projectRoot -Filter '*.csproj' -File
if ($projectFiles.Count -eq 0) {
    Write-Warning '[UnityVerify] No generated .csproj found. Open Unity Preferences > External Tools and regenerate project files.'
} elseif (Get-Command dotnet -ErrorAction SilentlyContinue) {
    foreach ($projectFile in $projectFiles) {
        Write-Host "[UnityVerify] dotnet build $($projectFile.Name)"
        & dotnet build $projectFile.FullName --nologo --no-restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed: $($projectFile.Name)" }
    }
} else {
    Write-Warning '[UnityVerify] dotnet was not found; skipping fast project-file compilation.'
}

Write-Host '[UnityVerify] Static checks passed. Unity Console compilation is still required.'

