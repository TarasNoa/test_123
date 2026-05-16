import { createSignal, For, Show, type Component } from 'solid-js';

interface ConnectionUser {
  userId: string;
  displayName: string;
  role?: string | null;
  avatarUrl?: string | null;
  skills?: string[] | null;
  rating?: number | null;
}

interface UserConnectionsProps {
  followers: ConnectionUser[];
  following: ConnectionUser[];
  onFollow?: (userId: string) => Promise<void>;
  onUnfollow?: (userId: string) => Promise<void>;
}

const UserCard: Component<{
  user: ConnectionUser;
  isFollowing: boolean;
  onFollow?: (id: string) => Promise<void>;
  onUnfollow?: (id: string) => Promise<void>;
}> = (props) => {
  const [loading, setLoading] = createSignal(false);

  const handleToggle = async () => {
    setLoading(true);
    try {
      if (props.isFollowing) {
        await props.onUnfollow?.(props.user.userId);
      } else {
        await props.onFollow?.(props.user.userId);
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div class="flex items-center gap-3 p-3 bg-surface rounded-xl border border-surface-3 hover:border-primary/30 transition-colors">
      <div class="w-10 h-10 rounded-full bg-primary/20 flex items-center justify-center text-sm font-semibold text-primary shrink-0 overflow-hidden">
        <Show when={props.user.avatarUrl} fallback={
          <span>{props.user.displayName?.[0]?.toUpperCase() ?? '?'}</span>
        }>
          <img src={props.user.avatarUrl!} class="w-full h-full object-cover" alt="" />
        </Show>
      </div>

      <div class="flex-1 min-w-0">
        <p class="text-sm font-medium text-foreground truncate">{props.user.displayName}</p>
        <Show when={props.user.role}>
          <p class="text-xs text-muted-foreground capitalize">{props.user.role}</p>
        </Show>
        <Show when={props.user.skills?.length}>
          <div class="flex flex-wrap gap-1 mt-1">
            {props.user.skills!.slice(0, 3).map((s) => (
              <span class="text-[10px] px-1.5 py-0.5 bg-primary/10 text-primary rounded">{s}</span>
            ))}
            {props.user.skills!.length > 3 && (
              <span class="text-[10px] text-muted-foreground">+{props.user.skills!.length - 3}</span>
            )}
          </div>
        </Show>
      </div>

      <Show when={props.user.rating}>
        <span class="text-xs text-warning shrink-0">{props.user.rating!.toFixed(1)} ★</span>
      </Show>

      <button
        onClick={handleToggle}
        disabled={loading()}
        class={[
          'shrink-0 text-xs px-3 py-1.5 rounded-lg transition-colors',
          props.isFollowing
            ? 'bg-surface-2 text-muted-foreground hover:bg-surface-3 border border-surface-3'
            : 'bg-primary text-primary-foreground hover:bg-primary/90'
        ].join(' ')}
      >
        {loading() ? '…' : props.isFollowing ? 'Unfollow' : 'Follow'}
      </button>
    </div>
  );
};

export const UserConnections: Component<UserConnectionsProps> = (props) => {
  const [tab, setTab] = createSignal<'followers' | 'following'>('followers');

  const followingIds = () => new Set(props.following.map((u) => u.userId));

  const currentList = () => tab() === 'followers' ? props.followers : props.following;

  return (
    <div>
      <div class="flex gap-1 mb-6 bg-surface-2 p-1 rounded-xl w-fit">
        <button
          onClick={() => setTab('followers')}
          class={[
            'px-4 py-2 text-sm rounded-lg transition-colors',
            tab() === 'followers'
              ? 'bg-surface text-foreground font-medium shadow-sm'
              : 'text-muted-foreground hover:text-foreground'
          ].join(' ')}
        >
          Followers ({props.followers.length})
        </button>
        <button
          onClick={() => setTab('following')}
          class={[
            'px-4 py-2 text-sm rounded-lg transition-colors',
            tab() === 'following'
              ? 'bg-surface text-foreground font-medium shadow-sm'
              : 'text-muted-foreground hover:text-foreground'
          ].join(' ')}
        >
          Following ({props.following.length})
        </button>
      </div>

      <Show when={currentList().length === 0}>
        <div class="text-center py-12 text-muted-foreground text-sm">
          <p class="text-3xl mb-3">👥</p>
          <p>No {tab()} yet.</p>
        </div>
      </Show>

      <div class="space-y-2">
        <For each={currentList()}>
          {(user) => (
            <UserCard
              user={user}
              isFollowing={followingIds().has(user.userId)}
              onFollow={props.onFollow}
              onUnfollow={props.onUnfollow}
            />
          )}
        </For>
      </div>
    </div>
  );
};

