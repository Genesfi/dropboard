Add-Type -AssemblyName System.Drawing

function Create-DropBoardBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    $scale = [float]$size / 256.0

    # Colors (Clean 2-color palette: Electric Cyan-Blue #38bdf8 & Crisp White #ffffff)
    $colBg = [System.Drawing.Color]::FromArgb(255, 13, 17, 26) # Sleek Dark Slate
    $colBorder = [System.Drawing.Color]::FromArgb(70, 56, 189, 248) # Cyan ambient rim
    $colBlue = [System.Drawing.Color]::FromArgb(255, 56, 189, 248) # Vibrant Cyan-Blue
    $colWhite = [System.Drawing.Color]::FromArgb(255, 255, 255, 255) # Pure White

    # 1. Outer Squircle (Rounded Square with smooth radius, NOT full sharp box)
    $pad = [float](10.0 * $scale)
    $sqSize = [float](236.0 * $scale)
    $sqRadius = [float](54.0 * $scale)

    $pathBg = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = [float]($sqRadius * 2.0)
    $pathBg.AddArc($pad, $pad, $d, $d, 180, 90)
    $pathBg.AddArc([float]($pad + $sqSize - $d), $pad, $d, $d, 270, 90)
    $pathBg.AddArc([float]($pad + $sqSize - $d), [float]($pad + $sqSize - $d), $d, $d, 0, 90)
    $pathBg.AddArc($pad, [float]($pad + $sqSize - $d), $d, $d, 90, 90)
    $pathBg.CloseFigure()

    $brushBg = New-Object System.Drawing.SolidBrush $colBg
    $g.FillPath($brushBg, $pathBg)

    $penBorder = New-Object System.Drawing.Pen ($colBorder, [float](2.5 * $scale))
    $g.DrawPath($penBorder, $pathBg)

    # 2. 3 BOARDS ARRANGED IN "D" SHAPE:
    # Thickness of boards
    $thick = [float](32.0 * $scale)

    # Board 1: Left Vertical Spine Board (Vibrant Blue)
    $brushBlue = New-Object System.Drawing.SolidBrush $colBlue
    $penBlue = New-Object System.Drawing.Pen ($brushBlue, $thick)
    $penBlue.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penBlue.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($penBlue, [float](68.0 * $scale), [float](68.0 * $scale), [float](68.0 * $scale), [float](188.0 * $scale))

    # Board 2: Top Arching Board (Crisp Pure White)
    $brushWhite = New-Object System.Drawing.SolidBrush $colWhite
    $penWhite = New-Object System.Drawing.Pen ($brushWhite, $thick)
    $penWhite.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penWhite.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penWhite.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $p1 = New-Object System.Drawing.PointF ([float](106.0 * $scale), [float](68.0 * $scale))
    $p2 = New-Object System.Drawing.PointF ([float](148.0 * $scale), [float](68.0 * $scale))
    $p3 = New-Object System.Drawing.PointF ([float](188.0 * $scale), [float](114.0 * $scale))
    [System.Drawing.PointF[]]$ptsTop = @($p1, $p2, $p3)
    $g.DrawLines($penWhite, $ptsTop)

    # Board 3: Bottom Arching Board (Vibrant Blue)
    $penBlueBottom = New-Object System.Drawing.Pen ($brushBlue, $thick)
    $penBlueBottom.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penBlueBottom.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $penBlueBottom.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $p4 = New-Object System.Drawing.PointF ([float](188.0 * $scale), [float](142.0 * $scale))
    $p5 = New-Object System.Drawing.PointF ([float](148.0 * $scale), [float](188.0 * $scale))
    $p6 = New-Object System.Drawing.PointF ([float](106.0 * $scale), [float](188.0 * $scale))
    [System.Drawing.PointF[]]$ptsBottom = @($p4, $p5, $p6)
    $g.DrawLines($penBlueBottom, $ptsBottom)

    # Clean up
    $penBlue.Dispose()
    $penWhite.Dispose()
    $penBlueBottom.Dispose()
    $brushBg.Dispose()
    $brushBlue.Dispose()
    $brushWhite.Dispose()
    $penBorder.Dispose()
    $pathBg.Dispose()
    $g.Dispose()

    return $bmp
}

# Generate 256x256 preview PNG
$bmp256 = Create-DropBoardBitmap 256
$pngPath = "f:\Native Win\DropBoard\assets\app_icon.png"
$bmp256.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Generate proper ICO for Win32
$icoPath = "f:\Native Win\DropBoard\src\app.ico"
$hIcon = $bmp256.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$fs = New-Object System.IO.FileStream $icoPath, ([System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Close()
$bmp256.Dispose()

# Also generate extension icons (16, 48, 128)
$sizes = @(16, 48, 128)
foreach ($s in $sizes) {
    $bmpS = Create-DropBoardBitmap $s
    $extPath = "f:\Native Win\DropBoard\extension\icon$s.png"
    $bmpS.Save($extPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmpS.Dispose()
}

Write-Output "DropBoard 3-Board 'D' icons generated successfully!"
