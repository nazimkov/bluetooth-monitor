# Regenerates placeholder PNG + ICO assets so the MSIX build has all required files.
# Run from the Assets folder:  powershell -ExecutionPolicy Bypass -File .\GeneratePlaceholders.ps1
# Replace the output files with real artwork before shipping.

Add-Type -AssemblyName System.Drawing

$accent      = [System.Drawing.Color]::FromArgb(255, 0x00, 0x78, 0xD4)
$ok          = [System.Drawing.Color]::FromArgb(255, 0x10, 0x7C, 0x10)
$warn        = [System.Drawing.Color]::FromArgb(255, 0x9D, 0x5D, 0x00)
$danger      = [System.Drawing.Color]::FromArgb(255, 0xC4, 0x2B, 0x1C)
$offlineGray = [System.Drawing.Color]::FromArgb(255, 0x8A, 0x8A, 0x8A)

function Render-Battery {
    param([System.Drawing.Graphics]$g, [int]$Size, [System.Drawing.Color]$Tint)
    $g.SmoothingMode = 'AntiAlias'
    $g.Clear([System.Drawing.Color]::Transparent)

    $padX = [int]([math]::Floor($Size * 0.15))
    $padY = [int]([math]::Floor($Size * 0.30))
    $tipW = [int][math]::Max(2, [math]::Floor($Size * 0.05))
    $bodyW = $Size - ($padX * 2) - $tipW - 2
    $bodyH = $Size - ($padY * 2)
    $body = [System.Drawing.Rectangle]::new($padX, $padY, $bodyW, $bodyH)
    $tipH = [int][math]::Max(2, [math]::Floor($bodyH * 0.5))
    $tipY = $padY + [int][math]::Floor($bodyH * 0.25)
    $tip  = [System.Drawing.Rectangle]::new($padX + $bodyW + 2, $tipY, $tipW, $tipH)

    $penWidth = [float]([math]::Max(1.0, $Size / 24.0))
    $pen = New-Object System.Drawing.Pen($Tint, $penWidth)
    $brush = New-Object System.Drawing.SolidBrush($Tint)
    $g.DrawRectangle($pen, $body)
    $g.FillRectangle($brush, $tip)

    $pad2 = [int][math]::Max(1, [math]::Floor($Size / 16))
    $fill = [System.Drawing.Rectangle]::new($body.X + $pad2, $body.Y + $pad2, $body.Width - $pad2 * 2, $body.Height - $pad2 * 2)
    $g.FillRectangle($brush, $fill)

    $pen.Dispose(); $brush.Dispose()
}

function New-BatteryPng {
    param([int]$Size, [System.Drawing.Color]$Tint, [string]$Path)
    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    Render-Battery -g $g -Size $Size -Tint $Tint
    $g.Dispose()
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function New-BatteryIco {
    param([System.Drawing.Color]$Tint, [string]$OutPath)
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $buffers = @()
    foreach ($s in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap($s, $s)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        Render-Battery -g $g -Size $s -Tint $Tint
        $g.Dispose()
        $mems = New-Object System.IO.MemoryStream
        $bmp.Save($mems, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        $buffers += ,$mems.ToArray()
    }
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    $bw.Write([uint16]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]$sizes.Count)
    $offset = 6 + (16 * $sizes.Count)
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $s = $sizes[$i]
        $data = $buffers[$i]
        $sbyte = if ($s -ge 256) { 0 } else { $s }
        $bw.Write([byte]$sbyte)
        $bw.Write([byte]$sbyte)
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([uint16]1)
        $bw.Write([uint16]32)
        $bw.Write([uint32]$data.Length)
        $bw.Write([uint32]$offset)
        $offset += $data.Length
    }
    foreach ($data in $buffers) { $bw.Write($data) }
    [System.IO.File]::WriteAllBytes($OutPath, $ms.ToArray())
}

$here = Split-Path -Parent $MyInvocation.MyCommand.Definition

New-BatteryPng -Size 44  -Tint $accent -Path (Join-Path $here 'Square44x44Logo.png')
New-BatteryPng -Size 150 -Tint $accent -Path (Join-Path $here 'Square150x150Logo.png')
New-BatteryPng -Size 50  -Tint $accent -Path (Join-Path $here 'StoreLogo.png')
New-BatteryPng -Size 310 -Tint $accent -Path (Join-Path $here 'Wide310x150Logo.png')
New-BatteryPng -Size 620 -Tint $accent -Path (Join-Path $here 'SplashScreen.png')
New-BatteryPng -Size 24  -Tint $accent -Path (Join-Path $here 'LockScreenLogo.png')

New-BatteryIco -Tint $accent      -OutPath (Join-Path $here 'app.ico')
New-BatteryIco -Tint $ok          -OutPath (Join-Path $here 'tray-connected.ico')
New-BatteryIco -Tint $warn        -OutPath (Join-Path $here 'tray-low.ico')
New-BatteryIco -Tint $danger      -OutPath (Join-Path $here 'tray-critical.ico')
New-BatteryIco -Tint $offlineGray -OutPath (Join-Path $here 'tray-offline.ico')

Write-Host "Generated placeholder assets in $here" -ForegroundColor Green
