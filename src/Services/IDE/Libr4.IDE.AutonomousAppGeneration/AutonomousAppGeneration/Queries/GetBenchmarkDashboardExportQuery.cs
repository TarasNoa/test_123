using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Queries;

public sealed record GetBenchmarkDashboardExportQuery(int Limit = 20)
    : IRequest<BenchmarkDashboardExportDto>;
