Add-Type -AssemblyName System.Drawing

function Render-BoardLogo([string]$outPath, [string]$style) {
    $size = 256
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # 1. Background Squircle (Kotak Rounded Modern)
    $pathBg = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pad = 12.0
    $sqSize = 232.0
    $d = 104.0
    $pathBg.AddArc($pad, $pad, $d, $d, 180, 90)
    $pathBg.AddArc(($pad + $sqSize - $d), $pad, $d, $d, 270, 90)
    $pathBg.AddArc(($pad + $sqSize - $d), ($pad + $sqSize - $d), $d, $d, 0, 90)
    $pathBg.AddArc($pad, ($pad + $sqSize - $d), $d, $d, 90, 90)
    $pathBg.CloseFigure()

    $brushBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 12, 16, 25))
    $g.FillPath($brushBg, $pathBg)
    $penRim = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(70, 56, 189, 248), 2.0)
    $g.DrawPath($penRim, $pathBg)

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

    $bBlue = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 56, 189, 248))
    $bWhite = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))

    if ($style -eq "A") {
        # KONSEP A: 3 Papan Pipih (Flat Cards/Boards) Membentuk Huruf D
        # Papan 1 (Papan Tegak Kiri / Vertical Board): Cyan-Blue
        Fill-RoundRect $g $bBlue 44.0 44.0 48.0 168.0 12.0

        # Papan 2 (Papan Atas / Top Board): White
        Fill-RoundRect $g $bWhite 104.0 44.0 106.0 76.0 14.0

        # Papan 3 (Papan Bawah / Bottom Board): Cyan-Blue
        Fill-RoundRect $g $bBlue 104.0 136.0 106.0 76.0 14.0
    }
    elseif ($style -eq "B") {
        # KONSEP B: 3 Papan Dinamis (Staggered Board Layout)
        # Papan 1: Vertical left
        Fill-RoundRect $g $bBlue 44.0 44.0 48.0 168.0 12.0

        # Papan 2: Top card extending right
        Fill-RoundRect $g $bWhite 102.0 44.0 108.0 68.0 12.0

        # Papan 3: Right/Bottom card closing the D
        Fill-RoundRect $g $bBlue 142.0 124.0 68.0 88.0 12.0
    }
    elseif ($style -eq "C") {
        # KONSEP C: 3 Papan Bersusun Menyilang Melengkung (Overlapping Clipboards D)
        # Papan 1 (Tegak): Cyan-Blue
        Fill-RoundRect $g $bBlue 44.0 44.0 46.0 168.0 12.0

        # Papan 2 (Top Angled Board): White
        $state1 = $g.Save()
        $g.TranslateTransform(100.0, 44.0)
        $g.RotateTransform(16.0)
        Fill-RoundRect $g $bWhite 0.0 0.0 108.0 54.0 12.0
        $g.Restore($state1)

        # Papan 3 (Bottom Angled Board): Cyan-Blue
        $state2 = $g.Save()
        $g.TranslateTransform(100.0, 212.0)
        $g.RotateTransform(-16.0)
        Fill-RoundRect $g $bBlue 0.0 -54.0 108.0 54.0 12.0
        $g.Restore($state2)
    }
    elseif ($style -eq "D") {
        # KONSEP D: 3 Papan dengan Kontur 'D' Melengkung Mulus
        # Papan 1 (Tegak): Cyan-Blue
        Fill-RoundRect $g $bBlue 46.0 46.0 48.0 164.0 14.0

        # Papan 2 (Top Board with curved D contour on top-right): White
        $pTop = New-Object System.Drawing.Drawing2D.GraphicsPath
        $x2 = 104.0; $y2 = 46.0; $w2 = 106.0; $h2 = 76.0; $r2 = 14.0; $rBig = 46.0
        $pTop.AddArc($x2, $y2, ($r2*2), ($r2*2), 180, 90)
        $pTop.AddArc(($x2 + $w2 - $rBig*2), $y2, ($rBig*2), ($rBig*2), 270, 90)
        $pTop.AddArc(($x2 + $w2 - $r2*2), ($y2 + $h2 - $r2*2), ($r2*2), ($r2*2), 0, 90)
        $pTop.AddArc($x2, ($y2 + $h2 - $r2*2), ($r2*2), ($r2*2), 90, 90)
        $pTop.CloseFigure()
        $g.FillPath($bWhite, $pTop)
        $pTop.Dispose()

        # Papan 3 (Bottom Board with curved D contour on bottom-right): Cyan-Blue
        $pBot = New-Object System.Drawing.Drawing2D.GraphicsPath
        $x3 = 104.0; $y3 = 134.0; $w3 = 106.0; $h3 = 76.0; $r3 = 14.0
        $pBot.AddArc($x3, $y3, ($r3*2), ($r3*2), 180, 90)
        $pBot.AddArc(($x3 + $w3 - $r3*2), $y3, ($r3*2), ($r3*2), 270, 90)
        $pBot.AddArc(($x3 + $w3 - $rBig*2), ($y3 + $h3 - $rBig*2), ($rBig*2), ($rBig*2), 0, 90)
        $pBot.AddArc($x3, ($y3 + $h3 - $r3*2), ($r3*2), ($r3*2), 90, 90)
        $pBot.CloseFigure()
        $g.FillPath($bBlue, $pBot)
        $pBot.Dispose()
    }
    elseif ($style -eq "E") {
        # KONSEP E: 3 Papan dengan Silhouette 'D' Murni & Sempurna
        # Papan 1 (Papan Tegak Kiri): Cyan-Blue
        Fill-RoundRect $g $bBlue 46.0 46.0 48.0 164.0 14.0

        # Papan 2 (Papan Atas Lengkung Luar D): Putih
        $p2 = New-Object System.Drawing.Drawing2D.GraphicsPath
        $x2 = 104.0; $y2 = 46.0; $w2 = 106.0; $h2 = 77.0
        $r2 = 14.0; $rBig2 = 72.0
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
        $x3 = 104.0; $y3 = 133.0; $w3 = 106.0; $h3 = 77.0
        $r3 = 14.0; $rBig3 = 72.0
        $p3.AddLine($x3, $y3, ($x3 + $w3), $y3)
        $p3.AddLine(($x3 + $w3), $y3, ($x3 + $w3), ($y3 + $h3 - $rBig3))
        $p3.AddArc(($x3 + $w3 - $rBig3*2), ($y3 + $h3 - $rBig3*2), ($rBig3*2), ($rBig3*2), 0, 90)
        $p3.AddArc($x3, ($y3 + $h3 - $r3*2), ($r3*2), ($r3*2), 90, 90)
        $p3.AddLine($x3, ($y3 + $h3 - $r3), $x3, $y3)
        $p3.CloseFigure()
        $g.FillPath($bBlue, $p3)
        $p3.Dispose()
    }

    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $g.Dispose()
}

Render-BoardLogo "f:\Native Win\DropBoard\assets\logo_concept_A.png" "A"
Render-BoardLogo "f:\Native Win\DropBoard\assets\logo_concept_B.png" "B"
Render-BoardLogo "f:\Native Win\DropBoard\assets\logo_concept_C.png" "C"
Render-BoardLogo "f:\Native Win\DropBoard\assets\logo_concept_D.png" "D"
Render-BoardLogo "f:\Native Win\DropBoard\assets\logo_concept_E.png" "E"
Write-Output "Generated Concepts A, B, C, D, and E successfully!"
