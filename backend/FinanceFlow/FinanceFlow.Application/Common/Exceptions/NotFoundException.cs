namespace FinanceFlow.Application.Common.Exceptions
{
    public class NotFoundException(string message) : Exception(message)
    {
        public NotFoundException(string entity, object key)
            : this($"{entity} com id '{key}' não foi encontrado.") { }
    }
}
