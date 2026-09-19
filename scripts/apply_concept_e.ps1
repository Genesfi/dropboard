Add-Type -AssemblyName System.Drawing

function Create-ConceptEBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    $scale = [float]$size / 256.0

    # Colors: Dark Slate Background, Electric Cyan-Blue, Crisp White
    $colBg = [System.Drawing.Color]::FromArgb(255, 12, 16, 25)
    $colBorder = [System.Drawing.Color]::FromArgb(65, 56, 189, 248)
    $colBlue = [System.Drawing.Color]::FromArgb(255, 56, 189, 248)
    $colWhite = [System.Drawing.Color]::FromArgb(255, 255, 255, 255)

    # 1. Background Squircle
    $pad = [float](12.0 * $scale)
    $sqSize = [float](232.0 * $scale)
    $d = [float](104.0 * $scale)

    $pathBg = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pathBg.AddArc($pad, $pad, $d, $d, 180, 90)
    $pathBg.AddArc(($pad + $sqSize - $d), $pad, $d, $d, 270, 90)
    $pathBg.AddArc(($pad + $sqSize - $d), ($pad + $sqSize - $d), $d, $d, 0, 90)
    $pathBg.AddArc($pad, ($pad + $sqSize - $d), $d, $d, 90, 90)
    $pathBg.CloseFigure()

    $brushBg = New-Object System.Drawing.SolidBrush $colBg
    $g.FillPath($brushBg, $pathBg)

    $penRim = New-Object System.Drawing.Pen ($colBorder, [float](2.0 * $scale))
    $g.DrawPath($penRim, $pathBg)

    $bBlue = New-Object System.Drawing.SolidBrush $colBlue
    $bWhite = New-Object System.Drawing.SolidBrush $colWhite

    function Fill-RoundRect($graph, $brush, [float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
        $p = New-Object System.Drawing.Drawing2D.GraphicsPath
        $rad = $r * 2.0
        $p.AddArc($x, $y, $rad, $rad, 180, 90)
        $p.AddArc(($x + $w - $rad), $y, $rad, $rad, 270, 90)
        $p.AddArc(($x + $w - $rad), ($y + $h - $rad), $rad, $rad, 0, 90)
        $p.AddArc($x, ($y + $h - $rad), $rad, $rad, 90, 90)
        $p.CloseFigure()
        $graph.FillPath($brush, $p)
        $p.Dispose()
    }

    # Papan 1 (Papan Tegak Kiri): Cyan-Blue
    Fill-RoundRect $g $bBlue (46.0 * $scale) (46.0 * $scale) (48.0 * $scale) (164.0 * $scale) (14.0 * $scale)

    # Papan 2 (Papan Atas Lengkung Luar D): Putih
    $p2 = New-Object System.Drawing.Drawing2D.GraphicsPath
    $x2 = [float](104.0 * $scale)
    $y2 = [float](46.0 * $scale)
    $w2 = [float](106.0 * $scale)
    $h2 = [float](77.0 * $scale)
    $r2 = [float](14.0 * $scale)
    $rBig2 = [float](72.0 * $scale)

    $p2.AddArc($x2, $y2, ($r2*2), ($r2*2), 180, 90)
    $p2.AddArc(($x2 + $w2 - $rBig2*2), $y2, ($rBig2*2), ($rBig2*2), 270, 90)
    $p2.AddLine(($x2 + $w2), ($y2 + $rBig2), ($x2 + $w2), ($y2 + $h2))
    $p2.AddLine(($x2 + $w2), ($y2 + $h2), $x2, ($y2 + $h2))
    $p2.AddLine($x2, ($y2 + $h2), $x2, ($y2 + $r2))
    $p2.CloseFigure()
    $g.FillPath($bWhite, $p2)
    $p2.Dispose()

    # Papan 3 (Papan Bawah Lengkung Luar D): Cyan-Blue
    $p3 = New-Object System.Drawing.Drawing2D.GraphicsPath
    $x3 = [float](104.0 * $scale)
    $y3 = [float](133.0 * $scale)
    $w3 = [float](106.0 * $scale)
    $h3 = [float](77.0 * $scale)
    $r3 = [float](14.0 * $scale)
    $rBig3 = [float](72.0 * $scale)

    $p3.AddLine($x3, $y3, ($x3 + $w3), $y3)
    $p3.AddLine(($x3 + $w3), $y3, ($x3 + $w3), ($y3 + $h3 - $rBig3))
    $p3.AddArc(($x3 + $w3 - $rBig3*2), ($y3 + $h3 - $rBig3*2), ($rBig3*2), ($rBig3*2), 0, 90)
    $p3.AddArc($x3, ($y3 + $h3 - $r3*2), ($r3*2), ($r3*2), 90, 90)
    $p3.AddLine($x3, ($y3 + $h3 - $r3), $x3, $y3)
    $p3.CloseFigure()
    $g.FillPath($bBlue, $p3)
    $p3.Dispose()

    $brushBg.Dispose()
    $penRim.Dispose()
    $bBlue.Dispose()
    $bWhite.Dispose()
    $g.Dispose()

    return $bmp
}

# 1. Save 256x256 app_icon.png
$bmp256 = Create-ConceptEBitmap 256
$pngPath = "f:\Native Win\DropBoard\assets\app_icon.png"
$bmp256.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# 2. Save src/app.ico
$icoPath = "f:\Native Win\DropBoard\src\app.ico"
$hIcon = $bmp256.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$fs = New-Object System.IO.FileStream $icoPath, ([System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Close()
$bmp256.Dispose()

# 3. Save extension icons (16, 48, 128)
$sizes = @(16, 48, 128)
foreach ($s in $sizes) {
    $bmpS = Create-ConceptEBitmap $s
    $extPath = "f:\Native Win\DropBoard\extension\icon$s.png"
    $bmpS.Save($extPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmpS.Dispose()
}

Write-Output "Concept E applied to app.ico, app_icon.png, and extension icons!"
