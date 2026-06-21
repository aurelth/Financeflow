namespace FinanceFlow.Application.Common.Interfaces;

public interface IAvatarService
{
    /// <summary>Salva e redimensiona o avatar para 256x256px. Retorna o path relativo.</summary>
    Task<string> SaveAsync(
        Stream fileStream,
        string contentType,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Exclui o arquivo de avatar pelo path relativo.</summary>
    void Delete(string avatarPath);

    /// <summary>Retorna a URL pública do avatar.</summary>
    string GetUrl(string avatarPath);
}
