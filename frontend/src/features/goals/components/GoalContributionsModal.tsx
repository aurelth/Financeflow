import { X, Loader2, Receipt } from "lucide-react";
import { useGoalContributions } from "../api/useGoals";
import type { GoalProgressResultDto } from "../types/goal.types";

interface GoalContributionsModalProps {
  goal: GoalProgressResultDto;
  onClose: () => void;
}

function fmt(v: number) {
  return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export default function GoalContributionsModal({
  goal,
  onClose,
}: GoalContributionsModalProps) {
  const { data, isLoading } = useGoalContributions(goal.linkedCategoryId);

  const contributions = data?.items ?? [];

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
      <div
        className="w-full max-w-lg rounded-2xl shadow-2xl max-h-[90vh] flex flex-col"
        style={{
          background: "var(--ff-bg-card)",
          border: "1px solid var(--ff-border)",
        }}
      >
        {/* Header */}
        <div
          className="flex items-center justify-between px-6 py-4 flex-shrink-0"
          style={{ borderBottom: "1px solid var(--ff-border)" }}
        >
          <div className="flex items-center gap-3">
            <span className="text-2xl">{goal.emoji}</span>
            <div>
              <h2
                className="font-semibold text-base"
                style={{ color: "var(--ff-text-primary)" }}
              >
                {goal.name}
              </h2>
              <p
                className="text-xs mt-0.5"
                style={{ color: "var(--ff-text-muted)" }}
              >
                Histórico de contribuições
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg transition-all"
            style={{ color: "var(--ff-text-muted)" }}
            onMouseEnter={(e) => {
              e.currentTarget.style.color = "var(--ff-text-primary)";
              e.currentTarget.style.background = "var(--ff-bg-elevated)";
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.color = "var(--ff-text-muted)";
              e.currentTarget.style.background = "transparent";
            }}
          >
            <X size={18} />
          </button>
        </div>

        {/* Resumo */}
        <div
          className="flex items-center justify-between px-6 py-3 flex-shrink-0"
          style={{
            borderBottom: "1px solid var(--ff-border)",
            background: "var(--ff-bg-elevated)",
          }}
        >
          <div>
            <p className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
              Acumulado
            </p>
            <p
              className="text-sm font-semibold"
              style={{ color: "var(--ff-emerald)" }}
            >
              {fmt(goal.accumulatedAmount)}
            </p>
          </div>
          <div className="text-right">
            <p className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
              Meta
            </p>
            <p
              className="text-sm font-semibold"
              style={{ color: "var(--ff-text-primary)" }}
            >
              {fmt(goal.targetAmount)}
            </p>
          </div>
          <div className="text-right">
            <p className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
              Progresso
            </p>
            <p
              className="text-sm font-semibold"
              style={{ color: "var(--ff-text-primary)" }}
            >
              {Math.round(goal.progressPercentage)}%
            </p>
          </div>
        </div>

        {/* Lista de contribuições */}
        <div className="flex-1 overflow-y-auto px-6 py-4">
          {isLoading && (
            <div className="flex items-center justify-center py-10">
              <Loader2
                size={20}
                className="animate-spin"
                style={{ color: "var(--ff-emerald)" }}
              />
            </div>
          )}

          {!isLoading && contributions.length === 0 && (
            <div className="flex flex-col items-center gap-3 py-10">
              <div
                className="w-12 h-12 rounded-xl flex items-center justify-center"
                style={{ background: "rgba(16,185,129,0.1)" }}
              >
                <Receipt size={20} style={{ color: "var(--ff-emerald)" }} />
              </div>
              <p
                className="text-sm text-center"
                style={{ color: "var(--ff-text-muted)" }}
              >
                Nenhuma contribuição registada ainda.
                <br />
                Cria uma transação com a categoria{" "}
                <strong>
                  {goal.emoji} Meta: {goal.name}
                </strong>
                .
              </p>
            </div>
          )}

          {!isLoading && contributions.length > 0 && (
            <div className="space-y-2">
              {contributions.map((tx) => {
                const [year, month, day] = tx.date
                  .split("T")[0]
                  .split("-")
                  .map(Number);
                const localDate = new Date(year, month - 1, day);
                const dateLabel = localDate.toLocaleDateString("pt-BR", {
                  day: "2-digit",
                  month: "short",
                  year: "numeric",
                });

                return (
                  <div
                    key={tx.id}
                    className="flex items-center justify-between rounded-xl px-4 py-3"
                    style={{
                      background: "var(--ff-bg-elevated)",
                      border: "1px solid var(--ff-border)",
                    }}
                  >
                    <div className="flex items-center gap-3">
                      <div
                        className="w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 text-sm"
                        style={{ background: "rgba(16,185,129,0.1)" }}
                      >
                        🎯
                      </div>
                      <div>
                        <p
                          className="text-sm font-medium"
                          style={{ color: "var(--ff-text-primary)" }}
                        >
                          {tx.description || "Contribuição"}
                        </p>
                        <p
                          className="text-xs"
                          style={{ color: "var(--ff-text-muted)" }}
                        >
                          {dateLabel}
                        </p>
                      </div>
                    </div>
                    <p
                      className="text-sm font-semibold"
                      style={{ color: "var(--ff-emerald)" }}
                    >
                      {fmt(tx.amount)}
                    </p>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer */}
        <div
          className="px-6 py-4 flex-shrink-0"
          style={{ borderTop: "1px solid var(--ff-border)" }}
        >
          <button
            onClick={onClose}
            className="w-full py-2.5 rounded-xl text-sm font-medium transition-colors"
            style={{
              border: "1px solid var(--ff-border)",
              color: "var(--ff-text-secondary)",
            }}
            onMouseEnter={(e) =>
              (e.currentTarget.style.background = "var(--ff-bg-elevated)")
            }
            onMouseLeave={(e) =>
              (e.currentTarget.style.background = "transparent")
            }
          >
            Fechar
          </button>
        </div>
      </div>
    </div>
  );
}
