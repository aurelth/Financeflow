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
        // Busca utilizador pelo email
        var user = await userRepository.GetByEmailAsync(
            request.Email.Trim().ToLowerInvariant(), cancellationToken);

        // Mensagem genérica — não revela se o email existe ou não
        if (user is null || !passwordService.Verify(request.Password, user.PasswordHash))
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
            ExpiresIn: 15 * 60, // 15 minutos em segundos
            User: mapper.Map<UserProfileDto>(user)
        );
    }
}
