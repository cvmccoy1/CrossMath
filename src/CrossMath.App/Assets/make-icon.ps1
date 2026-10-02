# Regenerates CrossMath.ico from code. Run from Windows PowerShell:
#   powershell -ExecutionPolicy Bypass -File src/CrossMath.App/Assets/make-icon.ps1
# Design: a plus-shaped crossing of tiles on an ink plate, using the app's palette
# (Ink #2E2A24, TileFill #FFF6DA, TileEdge #C9A44C, PlayerInk #1F5FB4).
# Drawn on a 256-unit canvas and scaled; digits are omitted below 48 px to stay legible.

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$out = Join-Path $PSScriptRoot 'CrossMath.ico'

function Brush($hex) { $b = [Windows.Media.SolidColorBrush]([Windows.Media.ColorConverter]::ConvertFromString($hex)); $b.Freeze(); $b }

$ink = Brush '#2E2A24'
$tileFill = Brush '#FFF6DA'
$tileEdge = Brush '#C9A44C'
$blue = Brush '#1F5FB4'
$white = Brush '#FFFFFF'
$typeface = New-Object Windows.Media.Typeface 'Segoe UI Semibold'

# Grid: 3x3 cells of 64 units with 8-unit gaps, starting at 24.
function CellRect($col, $row) { New-Object Windows.Rect (24 + $col * 72), (24 + $row * 72), 64, 64 }

function Render([int]$size) {
    $visual = New-Object Windows.Media.DrawingVisual
    $dc = $visual.RenderOpen()
    $dc.PushTransform((New-Object Windows.Media.ScaleTransform ($size / 256), ($size / 256)))

    $dc.DrawRoundedRectangle($ink, $null, (New-Object Windows.Rect 8, 8, 240, 240), 44, 44)

    $edgeWidth = [Math]::Max(4, 256 / $size * 0.75)
    $edge = New-Object Windows.Media.Pen $tileEdge, $edgeWidth
    $tiles = @{ '1,0' = '2'; '0,1' = '3'; '2,1' = '4'; '1,2' = '5' }
    foreach ($key in $tiles.Keys) {
        $c, $r = $key.Split(',')
        $rect = CellRect ([int]$c) ([int]$r)
        $inset = $edgeWidth / 2
        $rect.Inflate(-$inset, -$inset)
        $dc.DrawRoundedRectangle($tileFill, $edge, $rect, 12, 12)
        if ($size -ge 48) {
            $text = New-Object Windows.Media.FormattedText $tiles[$key], ([Globalization.CultureInfo]::InvariantCulture),
                ([Windows.FlowDirection]::LeftToRight), $typeface, 44, $ink, 1.0
            $dc.DrawText($text, (New-Object Windows.Point ($rect.X + ($rect.Width - $text.Width) / 2), ($rect.Y + ($rect.Height - $text.Height) / 2)))
        }
    }

    # Centre tile: the shared "+" where the two equations (3 + 4, 2 + 5) cross.
    $centre = CellRect 1 1
    $dc.DrawRoundedRectangle($blue, $null, $centre, 12, 12)
    $bar = 10
    $len = 38
    $cx = $centre.X + 32; $cy = $centre.Y + 32
    $dc.DrawRectangle($white, $null, (New-Object Windows.Rect ($cx - $len / 2), ($cy - $bar / 2), $len, $bar))
    $dc.DrawRectangle($white, $null, (New-Object Windows.Rect ($cx - $bar / 2), ($cy - $len / 2), $bar, $len))

    $dc.Pop()
    $dc.Close()

    $bitmap = New-Object Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = New-Object IO.MemoryStream
    $encoder.Save($stream)
    , $stream.ToArray()
}

# ICO container with PNG-compressed frames (supported since Windows Vista).
$images = foreach ($s in $sizes) { , (Render $s) }
$file = New-Object IO.MemoryStream
$w = New-Object IO.BinaryWriter $file
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$images[$i].Length); $w.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Flush()
[IO.File]::WriteAllBytes($out, $file.ToArray())
Write-Host "Wrote $out ($($file.Length) bytes)"
