import { For } from 'solid-js';

interface Activity {
  id: string;
  type: string;
  message: string;
  timestamp: Date;
}

interface AIActivityFeedProps {
  activities: Activity[];
}

export function AIActivityFeed(props: AIActivityFeedProps) {
  return (
    <div class="ai-activity-feed">
      <h4>AI Activity</h4>
      <For each={props.activities} fallback={<p>No recent activity</p>}>
        {(item) => (
          <div class="activity-item">
            <span class="activity-type">{item.type}</span>
            <span class="activity-message">{item.message}</span>
            <small>{item.timestamp.toLocaleTimeString()}</small>
          </div>
        )}
      </For>
    </div>
  );
}
