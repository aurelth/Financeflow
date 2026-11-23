namespace FinanceFlow.Domain.Enums;

/// <summary>Motivo pelo qual uma linha do extrato foi marcada como duplicada no preview.</summary>
public enum DuplicateReason
{
    /// <summary>Já existe um lançamento importado na mesma conta com o mesmo FITID e valor.</summary>
    SameFitId = 1,

    /// <summary>Já existe um lançamento importado na mesma conta com a mesma data, valor, descrição e ocorrência.</summary>
    SameContent = 2,

    /// <summary>Provável correspondência com um lançamento manual (mesmo valor e tipo, data próxima).</summary>
    PossibleManual = 3,
}
