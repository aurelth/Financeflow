namespace FinanceFlow.Domain.Interfaces;

public interface IPasswordService
{
    /// <summary>
    /// Gera um hash BCrypt da password com cost factor 12.
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifica se a password corresponde ao hash armazenado.
    /// </summary>
    bool Verify(string password, string hash);
}
