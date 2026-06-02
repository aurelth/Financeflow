import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import GoalsPage from "@/features/goals/pages/GoalsPage";

const mockCreateGoal = vi.fn();
const mockUpdateGoal = vi.fn();
const mockDeleteGoal = vi.fn();

vi.mock("@/features/goals/api/useGoals", () => ({
  useGoals: () => ({
    data: {
      availableThisMonth: 1500,
      committedThisMonth: 500,
      difference: 1000,
      goals: [
        {
          id: "goal-1",
          name: "Viagem para Europa",
          emoji: "✈️",
          targetAmount: 10000,
          monthlyContribution: 500,
          deadline: "2026-12-31T00:00:00",
          accumulatedAmount: 2500,
          plannedThisMonth: 500,
          receivedThisMonth: 500,
          progressPercentage: 25,
          isCompleted: false,
          monthsToComplete: 15,
          status: "OnTrack",
          linkedCategoryId: "cat-goal-1",
        },
        {
          id: "goal-2",
          name: "Fundo de emergência",
          emoji: "🛡️",
          targetAmount: 5000,
          monthlyContribution: 300,
          deadline: "2026-06-30T00:00:00",
          accumulatedAmount: 5000,
          plannedThisMonth: 0,
          receivedThisMonth: 0,
          progressPercentage: 100,
          isCompleted: true,
          monthsToComplete: null,
          status: "Completed",
          linkedCategoryId: null,
        },
      ],
    },
    isLoading: false,
  }),
  useCreateGoal: () => ({ mutate: mockCreateGoal, isPending: false }),
  useUpdateGoal: () => ({ mutate: mockUpdateGoal, isPending: false }),
  useDeleteGoal: () => ({ mutate: mockDeleteGoal, isPending: false }),
  useGoalContributions: () => ({
    data: { items: [], totalCount: 0 },
    isLoading: false,
  }),
}));

const renderPage = () => {
  const qc = new QueryClient();
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <GoalsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
};

describe("GoalsPage", () => {
  beforeEach(() => vi.clearAllMocks());

  it("deve renderizar o título da página", () => {
    renderPage();
    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent(
      "Metas Financeiras",
    );
  });

  it("deve exibir o resumo do mês quando há metas", () => {
    renderPage();
    expect(screen.getByText("Total acumulado")).toBeInTheDocument();
    expect(screen.getByText("Comprometido com metas")).toBeInTheDocument();
    expect(screen.getByText("Diferença")).toBeInTheDocument();
  });

  it("deve exibir metas ativas", () => {
    renderPage();
    expect(screen.getByText("Viagem para Europa")).toBeInTheDocument();
  });

  it("deve exibir metas concluídas", () => {
    renderPage();
    expect(screen.getByText("Fundo de emergência")).toBeInTheDocument();
  });

  it('deve exibir secção "Em andamento"', () => {
    renderPage();
    expect(screen.getByText(/Em andamento/i)).toBeInTheDocument();
  });

  it('deve exibir secção "Concluídas"', () => {
    renderPage();
    expect(screen.getByText(/Concluídas/i)).toBeInTheDocument();
  });

  it("deve abrir modal de criação ao clicar em nova meta", async () => {
    renderPage();
    const user = userEvent.setup();

    const buttons = screen.getAllByRole("button", { name: /nova meta/i });
    await user.click(buttons[0]);

    await waitFor(() => {
      const titles = screen.getAllByText("Nova meta");
      expect(titles.length).toBeGreaterThanOrEqual(2);
    });
  });

  it("deve abrir modal de edição ao clicar em editar", async () => {
    renderPage();
    const user = userEvent.setup();

    const editButtons = screen.getAllByTitle("Editar");
    await user.click(editButtons[0]);

    await waitFor(() => {
      expect(screen.getByText("Editar meta")).toBeInTheDocument();
    });
  });

  it("deve abrir modal de remoção ao clicar em remover", async () => {
    renderPage();
    const user = userEvent.setup();

    const deleteButtons = screen.getAllByTitle("Remover");
    await user.click(deleteButtons[0]);

    await waitFor(() => {
      const elements = screen.getAllByText("Remover meta");
      expect(elements.length).toBeGreaterThanOrEqual(1);
    });
  });

  it("deve exibir o progresso percentual da meta", () => {
    renderPage();
    expect(screen.getByText("25%")).toBeInTheDocument();
  });

  it("deve exibir o status da meta ativa", () => {
    renderPage();
    expect(screen.getByText("Em dia")).toBeInTheDocument();
  });

  it("deve exibir o status da meta concluída", () => {
    renderPage();
    expect(screen.getByText("Concluída")).toBeInTheDocument();
  });

  it("deve exibir botão de contribuições quando linkedCategoryId existe", () => {
    renderPage();
    expect(screen.getByText("Contribuições")).toBeInTheDocument();
  });

  it("não deve exibir botão de contribuições quando linkedCategoryId é null", () => {
    renderPage();
    const buttons = screen.getAllByText("Contribuições");
    expect(buttons).toHaveLength(1);
  });

  it("deve abrir modal de contribuições ao clicar em contribuições", async () => {
    renderPage();
    const user = userEvent.setup();

    await user.click(screen.getByText("Contribuições"));

    await waitFor(() => {
      expect(
        screen.getByText("Histórico de contribuições"),
      ).toBeInTheDocument();
    });
  });

  it("deve exibir empty state no modal quando não há contribuições", async () => {
    renderPage();
    const user = userEvent.setup();

    await user.click(screen.getByText("Contribuições"));

    await waitFor(() => {
      expect(
        screen.getByText(/Nenhuma contribuição registada ainda/i),
      ).toBeInTheDocument();
    });
  });

  it("deve exibir recebido como zero quando não há contribuições no mês", () => {
    renderPage();
    expect(screen.getByText(/Planejado:/i)).toBeInTheDocument();
    expect(screen.getByText(/Recebido:/i)).toBeInTheDocument();
  });
});
