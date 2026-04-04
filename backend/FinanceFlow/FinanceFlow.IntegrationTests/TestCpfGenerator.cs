namespace FinanceFlow.IntegrationTests;

public static class TestCpfGenerator
{
    private static int _counter = 1;
    private static readonly object _lock = new();

    public static string Next()
    {
        lock (_lock)
        {
            var cpf = GenerateValid(_counter++);
            return cpf;
        }
    }

    private static string GenerateValid(int seed)
    {
        // Gera os 9 primeiros dígitos baseados no seed
        var digits = new int[11];
        var n = seed;
        for (var i = 8; i >= 0; i--)
        {
            digits[i] = n % 10;
            n /= 10;
        }

        // Evita CPFs com todos os dígitos iguais
        if (digits.Take(9).Distinct().Count() == 1)
            digits[0] = (digits[0] + 1) % 10;

        // Calcula primeiro dígito verificador
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += digits[i] * (10 - i);
        var remainder = sum % 11;
        digits[9] = remainder < 2 ? 0 : 11 - remainder;

        // Calcula segundo dígito verificador
        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += digits[i] * (11 - i);
        remainder = sum % 11;
        digits[10] = remainder < 2 ? 0 : 11 - remainder;

        return $"{digits[0]}{digits[1]}{digits[2]}.{digits[3]}{digits[4]}{digits[5]}.{digits[6]}{digits[7]}{digits[8]}-{digits[9]}{digits[10]}";
    }
}
