using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using FinanceFlow.Application.Common.Mappings;

namespace FinanceFlow.Application.UseCases.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler(
    IUserRepository userRepository)
    : IRequestHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(
            request.UserId, cancellationToken);

        if (user is null)
            throw new NotFoundException("Utilizador", request.UserId);

        return user.ToProfileDto();
    }
}
