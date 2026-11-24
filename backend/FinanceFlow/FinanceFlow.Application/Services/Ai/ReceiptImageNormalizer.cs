using FinanceFlow.Application.Common.Exceptions;
using SkiaSharp;

namespace FinanceFlow.Application.Services.Ai;

public record NormalizedImage(byte[] Data, string MediaType, int Width, int Height);

/// <summary>
/// Prepara a foto do comprovante antes de ela sair do servidor:
/// confere que é mesmo uma imagem (não confia no Content-Type), corrige a rotação do celular,
/// reduz para o tamanho útil à IA e regrava em JPEG, o que também remove os metadados EXIF
/// (localização do GPS, modelo do aparelho). A imagem original nunca é guardada.
/// </summary>
public static class ReceiptImageNormalizer
{
    /// <summary>Lado maior que a API da Anthropic aproveita; acima disso ela só redimensiona.</summary>
    public const int MaxEdge = 1568;

    // Barra "bombas de descompressão": um PNG de 5 MB pode declarar bilhões de pixels
    private const long MaxPixels = 40_000_000;

    private const int JpegQuality = 85;

    private static readonly HashSet<SKEncodedImageFormat> AllowedFormats =
    [
        SKEncodedImageFormat.Jpeg,
        SKEncodedImageFormat.Png,
        SKEncodedImageFormat.Webp,
    ];

    public static NormalizedImage Normalize(byte[] bytes)
    {
        if (bytes.Length == 0)
            throw new ValidationException("A imagem está vazia.");

        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data)
            ?? throw new ValidationException("O arquivo enviado não é uma imagem válida.");

        if (!AllowedFormats.Contains(codec.EncodedFormat))
            throw new ValidationException("Formato de imagem não suportado. Use JPEG, PNG ou WebP.");

        var info = codec.Info;
        if (info.Width <= 0 || info.Height <= 0 || (long)info.Width * info.Height > MaxPixels)
            throw new ValidationException("A imagem é grande demais. Envie uma foto de até 40 megapixels.");

        using var source = SKBitmap.Decode(codec)
            ?? throw new ValidationException("Não foi possível ler a imagem enviada.");

        var rotation = codec.EncodedOrigin switch
        {
            SKEncodedOrigin.RightTop => 90,
            SKEncodedOrigin.BottomRight => 180,
            SKEncodedOrigin.LeftBottom => 270,
            _ => 0,
        };

        var scale = Math.Min(1.0, (double)MaxEdge / Math.Max(source.Width, source.Height));
        var drawWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        var drawHeight = Math.Max(1, (int)Math.Round(source.Height * scale));

        var swap = rotation is 90 or 270;
        var targetWidth = swap ? drawHeight : drawWidth;
        var targetHeight = swap ? drawWidth : drawHeight;

        using var target = new SKBitmap(new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(target))
        {
            // PNG/WebP transparentes virariam preto no JPEG: o comprovante vai sobre fundo branco
            canvas.Clear(SKColors.White);

            switch (rotation)
            {
                case 90:
                    canvas.Translate(targetWidth, 0);
                    canvas.RotateDegrees(90);
                    break;
                case 180:
                    canvas.Translate(targetWidth, targetHeight);
                    canvas.RotateDegrees(180);
                    break;
                case 270:
                    canvas.Translate(0, targetHeight);
                    canvas.RotateDegrees(270);
                    break;
            }

            using var image = SKImage.FromBitmap(source);
            canvas.DrawImage(
                image,
                new SKRect(0, 0, drawWidth, drawHeight),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }

        using var finalImage = SKImage.FromBitmap(target);
        using var encoded = finalImage.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);

        return new NormalizedImage(encoded.ToArray(), "image/jpeg", targetWidth, targetHeight);
    }
}
