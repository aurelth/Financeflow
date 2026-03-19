using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    IMapper mapper)
    : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // Extrai UserId do Access Token expirado
        var userId = tokenService.GetUserIdFromExpiredToken(request.AccessToken);

        if (userId is null)
            throw new UnauthorizedException("Token inválido.");

        // Valida Refresh Token no Redis
        var isValid = await refreshTokenService.ValidateAsync(
            userId.Value, request.RefreshToken, cancellationToken);

        if (!isValid)
            throw new UnauthorizedException("Refresh Token inválido ou expirado.");

        // Busca utilizador
        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);

        if (user is null)
            throw new UnauthorizedException("Utilizador não encontrado.");

        // Rotaciona tokens — revoga o antigo e gera novos
        await refreshTokenService.RevokeAsync(userId.Value, cancellationToken);

        var newAccessToken = tokenService.GenerateAccessToken(user);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        await refreshTokenService.SaveAsync(userId.Value, newRefreshToken, cancellationToken);

        return new AuthResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            TokenType: "Bearer",
            ExpiresIn: 15 * 60,
            User: mapper.Map<UserProfileDto>(user)
        );
    }
}
