Add-Type -AssemblyName System.Drawing

function Create-ConceptDBitmap([int]$size) {
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

    # Papan 2 (Top Board with curved D contour on top-right): White
    $pTop = New-Object System.Drawing.Drawing2D.GraphicsPath
    $x2 = [float](104.0 * $scale)
    $y2 = [float](46.0 * $scale)
    $w2 = [float](106.0 * $scale)
    $h2 = [float](76.0 * $scale)
    $r2 = [float](14.0 * $scale)
    $rBig = [float](46.0 * $scale)

    $pTop.AddArc($x2, $y2, ($r2*2), ($r2*2), 180, 90)
    $pTop.AddArc(($x2 + $w2 - $rBig*2), $y2, ($rBig*2), ($rBig*2), 270, 90)
    $pTop.AddArc(($x2 + $w2 - $r2*2), ($y2 + $h2 - $r2*2), ($r2*2), ($r2*2), 0, 90)
    $pTop.AddArc($x2, ($y2 + $h2 - $r2*2), ($r2*2), ($r2*2), 90, 90)
    $pTop.CloseFigure()
    $g.FillPath($bWhite, $pTop)
    $pTop.Dispose()

    # Papan 3 (Bottom Board with curved D contour on bottom-right): Cyan-Blue
    $pBot = New-Object System.Drawing.Drawing2D.GraphicsPath
    $x3 = [float](104.0 * $scale)
    $y3 = [float](134.0 * $scale)
    $w3 = [float](106.0 * $scale)
    $h3 = [float](76.0 * $scale)
    $r3 = [float](14.0 * $scale)

    $pBot.AddArc($x3, $y3, ($r3*2), ($r3*2), 180, 90)
    $pBot.AddArc(($x3 + $w3 - $r3*2), $y3, ($r3*2), ($r3*2), 270, 90)
    $pBot.AddArc(($x3 + $w3 - $rBig*2), ($y3 + $h3 - $rBig*2), ($rBig*2), ($rBig*2), 0, 90)
    $pBot.AddArc($x3, ($y3 + $h3 - $r3*2), ($r3*2), ($r3*2), 90, 90)
    $pBot.CloseFigure()
    $g.FillPath($bBlue, $pBot)
    $pBot.Dispose()

    $brushBg.Dispose()
    $penRim.Dispose()
    $bBlue.Dispose()
    $bWhite.Dispose()
    $g.Dispose()

    return $bmp
}

# 1. Save 256x256 app_icon.png
$bmp256 = Create-ConceptDBitmap 256
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
    $bmpS = Create-ConceptDBitmap $s
    $extPath = "f:\Native Win\DropBoard\extension\icon$s.png"
    $bmpS.Save($extPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmpS.Dispose()
}

Write-Output "Concept D successfully applied to app.ico, app_icon.png, and extension icons!"
