using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.DTOs.Admin;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Queries.GetMetrics;

public record GetMetricsQuery : IRequest<AdminMetricsDto>;
