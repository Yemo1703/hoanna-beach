Add-Type -AssemblyName System.Drawing

$srcPath = "C:\Users\gburgosh\AppData\Roaming\Code\agentSessionData\e2d076c6-296b-47e1-b45d-704553c78cd2\attachments\57aa6ea8-0068-4903-80b0-121b93ab31d2\Pasted Image 2.png"
$src = [System.Drawing.Bitmap]::FromFile($srcPath)
$corner = $src.GetPixel(0,0)
Write-Output ("Corner pixel: A={0} R={1} G={2} B={3}  Size={4}x{5}" -f $corner.A, $corner.R, $corner.G, $corner.B, $src.Width, $src.Height)

$outDir = "C:\Users\gburgosh\Downloads\hoanna-beach-main\hoanna-beach-main\src\XR18BarControl\Assets"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$icoPath = Join-Path $outDir "beer.ico"

$sizes = @(16,32,48,64,128,256)
$pngBuffers = @()

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.DrawImage($src, 0, 0, $size, $size)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBuffers += ,$ms.ToArray()
    $bmp.Dispose()
}
$src.Dispose()

$fs = [System.IO.File]::Open($icoPath, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter($fs)

$bw.Write([UInt16]0)      # reserved
$bw.Write([UInt16]1)      # type = icon
$bw.Write([UInt16]$sizes.Count)

$headerSize = 6 + (16 * $sizes.Count)
$offset = $headerSize
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $b = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$b)          # width
    $bw.Write([byte]$b)          # height
    $bw.Write([byte]0)           # color count
    $bw.Write([byte]0)           # reserved
    $bw.Write([UInt16]1)         # planes
    $bw.Write([UInt16]32)        # bit count
    $bw.Write([UInt32]$pngBuffers[$i].Length)
    $bw.Write([UInt32]$offset)
    $offset += $pngBuffers[$i].Length
}
foreach ($buf in $pngBuffers) { $bw.Write($buf) }

$bw.Flush()
$bw.Close()
$fs.Close()

Write-Output "ICO guardado en $icoPath"
