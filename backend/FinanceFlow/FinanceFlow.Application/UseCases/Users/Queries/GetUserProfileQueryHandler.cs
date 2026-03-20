using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler(
    IUserRepository userRepository,
    IMapper mapper)
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

        return mapper.Map<UserProfileDto>(user);
    }
}
