interface GoalsSummaryBarProps {
  available: number;
  committed: number;
  difference: number;
}

export default function GoalsSummaryBar({
  available,
  committed,
  difference,
}: GoalsSummaryBarProps) {
  const fmt = (v: number) =>
    v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

  // Diferença = planejado - recebido
  // Positivo = déficit, Negativo = excedente, Zero = em dia
  const isExcedente = difference < 0;
  const isEmDia = difference === 0;

  const differenceColor =
    isExcedente || isEmDia ? "var(--ff-income)" : "var(--ff-expense)";

  const differenceLabel = isEmDia
    ? "Meta cumprida este mês 🎯"
    : isExcedente
      ? "Além do planejado"
      : "Déficit este mês";

  const differenceDisplay = isExcedente
    ? `+${fmt(Math.abs(difference))}`
    : isEmDia
      ? fmt(0)
      : fmt(difference);

  return (
    <div
      className="rounded-2xl p-5 grid grid-cols-1 sm:grid-cols-3 gap-4"
      style={{
        background: "var(--ff-bg-card)",
        border: "1px solid var(--ff-border)",
      }}
    >
      <div className="flex flex-col gap-1">
        <span
          className="text-xs uppercase tracking-wide"
          style={{ color: "var(--ff-text-muted)" }}
        >
          Total acumulado
        </span>
        <span
          className="text-lg font-semibold"
          style={{ color: "var(--ff-emerald)" }}
        >
          {fmt(available)}
        </span>
        <span className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
          Em todas as metas
        </span>
      </div>

      <div className="flex flex-col gap-1">
        <span
          className="text-xs uppercase tracking-wide"
          style={{ color: "var(--ff-text-muted)" }}
        >
          Comprometido com metas
        </span>
        <span
          className="text-lg font-semibold"
          style={{ color: "var(--ff-expense)" }}
        >
          {fmt(committed)}
        </span>
        <span className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
          Soma das contribuições
        </span>
      </div>

      <div className="flex flex-col gap-1">
        <span
          className="text-xs uppercase tracking-wide"
          style={{ color: "var(--ff-text-muted)" }}
        >
          Diferença
        </span>
        <span
          className="text-lg font-semibold"
          style={{ color: differenceColor }}
        >
          {differenceDisplay}
        </span>
        <span className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
          {differenceLabel}
        </span>
      </div>
    </div>
  );
}
