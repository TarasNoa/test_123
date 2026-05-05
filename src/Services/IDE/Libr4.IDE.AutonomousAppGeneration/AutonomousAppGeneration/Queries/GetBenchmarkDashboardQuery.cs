using Libr4.IDE.Application.AutonomousAppGeneration.DTOs;
using MediatR;

namespace Libr4.IDE.Application.AutonomousAppGeneration.Queries;

public sealed record GetBenchmarkDashboardQuery(int Limit = 20)
    : IRequest<BenchmarkDashboardDto>;
