import { For } from 'solid-js';
import { type MetricDto } from '../lib/api-client';

interface MetricsChartProps {
  metrics: MetricDto[];
}

export function MetricsChart(props: MetricsChartProps) {
  // Simple chart placeholder - in real app, use Chart.js or D3
  return (
    <div class="metrics-chart">
      <h2>Metrics</h2>
      <ul>
        <For each={props.metrics}>
          {(metric) => (
            <li>
              {metric.name}: {metric.value} ({metric.type}) at {new Date(metric.timestamp).toLocaleString()}
            </li>
          )}
        </For>
      </ul>
    </div>
  );
}