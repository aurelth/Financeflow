import {
  useState,
  useEffect,
  forwardRef,
  useImperativeHandle,
} from "react";
import {
  ArrowDownCircle,
  ArrowUpCircle,
  AlertCircle,
  AlertTriangle,
  ArrowLeftRight,
  ChevronDown,
  ChevronRight,
} from "lucide-react";
import type {
  BankImportTransactionDto,
  DuplicateReason,
} from "../types/imports.types";
import {
  TransactionType,
  type Category,
} from "@/features/categories/types/category.types";
import { isLikelyTransfer } from "@/lib/transferUtils";

export interface ImportPreviewSummary {
  /** Linhas novas selecionadas */
  selected: number;
  /** Linhas duplicadas que o usuário marcou para importar mesmo assim */
  importAnyway: number;
  /** Linhas selecionadas (novas ou duplicadas) que ainda não têm categoria */
  missingCategory: number;
}

interface Props {
  transactions: BankImportTransactionDto[];
  categories: Category[];
  /** Linhas recusadas pela API na confirmação (destacadas em vermelho) */
  errorIds?: string[];
  onSummaryChange?: (summary: ImportPreviewSummary) => void;
}

export interface ImportPreviewTableHandle {
  getSelected: () => string[];
  getTransactions: () => {
    id: string;
    isSelected: boolean;
    categoryId: string;
    type: string;
    importAnyway?: boolean;
  }[];
  getCount: () => { selected: number; total: number };
}

function formatAmount(amount: number, type: string) {
  const formatted = new Intl.NumberFormat("pt-PT", {
    style: "currency",
    currency: "BRL",
  }).format(amount);
  if (type === "Transfer") return `↔ ${formatted}`;
  return type === "Expense" ? `- ${formatted}` : `+ ${formatted}`;
}

function getAmountColor(type: string): string {
  if (type === "Income") return "#34d399";
  if (type === "Transfer") return "#818cf8";
  return "#f87171";
}

function formatDate(date: string) {
  return new Date(`${date}Z`).toLocaleDateString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";
const GRID = "32px 1fr 120px 140px 100px";

type RowType = "Income" | "Expense" | "Transfer";

interface RowState extends BankImportTransactionDto {
  localType: RowType;
}

const REASON_TEXT: Record<DuplicateReason, string> = {
  SameFitId: "Já importada nesta conta (mesmo identificador do banco)",
  SameContent: "Já importada nesta conta (mesma data, valor e descrição)",
  PossibleManual: "Parece um lançamento que você digitou manualmente",
};

// Selecionada, não é transferência e sem categoria → bloqueia a confirmação
function isMissingCategory(row: RowState) {
  return row.isSelected && row.localType !== "Transfer" && !row.suggestedCategoryId;
}

const ImportPreviewTable = forwardRef<ImportPreviewTableHandle, Props>(
  ({ transactions: initialTransactions, categories, errorIds = [], onSummaryChange }, ref) => {
    const [rows, setRows] = useState<RowState[]>(
      initialTransactions.map((t) => ({ ...t, localType: t.type as RowType })),
    );
    const [selectAll, setSelectAll] = useState(true);
    const [showDuplicates, setShowDuplicates] = useState(false);

    const newRows = rows.filter((t) => !t.isDuplicate);
    const duplicateRows = rows.filter((t) => t.isDuplicate);

    const selectedNew = newRows.filter((t) => t.isSelected).length;
    const importAnywayCount = duplicateRows.filter((t) => t.isSelected).length;
    const missingCount = rows.filter(isMissingCategory).length;

    useEffect(() => {
      onSummaryChange?.({
        selected: selectedNew,
        importAnyway: importAnywayCount,
        missingCategory: missingCount,
      });
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedNew, importAnywayCount, missingCount]);

    useImperativeHandle(ref, () => ({
      getSelected: () => rows.filter((t) => t.isSelected).map((t) => t.id),

      getTransactions: () =>
        rows
          .filter((t) => t.isSelected)
          .map((t) => ({
            id: t.id,
            isSelected: true,
            categoryId: t.suggestedCategoryId ?? EMPTY_GUID,
            type: t.localType,
            // Só vai no payload quando a linha duplicada foi liberada pelo usuário
            ...(t.isDuplicate ? { importAnyway: true } : {}),
          })),

      getCount: () => ({
        selected: rows.filter((t) => t.isSelected).length,
        total: rows.length,
      }),
    }));

    function toggleAll(checked: boolean) {
      setSelectAll(checked);
      // Duplicadas nunca entram no "selecionar todas": exigem decisão explícita
      setRows(
        rows.map((t) => (t.isDuplicate ? t : { ...t, isSelected: checked })),
      );
    }

    function toggleOne(id: string, checked: boolean) {
      setRows(
        rows.map((t) => (t.id === id ? { ...t, isSelected: checked } : t)),
      );
    }

    function changeCategory(id: string, categoryId: string) {
      setRows(
        rows.map((t) =>
          t.id === id ? { ...t, suggestedCategoryId: categoryId || null } : t,
        ),
      );
    }

    function deselectUncategorized() {
      setRows(rows.map((t) => (isMissingCategory(t) ? { ...t, isSelected: false } : t)));
    }

    // Suporta toggle para Income e Expense — volta ao tipo original ao desmarcar
    function toggleTransfer(id: string) {
      setRows(
        rows.map((t) => {
          if (t.id !== id) return t;
          const newType: RowType =
            t.localType === "Transfer" ? (t.type as RowType) : "Transfer";
          return { ...t, localType: newType, suggestedCategoryId: null };
        }),
      );
    }

    function renderRow(t: RowState) {
      const isTransfer = t.localType === "Transfer";
      const likelyTransfer = isLikelyTransfer(t.description);
      const missing = isMissingCategory(t);
      const rejectedByServer = errorIds.includes(t.id);
      const hasError = missing || rejectedByServer;

      const filteredCategories = isTransfer
        ? categories
        : categories.filter((c) => {
            if (t.localType === "Income")
              return c.type === TransactionType.Income;
            if (t.localType === "Expense")
              return c.type === TransactionType.Expense;
            return true;
          });

      return (
        <div
          key={t.id}
          data-testid={`import-row-${t.id}`}
          data-error={hasError ? "true" : undefined}
          className="grid gap-4 px-4 py-3 items-center transition-colors"
          style={{
            gridTemplateColumns: GRID,
            background: hasError
              ? "rgba(248,113,113,0.06)"
              : t.isDuplicate
                ? "rgba(251,191,36,0.04)"
                : t.isSelected
                  ? "var(--ff-bg-card)"
                  : "var(--ff-bg-elevated)",
            borderLeft: hasError
              ? "3px solid #f87171"
              : "3px solid transparent",
          }}
        >
          {/* Checkbox */}
          <input
            type="checkbox"
            checked={t.isSelected}
            aria-label={
              t.isDuplicate
                ? `Importar mesmo assim: ${t.description}`
                : `Selecionar ${t.description}`
            }
            onChange={(e) => toggleOne(t.id, e.target.checked)}
            className="rounded"
            style={{ accentColor: "var(--ff-emerald)" }}
          />

          {/* Descrição */}
          <div className="flex flex-col gap-1 min-w-0">
            <div className="flex items-center gap-2 min-w-0">
              {isTransfer ? (
                <ArrowLeftRight
                  size={15}
                  style={{ color: "#818cf8", flexShrink: 0 }}
                />
              ) : t.type === "Expense" ? (
                <ArrowDownCircle
                  size={15}
                  style={{ color: "#f87171", flexShrink: 0 }}
                />
              ) : (
                <ArrowUpCircle
                  size={15}
                  style={{ color: "#34d399", flexShrink: 0 }}
                />
              )}
              <span
                className="text-sm truncate"
                style={{ color: "var(--ff-text-primary)" }}
                title={t.description}
              >
                {t.description}
              </span>
              {t.isDuplicate && (
                <span
                  className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-xs flex-shrink-0"
                  style={{
                    background: "rgba(251,191,36,0.12)",
                    color: "#fbbf24",
                  }}
                >
                  <AlertCircle size={10} />
                  {t.duplicateReason === "PossibleManual"
                    ? "possível duplicata"
                    : "duplicada"}
                </span>
              )}
              {isTransfer && (
                <span
                  className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-xs flex-shrink-0"
                  style={{
                    background: "rgba(99,102,241,0.1)",
                    color: "#818cf8",
                  }}
                >
                  Transferência
                </span>
              )}
              {/* Toggle visível apenas para transações com palavras-chave de transferência */}
              {likelyTransfer && !t.isDuplicate && (
                <button
                  onClick={() => toggleTransfer(t.id)}
                  title={
                    isTransfer
                      ? "Marcar como original"
                      : "Marcar como transferência"
                  }
                  className="flex-shrink-0 p-1 rounded transition-colors"
                  style={{
                    color: isTransfer ? "#818cf8" : "var(--ff-text-muted)",
                    background: isTransfer
                      ? "rgba(99,102,241,0.1)"
                      : "transparent",
                  }}
                  onMouseEnter={(e) =>
                    (e.currentTarget.style.color = "#818cf8")
                  }
                  onMouseLeave={(e) => {
                    if (!isTransfer)
                      e.currentTarget.style.color = "var(--ff-text-muted)";
                  }}
                >
                  <ArrowLeftRight size={13} />
                </button>
              )}
            </div>

            {/* Motivo da duplicata e o lançamento existente */}
            {t.isDuplicate && t.duplicateReason && (
              <div
                className="text-xs pl-6"
                style={{ color: "var(--ff-text-muted)" }}
              >
                <p>{REASON_TEXT[t.duplicateReason]}</p>
                {t.match && (
                  <p>
                    Já existe: {formatDate(t.match.date)} · {t.match.description || "Sem descrição"} ·{" "}
                    {new Intl.NumberFormat("pt-PT", {
                      style: "currency",
                      currency: "BRL",
                    }).format(t.match.amount)}
                  </p>
                )}
              </div>
            )}

            {hasError && (
              <p
                role="alert"
                className="text-xs pl-6"
                style={{ color: "#f87171" }}
              >
                {rejectedByServer && !missing
                  ? "A categoria desta linha é inválida. Escolha outra."
                  : "Escolha uma categoria ou desmarque esta linha."}
              </p>
            )}
          </div>

          {/* Data */}
          <span className="text-sm" style={{ color: "var(--ff-text-muted)" }}>
            {formatDate(t.date)}
          </span>

          {/* Categoria */}
          <select
            value={t.suggestedCategoryId ?? ""}
            disabled={t.isDuplicate && !t.isSelected}
            aria-label={`Categoria de ${t.description}`}
            aria-invalid={hasError || undefined}
            onChange={(e) => changeCategory(t.id, e.target.value)}
            className="text-xs rounded-lg px-2 py-1.5 outline-none w-full"
            style={{
              background: "var(--ff-bg-elevated)",
              color: t.suggestedCategoryId
                ? "var(--ff-text-primary)"
                : "var(--ff-text-muted)",
              border: hasError
                ? "1px solid #f87171"
                : "1px solid var(--ff-border)",
            }}
          >
            <option value="">Sem categoria</option>
            {filteredCategories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
            {filteredCategories.length === 0 && (
              <option value="" disabled>
                {t.localType === "Expense"
                  ? "Crie categorias de despesa"
                  : "Crie categorias de receita"}
              </option>
            )}
          </select>

          {/* Valor */}
          <span
            className="text-sm font-medium text-right"
            style={{ color: getAmountColor(t.localType) }}
          >
            {formatAmount(t.amount, t.localType)}
          </span>
        </div>
      );
    }

    return (
      <div className="space-y-3">
        {/* Resumo */}
        <div className="flex items-center justify-between px-1">
          <p className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
            <span style={{ color: "var(--ff-text-primary)", fontWeight: 500 }}>
              {selectedNew}
            </span>{" "}
            de{" "}
            <span style={{ color: "var(--ff-text-primary)", fontWeight: 500 }}>
              {newRows.length}
            </span>{" "}
            transações selecionadas
            {importAnywayCount > 0 &&
              ` (+ ${importAnywayCount} duplicada${importAnywayCount > 1 ? "s" : ""} para importar mesmo assim)`}
          </p>
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={selectAll}
              onChange={(e) => toggleAll(e.target.checked)}
              className="rounded"
              style={{ accentColor: "var(--ff-emerald)" }}
            />
            <span className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
              Selecionar todas
            </span>
          </label>
        </div>

        {/* Bloqueio: linhas selecionadas sem categoria */}
        {missingCount > 0 && (
          <div
            role="alert"
            className="flex items-center justify-between gap-3 rounded-xl px-3 py-2.5 text-xs"
            style={{
              background: "rgba(248,113,113,0.08)",
              border: "1px solid rgba(248,113,113,0.25)",
              color: "#f87171",
            }}
          >
            <span className="flex items-start gap-2">
              <AlertTriangle size={13} className="flex-shrink-0 mt-0.5" />
              <span>
                {missingCount} lançamento{missingCount > 1 ? "s" : ""} sem
                categoria. Escolha a categoria ou desmarque a linha para poder
                confirmar a importação.
              </span>
            </span>
            <button
              type="button"
              onClick={deselectUncategorized}
              className="flex-shrink-0 px-2.5 py-1 rounded-lg font-medium transition-colors"
              style={{
                border: "1px solid rgba(248,113,113,0.4)",
                color: "#f87171",
              }}
            >
              Desmarcar sem categoria
            </button>
          </div>
        )}

        {/* Aviso sobre transferências */}
        <div
          className="flex items-start gap-2 rounded-xl px-3 py-2.5 text-xs"
          style={{
            background: "rgba(99,102,241,0.06)",
            border: "1px solid rgba(99,102,241,0.15)",
            color: "#818cf8",
          }}
        >
          <ArrowLeftRight size={13} className="flex-shrink-0 mt-0.5" />
          <span>
            Transações identificadas como transferências entre contas mostram o
            ícone <ArrowLeftRight size={11} className="inline" /> — clique para
            marcá-las como transferência e evitar dupla contagem.
          </span>
        </div>

        {/* Tabela */}
        <div
          className="rounded-xl overflow-hidden"
          style={{ border: "1px solid var(--ff-border)" }}
        >
          {/* Header */}
          <div
            className="grid gap-4 px-4 py-2.5 text-xs font-medium"
            style={{
              gridTemplateColumns: GRID,
              background: "var(--ff-bg-elevated)",
              color: "var(--ff-text-muted)",
            }}
          >
            <span />
            <span>Descrição</span>
            <span>Data</span>
            <span>Categoria</span>
            <span className="text-right">Valor</span>
          </div>

          <div className="divide-y" style={{ borderColor: "var(--ff-border)" }}>
            {newRows.map(renderRow)}
            {newRows.length === 0 && (
              <p
                className="px-4 py-6 text-sm text-center"
                style={{ color: "var(--ff-text-muted)" }}
              >
                Todas as linhas deste arquivo já existem nesta conta.
              </p>
            )}
          </div>
        </div>

        {/* Duplicadas: ficam fora da importação até o usuário decidir */}
        {duplicateRows.length > 0 && (
          <div
            className="rounded-xl overflow-hidden"
            style={{ border: "1px solid rgba(251,191,36,0.25)" }}
          >
            <button
              type="button"
              onClick={() => setShowDuplicates((v) => !v)}
              aria-expanded={showDuplicates}
              className="w-full flex items-center justify-between gap-3 px-4 py-3 text-left"
              style={{ background: "rgba(251,191,36,0.06)" }}
            >
              <span className="flex items-center gap-2 text-sm">
                {showDuplicates ? (
                  <ChevronDown size={15} style={{ color: "#fbbf24" }} />
                ) : (
                  <ChevronRight size={15} style={{ color: "#fbbf24" }} />
                )}
                <span style={{ color: "var(--ff-text-primary)", fontWeight: 500 }}>
                  {duplicateRows.length} possível
                  {duplicateRows.length > 1 ? " duplicadas" : " duplicada"}{" "}
                  (não serão importadas)
                </span>
              </span>
              <span className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
                {showDuplicates ? "Ocultar" : "Revisar"}
              </span>
            </button>

            {showDuplicates && (
              <div>
                <p
                  className="px-4 py-2 text-xs"
                  style={{
                    color: "var(--ff-text-muted)",
                    background: "var(--ff-bg-elevated)",
                  }}
                >
                  Estes lançamentos já parecem existir nesta conta. Marque os que
                  quiser importar mesmo assim.
                </p>
                <div
                  className="divide-y"
                  style={{ borderColor: "var(--ff-border)" }}
                >
                  {duplicateRows.map(renderRow)}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    );
  },
);

ImportPreviewTable.displayName = "ImportPreviewTable";
export default ImportPreviewTable;
