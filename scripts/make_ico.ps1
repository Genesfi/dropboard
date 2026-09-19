Add-Type -AssemblyName System.Drawing

$bmp = New-Object System.Drawing.Bitmap 256, 256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# Background dark rounded rect
$brushBg = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 15, 17, 23))
$g.FillRectangle($brushBg, 0, 0, 256, 256)

# Outer glow gradient
$rect = New-Object System.Drawing.Rectangle 16, 16, 224, 224
$brushGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush ($rect, [System.Drawing.Color]::FromArgb(255, 59, 130, 246), [System.Drawing.Color]::FromArgb(255, 139, 92, 246), 45)
$penGrad = New-Object System.Drawing.Pen ($brushGrad, 14)
$g.DrawEllipse($penGrad, 32, 32, 192, 192)

# Inner glyph
$brushInner = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 96, 165, 250))
$g.FillRectangle($brushInner, 88, 88, 80, 80)
$brushDot = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255))
$g.FillEllipse($brushDot, 112, 112, 32, 32)

$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$outPath = 'f:\Native Win\DropBoard\src\app.ico'
$fs = New-Object System.IO.FileStream $outPath, ([System.IO.FileMode]::Create)
$icon.Save($fs)
$fs.Close()
$g.Dispose()
$bmp.Dispose()
Write-Output "Created $outPath successfully"
