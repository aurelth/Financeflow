using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.ValueObjects;

namespace FinanceFlow.Application.Services;

/// <summary>Arquivo CSV já decodificado e separado em linhas e células.</summary>
/// <param name="Lines">Linha do arquivo (base 1) em que cada linha lógica começa; célula com quebra de linha ocupa várias.</param>
public sealed record CsvDocument(
    string Encoding,
    char Delimiter,
    IReadOnlyList<string[]> Rows,
    IReadOnlyList<int> Lines)
{
    public int Columns => Rows.Count == 0 ? 0 : Rows.Max(r => r.Length);
}

public sealed record ParsedCsvRow(int Line, DateTime Date, decimal Amount, string Description);

public sealed record CsvParseOutcome(
    IReadOnlyList<ParsedCsvRow> Rows,
    IReadOnlyList<CsvRowErrorDto> Errors,
    int TotalRows);

/// <summary>
/// Lê extratos em CSV de qualquer banco: decodifica o arquivo, descobre o separador, sugere o mapeamento
/// das colunas e converte as linhas em lançamentos. Não depende de nada de infraestrutura, por isso vive na
/// camada de aplicação (o OFX precisa de uma biblioteca externa; o CSV não).
/// </summary>
public static partial class CsvStatementParser
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public const int MaxRows = 5000;
    public const int SampleSize = 5;
    public const int PreviewSize = 8;
    public const string FallbackDescription = "Sem descrição";
    private const int MaxDescriptionLength = 500;
    private const decimal MaxAmount = 999_999_999_999m;

    private static readonly char[] Delimiters = [';', ',', '\t', '|'];

    // ───────────── Leitura ─────────────

    public static CsvDocument Read(Stream stream, char? delimiter = null)
    {
        var bytes = ReadBytes(stream);
        var (text, encoding) = Decode(bytes);
        var chosen = delimiter ?? DetectDelimiter(text);
        var (rows, lines) = Split(text, chosen);

        if (rows.Count == 0)
            throw new ValidationException("O arquivo CSV está vazio.");

        if (rows.Count > MaxRows + 1)
            throw new ValidationException(
                "O arquivo tem mais de 5.000 linhas. Divida o extrato em arquivos menores.");

        if (rows.Max(r => r.Length) < 2)
            throw new ValidationException(
                "Não foi possível identificar as colunas. Confira se o arquivo é um CSV e o separador está correto.");

        return new CsvDocument(encoding, chosen, rows, lines);
    }

    private static byte[] ReadBytes(Stream stream)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > MaxBytes)
                throw new ValidationException("O arquivo é maior que 5 MB.");
        }

        return buffer.ToArray();
    }

    // Bancos brasileiros exportam em UTF-8 (com ou sem BOM) ou em Windows-1252/ISO-8859-1
    private static (string Text, string Encoding) Decode(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return (Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3), "UTF-8");

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return (Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), "UTF-16");

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return (Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), "UTF-16");

        try
        {
            return (new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes), "UTF-8");
        }
        catch (DecoderFallbackException)
        {
            return (Encoding.Latin1.GetString(bytes), "ISO-8859-1");
        }
    }

    private static char DetectDelimiter(string text)
    {
        var lines = text
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => l.Length > 0)
            .Take(10)
            .ToList();

        var best = ';';
        var bestScore = 0;

        foreach (var candidate in Delimiters)
        {
            var mode = lines
                .Select(l => CountOutsideQuotes(l, candidate))
                .Where(c => c > 0)
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Key)
                .FirstOrDefault();

            if (mode is null)
                continue;

            // Quantas linhas concordam pesa mais; depois, quantas colunas o separador cria
            var score = mode.Count() * 1000 + mode.Key;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static int CountOutsideQuotes(string line, char delimiter)
    {
        var count = 0;
        var inQuotes = false;
        foreach (var c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == delimiter && !inQuotes) count++;
        }

        return count;
    }

    // RFC 4180: aspas protegem separador e quebra de linha; "" dentro de aspas é uma aspa literal
    private static (List<string[]> Rows, List<int> Lines) Split(string text, char delimiter)
    {
        var rows = new List<string[]>();
        var lines = new List<int>();
        var cells = new List<string>();
        var cell = new StringBuilder();
        var inQuotes = false;
        var line = 1;
        var rowLine = 1;

        void EndCell()
        {
            cells.Add(cell.ToString().Trim());
            cell.Clear();
        }

        void EndRow()
        {
            EndCell();
            if (cells.Any(c => c.Length > 0))
            {
                rows.Add([.. cells]);
                lines.Add(rowLine);
            }

            cells.Clear();
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    if (c == '\n') line++;
                    cell.Append(c);
                }

                continue;
            }

            if (c == '"' && cell.Length == 0)
            {
                inQuotes = true;
            }
            else if (c == delimiter)
            {
                EndCell();
            }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                EndRow();
                line++;
                rowLine = line;
            }
            else
            {
                cell.Append(c);
            }
        }

        EndRow();
        return (rows, lines);
    }

    // ───────────── Sugestão de mapeamento ─────────────

    /// <summary>Se a primeira linha é cabeçalho: nenhuma célula é data nem valor.</summary>
    public static bool LooksLikeHeader(string[] row) =>
        row.All(c => c.Length == 0
                     || (FindDateFormat([c]) is null && ParseAmount(c, ",") is null && ParseAmount(c, ".") is null));

    public static CsvMappingDto Suggest(CsvDocument doc, bool? hasHeader = null)
    {
        var header = hasHeader ?? LooksLikeHeader(doc.Rows[0]);
        var headers = header ? doc.Rows[0] : Array.Empty<string>();
        var data = doc.Rows.Skip(header ? 1 : 0).Take(60).ToList();
        var width = doc.Columns;

        string Cell(string[] row, int col) => col < row.Length ? row[col] : string.Empty;
        string Header(int col) => col < headers.Length ? Normalize(headers[col]) : string.Empty;
        List<string> Values(int col) => data.Select(r => Cell(r, col)).Where(v => v.Length > 0).ToList();

        // Data: a coluna cujas células mais batem com algum formato conhecido
        var dateCandidates = new List<(int Col, string Format, int Hits)>();
        for (var col = 0; col < width; col++)
        {
            var values = Values(col);
            var format = FindDateFormat(values);
            if (format is null || values.Count == 0)
                continue;

            var hits = values.Count(v => ParseDate(v, format) is not null);
            if (hits >= Math.Max(1, values.Count * 0.6))
                dateCandidates.Add((col, format, hits));
        }

        var date = dateCandidates.FirstOrDefault(c => HasAny(Header(c.Col), "data", "date", "fecha", "dt"));
        if (date == default && dateCandidates.Count > 0)
            date = dateCandidates[0];

        var dateCol = date == default ? 0 : date.Col;
        var dateFormat = date == default ? CsvDateFormats.DayMonthYear : date.Format;

        // Separador decimal: o que mais produz valores com casas decimais nas demais colunas
        var others = Enumerable.Range(0, width).Where(c => c != dateCol).ToList();
        var comma = others.Sum(c => Values(c).Count(v => v.Contains(',') && ParseAmount(v, ",") is not null));
        var dot = others.Sum(c => Values(c).Count(v => v.Contains('.') && ParseAmount(v, ".") is not null));
        var decimalSeparator = dot > comma ? "." : ",";

        bool IsNumeric(int col)
        {
            var values = Values(col);
            return values.Count > 0
                   && values.Count(v => ParseAmount(v, decimalSeparator) is not null) >= Math.Max(1, values.Count * 0.6);
        }

        bool HasDecimals(int col) =>
            Values(col).Any(v => DecimalPart().IsMatch(v));

        var numeric = others.Where(IsNumeric).ToList();

        // Valor: por cabeçalho; sem cabeçalho, a primeira coluna numérica com centavos (ignora ids e saldo)
        int? amountCol = numeric.Cast<int?>().FirstOrDefault(c =>
            HasAny(Header(c!.Value), "valor", "amount", "montante", "importe", "value") &&
            !HasAny(Header(c.Value), "saldo", "balance"));

        int? debitCol = numeric.Cast<int?>().FirstOrDefault(c =>
            HasAny(Header(c!.Value), "debito", "saida", "debit", "withdraw", "retirada"));
        int? creditCol = numeric.Cast<int?>().FirstOrDefault(c =>
            HasAny(Header(c!.Value), "credito", "entrada", "credit", "deposit", "deposito"));

        if (amountCol is null && debitCol is null && creditCol is null)
        {
            amountCol = numeric.Cast<int?>().FirstOrDefault(c =>
                              HasDecimals(c!.Value) && !HasAny(Header(c.Value), "saldo", "balance"))
                          ?? numeric.Cast<int?>().FirstOrDefault(c => !HasAny(Header(c!.Value), "saldo", "balance"));
        }

        if (amountCol is not null)
        {
            debitCol = null;
            creditCol = null;
        }

        // Descrição: por cabeçalho; senão a coluna de texto com as células mais longas
        var used = new HashSet<int> { dateCol };
        if (amountCol is { } a) used.Add(a);
        if (debitCol is { } d) used.Add(d);
        if (creditCol is { } cr) used.Add(cr);

        var textCols = Enumerable.Range(0, width).Where(c => !used.Contains(c) && !numeric.Contains(c)).ToList();
        if (textCols.Count == 0)
            textCols = Enumerable.Range(0, width).Where(c => !used.Contains(c)).ToList();

        var descriptionCol = textCols.Cast<int?>().FirstOrDefault(c =>
                                 HasAny(Header(c!.Value), "descri", "historico", "memo", "estabelec", "title", "detalhe", "lancamento", "nome"))
                             ?? textCols.OrderByDescending(c => Values(c).DefaultIfEmpty(string.Empty).Average(v => v.Length)).Cast<int?>().FirstOrDefault()
                             ?? Math.Min(1, width - 1);

        // Fatura de cartão lista as compras como valores positivos: se (quase) tudo é positivo, sugere inverter
        var invertSign = false;
        if (amountCol is { } amountColumn)
        {
            var amounts = Values(amountColumn)
                .Select(v => ParseAmount(v, decimalSeparator))
                .Where(v => v is not null)
                .ToList();
            invertSign = amounts.Count >= 5 && amounts.Count(v => v > 0) >= amounts.Count * 0.9;
        }

        return new CsvMappingDto(
            Delimiter: doc.Delimiter.ToString(),
            HasHeader: header,
            DateColumn: dateCol,
            DescriptionColumn: descriptionCol,
            AmountColumn: amountCol,
            DebitColumn: debitCol,
            CreditColumn: creditCol,
            DateFormat: dateFormat,
            DecimalSeparator: decimalSeparator,
            InvertSign: invertSign);
    }

    private static bool HasAny(string normalizedHeader, params string[] terms) =>
        normalizedHeader.Length > 0 && terms.Any(normalizedHeader.Contains);

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(decomposed
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
    }

    // ───────────── Conversão ─────────────

    /// <summary>O mapeamento aponta para colunas que existem neste arquivo?</summary>
    public static bool Fits(CsvDocument doc, CsvMappingDto m)
    {
        var width = doc.Columns;
        var columns = new[] { m.DateColumn, m.DescriptionColumn, m.AmountColumn ?? 0, m.DebitColumn ?? 0, m.CreditColumn ?? 0 };
        return columns.All(c => c >= 0 && c < width);
    }

    /// <summary>Formato, separador e colunas válidos para este arquivo, com ao menos uma coluna de valor.</summary>
    public static bool IsUsable(CsvDocument doc, CsvMappingDto m) =>
        CsvDateFormats.All.Contains(m.DateFormat)
        && m.DecimalSeparator is "," or "."
        && (m.AmountColumn is not null || m.DebitColumn is not null || m.CreditColumn is not null)
        && Fits(doc, m);

    /// <summary>Separador informado pelo usuário, se for um dos aceitos.</summary>
    public static char? ParseDelimiter(string? value) =>
        value is { Length: 1 } && Delimiters.Contains(value[0]) ? value[0] : null;

    public static CsvParseOutcome Apply(CsvDocument doc, CsvMappingDto m)
    {
        var rows = new List<ParsedCsvRow>();
        var errors = new List<CsvRowErrorDto>();
        var start = m.HasHeader ? 1 : 0;
        var needed = new[] { m.DateColumn, m.DescriptionColumn, m.AmountColumn, m.DebitColumn, m.CreditColumn }
            .Where(c => c.HasValue).Max()!.Value;

        for (var i = start; i < doc.Rows.Count; i++)
        {
            var row = doc.Rows[i];
            var line = doc.Lines[i];

            if (row.Length <= needed)
            {
                errors.Add(new CsvRowErrorDto(line, CsvRowErrors.MissingColumns));
                continue;
            }

            var date = ParseDate(row[m.DateColumn], m.DateFormat);
            if (date is null)
            {
                errors.Add(new CsvRowErrorDto(line, CsvRowErrors.InvalidDate));
                continue;
            }

            var amount = ReadAmount(row, m);
            if (amount is null)
            {
                errors.Add(new CsvRowErrorDto(line, CsvRowErrors.InvalidAmount));
                continue;
            }

            if (amount == 0)
            {
                errors.Add(new CsvRowErrorDto(line, CsvRowErrors.ZeroAmount));
                continue;
            }

            rows.Add(new ParsedCsvRow(line, date.Value, amount.Value, CleanDescription(row[m.DescriptionColumn])));
        }

        return new CsvParseOutcome(rows, errors, doc.Rows.Count - start);
    }

    private static decimal? ReadAmount(string[] row, CsvMappingDto m)
    {
        if (m.AmountColumn is { } single)
        {
            var value = ParseAmount(row[single], m.DecimalSeparator);
            return value is null ? null : m.InvertSign ? -value : value;
        }

        // Colunas separadas: débito e crédito (cada uma pode ficar vazia na linha)
        decimal? debit = m.DebitColumn is { } d ? ParseOptional(row[d], m.DecimalSeparator) : 0m;
        decimal? credit = m.CreditColumn is { } c ? ParseOptional(row[c], m.DecimalSeparator) : 0m;

        if (debit is null || credit is null)
            return null;

        if (debit == 0 && credit == 0)
        {
            var bothBlank = (m.DebitColumn is null || row[m.DebitColumn.Value].Length == 0)
                            && (m.CreditColumn is null || row[m.CreditColumn.Value].Length == 0);
            return bothBlank ? null : 0m;
        }

        return Math.Abs(credit.Value) - Math.Abs(debit.Value);
    }

    private static decimal? ParseOptional(string cell, string decimalSeparator) =>
        cell.Trim().Length == 0 ? 0m : ParseAmount(cell, decimalSeparator);

    private static string CleanDescription(string raw)
    {
        var text = WhiteSpace().Replace(raw, " ").Trim();
        if (text.Length == 0)
            return FallbackDescription;

        return text.Length > MaxDescriptionLength ? text[..MaxDescriptionLength] : text;
    }

    /// <summary>
    /// Aceita "1.234,56", "-150,00", "(150,00)", "150,00-", "R$ 150,00" e "150,00 D" (débito) / "150,00 C" (crédito).
    /// Qualquer outra coisa devolve null, para a linha ser apontada em vez de importada errada.
    /// </summary>
    public static decimal? ParseAmount(string raw, string decimalSeparator)
    {
        var s = raw.Replace(' ', ' ').Trim();
        if (s.Length == 0)
            return null;

        var negative = false;

        // Sufixo D/C, comum em extratos de banco
        if (s.Length > 1 && "DdCc".Contains(s[^1]))
        {
            var rest = s[..^1].TrimEnd();
            if (rest.Length > 0 && (char.IsDigit(rest[^1]) || rest[^1] == ')'))
            {
                negative = char.ToUpperInvariant(s[^1]) == 'D';
                s = rest;
            }
        }

        s = Currency().Replace(s, string.Empty).Replace(" ", string.Empty);

        if (s.StartsWith('(') && s.EndsWith(')'))
        {
            negative = true;
            s = s[1..^1];
        }

        if (s.EndsWith('-'))
        {
            negative = true;
            s = s[..^1];
        }

        if (s.StartsWith('-'))
        {
            negative = true;
            s = s[1..];
        }
        else if (s.StartsWith('+'))
        {
            s = s[1..];
        }

        var comma = decimalSeparator == ",";
        if (!(comma ? CommaNumber() : DotNumber()).IsMatch(s))
            return null;

        s = comma
            ? s.Replace(".", string.Empty).Replace(',', '.')
            : s.Replace(",", string.Empty);

        if (!decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            return null;

        if (value > MaxAmount)
            return null;

        value = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return negative ? -value : value;
    }

    public static DateTime? ParseDate(string raw, string format)
    {
        var cell = raw.Trim();
        if (cell.Length == 0)
            return null;

        // "01/10/2026 14:32" e "2026-10-01T14:32:00": só a data interessa
        var cut = cell.IndexOfAny([' ', 'T']);
        if (cut > 0)
            cell = cell[..cut];

        if (!DateTime.TryParseExact(cell, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return null;

        if (date.Year is < 1990 or > 2100)
            return null;

        // CSV só traz o dia. Meio-dia (sem fuso) mantém o dia certo quando o front converte o horário para o
        // fuso local, o que meia-noite não faz (00:00 UTC vira o dia anterior no Brasil). Igual ao que os OFX trazem.
        return DateTime.SpecifyKind(date.Date.AddHours(12), DateTimeKind.Unspecified);
    }

    private static string? FindDateFormat(IReadOnlyCollection<string> values)
    {
        string? best = null;
        var bestHits = 0;
        foreach (var format in CsvDateFormats.All)
        {
            var hits = values.Count(v => ParseDate(v, format) is not null);
            if (hits > bestHits)
            {
                best = format;
                bestHits = hits;
            }
        }

        return best;
    }

    // ───────────── Resultado para o pipeline de importação ─────────────

    /// <summary>
    /// Converte as linhas lidas no mesmo formato do OFX, para o restante da importação (categorias, duplicatas,
    /// preview, confirmação) ser compartilhado. CSV não traz identificação de conta nem saldo.
    /// </summary>
    public static OFXParseResult ToParseResult(CsvParseOutcome outcome)
    {
        var transactions = outcome.Rows
            .Select(r => new OFXTransaction(
                FitId: string.Empty,
                Date: r.Date,
                Amount: r.Amount,
                Description: r.Description,
                Type: r.Amount > 0 ? "CREDIT" : "DEBIT"))
            .ToList();

        return new OFXParseResult(
            AccountId: string.Empty,
            BankId: string.Empty,
            StartDate: transactions.Count == 0 ? default : transactions.Min(t => t.Date),
            EndDate: transactions.Count == 0 ? default : transactions.Max(t => t.Date),
            Transactions: transactions);
    }

    public static CsvPreviewRowDto ToPreview(ParsedCsvRow row) =>
        new(row.Line, row.Date, row.Amount, row.Description,
            row.Amount > 0 ? TransactionType.Income : TransactionType.Expense);

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();

    [GeneratedRegex(@"R\$|US\$|BRL|USD|EUR|[$€£]", RegexOptions.IgnoreCase)]
    private static partial Regex Currency();

    [GeneratedRegex(@"^(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$")]
    private static partial Regex CommaNumber();

    [GeneratedRegex(@"^(\d{1,3}(,\d{3})+|\d+)(\.\d+)?$")]
    private static partial Regex DotNumber();

    [GeneratedRegex(@"[.,]\d{1,2}\s*\)?-?\s*[DdCc]?$")]
    private static partial Regex DecimalPart();
}
