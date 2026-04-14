using FinanceFlow.Domain.ValueObjects;

namespace FinanceFlow.Domain.Interfaces;

public interface IOFXParserService
{
    OFXParseResult Parse(Stream stream);
}
