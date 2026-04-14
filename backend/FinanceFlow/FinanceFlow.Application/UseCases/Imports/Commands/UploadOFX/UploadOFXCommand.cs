using FinanceFlow.Application.DTOs.Imports;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.UploadOFX;

public record UploadOFXCommand(
    Guid UserId,
    Stream FileStream,
    string FileName
) : IRequest<BankImportDto>;
