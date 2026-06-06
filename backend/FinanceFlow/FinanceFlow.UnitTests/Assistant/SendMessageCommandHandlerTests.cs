using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.UseCases.Assistant.Commands.SendMessage;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Assistant;

public class SendMessageCommandHandlerTests
{
    private readonly Mock<IFinancialContextService> _financialContextService = new();
    private readonly Mock<IAnthropicService> _anthropicService = new();

    private static readonly Guid UserId = Guid.NewGuid();

    private SendMessageCommandHandler CreateHandler() =>
        new(_financialContextService.Object, _anthropicService.Object);

    [Fact]
    public async Task Handle_DeveRetornarResposta_QuandoServicosFuncionam()
    {
        // Arrange
        var command = new SendMessageCommand(UserId, "Quanto gastei este mês?");

        _financialContextService
            .Setup(s => s.BuildContextAsync(UserId, It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync("=== CONTEXTO FINANCEIRO ===\nDespesas: R$ 500,00");

        _anthropicService
            .Setup(s => s.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                default))
            .ReturnsAsync("Você gastou R$ 500,00 este mês.");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Reply.Should().Be("Você gastou R$ 500,00 este mês.");

        _financialContextService.Verify(s =>
            s.BuildContextAsync(UserId, It.IsAny<int>(), It.IsAny<int>(), default), Times.Once);

        _anthropicService.Verify(s =>
            s.SendMessageAsync(
                It.IsAny<string>(),
                It.Is<string>(m => m.Contains("Quanto gastei este mês?")),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarStringVazia_QuandoAPIRetornaVazio()
    {
        // Arrange
        var command = new SendMessageCommand(UserId, "Pergunta qualquer");

        _financialContextService
            .Setup(s => s.BuildContextAsync(UserId, It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync("=== CONTEXTO ===");

        _anthropicService
            .Setup(s => s.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                default))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Reply.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DevePassarContextoNaMensagem_QuandoChamadaRealizada()
    {
        // Arrange
        var command = new SendMessageCommand(UserId, "Estou no limite do orçamento?");
        var contextoEsperado = "=== CONTEXTO FINANCEIRO ===\nOrçamentos: OK";

        _financialContextService
            .Setup(s => s.BuildContextAsync(UserId, It.IsAny<int>(), It.IsAny<int>(), default))
            .ReturnsAsync(contextoEsperado);

        _anthropicService
            .Setup(s => s.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                default))
            .ReturnsAsync("Não, está dentro do limite.");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — verifica que o contexto financeiro foi embutido na mensagem enviada à API
        _anthropicService.Verify(s =>
            s.SendMessageAsync(
                It.IsAny<string>(),
                It.Is<string>(m =>
                    m.Contains(contextoEsperado) &&
                    m.Contains("Estou no limite do orçamento?")),
                default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveIdentificarMesNaMensagem_QuandoUtilizadorMencionaMes()
    {
        // Arrange
        var command = new SendMessageCommand(UserId, "Como foram as minhas finanças em abril?");

        _financialContextService
            .Setup(s => s.BuildContextAsync(UserId, 4, It.IsAny<int>(), default))
            .ReturnsAsync("=== CONTEXTO FINANCEIRO — ABRIL ===");

        _anthropicService
            .Setup(s => s.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                default))
            .ReturnsAsync("Em abril as suas finanças estavam equilibradas.");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert — verifica que o mês 4 (abril) foi extraído corretamente
        result.Reply.Should().Be("Em abril as suas finanças estavam equilibradas.");

        _financialContextService.Verify(s =>
            s.BuildContextAsync(UserId, 4, It.IsAny<int>(), default), Times.Once);
    }
}
