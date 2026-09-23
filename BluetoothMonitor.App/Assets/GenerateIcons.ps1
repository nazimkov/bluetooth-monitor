# Generate the app, tray, and package logo artwork.
# Run from the repository root with Windows PowerShell 5.1:
# powershell -ExecutionPolicy Bypass -File .\BluetoothMonitor.App\Assets\GenerateIcons.ps1

Add-Type -AssemblyName System.Drawing

$script:RenderScale = 4
$script:WorkSize = 256 * $script:RenderScale

function Get-Color {
    param([string]$Hex)

    $value = $Hex.TrimStart('#')
    return [System.Drawing.Color]::FromArgb(
        [Convert]::ToInt32($value.Substring(0, 2), 16),
        [Convert]::ToInt32($value.Substring(2, 2), 16),
        [Convert]::ToInt32($value.Substring(4, 2), 16)
    )
}

function New-RoundedRectanglePath {
    param(
        [single]$X,
        [single]$Y,
        [single]$Width,
        [single]$Height,
        [single]$Radius
    )

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Fill-RoundedRectangle {
    param(
        [System.Drawing.Graphics]$Graphics,
        [single]$X,
        [single]$Y,
        [single]$Width,
        [single]$Height,
        [single]$Radius,
        [System.Drawing.Brush]$Brush
    )

    $path = New-RoundedRectanglePath -X $X -Y $Y -Width $Width -Height $Height -Radius $Radius
    $Graphics.FillPath($Brush, $path)
    $path.Dispose()
}

function Draw-RoundedRectangle {
    param(
        [System.Drawing.Graphics]$Graphics,
        [single]$X,
        [single]$Y,
        [single]$Width,
        [single]$Height,
        [single]$Radius,
        [System.Drawing.Pen]$Pen
    )

    $path = New-RoundedRectanglePath -X $X -Y $Y -Width $Width -Height $Height -Radius $Radius
    $Graphics.DrawPath($Pen, $path)
    $path.Dispose()
}

function Set-HighQualityGraphics {
    param([System.Drawing.Graphics]$Graphics)

    $Graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $Graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $Graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $Graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
}

function Draw-AppIcon {
    param([System.Drawing.Graphics]$Graphics)

    $tile = New-RoundedRectanglePath -X 14 -Y 14 -Width 228 -Height 228 -Radius 56
    $tileBounds = [System.Drawing.Rectangle]::new(14, 14, 228, 228)
    $background = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        $tileBounds,
        (Get-Color '102642'),
        (Get-Color '117FA1'),
        40
    )
    $Graphics.FillPath($background, $tile)
    $background.Dispose()

    $edgePen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(48, 255, 255, 255), 3)
    Draw-RoundedRectangle -Graphics $Graphics -X 15.5 -Y 15.5 -Width 225 -Height 225 -Radius 55 -Pen $edgePen
    $edgePen.Dispose()
    $tile.Dispose()

    # The battery uses a dark inset and a teal charge field for depth at small sizes.
    $wellBrush = [System.Drawing.SolidBrush]::new((Get-Color '092B49'))
    Fill-RoundedRectangle -Graphics $Graphics -X 48 -Y 82 -Width 156 -Height 92 -Radius 15 -Brush $wellBrush
    $wellBrush.Dispose()

    $chargeBounds = [System.Drawing.Rectangle]::new(51, 85, 150, 86)
    $charge = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        $chargeBounds,
        (Get-Color '32D3BE'),
        (Get-Color '26A9D1'),
        0
    )
    Fill-RoundedRectangle -Graphics $Graphics -X 51 -Y 85 -Width 150 -Height 86 -Radius 12 -Brush $charge
    $charge.Dispose()

    $bodyPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(250, 247, 253, 255), 9)
    $bodyPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    Draw-RoundedRectangle -Graphics $Graphics -X 39 -Y 73 -Width 173 -Height 110 -Radius 23 -Pen $bodyPen
    $bodyPen.Dispose()

    $tipBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(250, 247, 253, 255))
    Fill-RoundedRectangle -Graphics $Graphics -X 210 -Y 106 -Width 19 -Height 44 -Radius 8 -Brush $tipBrush
    $tipBrush.Dispose()

    # The Bluetooth rune sits inside the battery body as the app's identifying mark.
    $runePen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, 8)
    $runePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $runePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $runePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $Graphics.DrawLine($runePen, 126, 91, 126, 165)
    $Graphics.DrawLine($runePen, 126, 91, 160, 117)
    $Graphics.DrawLine($runePen, 160, 117, 126, 128)
    $Graphics.DrawLine($runePen, 126, 128, 160, 143)
    $Graphics.DrawLine($runePen, 160, 143, 126, 165)
    $runePen.Dispose()
}

function Draw-TrayIcon {
    param(
        [System.Drawing.Graphics]$Graphics,
        [ValidateSet('Connected', 'Low', 'Critical', 'Offline')][string]$State
    )

    $spec = switch ($State) {
        'Connected' { @{ Color = '43C697'; Level = 0.82 } }
        'Low'       { @{ Color = 'F0B44F'; Level = 0.34 } }
        'Critical'  { @{ Color = 'F0717A'; Level = 0.16 } }
        'Offline'   { @{ Color = 'A2AFBD'; Level = 0.00 } }
    }
    $tint = Get-Color $spec.Color

    if ($spec.Level -gt 0) {
        $fillWidth = 143 * $spec.Level
        $fillRadius = [single][math]::Min(11.0, $fillWidth / 2.0)
        $fillBrush = [System.Drawing.SolidBrush]::new($tint)
        Fill-RoundedRectangle -Graphics $Graphics -X 48 -Y 96 -Width $fillWidth -Height 64 -Radius $fillRadius -Brush $fillBrush
        $fillBrush.Dispose()
    }

    $outline = [System.Drawing.Pen]::new($tint, 11)
    $outline.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    Draw-RoundedRectangle -Graphics $Graphics -X 29 -Y 74 -Width 181 -Height 108 -Radius 21 -Pen $outline
    $outline.Dispose()

    $tip = [System.Drawing.SolidBrush]::new($tint)
    Fill-RoundedRectangle -Graphics $Graphics -X 207 -Y 108 -Width 20 -Height 41 -Radius 8 -Brush $tip
    $tip.Dispose()

    if ($State -eq 'Offline') {
        $slashPen = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(220, $tint), 12)
        $slashPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $slashPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $Graphics.DrawLine($slashPen, 76, 151, 166, 103)
        $slashPen.Dispose()
    }
}

function New-RenderedIcon {
    param(
        [int]$Size,
        [ValidateSet('App', 'Connected', 'Low', 'Critical', 'Offline', 'LockScreen')][string]$Style
    )

    $large = [System.Drawing.Bitmap]::new(
        $script:WorkSize,
        $script:WorkSize,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $largeGraphics = [System.Drawing.Graphics]::FromImage($large)
    Set-HighQualityGraphics -Graphics $largeGraphics
    $largeGraphics.Clear([System.Drawing.Color]::Transparent)
    $largeGraphics.ScaleTransform([single]$script:RenderScale, [single]$script:RenderScale)

    if ($Style -eq 'App') {
        Draw-AppIcon -Graphics $largeGraphics
    }
    elseif ($Style -eq 'LockScreen') {
        $white = [System.Drawing.Color]::White
        $pen = [System.Drawing.Pen]::new($white, 10)
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        Draw-RoundedRectangle -Graphics $largeGraphics -X 39 -Y 73 -Width 173 -Height 110 -Radius 23 -Pen $pen
        $largeGraphics.DrawLine($pen, 126, 91, 126, 165)
        $largeGraphics.DrawLine($pen, 126, 91, 160, 117)
        $largeGraphics.DrawLine($pen, 160, 117, 126, 128)
        $largeGraphics.DrawLine($pen, 126, 128, 160, 143)
        $largeGraphics.DrawLine($pen, 160, 143, 126, 165)
        $pen.Dispose()
        $tip = [System.Drawing.SolidBrush]::new($white)
        Fill-RoundedRectangle -Graphics $largeGraphics -X 210 -Y 106 -Width 19 -Height 44 -Radius 8 -Brush $tip
        $tip.Dispose()
    }
    else {
        Draw-TrayIcon -Graphics $largeGraphics -State $Style
    }

    $largeGraphics.Dispose()
    $small = [System.Drawing.Bitmap]::new(
        $Size,
        $Size,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $smallGraphics = [System.Drawing.Graphics]::FromImage($small)
    Set-HighQualityGraphics -Graphics $smallGraphics
    $smallGraphics.Clear([System.Drawing.Color]::Transparent)
    $smallGraphics.DrawImage(
        $large,
        [System.Drawing.Rectangle]::new(0, 0, $Size, $Size),
        [single]0,
        [single]0,
        [single]$script:WorkSize,
        [single]$script:WorkSize,
        [System.Drawing.GraphicsUnit]::Pixel
    )
    $smallGraphics.Dispose()
    $large.Dispose()
    return $small
}

function Write-IconPng {
    param(
        [int]$Size,
        [string]$Style,
        [string]$Path
    )

    $bitmap = New-RenderedIcon -Size $Size -Style $Style
    try {
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $bitmap.Dispose()
    }
}

function New-IconPngBytes {
    param([int]$Size, [string]$Style)

    $bitmap = New-RenderedIcon -Size $Size -Style $Style
    $stream = [System.IO.MemoryStream]::new()
    try {
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return ,$stream.ToArray()
    }
    finally {
        $stream.Dispose()
        $bitmap.Dispose()
    }
}

function Write-IconFile {
    param(
        [string]$Style,
        [string]$Path
    )

    $sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
    $images = @()
    foreach ($size in $sizes) {
        $images += ,(New-IconPngBytes -Size $size -Style $Style)
    }

    $stream = [System.IO.MemoryStream]::new()
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$sizes.Count)
        $offset = 6 + (16 * $sizes.Count)

        for ($index = 0; $index -lt $sizes.Count; $index++) {
            $size = $sizes[$index]
            $data = $images[$index]
            $dimension = if ($size -ge 256) { 0 } else { $size }
            $writer.Write([byte]$dimension)
            $writer.Write([byte]$dimension)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([uint16]1)
            $writer.Write([uint16]32)
            $writer.Write([uint32]$data.Length)
            $writer.Write([uint32]$offset)
            $offset += $data.Length
        }

        foreach ($data in $images) {
            $writer.Write([byte[]]$data)
        }

        [System.IO.File]::WriteAllBytes($Path, $stream.ToArray())
    }
    finally {
        $writer.Dispose()
        $stream.Dispose()
    }
}

function Write-WideLogo {
    param([string]$Path)

    $canvas = [System.Drawing.Bitmap]::new(
        310,
        150,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    Set-HighQualityGraphics -Graphics $graphics
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $mark = New-RenderedIcon -Size 124 -Style App
    $graphics.DrawImage($mark, [System.Drawing.Rectangle]::new(93, 13, 124, 124))
    $mark.Dispose()
    $graphics.Dispose()
    try {
        $canvas.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $canvas.Dispose()
    }
}

function Write-SplashLogo {
    param([string]$Path)

    $canvas = [System.Drawing.Bitmap]::new(
        620,
        300,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb
    )
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    Set-HighQualityGraphics -Graphics $graphics
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $mark = New-RenderedIcon -Size 150 -Style App
    $graphics.DrawImage($mark, [System.Drawing.Rectangle]::new(235, 75, 150, 150))
    $mark.Dispose()
    $graphics.Dispose()
    try {
        $canvas.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $canvas.Dispose()
    }
}

$assetDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-IconFile -Style App -Path (Join-Path $assetDirectory 'app.ico')
Write-IconFile -Style Connected -Path (Join-Path $assetDirectory 'tray-connected.ico')
Write-IconFile -Style Low -Path (Join-Path $assetDirectory 'tray-low.ico')
Write-IconFile -Style Critical -Path (Join-Path $assetDirectory 'tray-critical.ico')
Write-IconFile -Style Offline -Path (Join-Path $assetDirectory 'tray-offline.ico')

Write-IconPng -Size 44 -Style App -Path (Join-Path $assetDirectory 'Square44x44Logo.png')
Write-IconPng -Size 150 -Style App -Path (Join-Path $assetDirectory 'Square150x150Logo.png')
Write-IconPng -Size 50 -Style App -Path (Join-Path $assetDirectory 'StoreLogo.png')
Write-IconPng -Size 24 -Style LockScreen -Path (Join-Path $assetDirectory 'LockScreenLogo.png')
Write-WideLogo -Path (Join-Path $assetDirectory 'Wide310x150Logo.png')
Write-SplashLogo -Path (Join-Path $assetDirectory 'SplashScreen.png')

Write-Host "Generated app and tray icons in $assetDirectory" -ForegroundColor Green
