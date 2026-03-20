using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Users.Queries.GetUserProfile;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Users;

public class GetUserProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly IMapper _mapper;

    public GetUserProfileQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<UserMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private GetUserProfileQueryHandler CreateHandler() =>
        new(_userRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveRetornarPerfil_QuandoUtilizadorExiste()
    {
        // Arrange
        var user = UserBuilder.Build();
        var query = new GetUserProfileQuery(user.Id);

        _userRepository
            .Setup(r => r.GetByIdAsync(user.Id, default))
            .ReturnsAsync(user);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id);
        result.Name.Should().Be(user.Name);
        result.Email.Should().Be(user.Email);
        result.Currency.Should().Be(user.Currency);
        result.Timezone.Should().Be(user.Timezone);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoUtilizadorNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserProfileQuery(userId);

        _userRepository
            .Setup(r => r.GetByIdAsync(userId, default))
            .ReturnsAsync((Domain.Entities.User?)null);

        // Act
        var act = async () => await CreateHandler().Handle(query, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{userId}*");
    }
}
