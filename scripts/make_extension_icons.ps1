Add-Type -AssemblyName System.Drawing

function Generate-Icon($size, $outputPath) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Background rounded container
    $brushBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 15, 17, 23))
    $g.FillRectangle($brushBg, 0, 0, $size, $size)

    # Gradient border / ring
    $rect = New-Object System.Drawing.Rectangle 0, 0, $size, $size
    $brushGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush ($rect, [System.Drawing.Color]::FromArgb(255, 59, 130, 246), [System.Drawing.Color]::FromArgb(255, 139, 92, 246), 45)
    $pen = New-Object System.Drawing.Pen ($brushGrad, [Math]::Max(1.5, $size * 0.08))
    $pad = [Math]::Max(1, $size * 0.1)
    $g.DrawRectangle($pen, $pad, $pad, $size - ($pad * 2), $size - ($pad * 2))

    # Inner board symbol
    $innerPad = $size * 0.3
    $innerSize = $size - ($innerPad * 2)
    $brushInner = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 96, 165, 250))
    $g.FillRectangle($brushInner, $innerPad, $innerPad, $innerSize, $innerSize)

    $bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
    Write-Output "Saved $outputPath"
}

$extDir = "f:\Native Win\DropBoard\extension"
Generate-Icon 16 "$extDir\icon16.png"
Generate-Icon 48 "$extDir\icon48.png"
Generate-Icon 128 "$extDir\icon128.png"
