using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.RegisterUser;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IMapper mapper)
    : IRequestHandler<RegisterUserCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        // Verifica se email já está em uso
        var emailExists = await userRepository.ExistsByEmailAsync(
            request.Email, cancellationToken);

        if (emailExists)
            throw new ValidationException(
                "O email informado já está em uso.",
                new Dictionary<string, string[]>
                {
                    { "Email", ["O email informado já está em uso."] }
                });

        // Cria o utilizador
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordService.Hash(request.Password),
            Currency = request.Currency ?? "BRL",
            Timezone = request.Timezone ?? "America/Sao_Paulo",
            CreatedAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user, cancellationToken);

        return mapper.Map<UserProfileDto>(user);
    }
}
