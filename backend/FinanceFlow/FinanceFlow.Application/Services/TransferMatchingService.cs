using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;

namespace FinanceFlow.Application.Services;

public interface ITransferMatchingService
{
    /// <summary>
    /// Procura, para cada linha do extrato, a outra ponta da transferência em lançamentos de OUTRAS contas
    /// do mesmo usuário e grava o resultado no staging (candidato, pontuação e confiança).
    /// É idempotente: limpa os pareamentos anteriores antes de recalcular e ignora linhas duplicadas.
    /// Retorna a quantidade de linhas com par (automático ou sugerido).
    /// </summary>
    Task<int> MatchAsync(BankImport bankImport, CancellationToken cancellationToken = default);
}

public class TransferMatchingService(ITransferRepository transferRepository) : ITransferMatchingService
{
    /// <summary>Diferença máxima (em dias) entre as duas pontas de uma transferência.</summary>
    public const int MaxDaysApart = 3;

    /// <summary>A partir desta pontuação o par é vinculado sozinho (o usuário ainda pode desfazer).</summary>
    public const int AutoThreshold = 80;

    /// <summary>Entre este valor e <see cref="AutoThreshold"/> o par vira apenas uma sugestão.</summary>
    public const int SuggestedThreshold = 50;

    public async Task<int> MatchAsync(BankImport bankImport, CancellationToken cancellationToken = default)
    {
        var rows = bankImport.Transactions.ToList();

        foreach (var row in rows)
        {
            row.TransferMatchTransactionId = null;
            row.TransferMatchScore = null;
            row.TransferMatchStatus = null;
        }

        // Linha duplicada já existe na conta: não é a ponta de uma transferência nova
        var eligible = rows
            .Where(r => !r.IsDuplicate && DirectionOf(r.Type, null) is not null)
            .ToList();

        if (eligible.Count == 0)
            return 0;

        var from = eligible.Min(r => r.Date).Date.AddDays(-MaxDaysApart);
        var to = eligible.Max(r => r.Date).Date.AddDays(MaxDaysApart + 1).AddTicks(-1);

        var candidates = await transferRepository.GetCandidatesAsync(
            bankImport.UserId, bankImport.AccountId, from, to, cancellationToken);

        if (candidates.Count == 0)
            return 0;

        // 1) Todos os pares possíveis com pontuação suficiente
        var pairs = new List<Pair>();

        foreach (var row in eligible)
        {
            var rowDirection = DirectionOf(row.Type, null)!.Value;

            foreach (var candidate in candidates)
            {
                var score = Score(row, rowDirection, candidate);
                if (score >= SuggestedThreshold)
                    pairs.Add(new Pair(row, candidate, score));
            }
        }

        if (pairs.Count == 0)
            return 0;

        // 2) Ambiguidade: dois candidatos empatados para a mesma linha (ou duas linhas empatadas
        //    para o mesmo candidato) nunca viram vínculo automático — o usuário escolhe.
        var bestByRow = pairs.GroupBy(p => p.Row.Id).ToDictionary(g => g.Key, g => g.Max(p => p.Score));
        var bestByCandidate = pairs.GroupBy(p => p.Candidate.Id).ToDictionary(g => g.Key, g => g.Max(p => p.Score));

        var tiedRows = pairs
            .GroupBy(p => p.Row.Id)
            .Where(g => g.Count(p => p.Score == bestByRow[g.Key]) > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var tiedCandidates = pairs
            .GroupBy(p => p.Candidate.Id)
            .Where(g => g.Count(p => p.Score == bestByCandidate[g.Key]) > 1)
            .Select(g => g.Key)
            .ToHashSet();

        // 3) Atribuição gulosa: maiores pontuações primeiro; cada linha e cada candidato só entram uma vez
        var usedRows = new HashSet<Guid>();
        var usedCandidates = new HashSet<Guid>();
        var matched = 0;

        foreach (var pair in pairs
                     .OrderByDescending(p => p.Score)
                     .ThenBy(p => Math.Abs((p.Candidate.Date.Date - p.Row.Date.Date).TotalDays))
                     .ThenBy(p => p.Row.Date))
        {
            if (usedRows.Contains(pair.Row.Id) || usedCandidates.Contains(pair.Candidate.Id))
                continue;

            var ambiguous = tiedRows.Contains(pair.Row.Id) || tiedCandidates.Contains(pair.Candidate.Id);

            pair.Row.TransferMatchTransactionId = pair.Candidate.Id;
            pair.Row.TransferMatchScore = pair.Score;
            pair.Row.TransferMatchStatus = pair.Score >= AutoThreshold && !ambiguous
                ? TransferMatchStatus.Auto
                : TransferMatchStatus.Suggested;

            usedRows.Add(pair.Row.Id);
            usedCandidates.Add(pair.Candidate.Id);
            matched++;
        }

        return matched;
    }

    /// <summary>
    /// Pontuação (0–100) de um par. Retorna 0 quando algum critério obrigatório falha:
    /// valor idêntico, sentidos opostos e no máximo <see cref="MaxDaysApart"/> dias de diferença.
    /// </summary>
    public static int Score(BankImportTransaction row, TransferDirection rowDirection, Transaction candidate)
    {
        var candidateDirection = DirectionOf(candidate.Type, candidate.TransferDirection);

        if (candidateDirection is null || candidateDirection == rowDirection)
            return 0;

        return Score(
            row.Amount, row.Date, row.Description,
            candidate.Amount, candidate.Date, candidate.Description,
            candidate.Type == TransactionType.Transfer);
    }

    public static int Score(
        decimal amount, DateTime date, string description,
        decimal candidateAmount, DateTime candidateDate, string candidateDescription,
        bool candidateIsTransfer)
    {
        if (amount != candidateAmount)
            return 0;

        var daysApart = (int)Math.Abs((candidateDate.Date - date.Date).TotalDays);
        if (daysApart > MaxDaysApart)
            return 0;

        var score = daysApart switch
        {
            0 => 45,
            1 => 35,
            2 => 25,
            _ => 15
        };

        var keywordMatches = (LooksLikeTransfer(description) ? 1 : 0)
                             + (LooksLikeTransfer(candidateDescription) ? 1 : 0);

        score += keywordMatches switch
        {
            2 => 35,
            1 => 15,
            _ => 0
        };

        // Quem já lançou como transferência demonstrou a intenção: o sinal mais forte
        if (candidateIsTransfer)
            score += 20;

        return score;
    }

    /// <summary>Sentido da ponta: receita entra, despesa sai; transferência usa a direção gravada.</summary>
    public static TransferDirection? DirectionOf(TransactionType type, TransferDirection? transferDirection) =>
        type switch
        {
            TransactionType.Income => TransferDirection.In,
            TransactionType.Expense => TransferDirection.Out,
            TransactionType.Transfer => transferDirection,
            _ => null
        };

    private static readonly Regex TransferKeywords = new(
        @"\b(PIX|TED|DOC|TRANSF[A-Z]*)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Descrições com PIX/TED/DOC/TRANSFERÊNCIA sugerem movimentação entre contas.</summary>
    public static bool LooksLikeTransfer(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return false;

        return TransferKeywords.IsMatch(RemoveDiacritics(description).ToUpperInvariant());
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private sealed record Pair(BankImportTransaction Row, Transaction Candidate, int Score);
}
