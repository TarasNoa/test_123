import { For, Show, type Component } from 'solid-js';

export interface ActivityItem {
  id: string;
  type: 'post_created' | 'post_liked' | 'comment_added' | 'project_joined' | 'task_completed' | 'user_followed' | string;
  actorDisplayName?: string;
  actorAvatarUrl?: string;
  targetTitle?: string;
  targetId?: string;
  createdAt: string;
  metadata?: Record<string, string>;
}

interface ActivityFeedProps {
  activities: ActivityItem[];
}

const activityIcon: Record<string, string> = {
  post_created: '📝',
  post_liked: '❤️',
  comment_added: '💬',
  project_joined: '📁',
  task_completed: '✅',
  user_followed: '👤',
};

const activityLabel = (item: ActivityItem): string => {
  const actor = item.actorDisplayName ?? 'Someone';
  switch (item.type) {
    case 'post_created': return `${actor} published a new post`;
    case 'post_liked': return `${actor} liked a post`;
    case 'comment_added': return `${actor} commented${item.targetTitle ? ` on "${item.targetTitle}"` : ''}`;
    case 'project_joined': return `${actor} joined project${item.targetTitle ? ` "${item.targetTitle}"` : ''}`;
    case 'task_completed': return `${actor} completed task${item.targetTitle ? ` "${item.targetTitle}"` : ''}`;
    case 'user_followed': return `${actor} started following${item.targetTitle ? ` ${item.targetTitle}` : ''}`;
    default: return `${actor} performed action: ${item.type}`;
  }
};

const timeAgo = (dateStr: string) => {
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  return `${Math.floor(hrs / 24)}d ago`;
};

export const ActivityFeed: Component<ActivityFeedProps> = (props) => {
  return (
    <div>
      <Show when={props.activities.length === 0}>
        <div class="text-center py-12 text-muted-foreground text-sm">
          <p class="text-3xl mb-3">⏰</p>
          <p>No activity yet.</p>
        </div>
      </Show>

      <div class="relative">
        <div class="absolute left-5 top-0 bottom-0 w-px bg-surface-3" />
        <div class="space-y-1">
          <For each={props.activities}>
            {(item) => (
              <div class="relative flex items-start gap-4 pl-10 py-3">
                <div class="absolute left-3 w-5 h-5 rounded-full bg-surface border-2 border-surface-3 flex items-center justify-center text-[11px] -translate-x-1">
                  {activityIcon[item.type] ?? '🔔'}
                </div>

                <div class="flex items-start gap-3 min-w-0 flex-1">
                  <Show when={item.actorAvatarUrl}>
                    <img
                      src={item.actorAvatarUrl}
                      class="w-7 h-7 rounded-full object-cover shrink-0 mt-0.5"
                      alt=""
                    />
                  </Show>

                  <div class="flex-1 min-w-0">
                    <p class="text-sm text-foreground leading-snug">{activityLabel(item)}</p>
                    <Show when={item.metadata?.description}>
                      <p class="text-xs text-muted-foreground mt-0.5 line-clamp-1">{item.metadata!.description}</p>
                    </Show>
                    <p class="text-[11px] text-muted-foreground mt-1">{timeAgo(item.createdAt)}</p>
                  </div>
                </div>
              </div>
            )}
          </For>
        </div>
      </div>
    </div>
  );
};

