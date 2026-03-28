namespace FinanceFlow.Application.Common.Interfaces;

public interface IAttachmentService
{
    /// <summary>
    /// Salva o anexo em disco e retorna o caminho relativo.
    /// </summary>
    Task<string> SaveAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o anexo do disco.
    /// </summary>
    Task DeleteAsync(string attachmentPath);

    /// <summary>
    /// Retorna o caminho absoluto do anexo para download.
    /// </summary>
    string GetAbsolutePath(string attachmentPath);
}
