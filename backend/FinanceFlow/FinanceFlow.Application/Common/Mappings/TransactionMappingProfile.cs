using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using System.Text.Json;

namespace FinanceFlow.Application.Common.Mappings;

public class TransactionMappingProfile : Profile
{
    public TransactionMappingProfile()
    {
        // Transaction → TransactionDto
        CreateMap<Transaction, TransactionDto>()
            .ForCtorParam(nameof(TransactionDto.CategoryName),
                opt => opt.MapFrom(src => src.Category.Name))
            .ForCtorParam(nameof(TransactionDto.CategoryIcon),
                opt => opt.MapFrom(src => src.Category.Icon))
            .ForCtorParam(nameof(TransactionDto.CategoryColor),
                opt => opt.MapFrom(src => src.Category.Color))
            .ForCtorParam(nameof(TransactionDto.SubcategoryName),
                opt => opt.MapFrom(src =>
                    src.Subcategory != null ? src.Subcategory.Name : null))
            .ForCtorParam(nameof(TransactionDto.AccountName),
                opt => opt.MapFrom(src =>
                    src.Account != null ? src.Account.Name : null))
            .ForCtorParam(nameof(TransactionDto.TransferLinkId),
                opt => opt.MapFrom(src => TransferInfo.LinkId(src)))
            .ForCtorParam(nameof(TransactionDto.CounterpartTransactionId),
                opt => opt.MapFrom(src => TransferInfo.CounterpartId(src)))
            .ForCtorParam(nameof(TransactionDto.CounterpartAccountId),
                opt => opt.MapFrom(src => TransferInfo.CounterpartAccountId(src)))
            .ForCtorParam(nameof(TransactionDto.CounterpartAccountName),
                opt => opt.MapFrom(src => TransferInfo.CounterpartAccountName(src)))
            .ForCtorParam(nameof(TransactionDto.Tags),
                opt => opt.MapFrom(src =>
                    JsonSerializer.Deserialize<string[]>(
                        src.Tags, (JsonSerializerOptions?)null) ?? Array.Empty<string>()));
    }
}

/// <summary>
/// Dados da outra ponta de uma transferência. Ficam em métodos (e não inline no mapa)
/// porque árvores de expressão não aceitam o operador "?.".
/// </summary>
internal static class TransferInfo
{
    private static Transaction? Counterpart(Transaction t) =>
        t.LinkAsOut?.InTransaction ?? t.LinkAsIn?.OutTransaction;

    public static Guid? LinkId(Transaction t) => t.LinkAsOut?.Id ?? t.LinkAsIn?.Id;

    public static Guid? CounterpartId(Transaction t) =>
        t.LinkAsOut?.InTransactionId ?? t.LinkAsIn?.OutTransactionId;

    public static Guid? CounterpartAccountId(Transaction t) => Counterpart(t)?.AccountId;

    public static string? CounterpartAccountName(Transaction t) => Counterpart(t)?.Account?.Name;
}
