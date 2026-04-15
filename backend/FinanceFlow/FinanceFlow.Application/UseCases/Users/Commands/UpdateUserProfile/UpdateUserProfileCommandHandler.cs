using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler(
    IUserRepository userRepository,
    IMapper mapper)
    : IRequestHandler<UpdateUserProfileCommand, UserProfileDto>
{
    private static readonly HashSet<string> SupportedLanguages =
        new HashSet<string> { "pt-BR", "en-US", "es-ES", "fr-FR" };
    public async Task<UserProfileDto> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            request.UserId, cancellationToken);

        if (user is null)
            throw new NotFoundException("Utilizador", request.UserId);
        
        user.Currency = request.Currency.Trim();
        user.Timezone = request.Timezone.Trim();
        user.Language = SupportedLanguages.Contains(request.Language.Trim())
            ? request.Language.Trim()
            : "pt-BR";
        user.UpdatedAt = DateTime.UtcNow;

        await userRepository.UpdateAsync(user, cancellationToken);

        return mapper.Map<UserProfileDto>(user);
    }
}
