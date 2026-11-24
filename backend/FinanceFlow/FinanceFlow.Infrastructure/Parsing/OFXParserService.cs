using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Domain.ValueObjects;
using System.Text.RegularExpressions;
using OFXSharp;

namespace FinanceFlow.Infrastructure.Parsing;

public partial class OFXParserService : IOFXParserService
{
    private const string PlaceholderBalance =
        "<LEDGERBAL><BALAMT>0.00</BALAMT><DTASOF>20000101</DTASOF></LEDGERBAL>";

    [GeneratedRegex(@"<LEDGERBAL\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex LedgerBalTag { get; }

    [GeneratedRegex(@"</(CC)?STMTRS\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex StatementEndTag { get; }

    public OFXParseResult Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var ofxContent = reader.ReadToEnd();
   
        ofxContent = ofxContent
            .Replace("ENCODING:UTF-8", "ENCODING:USASCII")
            .Replace("ENCODING:UTF8", "ENCODING:USASCII")
            .Replace("ENCODING:UNICODE", "ENCODING:USASCII")
            .Replace("CHARSET:NONE", "CHARSET:1252");

        // LEDGERBAL é opcional no OFX, mas o OFXSharp recusa o arquivo sem ele: insere um saldo neutro
        // só para o arquivo ser lido e ignora esse valor depois (sem saldo, sem conciliação)
        var hasBalance = LedgerBalTag.IsMatch(ofxContent);
        if (!hasBalance)
            ofxContent = StatementEndTag.Replace(ofxContent, PlaceholderBalance + "$0", 1);

        var parser = new OFXDocumentParser();
        var document = parser.Import(ofxContent);

        var transactions = document.Transactions?
            .Select(t => new OFXTransaction(
                // Sem FITID o valor fica vazio: um Guid aleatório faria o mesmo lançamento parecer novo a cada importação
                FitId: (t.TransactionID ?? string.Empty).Trim(),
                Date: t.Date,
                Amount: t.Amount,
                Description: t.Memo ?? t.Name ?? string.Empty,
                Type: t.TransType.ToString()
            )) ?? [];

        var (ledgerBalance, ledgerBalanceDate) = hasBalance
            ? ReadLedgerBalance(document)
            : (null, null);

        return new OFXParseResult(
            AccountId: document.Account?.AccountID ?? string.Empty,
            BankId: document.Account?.BankID ?? string.Empty,
            StartDate: document.StatementStart,
            EndDate: document.StatementEnd,
            Transactions: transactions,
            LedgerBalance: ledgerBalance,
            LedgerBalanceDate: ledgerBalanceDate
        );
    }

    // O saldo (LEDGERBAL) é opcional no OFX e o OFXSharp lança exceção quando ele não foi informado;
    // sem saldo o arquivo continua importável, apenas não entra na conciliação.
    private static (decimal? Balance, DateTime? Date) ReadLedgerBalance(OFXDocument document)
    {
        try
        {
            var balance = document.Balance;
            if (balance is null)
                return (null, null);

            var date = balance.LedgerBalanceDate;
            return (balance.LedgerBalance, date == default ? null : date);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }
}
