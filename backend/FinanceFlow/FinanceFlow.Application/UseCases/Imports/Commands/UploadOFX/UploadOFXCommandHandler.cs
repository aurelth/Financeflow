using System.Security.Cryptography;
using System.Text;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.Events;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace FinanceFlow.Application.UseCases.Imports.Commands.UploadOFX;

public class UploadOFXCommandHandler(
    IBankImportRepository bankImportRepository,
    IOFXParserService ofxParserService,
    ICategoryRepository categoryRepository,
    IEventPublisher eventPublisher,
    IConfiguration configuration)
    : IRequestHandler<UploadOFXCommand, BankImportDto>
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

    public async Task<BankImportDto> Handle(
        UploadOFXCommand request,
        CancellationToken cancellationToken)
    {
        // Parsing do OFX
        var parseResult = ofxParserService.Parse(request.FileStream);

        // Busca categorias padrão para sugestão automática
        var defaultCategories = await categoryRepository.GetAllDefaultAsync(cancellationToken);
        var categoryList = defaultCategories.ToList();

        // Cria o BankImport
        var bankImport = new BankImport
        {
            UserId = request.UserId,
            FileName = request.FileName,
            Status = BankImportStatus.Pending,
            TotalRecords = parseResult.Transactions.Count(),
        };

        // Mapeia as transações extraídas
        foreach (var t in parseResult.Transactions)
        {
            Console.WriteLine($"FitId: {t.FitId} | Type: '{t.Type}' | Amount: {t.Amount}");
            var hash = ComputeHash(t.Date, t.Amount, t.Description, t.Type);

            // Usa o TRNTYPE do OFX para determinar o tipo, com fallback para o valor
            var type = t.Type.Equals("CREDIT", StringComparison.OrdinalIgnoreCase)
                ? TransactionType.Income
                : t.Type.Equals("DEBIT", StringComparison.OrdinalIgnoreCase)
                    ? TransactionType.Expense
                    : t.Amount >= 0 ? TransactionType.Income : TransactionType.Expense;

            // Categorização automática por palavras-chave
            var suggestedCategoryId = SuggestCategory(t.Description, categoryList, type);

            bankImport.Transactions.Add(new BankImportTransaction
            {
                ExternalId = t.FitId,
                Date = t.Date,
                Amount = Math.Abs(t.Amount),
                Description = t.Description,
                Type = type,
                Hash = hash,
                SuggestedCategoryId = suggestedCategoryId,
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
            CreatedAt: bankImport.CreatedAt);
    }

    private static string ComputeHash(DateTime date, decimal amount, string description, string type)
    {
        var raw = $"{date:yyyy-MM-dd}|{amount}|{description.Trim().ToUpperInvariant()}|{type}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
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
