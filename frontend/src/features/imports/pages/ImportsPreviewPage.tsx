import { useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { ArrowLeft, CheckCircle, Loader2, FileText } from "lucide-react";
import axios from "axios";
import {
  useImportPreview,
  useConfirmImport,
  useChangeImportAccount,
} from "../api/useImports";
import {
  useCategories,
  useGoalCategories,
} from "@/features/categories/api/useCategories";
import AccountSelect from "@/features/accounts/components/AccountSelect";
import ImportPreviewTable, {
  type ImportPreviewTableHandle,
  type ImportPreviewSummary,
} from "../components/ImportPreviewTable";
import { toast } from "sonner";

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

interface ApiErrorBody {
  message?: string;
  errors?: Record<string, string[]>;
}

export default function ImportsPreviewPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: preview, isLoading } = useImportPreview(id ?? null);
  const { data: categories = [] } = useCategories();
  const { data: goalCategories = [] } = useGoalCategories();
  const { mutate: confirm, isPending } = useConfirmImport(id ?? "");
  const { mutate: changeAccount, isPending: isChangingAccount } =
    useChangeImportAccount(id ?? "");
  const tableRef = useRef<ImportPreviewTableHandle>(null);

  const [summary, setSummary] = useState<ImportPreviewSummary>({
    selected: 0,
    importAnyway: 0,
    missingCategory: 0,
    linked: 0,
  });
  const [errorIds, setErrorIds] = useState<string[]>([]);

  // Enquanto o Worker não processa o arquivo, as duplicatas ainda não foram detectadas
  const isProcessing =
    preview?.status === "Pending" || preview?.status === "Processing";
  const hasFailed = preview?.status === "Failed";

  const toImport = summary.selected + summary.importAnyway;
  const hasTransactions =
    !isLoading && !isProcessing && (preview?.transactions.length ?? 0) > 0;
  const canConfirm =
    !isPending &&
    !isChangingAccount &&
    toImport > 0 &&
    summary.missingCategory === 0;

  function handleConfirm() {
    const transactions = tableRef.current?.getTransactions() ?? [];

    if (transactions.length === 0) {
      toast.error("Selecione ao menos uma transação para importar.");
      return;
    }

    // Segunda barreira: a tabela já bloqueia o botão, mas o back-end também recusa
    if (transactions.some((t) => t.categoryId === EMPTY_GUID && t.type !== "Transfer")) {
      toast.error("Escolha a categoria de todas as linhas selecionadas.");
      return;
    }

    setErrorIds([]);

    confirm(
      { transactions },
      {
        onSuccess: () => {
          toast.success(
            `${transactions.length} transações importadas com sucesso!`,
          );
          navigate("/imports");
        },
        onError: (error) => {
          const body = axios.isAxiosError<ApiErrorBody>(error)
            ? error.response?.data
            : undefined;

          // A API devolve os ids das linhas recusadas: destaca-as na tabela
          if (body?.errors && Object.keys(body.errors).length > 0) {
            setErrorIds(Object.keys(body.errors));
            toast.error(
              "Algumas linhas precisam de ajuste. Corrija as linhas destacadas.",
            );
            return;
          }

          toast.error(
            body?.message ?? "Erro ao confirmar importação. Tente novamente.",
          );
        },
      },
    );
  }

  function handleChangeAccount(accountId: string) {
    if (!accountId || accountId === preview?.accountId) return;
    changeAccount(accountId, {
      onSuccess: () => {
        setErrorIds([]);
        toast.success("Conta alterada. Duplicatas verificadas novamente.");
      },
      onError: (error) => {
        const body = axios.isAxiosError<ApiErrorBody>(error)
          ? error.response?.data
          : undefined;
        toast.error(body?.message ?? "Não foi possível trocar a conta.");
      },
    });
  }

  const duplicateCount =
    preview?.transactions.filter((t) => t.isDuplicate).length ?? 0;

  return (
    <div className="space-y-6">
      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate("/imports")}
            aria-label="Voltar"
            className="p-2 rounded-xl transition-colors"
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
            <ArrowLeft size={18} />
          </button>
          <div>
            <h1
              className="text-xl font-semibold"
              style={{ color: "var(--ff-text-primary)" }}
            >
              Preview da importação
            </h1>
            <p
              className="text-sm mt-0.5"
              style={{ color: "var(--ff-text-muted)" }}
            >
              {preview?.fileName ?? "..."}
            </p>
          </div>
        </div>

        {hasTransactions && (
          <button
            onClick={handleConfirm}
            disabled={!canConfirm}
            title={
              summary.missingCategory > 0
                ? "Escolha a categoria das linhas selecionadas ou desmarque-as"
                : undefined
            }
            className="flex items-center gap-2 h-9 px-4 rounded-xl text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            style={{
              background: "var(--ff-emerald)",
              color: "var(--ff-emerald-subtle)",
            }}
            onMouseEnter={(e) => {
              if (canConfirm)
                e.currentTarget.style.background = "var(--ff-emerald-hover)";
            }}
            onMouseLeave={(e) =>
              (e.currentTarget.style.background = "var(--ff-emerald)")
            }
          >
            {isPending ? (
              <>
                <Loader2 size={15} className="animate-spin" /> A importar...
              </>
            ) : (
              <>
                <CheckCircle size={15} /> Confirmar
              </>
            )}
          </button>
        )}
      </div>

      {/* Resumo do arquivo */}
      {preview && (
        <div
          className="flex flex-wrap items-center gap-6 px-5 py-4 rounded-xl"
          style={{
            background: "var(--ff-bg-card)",
            border: "1px solid var(--ff-border)",
          }}
        >
          <div className="flex items-center gap-3">
            <div
              className="w-9 h-9 rounded-xl flex items-center justify-center"
              style={{ background: "var(--ff-bg-elevated)" }}
            >
              <FileText size={16} style={{ color: "var(--ff-emerald)" }} />
            </div>
            <div>
              <p
                className="text-sm font-medium"
                style={{ color: "var(--ff-text-primary)" }}
              >
                {preview.fileName}
              </p>
              <p className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
                {preview.totalRecords} transações encontradas
              </p>
            </div>
          </div>

          {/* Conta de destino: duplicatas só fazem sentido dentro de uma conta */}
          {preview.accountId && (
            <div className="flex items-center gap-2">
              <span className="text-xs" style={{ color: "var(--ff-text-muted)" }}>
                Conta de destino
              </span>
              <div style={{ minWidth: 180 }}>
                <AccountSelect
                  value={preview.accountId}
                  onChange={handleChangeAccount}
                  currentAccountId={preview.accountId}
                  style={{ opacity: isChangingAccount ? 0.6 : 1 }}
                />
              </div>
              {preview.accountName && (
                <span className="sr-only">{preview.accountName}</span>
              )}
            </div>
          )}

          {!isProcessing && duplicateCount > 0 && (
            <div
              className="flex items-center gap-2 px-3 py-1.5 rounded-lg text-xs"
              style={{ background: "rgba(251,191,36,0.12)", color: "#fbbf24" }}
            >
              {duplicateCount} duplicada{duplicateCount > 1 ? "s" : ""} detetada
              {duplicateCount > 1 ? "s" : ""}
            </div>
          )}
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <Loader2
            size={24}
            className="animate-spin"
            style={{ color: "var(--ff-emerald)" }}
          />
        </div>
      )}

      {/* Processando */}
      {!isLoading && isProcessing && (
        <div
          role="status"
          className="flex flex-col items-center justify-center py-16 space-y-3"
        >
          <Loader2
            size={24}
            className="animate-spin"
            style={{ color: "var(--ff-emerald)" }}
          />
          <p className="text-sm" style={{ color: "var(--ff-text-muted)" }}>
            Processando o arquivo e verificando duplicatas...
          </p>
        </div>
      )}

      {/* Falha no processamento */}
      {!isLoading && hasFailed && (
        <p
          role="alert"
          className="text-sm text-center py-10"
          style={{ color: "#f87171" }}
        >
          Não foi possível processar este arquivo. Envie-o novamente.
        </p>
      )}

      {/* Tabela — recriada quando a conta muda, pois as duplicatas são recalculadas */}
      {hasTransactions && (
        <ImportPreviewTable
          key={`${preview!.accountId ?? "none"}:${preview!.status}`}
          ref={tableRef}
          transactions={preview!.transactions}
          categories={[...categories, ...goalCategories]}
          errorIds={errorIds}
          onSummaryChange={setSummary}
        />
      )}

      {/* Estado vazio */}
      {!isLoading &&
        !isProcessing &&
        !hasFailed &&
        (preview?.transactions.length ?? 0) === 0 && (
          <div className="flex flex-col items-center justify-center py-20 space-y-3">
            <div
              className="w-14 h-14 rounded-2xl flex items-center justify-center"
              style={{ background: "var(--ff-bg-card)" }}
            >
              <FileText size={24} style={{ color: "var(--ff-text-muted)" }} />
            </div>
            <p className="text-sm" style={{ color: "var(--ff-text-muted)" }}>
              Nenhuma transação encontrada neste arquivo
            </p>
          </div>
        )}
    </div>
  );
}
