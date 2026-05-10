import { For } from 'solid-js';
import { type DashboardDto } from '../lib/api-client';

interface DashboardGridProps {
  dashboards: DashboardDto[];
}

export function DashboardGrid(props: DashboardGridProps) {
  return (
    <div class="dashboard-grid">
      <h2>Dashboards</h2>
      <For each={props.dashboards}>
        {(dashboard) => (
          <div class="dashboard-card">
            <h3>{dashboard.title}</h3>
            <p>{dashboard.description}</p>
            <p>Widgets: {dashboard.widgets.length}</p>
          </div>
        )}
      </For>
    </div>
  );
}