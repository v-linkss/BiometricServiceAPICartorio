# Gera o .ico do agente a partir do PNG da marca usada no front.
#
# Por que existe: o logo e 202x97 (proporcao ~2:1) e icone precisa ser quadrado.
# Esticar para 256x256 distorceria a marca, entao a imagem e encaixada por
# LARGURA e centralizada verticalmente, com o resto transparente. A arte fica
# fiel; o custo e o "360" ocupar so a faixa central do quadrado.
#
# Gera um ICO multi-resolucao com entradas PNG (suportado desde o Windows Vista),
# para o Windows escolher o tamanho certo em cada contexto: bandeja, barra de
# tarefas, Explorer, tela de Programas e Recursos.
#
# Rodar:  .\tools\gerar-icone.ps1

[CmdletBinding()]
param(
    [string] $Origem  = "$PSScriptRoot\..\..\Cartorio360-front\public\cartorio_icon.png",
    [string] $Destino = "$PSScriptRoot\..\Cartorio360.Agente\cartorio_icon.ico"
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $Origem)) { throw "PNG de origem nao encontrado: $Origem" }

$tamanhos = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

$origemImg = [System.Drawing.Image]::FromFile((Resolve-Path $Origem))
Write-Host ("Origem: {0}x{1}" -f $origemImg.Width, $origemImg.Height)

$imagens = @()
foreach ($lado in $tamanhos) {
    $bmp = New-Object System.Drawing.Bitmap($lado, $lado, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $g.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Encaixa preservando proporcao. Uma margem minima evita o traco da marca
    # encostar na borda do icone, que fica feio na barra de tarefas.
    $margem = [Math]::Max(1, [int]($lado * 0.04))
    $util = $lado - (2 * $margem)
    $escala = [Math]::Min($util / $origemImg.Width, $util / $origemImg.Height)
    $largura = [int][Math]::Round($origemImg.Width * $escala)
    $altura  = [int][Math]::Round($origemImg.Height * $escala)
    $x = [int](($lado - $largura) / 2)
    $y = [int](($lado - $altura) / 2)

    $g.DrawImage($origemImg, $x, $y, $largura, $altura)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $imagens += ,@{ Lado = $lado; Bytes = $ms.ToArray() }
    $ms.Dispose(); $bmp.Dispose()
}
$origemImg.Dispose()

# --- Montagem do container ICO ------------------------------------------------
# ICONDIR (6 bytes) + N x ICONDIRENTRY (16 bytes) + os PNGs em sequencia.
$saida = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter($saida)

$w.Write([UInt16]0)                  # reservado
$w.Write([UInt16]1)                  # tipo 1 = icone
$w.Write([UInt16]$imagens.Count)

$offset = 6 + (16 * $imagens.Count)
foreach ($img in $imagens) {
    # 256 e gravado como 0: o campo tem 1 byte so.
    $dim = if ($img.Lado -ge 256) { 0 } else { $img.Lado }
    $w.Write([Byte]$dim)             # largura
    $w.Write([Byte]$dim)             # altura
    $w.Write([Byte]0)                # cores na paleta (0 = sem paleta)
    $w.Write([Byte]0)                # reservado
    $w.Write([UInt16]1)              # planos de cor
    $w.Write([UInt16]32)             # bits por pixel
    $w.Write([UInt32]$img.Bytes.Length)
    $w.Write([UInt32]$offset)
    $offset += $img.Bytes.Length
}
foreach ($img in $imagens) { $w.Write($img.Bytes) }

$w.Flush()
$destinoAbs = [System.IO.Path]::GetFullPath($Destino)
[System.IO.File]::WriteAllBytes($destinoAbs, $saida.ToArray())
$w.Dispose(); $saida.Dispose()

Write-Host ("Gerado: {0} ({1:N0} bytes, {2} resolucoes: {3})" -f `
    $destinoAbs, (Get-Item $destinoAbs).Length, $imagens.Count, ($tamanhos -join ', '))

# Conferencia: se o .NET consegue abrir, o Windows tambem consegue.
$ico = New-Object System.Drawing.Icon($destinoAbs)
Write-Host ("Verificado: o .NET carregou o icone em {0}x{1}" -f $ico.Width, $ico.Height)
$ico.Dispose()
