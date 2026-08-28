[CmdletBinding()]
param()

$projectRoot = Split-Path -Parent $PSScriptRoot
$assetsRoot = Join-Path $projectRoot 'Assets'

if (-not (Get-Command rg -ErrorAction SilentlyContinue)) {
    throw 'ripgrep (rg) is required.'
}

& rg --files $assetsRoot -g '*.cs' -g '*.asmdef' -g '*.asmref'

