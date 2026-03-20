using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Users.Commands.UpdateUserProfile;

public record UpdateUserProfileCommand(
    Guid UserId,
    string Name,
    string Currency,
    string Timezone
) : IRequest<UserProfileDto>;
