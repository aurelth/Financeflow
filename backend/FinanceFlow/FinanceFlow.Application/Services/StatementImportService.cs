using System.Text;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.Events;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace FinanceFlow.Application.Services;

/// <summary>
/// Cria a importação (staging) a partir de um extrato já lido, seja OFX ou CSV: resolve a conta, sugere
/// categorias, calcula os hashes de deduplicação e avisa o Worker. Tudo que vem depois (preview,
/// duplicatas, transferências, confirmação) é igual para os dois formatos.
/// </summary>
public interface IStatementImportService
{
    Task<BankImportDto> CreateAsync(
        Guid userId,
        string fileName,
        Guid? accountId,
        OFXParseResult parseResult,
        CancellationToken cancellationToken);
}

public class StatementImportService(
    IBankImportRepository bankImportRepository,
    ICategoryRepository categoryRepository,
    ICategoryRuleRepository categoryRuleRepository,
    IAccountRepository accountRepository,
    IAccountKeyHasher accountKeyHasher,
    IEventPublisher eventPublisher,
    IConfiguration configuration)
    : IStatementImportService
{
    // Palavras-chave para categorização automática
    private static readonly Dictionary<string, string[]> KeywordMap = new()
    {
        { "alimentacao", ["mercado", "supermercado", "restaurante", "lanche", "padaria", "ifood", "rappi", "uber eats", "food"] },
        { "transporte",  ["uber", "99", "taxi", "onibus", "metro", "combustivel", "gasolina", "posto", "estacionamento"] },
        { "saude",       ["farmacia", "drogaria", "hospital", "clinica", "medico", "laboratorio", "exame"] },
        { "educacao",    ["escola", "faculdade", "universidade", "curso", "livro", "livraria"] },
        { "lazer",       ["cinema", "teatro", "netflix", "spotify", "steam", "jogo", "viagem", "hotel"] },
        { "moradia",     ["aluguel", "condominio", "luz", "agua", "energia", "internet", "telefone"] },
        { "vestuario",   ["roupa", "calcado", "tenis", "camisa", "calca", "vestido"] },
    };

    public async Task<BankImportDto> CreateAsync(
        Guid userId,
        string fileName,
        Guid? accountId,
        OFXParseResult parseResult,
        CancellationToken cancellationToken)
    {
        // Define a conta da importação (escolhida, reconhecida pelo OFX ou "Conta principal")
        var (account, resolution) = await ResolveAccountAsync(userId, accountId, parseResult, cancellationToken);

        // Busca categorias padrão para sugestão automática (palavras-chave)
        var defaultCategories = await categoryRepository.GetAllDefaultAsync(cancellationToken);
        var categoryList = defaultCategories.ToList();

        // Regras aprendidas com as confirmações anteriores do usuário têm prioridade sobre as palavras-chave
        var learnedRules = await LoadLearnedRulesAsync(userId, parseResult, cancellationToken);

        // Cria o BankImport
        var bankImport = new BankImport
        {
            UserId = userId,
            AccountId = account.Id,
            FileName = fileName,
            Status = BankImportStatus.Pending,
            TotalRecords = parseResult.Transactions.Count(),
            LedgerBalance = parseResult.LedgerBalance,
            LedgerBalanceDate = parseResult.LedgerBalanceDate,
            StatementStart = ValidDate(parseResult.StartDate),
            StatementEnd = ValidDate(parseResult.EndDate),
        };

        // Guarda a identificação do arquivo (já em hash) para aprender/validar a conta ao trocá-la no preview
        if (!string.IsNullOrWhiteSpace(parseResult.AccountId))
        {
            bankImport.BankId = string.IsNullOrWhiteSpace(parseResult.BankId) ? null : parseResult.BankId.Trim();
            bankImport.AccountKeyHash = accountKeyHasher.ComputeKey(userId, bankImport.BankId, parseResult.AccountId);
            bankImport.AccountLast4 = accountKeyHasher.Last4(parseResult.AccountId);
        }

        // Linhas idênticas no mesmo arquivo recebem hashes distintos (1ª, 2ª, 3ª ocorrência...)
        var occurrences = new Dictionary<string, int>();

        // Mapeia as transações extraídas
        foreach (var t in parseResult.Transactions)
        {
            var baseHash = ImportHashing.ComputeBaseHash(t.Date, t.Amount, t.Description, t.Type);
            occurrences[baseHash] = occurrences.GetValueOrDefault(baseHash) + 1;
            var hash = ImportHashing.WithOccurrence(baseHash, occurrences[baseHash]);

            // Usa o TRNTYPE do OFX para determinar o tipo, com fallback para o valor
            var type = TypeOf(t);

            // Categorização: regra aprendida do usuário > palavras-chave
            var (suggestedCategoryId, suggestionSource) = Suggest(t.Description, type, learnedRules, categoryList);

            bankImport.Transactions.Add(new BankImportTransaction
            {
                ExternalId = t.FitId,
                Date = t.Date,
                Amount = Math.Abs(t.Amount),
                Description = t.Description,
                Type = type,
                Hash = hash,
                SuggestedCategoryId = suggestedCategoryId,
                SuggestionSource = suggestionSource,
                IsDuplicate = false,
                IsSelected = true,
            });
        }

        await bankImportRepository.AddAsync(bankImport, cancellationToken);

        // Publica evento Kafka para o Worker processar
        var topic = configuration["Kafka:Topics:BankImportCreated"] ?? "finance.bankimport.created";
        await eventPublisher.PublishAsync(topic, new BankImportCreatedEvent(
            ImportId: bankImport.Id,
            UserId: bankImport.UserId,
            FileName: bankImport.FileName,
            CreatedAt: bankImport.CreatedAt),
            cancellationToken);

        return new BankImportDto(
            Id: bankImport.Id,
            FileName: bankImport.FileName,
            Status: bankImport.Status,
            TotalRecords: bankImport.TotalRecords,
            Imported: 0,
            Duplicates: 0,
            Errors: 0,
            ErrorMessage: null,
            CreatedAt: bankImport.CreatedAt,
            AccountId: account.Id,
            AccountName: account.Name,
            AccountResolution: resolution);
    }

    private static TransactionType TypeOf(OFXTransaction t) =>
        t.Type.Equals("CREDIT", StringComparison.OrdinalIgnoreCase)
            ? TransactionType.Income
            : t.Type.Equals("DEBIT", StringComparison.OrdinalIgnoreCase)
                ? TransactionType.Expense
                : t.Amount >= 0 ? TransactionType.Income : TransactionType.Expense;

    /// <summary>
    /// Regras do usuário que valem para este arquivo: só entram as que apontam para uma categoria
    /// ainda visível para ele (não excluída, arquivada ou de meta) e do mesmo tipo da regra.
    /// </summary>
    private async Task<Dictionary<(TransactionType Type, string Key), Guid>> LoadLearnedRulesAsync(
        Guid userId,
        OFXParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var keys = parseResult.Transactions
            .Select(t => DescriptionNormalizer.ToKey(t.Description))
            .Where(k => k.Length > 0)
            .Distinct()
            .ToList();

        var result = new Dictionary<(TransactionType, string), Guid>();
        if (keys.Count == 0)
            return result;

        var rules = await categoryRuleRepository.GetByKeysAsync(userId, keys, cancellationToken);
        if (rules.Count == 0)
            return result;

        var visible = (await categoryRepository.GetAllByUserAsync(userId, cancellationToken))
            .ToDictionary(c => c.Id, c => c.Type);

        foreach (var rule in rules)
        {
            if (visible.TryGetValue(rule.CategoryId, out var categoryType) && categoryType == rule.Type)
                result[(rule.Type, rule.Key)] = rule.CategoryId;
        }

        return result;
    }

    private static (Guid? CategoryId, CategorySuggestionSource? Source) Suggest(
        string description,
        TransactionType type,
        Dictionary<(TransactionType Type, string Key), Guid> learnedRules,
        List<Category> defaultCategories)
    {
        var key = DescriptionNormalizer.ToKey(description);
        if (key.Length > 0 && learnedRules.TryGetValue((type, key), out var learned))
            return (learned, CategorySuggestionSource.Rule);

        var byKeyword = SuggestCategory(description, defaultCategories, type);
        return byKeyword is { } id ? (id, CategorySuggestionSource.Keyword) : (null, null);
    }

    // O parser devolve a data mínima quando o período não vem no arquivo
    private static DateTime? ValidDate(DateTime date) => date == default ? null : date;

    // Prioridade: conta escolhida pelo usuário > conta reconhecida pelo OFX > "Conta principal"
    private async Task<(Account Account, string Resolution)> ResolveAccountAsync(
        Guid userId,
        Guid? requestedAccountId,
        OFXParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var bankId = string.IsNullOrWhiteSpace(parseResult.BankId) ? null : parseResult.BankId.Trim();
        var hasKey = !string.IsNullOrWhiteSpace(parseResult.AccountId);
        var key = hasKey
            ? accountKeyHasher.ComputeKey(userId, bankId, parseResult.AccountId)
            : null;

        if (requestedAccountId.HasValue)
        {
            var chosen = await accountRepository.GetByIdAsync(
                requestedAccountId.Value, userId, cancellationToken)
                ?? throw new NotFoundException(nameof(Account), requestedAccountId.Value);

            if (chosen.IsArchived)
                throw new ValidationException("Não é possível importar para uma conta arquivada.");

            if (key is null)
                return (chosen, AccountResolutions.Provided);

            var owner = await accountRepository.GetByKeyAsync(
                userId, bankId, key, cancellationToken);

            // O arquivo já pertence a outra conta do usuário: evita importar na conta errada
            if (owner is not null && owner.Id != chosen.Id)
                throw new ValidationException(
                    $"Este arquivo pertence à conta \"{owner.Name}\". Selecione essa conta para importar.");

            if (string.IsNullOrEmpty(chosen.AccountKeyHash))
            {
                chosen.AccountKeyHash = key;
                chosen.BankId ??= bankId;
                chosen.AccountLast4 = accountKeyHasher.Last4(parseResult.AccountId);
                await accountRepository.UpdateAsync(chosen, cancellationToken);
                return (chosen, AccountResolutions.Learned);
            }

            return (chosen, AccountResolutions.Provided);
        }

        if (key is not null)
        {
            var recognized = await accountRepository.GetByKeyAsync(
                userId, bankId, key, cancellationToken);

            if (recognized is not null)
            {
                if (recognized.IsArchived)
                    throw new ValidationException(
                        $"A conta \"{recognized.Name}\" está arquivada. Reative-a para importar este arquivo.");

                return (recognized, AccountResolutions.Recognized);
            }
        }

        var fallback = await accountRepository.GetOrCreateDefaultAsync(userId, cancellationToken);
        return (fallback, AccountResolutions.Defaulted);
    }

    // Recebe o tipo para filtrar categorias do tipo correto
    private static Guid? SuggestCategory(string description, List<Category> categories, TransactionType type)
    {
        var normalized = RemoveAccents(description.ToLowerInvariant());

        foreach (var (keyword, terms) in KeywordMap)
        {
            if (terms.Any(term => normalized.Contains(term)))
            {
                // Filtra apenas categorias do mesmo tipo da transação
                var match = categories.FirstOrDefault(c =>
                    c.Type == type &&
                    RemoveAccents(c.Name.ToLowerInvariant()).Contains(keyword));
                if (match is not null)
                    return match.Id;
            }
        }

        return null;
    }

    private static string RemoveAccents(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        return new string(normalized
            .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                        != System.Globalization.UnicodeCategory.NonSpacingMark)
            .ToArray());
    }
}
