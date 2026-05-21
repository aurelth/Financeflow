import { useState, useEffect } from "react";
import {
  ChevronLeft,
  ChevronRight,
  TrendingUp,
  TrendingDown,
  Wallet,
  CalendarClock,
  Loader2,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import {
  useDashboardSummary,
  useBalanceEvolution,
  useExpensesByCategory,
  useWeeklyComparison,
} from "../api/useDashboard";
import { useBudgetSummary } from "@/features/budgets/api/useBudgets";
import { useTransactions } from "@/features/transactions/api/useTransactions";
import LineChartCard from "../components/LineChartCard";
import PieChartCard from "../components/PieChartCard";
import BarChartCard from "../components/BarChartCard";
import RecentTransactionsWidget from "../components/RecentTransactionsWidget";
import TopBudgetsWidget from "../components/TopBudgetsWidget";
import PdfExportButton from "@/features/reports/components/PdfExportButton";
import { useOnboarding } from "@/hooks/useOnboarding";
import { createDriver } from "@/lib/driver";
import { getTourSteps } from "@/features/onboarding/steps/tourSteps";

function getCurrentPeriod() {
  const now = new Date();
  return { month: now.getMonth() + 1, year: now.getFullYear() };
}

interface SummaryCardProps {
  title: string;
  value: number;
  icon: React.ReactNode;
  iconBg: string;
  subtitle?: string;
}

function SummaryCard({
  title,
  value,
  icon,
  iconBg,
  subtitle,
}: SummaryCardProps) {
  return (
    <div
      className="rounded-2xl p-5 flex items-center gap-4"
      style={{
        background: "var(--ff-bg-card)",
        border: "1px solid var(--ff-border)",
      }}
    >
      <div
        className="w-11 h-11 rounded-xl flex items-center justify-center flex-shrink-0"
        style={{ background: iconBg }}
      >
        {icon}
      </div>
      <div>
        <p
          className="text-xs uppercase tracking-wide mb-0.5"
          style={{ color: "var(--ff-text-muted)" }}
        >
          {title}
        </p>
        <p
          className="text-lg font-semibold"
          style={{ color: "var(--ff-text-primary)" }}
        >
          {value.toLocaleString("pt-BR", {
            style: "currency",
            currency: "BRL",
          })}
        </p>
        {subtitle && (
          <p
            className="text-xs mt-0.5"
            style={{ color: "var(--ff-text-muted)" }}
          >
            {subtitle}
          </p>
        )}
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const { t } = useTranslation("onboarding");
  const [period, setPeriod] = useState(getCurrentPeriod);
  const { markAsSeen } = useOnboarding();

  const monthLabel = new Date(period.year, period.month - 1).toLocaleString(
    "pt-BR",
    {
      month: "long",
      year: "numeric",
    },
  );

  const dateFrom = new Date(period.year, period.month - 1, 1)
    .toISOString()
    .split("T")[0];
  const dateTo = new Date(period.year, period.month, 0)
    .toISOString()
    .split("T")[0];

  const { data: summary, isLoading: l1 } = useDashboardSummary(period);
  const { data: balanceEvolution = [], isLoading: l2 } =
    useBalanceEvolution(period);
  const { data: expensesByCategory = [], isLoading: l3 } =
    useExpensesByCategory(period);
  const { data: weeklyComparison = [], isLoading: l4 } =
    useWeeklyComparison(period);
  const { data: budgetSummaries = [], isLoading: l5 } =
    useBudgetSummary(period);
  const { data: transactionsData, isLoading: l6 } = useTransactions({
    page: 1,
    pageSize: 5,
    dateFrom,
    dateTo,
  });

  const isLoading = l1 || l2 || l3 || l4 || l5 || l6;

  useEffect(() => {    
    if (isLoading) return;

    const shouldStart = !localStorage.getItem("onboarding_seen");
    console.log("shouldStart:", shouldStart);
    if (!shouldStart) return;

    const timeout = setTimeout(() => {
      const steps = getTourSteps(t);
      console.log("steps gerados:", steps);
      console.log(
        "elemento da sidebar existe?",
        document.querySelector('a[href="/dashboard"]'),
      );

      const driverObj = createDriver({
        steps,
        onDestroyStarted: () => {
          markAsSeen();
          driverObj.destroy();
        },
      });
      driverObj.drive();
    }, 800);

    return () => clearTimeout(timeout);
  }, [isLoading, markAsSeen, t]);

  const recentTx = transactionsData?.items ?? [];

  function handlePrevMonth() {
    setPeriod((p) => {
      const date = new Date(p.year, p.month - 2);
      return { month: date.getMonth() + 1, year: date.getFullYear() };
    });
  }

  function handleNextMonth() {
    setPeriod((p) => {
      const date = new Date(p.year, p.month);
      return { month: date.getMonth() + 1, year: date.getFullYear() };
    });
  }

  return (
    <div className="space-y-6">
      {/* Cabeçalho */}
      <div className="flex items-center justify-between">
        <div>
          <h1
            className="text-xl font-semibold"
            style={{ color: "var(--ff-text-primary)" }}
          >
            Dashboard
          </h1>
          <p
            className="text-sm mt-0.5"
            style={{ color: "var(--ff-text-muted)" }}
          >
            Visão geral das suas finanças
          </p>
        </div>
        <PdfExportButton
          defaultMonth={period.month}
          defaultYear={period.year}
        />
      </div>

      {/* Seletor de mês/ano */}
      <div
        className="flex items-center justify-between rounded-2xl px-5 py-3"
        style={{
          background: "var(--ff-bg-card)",
          border: "1px solid var(--ff-border)",
        }}
      >
        <button
          onClick={handlePrevMonth}
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
          <ChevronLeft size={18} />
        </button>
        <span
          className="font-medium capitalize"
          style={{ color: "var(--ff-text-primary)" }}
        >
          {monthLabel}
        </span>
        <button
          onClick={handleNextMonth}
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
          <ChevronRight size={18} />
        </button>
      </div>

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

      {!isLoading && (
        <div className="space-y-6">
          {/* Cards de sumário */}
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
            <SummaryCard
              title="Receitas"
              value={summary?.totalIncome ?? 0}
              icon={
                <TrendingUp size={20} style={{ color: "var(--ff-income)" }} />
              }
              iconBg="rgba(16, 185, 129, 0.1)"
            />
            <SummaryCard
              title="Despesas"
              value={summary?.totalExpenses ?? 0}
              icon={
                <TrendingDown
                  size={20}
                  style={{ color: "var(--ff-expense)" }}
                />
              }
              iconBg="rgba(244, 63, 94, 0.1)"
            />
            <SummaryCard
              title="Saldo"
              value={summary?.balance ?? 0}
              icon={
                <Wallet size={20} style={{ color: "var(--ff-scheduled)" }} />
              }
              iconBg="rgba(99, 102, 241, 0.1)"
            />
            <SummaryCard
              title="Saldo Projetado"
              value={summary?.projectedBalance ?? 0}
              icon={
                <CalendarClock
                  size={20}
                  style={{ color: "var(--ff-pending)" }}
                />
              }
              iconBg="rgba(245, 158, 11, 0.1)"
              subtitle="Inclui transações agendadas"
            />
          </div>

          {/* Gráficos — linha 1 */}
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
            <LineChartCard data={balanceEvolution} />
            <PieChartCard data={expensesByCategory} />
          </div>

          {/* Gráfico de barras */}
          <BarChartCard data={weeklyComparison} />

          {/* Widgets — linha final */}
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
            <RecentTransactionsWidget transactions={recentTx} />
            <TopBudgetsWidget summaries={budgetSummaries} />
          </div>
        </div>
      )}
    </div>
  );
}
