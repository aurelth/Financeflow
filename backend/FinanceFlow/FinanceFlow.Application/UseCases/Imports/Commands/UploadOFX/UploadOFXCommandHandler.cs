using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.UploadOFX;

public class UploadOFXCommandHandler(
    IOFXParserService ofxParserService,
    IStatementImportService statementImportService)
    : IRequestHandler<UploadOFXCommand, BankImportDto>
{
    public async Task<BankImportDto> Handle(
        UploadOFXCommand request,
        CancellationToken cancellationToken)
    {
        var parseResult = ofxParserService.Parse(request.FileStream);

        return await statementImportService.CreateAsync(
            request.UserId, request.FileName, request.AccountId, parseResult, cancellationToken);
    }
}
