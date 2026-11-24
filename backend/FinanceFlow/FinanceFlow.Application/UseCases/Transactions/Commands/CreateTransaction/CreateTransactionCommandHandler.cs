using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.Events;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using FinanceFlow.Application.Common.Mappings;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.CreateTransaction;

public class CreateTransactionCommandHandler(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IAccountRepository accountRepository,
    IEventPublisher eventPublisher,
    IAttachmentService attachmentService,
    IConfiguration configuration,
    IDashboardCacheInvalidator dashboardCacheInvalidator)
    : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(
            request.CategoryId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        if (category.Type != request.Type)
            throw new ValidationException(
                "O tipo da transação não coincide com o tipo da categoria.");

        // Uma transferência avulsa precisa dizer se o dinheiro entra ou sai desta conta; a outra ponta
        // é lançada na outra conta (ou vinculada depois). Para as duas pontas de uma vez, use POST /api/transfers.
        if (request.Type == TransactionType.Transfer && request.TransferDirection is null)
            throw new ValidationException("Informe se a transferência é uma entrada ou uma saída desta conta.");

        var account = request.AccountId.HasValue
            ? await accountRepository.GetByIdAsync(request.AccountId.Value, request.UserId, cancellationToken)
                ?? throw new NotFoundException(nameof(Account), request.AccountId.Value)
            : await accountRepository.GetOrCreateDefaultAsync(request.UserId, cancellationToken);

        if (account.IsArchived)
            throw new ValidationException("Não é possível lançar em uma conta arquivada.");

        // Processa o anexo se fornecido
        string? attachmentPath = null;
        string? attachmentName = null;
        if (request.AttachmentStream != null &&
            request.AttachmentFileName != null &&
            request.AttachmentContentType != null)
        {
            var result = await attachmentService.SaveAsync(
                request.AttachmentStream,
                request.AttachmentFileName,
                request.AttachmentContentType,
                request.UserId,
                cancellationToken);
            attachmentPath = result.Path;
            attachmentName = result.Name;
        }

        // Gera o RecurrenceGroupId se a transação for recorrente
        Guid? recurrenceGroupId = request.IsRecurring ? Guid.NewGuid() : null;

        // Compra parcelada: Amount é o total; cada parcela leva a sua fatia (a soma fecha o total)
        var installmentCount = request.InstallmentCount;
        var installmentAmounts = installmentCount.HasValue
            ? InstallmentPlanner.Split(request.Amount, installmentCount.Value)
            : null;
        Guid? installmentGroupId = installmentCount.HasValue ? Guid.NewGuid() : null;

        var transaction = new Transaction
        {
            UserId = request.UserId,
            Amount = installmentAmounts?[0] ?? request.Amount,
            Type = request.Type,
            TransferDirection = request.Type == TransactionType.Transfer ? request.TransferDirection : null,
            Date = request.Date,
            Description = request.Description,
            Status = request.Status,
            IsRecurring = request.IsRecurring,
            RecurrenceType = installmentCount.HasValue ? RecurrenceType.None : request.RecurrenceType,
            AccountId = account.Id,
            CategoryId = request.CategoryId,
            SubcategoryId = request.SubcategoryId,
            Tags = JsonSerializer.Serialize(request.Tags),
            AttachmentPath = attachmentPath,
            AttachmentName = attachmentName,
            RecurrenceGroupId = recurrenceGroupId,
            InstallmentGroupId = installmentGroupId,
            InstallmentNumber = installmentCount.HasValue ? 1 : null,
            InstallmentCount = installmentCount,
        };

        // Todas as datas afetadas, para invalidar o cache do dashboard de cada mês
        var affectedDates = new List<DateTime> { request.Date };

        if (installmentAmounts is not null)
        {
            // A primeira parcela já é a própria transação (com o status e o anexo escolhidos); as demais
            // ficam Agendadas nos meses seguintes. Tudo entra num único SaveChanges: ou todas as parcelas, ou nenhuma.
            var parcels = new List<Transaction> { transaction };
            parcels.AddRange(GerarParcelasRestantes(transaction, installmentAmounts, request.Tags));

            await transactionRepository.AddRangeAsync(parcels, cancellationToken);
            affectedDates.AddRange(parcels.Select(p => p.Date));
        }
        else
        {
            await transactionRepository.AddAsync(transaction, cancellationToken);

            // Gera cópias para os meses restantes do ano se recorrente
            if (request.IsRecurring)
            {
                var copies = GerarCopiasRecorrentes(transaction, request.Tags);
                foreach (var copy in copies)
                    await transactionRepository.AddAsync(copy, cancellationToken);
            }
        }

        // Invalida o cache do dashboard de cada mês tocado
        await dashboardCacheInvalidator.InvalidateAsync(request.UserId, affectedDates, cancellationToken);

        var topic = configuration["Kafka:Topics:TransactionCreated"]
                    ?? "finance.transactions.created";

        await eventPublisher.PublishAsync(topic, new TransactionCreatedEvent(
            TransactionId: transaction.Id,
            UserId: transaction.UserId,
            Amount: transaction.Amount,
            Type: transaction.Type,
            Date: transaction.Date,
            Description: transaction.Description,
            Status: transaction.Status,
            CategoryId: transaction.CategoryId,
            CategoryName: category.Name,
            CreatedAt: transaction.CreatedAt),
            cancellationToken);

        var created = await transactionRepository.GetByIdAsync(
            transaction.Id, request.UserId, cancellationToken);

        // O registro acabou de ser gravado: se não volta na leitura, algo está errado (ex.: exclusão concorrente)
        if (created is null)
            throw new InvalidOperationException(
                "Transação criada, mas não foi possível recarregá-la para montar a resposta.");

        return created.ToDto();
    }

    // Parcelas 2..N: mesmo lançamento, uma por mês, agendadas, sem anexo (o comprovante fica na primeira)
    private static List<Transaction> GerarParcelasRestantes(
        Transaction primeira,
        IReadOnlyList<decimal> valores,
        string[] tags)
    {
        var parcelas = new List<Transaction>();

        for (var numero = 2; numero <= valores.Count; numero++)
        {
            parcelas.Add(new Transaction
            {
                UserId = primeira.UserId,
                Amount = valores[numero - 1],
                Type = primeira.Type,
                Date = InstallmentPlanner.DueDate(primeira.Date, numero),
                Description = primeira.Description,
                Status = TransactionStatus.Scheduled,
                IsRecurring = false,
                RecurrenceType = RecurrenceType.None,
                AccountId = primeira.AccountId,
                CategoryId = primeira.CategoryId,
                SubcategoryId = primeira.SubcategoryId,
                Tags = JsonSerializer.Serialize(tags),
                InstallmentGroupId = primeira.InstallmentGroupId,
                InstallmentNumber = numero,
                InstallmentCount = valores.Count,
            });
        }

        return parcelas;
    }

    // Gera cópias mensais para os meses restantes do ano corrente
    private static List<Transaction> GerarCopiasRecorrentes(
        Transaction origem,
        string[] tags)
    {
        var copies = new List<Transaction>();
        var mesAtual = origem.Date.Month;
        var ano = origem.Date.Year;

        for (var mes = mesAtual + 1; mes <= 12; mes++)
        {
            // Garante que o dia é válido para o mês de destino
            var diasNoMes = DateTime.DaysInMonth(ano, mes);
            var dia = Math.Min(origem.Date.Day, diasNoMes);

            copies.Add(new Transaction
            {
                UserId = origem.UserId,
                Amount = origem.Amount,
                Type = origem.Type,
                TransferDirection = origem.TransferDirection,
                Date = new DateTime(ano, mes, dia, origem.Date.Hour, origem.Date.Minute, origem.Date.Second),
                Description = origem.Description,
                Status = TransactionStatus.Scheduled,
                IsRecurring = true,
                RecurrenceType = origem.RecurrenceType,
                RecurrenceGroupId = origem.RecurrenceGroupId,
                AccountId = origem.AccountId,
                CategoryId = origem.CategoryId,
                SubcategoryId = origem.SubcategoryId,
                Tags = JsonSerializer.Serialize(tags),
                AttachmentPath = null,
                AttachmentName = null,
            });
        }

        return copies;
    }
}
