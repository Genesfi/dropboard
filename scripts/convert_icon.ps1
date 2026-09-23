Add-Type -AssemblyName System.Drawing

$pngPath = "f:\Native Win\DropBoard\assets\app_icon.png"
$icoPath = "f:\Native Win\DropBoard\DropBoard.Native\app_icon.ico"
$assetsIcoPath = "f:\Native Win\DropBoard\assets\app_icon.ico"

$bmp = [System.Drawing.Bitmap]::FromFile($pngPath)
$sizes = @(16, 32, 48, 64, 128, 256)

$ms = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($ms)

# ICONDIR
$bw.Write([uint16]0) # Reserved
$bw.Write([uint16]1) # Type 1 = ICO
$bw.Write([uint16]$sizes.Count) # Count

$offset = 6 + ($sizes.Count * 16)
$images = [System.Collections.Generic.List[byte[]]]::new()

foreach ($sz in $sizes) {
    $resized = New-Object System.Drawing.Bitmap $sz, $sz
    $g = [System.Drawing.Graphics]::FromImage($resized)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($bmp, 0, 0, $sz, $sz)
    $g.Dispose()

    $imgMs = New-Object System.IO.MemoryStream
    $resized.Save($imgMs, [System.Drawing.Imaging.ImageFormat]::Png)
    $bytes = $imgMs.ToArray()
    $imgMs.Dispose()
    $resized.Dispose()

    # ICONDIRENTRY
    $bWidth = if ($sz -ge 256) { [byte]0 } else { [byte]$sz }
    $bHeight = if ($sz -ge 256) { [byte]0 } else { [byte]$sz }
    $bw.Write($bWidth)
    $bw.Write($bHeight)
    $bw.Write([byte]0) # Colors
    $bw.Write([byte]0) # Reserved
    $bw.Write([uint16]1) # Color planes
    $bw.Write([uint16]32) # Bits per pixel
    $bw.Write([uint32]$bytes.Length) # SizeInBytes
    $bw.Write([uint32]$offset) # FileOffset

    $offset += $bytes.Length
    $images.Add($bytes)
}

foreach ($imgBytes in $images) {
    $bw.Write($imgBytes)
}

$bmp.Dispose()
$finalBytes = $ms.ToArray()
[System.IO.File]::WriteAllBytes($icoPath, $finalBytes)
[System.IO.File]::WriteAllBytes($assetsIcoPath, $finalBytes)
$bw.Dispose()
$ms.Dispose()

Write-Host "Generated ico successfully: $(Test-Path $icoPath), Size: $($finalBytes.Length) bytes"
