import { createSignal, onMount } from 'solid-js';
import { apiClient, type MetricDto, type DashboardDto } from '../../lib/api-client';
import { MetricsChart } from '../../widgets/MetricsChart';
import { DashboardGrid } from '../../widgets/DashboardGrid';

export default function Analytics() {
  const [metrics, setMetrics] = createSignal<MetricDto[]>([]);
  const [dashboards, setDashboards] = createSignal<DashboardDto[]>([]);
  const [loading, setLoading] = createSignal(false);

  onMount(async () => {
    setLoading(true);
    try {
      const [metricsData, dashboardsData] = await Promise.all([
        apiClient.getMetrics(),
        apiClient.getDashboards('user-123'), // Replace with actual user ID
      ]);
      setMetrics(metricsData);
      setDashboards(dashboardsData);
    } catch (error) {
      console.error('Failed to load analytics data:', error);
    } finally {
      setLoading(false);
    }
  });

  return (
    <div class="analytics-page">
      <h1>Analytics Dashboard</h1>
      {loading() && <p>Loading...</p>}
      <MetricsChart metrics={metrics()} />
      <DashboardGrid dashboards={dashboards()} />
    </div>
  );
}