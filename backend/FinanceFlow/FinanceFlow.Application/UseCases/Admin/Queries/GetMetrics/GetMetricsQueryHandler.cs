using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Queries.GetMetrics;

public class GetMetricsQueryHandler(
    IUserRepository userRepository,
    ICategoryRepository categoryRepository
) : IRequestHandler<GetMetricsQuery, AdminMetricsDto>
{
    public async Task<AdminMetricsDto> Handle(
        GetMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var (allUsers, totalUsers) = await userRepository.GetAllPagedAsync(
            1, int.MaxValue, null, null, cancellationToken);

        var activeUsers = allUsers.Count(u => u.DeletedAt == null);
        var inactiveUsers = allUsers.Count(u => u.DeletedAt != null);
        var totalAdmins = allUsers.Count(u => u.Role == UserRole.Admin && u.DeletedAt == null);

        var allCategories = await categoryRepository.GetAllDefaultAsync(cancellationToken);
        var defaultCategories = allCategories.Count();

        // Total de categorias inclui as padrão
        var totalCategories = defaultCategories;

        return new AdminMetricsDto(
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            InactiveUsers: inactiveUsers,
            TotalAdmins: totalAdmins,
            TotalCategories: totalCategories,
            DefaultCategories: defaultCategories
        );
    }
}
