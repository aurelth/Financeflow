using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Domain.ValueObjects;
using OFXSharp;

namespace FinanceFlow.Infrastructure.Parsing;

public class OFXParserService : IOFXParserService
{
    public OFXParseResult Parse(Stream stream)
    {
        // Lê o stream como string pois OFXSharp aceita string ou FileStream
        using var reader = new StreamReader(stream);
        var ofxContent = reader.ReadToEnd();

        var parser = new OFXDocumentParser();
        var document = parser.Import(ofxContent);

        var transactions = document.Transactions?
            .Select(t => new OFXTransaction(
                FitId: t.TransactionID ?? Guid.NewGuid().ToString(),
                Date: t.Date,
                Amount: t.Amount,
                Description: t.Memo ?? t.Name ?? string.Empty,
                Type: t.TransType.ToString()
            )) ?? [];

        return new OFXParseResult(
            AccountId: document.Account?.AccountID ?? string.Empty,
            BankId: document.Account?.BankID ?? string.Empty,
            StartDate: document.StatementStart,
            EndDate: document.StatementEnd,
            Transactions: transactions
        );
    }
}
