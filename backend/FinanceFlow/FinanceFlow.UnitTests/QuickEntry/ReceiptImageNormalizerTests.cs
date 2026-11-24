using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Services.Ai;
using FluentAssertions;
using SkiaSharp;

namespace FinanceFlow.UnitTests.QuickEntry;

public class ReceiptImageNormalizerTests
{
    private static byte[] Encode(int width, int height, SKEncodedImageFormat format, SKColor? color = null, bool transparent = false)
    {
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888,
            transparent ? SKAlphaType.Premul : SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(bitmap))
            canvas.Clear(transparent ? SKColors.Transparent : color ?? SKColors.Red);

        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(format, 90).ToArray();
    }

    // Insere um EXIF mínimo (só a tag Orientation) logo após o SOI do JPEG
    private static byte[] WithExifOrientation(byte[] jpeg, byte orientation)
    {
        byte[] exif =
        [
            0xFF, 0xE1, 0x00, 0x22,
            0x45, 0x78, 0x69, 0x66, 0x00, 0x00,             // "Exif\0\0"
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, // TIFF little-endian, IFD em 8
            0x01, 0x00,                                     // 1 entrada
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, orientation, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];

        return [.. jpeg[..2], .. exif, .. jpeg[2..]];
    }

    private static SKBitmap Decode(byte[] bytes) => SKBitmap.Decode(bytes);

    [Fact]
    public void Normalize_DeveReduzirImagemGrandeParaOLimiteEManterAProporcao()
    {
        var result = ReceiptImageNormalizer.Normalize(Encode(3136, 1568, SKEncodedImageFormat.Png));

        result.Width.Should().Be(1568);
        result.Height.Should().Be(784);
        result.MediaType.Should().Be("image/jpeg");
    }

    [Fact]
    public void Normalize_NaoDeveAmpliarImagemPequena()
    {
        var result = ReceiptImageNormalizer.Normalize(Encode(300, 200, SKEncodedImageFormat.Jpeg));

        (result.Width, result.Height).Should().Be((300, 200));
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Webp)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    public void Normalize_DeveAceitarOsFormatosSuportadosEDevolverJpeg(SKEncodedImageFormat format)
    {
        var result = ReceiptImageNormalizer.Normalize(Encode(120, 80, format));

        using var codec = SKCodec.Create(SKData.CreateCopy(result.Data));
        codec.EncodedFormat.Should().Be(SKEncodedImageFormat.Jpeg);
    }

    [Fact]
    public void Normalize_DevePintarOFundoTransparenteDeBranco()
    {
        var result = ReceiptImageNormalizer.Normalize(Encode(40, 40, SKEncodedImageFormat.Png, transparent: true));

        using var bitmap = Decode(result.Data);
        var pixel = bitmap.GetPixel(20, 20);
        (pixel.Red, pixel.Green, pixel.Blue).Should().Match<(byte, byte, byte)>(p => p.Item1 > 240 && p.Item2 > 240 && p.Item3 > 240);
    }

    [Theory]
    [InlineData(6, 50, 100)]   // girada 90°: largura e altura trocam
    [InlineData(8, 50, 100)]
    [InlineData(3, 100, 50)]   // 180°: mesmas dimensões
    [InlineData(1, 100, 50)]   // normal
    public void Normalize_DeveCorrigirARotacaoDoCelular(byte orientation, int expectedWidth, int expectedHeight)
    {
        var jpeg = WithExifOrientation(Encode(100, 50, SKEncodedImageFormat.Jpeg), orientation);

        var result = ReceiptImageNormalizer.Normalize(jpeg);

        (result.Width, result.Height).Should().Be((expectedWidth, expectedHeight));
    }

    [Fact]
    public void Normalize_DeveRemoverOExifDaSaida()
    {
        var jpeg = WithExifOrientation(Encode(100, 50, SKEncodedImageFormat.Jpeg), 6);

        var result = ReceiptImageNormalizer.Normalize(jpeg);

        // A saída é regravada do zero: não sobra APP1/Exif (onde ficam GPS e modelo do aparelho)
        System.Text.Encoding.Latin1.GetString(result.Data).Should().NotContain("Exif");
        using var codec = SKCodec.Create(SKData.CreateCopy(result.Data));
        codec.EncodedOrigin.Should().Be(SKEncodedOrigin.TopLeft);
    }

    [Fact]
    public void Normalize_DeveLancarValidacao_QuandoOArquivoNaoEImagem()
    {
        var act = () => ReceiptImageNormalizer.Normalize("isto é um texto, não uma foto"u8.ToArray());

        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Normalize_DeveLancarValidacao_QuandoVazia()
    {
        var act = () => ReceiptImageNormalizer.Normalize([]);

        act.Should().Throw<ValidationException>().WithMessage("*vazia*");
    }

    [Fact]
    public void Normalize_DeveRecusarFormatoNaoSuportado()
    {
        // GIF 1x1 válido
        var gif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

        var act = () => ReceiptImageNormalizer.Normalize(gif);

        act.Should().Throw<ValidationException>().WithMessage("*não suportado*");
    }

    [Fact]
    public void Normalize_DeveRecusarImagemQueDeclaraPixelsDemais()
    {
        // Cabeçalho PNG válido que declara 10000x10000 (100 MP) e não traz dados: nunca deve ser decodificada
        byte[] header =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x27, 0x10, 0x00, 0x00, 0x27, 0x10,
            0x08, 0x06, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];

        var act = () => ReceiptImageNormalizer.Normalize(header);

        act.Should().Throw<ValidationException>();
    }
}
