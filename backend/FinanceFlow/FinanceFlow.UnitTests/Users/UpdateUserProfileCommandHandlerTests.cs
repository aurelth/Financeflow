using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Users.Commands.UpdateUserProfile;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Users;

public class UpdateUserProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();

    private UpdateUserProfileCommandHandler CreateHandler() =>
        new(_userRepository.Object);

    private void SetupUser(Guid userId, User user) =>
        _userRepository
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

    [Fact]
    public async Task Handle_DeveActualizarLanguage_QuandoIdiomaValido()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Currency = "BRL", Timezone = "America/Sao_Paulo", Language = "pt-BR" };

        SetupUser(userId, user);

        var command = new UpdateUserProfileCommand(userId, "USD", "America/New_York", "en-US");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        user.Language.Should().Be("en-US");
        user.Currency.Should().Be("USD");
        result.Id.Should().Be(userId);
        result.Language.Should().Be("en-US");
        result.Currency.Should().Be("USD");
        result.Timezone.Should().Be("America/New_York");
        _userRepository.Verify(r => r.UpdateAsync(user, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveUsarPtBr_QuandoLanguageInvalido()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Currency = "BRL", Timezone = "America/Sao_Paulo", Language = "pt-BR" };

        SetupUser(userId, user);

        var command = new UpdateUserProfileCommand(userId, "BRL", "America/Sao_Paulo", "de-DE");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert — idioma inválido deve fazer fallback para pt-BR
        user.Language.Should().Be("pt-BR");
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoUserNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepository
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((User?)null);

        var command = new UpdateUserProfileCommand(userId, "BRL", "America/Sao_Paulo", "pt-BR");

        // Act
        var act = () => CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _userRepository.Verify(r => r.UpdateAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveActualizarTodosCampos_QuandoDadosValidos()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Currency = "BRL", Timezone = "America/Sao_Paulo", Language = "pt-BR" };

        SetupUser(userId, user);

        var command = new UpdateUserProfileCommand(userId, "EUR", "Europe/Paris", "fr-FR");

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        user.Currency.Should().Be("EUR");
        user.Timezone.Should().Be("Europe/Paris");
        user.Language.Should().Be("fr-FR");
    }
}
