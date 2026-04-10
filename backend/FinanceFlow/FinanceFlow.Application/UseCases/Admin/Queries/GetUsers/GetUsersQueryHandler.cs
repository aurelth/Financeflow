using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Queries.GetUsers;

public class GetUsersQueryHandler(
    IUserRepository userRepository
) : IRequestHandler<GetUsersQuery, AdminUserListDto>
{
    public async Task<AdminUserListDto> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var (users, total) = await userRepository.GetAllPagedAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.IsActive,
            cancellationToken);

        var dtos = users.Select(u => new AdminUserDto(
            Id: u.Id,
            Name: u.Name,
            Email: u.Email,
            Cpf: u.Cpf,
            Gender: u.Gender.ToString(),
            Role: u.Role.ToString(),
            Currency: u.Currency,
            Timezone: u.Timezone,
            IsActive: u.DeletedAt == null,
            CreatedAt: u.CreatedAt,
            DeletedAt: u.DeletedAt
        ));

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);

        return new AdminUserListDto(
            Users: dtos,
            Total: total,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages
        );
    }
}
