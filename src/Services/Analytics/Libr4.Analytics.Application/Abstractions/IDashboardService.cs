using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Libr4.Analytics.Application.Abstractions;

public record DashboardDto(
    Guid Id,
    string Title,
    string Description,
    Guid OwnerId,
    List<DashboardWidgetDto> Widgets,
    DateTimeOffset CreatedAt);

public record DashboardWidgetDto(
    Guid Id,
    string Type,
    string Config);

public record CreateDashboardRequest(
    string Title,
    string Description,
    Guid OwnerId);

public interface IDashboardService
{
    Task<List<DashboardDto>> GetDashboardsAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<DashboardDto> CreateDashboardAsync(CreateDashboardRequest request, CancellationToken cancellationToken = default);
    Task AddWidgetAsync(Guid dashboardId, string widgetType, string config, CancellationToken cancellationToken = default);
}