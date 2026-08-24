$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$rows = @()
foreach ($stage in 5, 6) {
  $dir = ".\assets\frames\stage_${stage}_right_hand"
  foreach ($action in 'idle', 'walk', 'attack', 'hit', 'death') {
    foreach ($file in (Get-ChildItem "$dir\grania_${action}_*.png" | Sort-Object Name)) {
      $bitmap = [System.Drawing.Bitmap]::new($file.FullName)
      try {
        $left = $bitmap.Width
        $top = $bitmap.Height
        $right = -1
        $bottom = -1
        for ($y = 0; $y -lt $bitmap.Height; $y++) {
          for ($x = 0; $x -lt $bitmap.Width; $x++) {
            if ($bitmap.GetPixel($x, $y).A -gt 18) {
              $left = [Math]::Min($left, $x)
              $top = [Math]::Min($top, $y)
              $right = [Math]::Max($right, $x)
              $bottom = [Math]::Max($bottom, $y)
            }
          }
        }
        $rows += [pscustomobject]@{
          Stage = $stage
          Action = $action
          Frame = $file.BaseName
          Left = $left
          Top = $top
          Right = $right
          Bottom = $bottom
          Width = $right - $left + 1
          Height = $bottom - $top + 1
          TouchesEdge = $left -le 1 -or $top -le 1 -or $right -ge 254 -or $bottom -ge 254
        }
      } finally {
        $bitmap.Dispose()
      }
    }
  }
}
$rows | Format-Table -AutoSize
