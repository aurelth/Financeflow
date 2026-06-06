import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import AssistantPage from "@/features/assistant/pages/AssistantPage";

// Mock do hook useAssistant
const mockSendMessage = vi.fn();
const mockClearMessages = vi.fn();

const mockUseAssistant = {
  messages: [] as { role: "user" | "assistant"; content: string }[],
  isLoading: false,
  error: null as string | null,
  sendMessage: mockSendMessage,
  clearMessages: mockClearMessages,
};

vi.mock("@/features/assistant/api/useAssistant", () => ({
  useAssistant: () => mockUseAssistant,
}));

const renderPage = () => {
  const qc = new QueryClient();
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <AssistantPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
};

describe("AssistantPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAssistant.messages = [];
    mockUseAssistant.isLoading = false;
    mockUseAssistant.error = null;
  });

  it("deve renderizar o título da página", () => {
    renderPage();
    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent(
      "Assistente IA",
    );
  });

  it("deve exibir mensagem de boas-vindas quando sem mensagens", () => {
    renderPage();
    expect(
      screen.getByText(/Olá! Sou o seu assistente financeiro/i),
    ).toBeInTheDocument();
  });

  it("deve exibir sugestões de perguntas no estado inicial", () => {
    renderPage();
    expect(screen.getByText("Quanto gastei este mês?")).toBeInTheDocument();
    expect(
      screen.getByText("Estou dentro do meu orçamento?"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Como foram minhas finanças no mês passado?"),
    ).toBeInTheDocument();
  });

  it("deve preencher o input ao clicar numa sugestão", async () => {
    renderPage();
    const user = userEvent.setup();

    await user.click(screen.getByText("Quanto gastei este mês?"));

    const textarea = screen.getByPlaceholderText(/Escreva a sua pergunta/i);
    expect(textarea).toHaveValue("Quanto gastei este mês?");
  });

  it("deve chamar sendMessage ao clicar no botão enviar", async () => {
    renderPage();
    const user = userEvent.setup();

    const textarea = screen.getByPlaceholderText(/Escreva a sua pergunta/i);
    await user.type(textarea, "Qual o meu saldo?");
    await user.click(screen.getByRole("button", { name: "" })); // botão Send

    expect(mockSendMessage).toHaveBeenCalledWith("Qual o meu saldo?");
  });

  it("deve chamar sendMessage ao pressionar Enter", async () => {
    renderPage();
    const user = userEvent.setup();

    const textarea = screen.getByPlaceholderText(/Escreva a sua pergunta/i);
    await user.type(textarea, "Como estão meus gastos?");
    await user.keyboard("{Enter}");

    expect(mockSendMessage).toHaveBeenCalledWith("Como estão meus gastos?");
  });

  it("deve exibir mensagens do histórico", () => {
    mockUseAssistant.messages = [
      { role: "user", content: "Quanto gastei?" },
      { role: "assistant", content: "Você gastou R$ 500,00." },
    ];

    renderPage();

    expect(screen.getByText("Quanto gastei?")).toBeInTheDocument();
    expect(screen.getByText("Você gastou R$ 500,00.")).toBeInTheDocument();
  });

  it("deve exibir loading state quando isLoading é true", () => {
    mockUseAssistant.isLoading = true;
    mockUseAssistant.messages = [{ role: "user", content: "Pergunta" }];

    renderPage();

    expect(screen.getByText("A analisar os seus dados...")).toBeInTheDocument();
  });

  it("deve exibir mensagem de erro quando error não é null", () => {
    mockUseAssistant.error =
      "Não foi possível obter uma resposta. Tente novamente.";

    renderPage();

    expect(
      screen.getByText("Não foi possível obter uma resposta. Tente novamente."),
    ).toBeInTheDocument();
  });

  it("deve exibir botão de limpar quando há mensagens", () => {
    mockUseAssistant.messages = [{ role: "user", content: "Olá" }];

    renderPage();

    expect(screen.getByText("Limpar conversa")).toBeInTheDocument();
  });

  it("deve chamar clearMessages ao clicar em limpar conversa", async () => {
    mockUseAssistant.messages = [{ role: "user", content: "Olá" }];

    renderPage();
    const user = userEvent.setup();

    await user.click(screen.getByText("Limpar conversa"));

    expect(mockClearMessages).toHaveBeenCalledOnce();
  });

  it("não deve exibir botão de limpar quando não há mensagens", () => {
    renderPage();
    expect(screen.queryByText("Limpar conversa")).not.toBeInTheDocument();
  });

  it("deve limpar o input após enviar mensagem", async () => {
    mockSendMessage.mockResolvedValueOnce(undefined);

    renderPage();
    const user = userEvent.setup();

    const textarea = screen.getByPlaceholderText(/Escreva a sua pergunta/i);
    await user.type(textarea, "Teste de limpeza");
    await user.keyboard("{Enter}");

    await waitFor(() => {
      expect(textarea).toHaveValue("");
    });
  });

  it("deve preencher o input ao clicar na sugestão de mês passado", async () => {
    renderPage();
    const user = userEvent.setup();

    await user.click(
      screen.getByText("Como foram minhas finanças no mês passado?"),
    );

    const textarea = screen.getByPlaceholderText(/Escreva a sua pergunta/i);
    expect(textarea).toHaveValue("Como foram minhas finanças no mês passado?");
  });
});
