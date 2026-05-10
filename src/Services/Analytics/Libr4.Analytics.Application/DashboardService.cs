using Libr4.Analytics.Application.Abstractions;
using Libr4.Analytics.Domain.Dashboards;

namespace Libr4.Analytics.Application;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardService(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<List<DashboardDto>> GetDashboardsAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var dashboards = await _dashboardRepository.GetByOwnerAsync(ownerId, cancellationToken);
        return dashboards.Select(d => new DashboardDto(
            d.Id,
            d.Title,
            d.Description,
            d.OwnerId,
            d.Widgets.Select(w => new DashboardWidgetDto(w.Id, w.Type, w.Config)).ToList(),
            d.CreatedAt)).ToList();
    }

    public async Task<DashboardDto> CreateDashboardAsync(CreateDashboardRequest request, CancellationToken cancellationToken = default)
    {
        var dashboard = Dashboard.Create(request.Title, request.Description, request.OwnerId);
        await _dashboardRepository.AddAsync(dashboard, cancellationToken);

        return new DashboardDto(dashboard.Id, dashboard.Title, dashboard.Description, dashboard.OwnerId, new List<DashboardWidgetDto>(), dashboard.CreatedAt);
    }

    public async Task AddWidgetAsync(Guid dashboardId, string widgetType, string config, CancellationToken cancellationToken = default)
    {
        var dashboard = await _dashboardRepository.GetByIdAsync(dashboardId, cancellationToken);
        if (dashboard == null) throw new InvalidOperationException("Dashboard not found");

        dashboard.AddWidget(widgetType, config);
        await _dashboardRepository.UpdateAsync(dashboard, cancellationToken);
    }
}