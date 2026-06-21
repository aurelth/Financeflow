using FinanceFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace FinanceFlow.Application.Services;

public class AvatarService(
    IConfiguration configuration,
    ILogger<AvatarService> logger) : IAvatarService
{
    private readonly string _storagePath = configuration["Avatar:StoragePath"]
        ?? "/var/www/financeflow/uploads/avatars";

    private readonly string _baseUrl = configuration["Avatar:BaseUrl"]
        ?? "https://financeflowapp.io/uploads/avatars";

    public async Task<string> SaveAsync(
        Stream fileStream,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_storagePath);

        var extension = contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "jpg"
        };

        var fileName = $"{userId}.{extension}";
        var fullPath = Path.Combine(_storagePath, fileName);

        // Lê o stream em memória
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        // Redimensiona para 256x256px usando SkiaSharp
        using var original = SKBitmap.Decode(ms);
        using var resized = original.Resize(new SKImageInfo(256, 256), SKSamplingOptions.Default);
        using var skImage = SKImage.FromBitmap(resized);

        var format = extension switch
        {
            "png" => SKEncodedImageFormat.Png,
            "webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg
        };

        using var data = skImage.Encode(format, 90);
        await File.WriteAllBytesAsync(fullPath, data.ToArray(), cancellationToken);

        logger.LogInformation("Avatar salvo em {Path}", fullPath);

        return fileName;
    }

    public void Delete(string avatarPath)
    {
        var fullPath = Path.Combine(_storagePath, avatarPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            logger.LogInformation("Avatar excluído: {Path}", fullPath);
        }
    }

    public string GetUrl(string avatarPath) =>
        $"{_baseUrl.TrimEnd('/')}/{avatarPath}";
}
