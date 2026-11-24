using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using System.Text.Json;

namespace FinanceFlow.Application.Common.Mappings;

/// <summary>
/// Mapeamento Transaction → TransactionDto. Exige Category carregada e, para os dados da
/// transferência, os dois lados do vínculo com a conta da outra ponta (o TransactionRepository já faz os Includes).
/// </summary>
public static class TransactionMappings
{
    public static TransactionDto ToDto(this Transaction transaction)
    {
        var counterpart = Counterpart(transaction);

        return new TransactionDto(
            Id:                      transaction.Id,
            Amount:                  transaction.Amount,
            Type:                    transaction.Type,
            Date:                    transaction.Date,
            Description:             transaction.Description,
            Status:                  transaction.Status,
            IsRecurring:             transaction.IsRecurring,
            RecurrenceType:          transaction.RecurrenceType,
            RecurrenceGroupId:       transaction.RecurrenceGroupId,
            AttachmentPath:          transaction.AttachmentPath,
            AttachmentName:          transaction.AttachmentName,
            Tags:                    ParseTags(transaction.Tags),
            CategoryId:              transaction.CategoryId,
            CategoryName:            transaction.Category.Name,
            CategoryIcon:            transaction.Category.Icon,
            CategoryColor:           transaction.Category.Color,
            SubcategoryId:           transaction.SubcategoryId,
            SubcategoryName:         transaction.Subcategory?.Name,
            CreatedAt:               transaction.CreatedAt,
            UpdatedAt:               transaction.UpdatedAt,
            AccountId:               transaction.AccountId,
            AccountName:             transaction.Account?.Name,
            TransferDirection:       transaction.TransferDirection,
            TransferLinkId:          transaction.LinkAsOut?.Id ?? transaction.LinkAsIn?.Id,
            CounterpartTransactionId: transaction.LinkAsOut?.InTransactionId ?? transaction.LinkAsIn?.OutTransactionId,
            CounterpartAccountId:    counterpart?.AccountId,
            CounterpartAccountName:  counterpart?.Account?.Name,
            InstallmentGroupId:      transaction.InstallmentGroupId,
            InstallmentNumber:       transaction.InstallmentNumber,
            InstallmentCount:        transaction.InstallmentCount);
    }

    public static List<TransactionDto> ToDtos(this IEnumerable<Transaction> transactions) =>
        transactions.Select(ToDto).ToList();

    // A outra ponta de uma transferência: se esta é a saída, a entrada, e vice-versa
    private static Transaction? Counterpart(Transaction t) =>
        t.LinkAsOut?.InTransaction ?? t.LinkAsIn?.OutTransaction;

    // Tags ficam serializadas em JSON; vazio (lançamento antigo ou criado sem tags) vale lista vazia
    private static string[] ParseTags(string tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : JsonSerializer.Deserialize<string[]>(tags, (JsonSerializerOptions?)null) ?? [];
}
