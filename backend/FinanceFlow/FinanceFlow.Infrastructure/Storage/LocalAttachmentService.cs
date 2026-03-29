using FinanceFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace FinanceFlow.Infrastructure.Storage;

public class LocalAttachmentService(
    IConfiguration configuration,
    IWebHostEnvironment environment) : IAttachmentService
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    ];

    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private string BasePath =>
        Path.Combine(
            environment.ContentRootPath,
            configuration["Storage:BasePath"] ?? "storage",
            "attachments");

    public async Task<(string Path, string Name)> SaveAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException(
                "Tipo de ficheiro não permitido. Apenas imagens (JPEG, PNG, WebP) e PDF são aceites.");

        if (fileStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException(
                "O ficheiro excede o tamanho máximo permitido de 10MB.");

        var userFolder = Path.Combine(BasePath, userId.ToString());
        Directory.CreateDirectory(userFolder);

        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(userFolder, uniqueName);

        await using var fileStreamOut = File.Create(fullPath);
        await fileStream.CopyToAsync(fileStreamOut, cancellationToken);

        return (
            Path: Path.Combine("attachments", userId.ToString(), uniqueName).Replace("\\", "/"),
            Name: fileName
        );
    }

    public Task DeleteAsync(string attachmentPath)
    {
        var fullPath = GetAbsolutePath(attachmentPath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetAbsolutePath(string attachmentPath) =>
        Path.Combine(
            environment.ContentRootPath,
            configuration["Storage:BasePath"] ?? "storage",
            attachmentPath);
}
