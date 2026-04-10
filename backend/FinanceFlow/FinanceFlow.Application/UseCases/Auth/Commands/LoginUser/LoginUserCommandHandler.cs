using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.LoginUser;

public class LoginUserCommandHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    IMapper mapper)
    : IRequestHandler<LoginUserCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        // Busca utilizador activo pelo email (respeita soft-delete)
        var user = await userRepository.GetActiveByEmailAsync(
            request.Email.Trim().ToLowerInvariant(), cancellationToken);

        if (user is null)
        {
            // Verifica se existe conta excluída com este email
            var deletedUser = await userRepository.GetByEmailAsync(
                request.Email.Trim().ToLowerInvariant(), cancellationToken);

            if (deletedUser is not null)
                throw new UnauthorizedException("Esta conta foi excluída e não pode ser acessada.");

            // Mensagem genérica — não revela se o email existe ou não
            throw new UnauthorizedException("Email ou senha incorreto.");
        }

        if (!passwordService.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email ou senha incorreto.");

        // Gera tokens
        var accessToken = tokenService.GenerateAccessToken(user);
        var refreshToken = tokenService.GenerateRefreshToken();

        // Salva Refresh Token no Redis
        await refreshTokenService.SaveAsync(user.Id, refreshToken, cancellationToken);

        return new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            TokenType: "Bearer",
            ExpiresIn: 15 * 60,
            User: mapper.Map<UserProfileDto>(user)
        );
    }
}
