Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$proc = Get-Process -Name "XR18BarControl" -ErrorAction SilentlyContinue
if (-not $proc) { Write-Output "ERROR: proceso no encontrado"; exit 1 }
$hwnd = $proc.MainWindowHandle
$el = [System.Windows.Automation.AutomationElement]::FromHandle($hwnd)
$rect = $el.Current.BoundingRectangle

$x = [int]$rect.X
$y = [int]$rect.Y
$w = [int]$rect.Width
$h = [int]$rect.Height

Write-Output "Rect: x=$x y=$y w=$w h=$h"

$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($x, $y, 0, 0, (New-Object System.Drawing.Size $w, $h))
$outPath = "C:\Users\gburgosh\Downloads\hoanna-beach-main\hoanna-beach-main\screenshot.png"
$bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
Write-Output "Guardado en $outPath"
