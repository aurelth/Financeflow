using FinanceFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FinanceFlow.Infrastructure.Storage;

public class LocalAttachmentService(IConfiguration configuration) : IAttachmentService
{
    // Tipos permitidos: imagem e PDF
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf"
    ];

    // Tamanho máximo: 10MB
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    private string BasePath =>
        Path.Combine(
            Directory.GetCurrentDirectory(),
            configuration["Storage:BasePath"] ?? "storage",
            "attachments");

    public async Task<string> SaveAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Valida tipo de ficheiro
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException(
                "Tipo de ficheiro não permitido. Apenas imagens (JPEG, PNG, WebP) e PDF são aceites.");

        // Valida tamanho
        if (fileStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException(
                "O ficheiro excede o tamanho máximo permitido de 10MB.");

        // Cria pasta do utilizador se não existir
        var userFolder = Path.Combine(BasePath, userId.ToString());
        Directory.CreateDirectory(userFolder);

        // Gera nome único preservando a extensão
        var extension = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(userFolder, uniqueName);

        await using var fileStreamOut = File.Create(fullPath);
        await fileStream.CopyToAsync(fileStreamOut, cancellationToken);

        // Retorna caminho relativo para guardar na base de dados
        return Path.Combine("attachments", userId.ToString(), uniqueName)
                   .Replace("\\", "/");
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
            Directory.GetCurrentDirectory(),
            configuration["Storage:BasePath"] ?? "storage",
            attachmentPath);
}
