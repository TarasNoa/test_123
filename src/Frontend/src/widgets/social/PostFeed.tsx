import { createSignal, For, Show, type Component } from 'solid-js';
import type { PostDto } from '../../lib/api-client';
import { apiClient } from '../../lib/api-client';

interface PostFeedProps {
  posts: PostDto[];
  onRefresh?: () => void;
}

const PostCard: Component<{ post: PostDto; onRefresh?: () => void }> = (props) => {
  const [liking, setLiking] = createSignal(false);
  const [comment, setComment] = createSignal('');
  const [commenting, setCommenting] = createSignal(false);
  const [showComment, setShowComment] = createSignal(false);

  const handleLike = async () => {
    if (liking()) return;
    setLiking(true);
    try {
      await apiClient.likePost(props.post.id);
      props.onRefresh?.();
    } finally {
      setLiking(false);
    }
  };

  const handleComment = async () => {
    if (!comment().trim() || commenting()) return;
    setCommenting(true);
    try {
      await apiClient.addComment(props.post.id, comment().trim());
      setComment('');
      setShowComment(false);
      props.onRefresh?.();
    } finally {
      setCommenting(false);
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

  return (
    <div class="bg-surface rounded-xl border border-surface-3 p-4 hover:border-surface-2 transition-colors">
      <div class="flex items-center gap-3 mb-3">
        <div class="w-9 h-9 rounded-full bg-primary/20 flex items-center justify-center text-sm font-semibold text-primary">
          {props.post.authorId.slice(0, 2).toUpperCase()}
        </div>
        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium text-foreground">Developer</p>
          <p class="text-xs text-muted-foreground">{timeAgo(props.post.createdAt)}</p>
        </div>
      </div>

      <Show when={props.post.title}>
        <h3 class="text-sm font-semibold mb-1">{props.post.title}</h3>
      </Show>
      <p class="text-sm text-foreground mb-3 whitespace-pre-wrap">{props.post.content}</p>

      <Show when={props.post.tags.length > 0}>
        <div class="flex flex-wrap gap-1.5 mb-3">
          <For each={props.post.tags}>
            {(tag) => (
              <span class="text-[11px] px-2 py-0.5 bg-primary/10 text-primary rounded-full">#{tag}</span>
            )}
          </For>
        </div>
      </Show>

      <div class="flex items-center gap-4 pt-2 border-t border-surface-3">
        <button
          onClick={handleLike}
          disabled={liking()}
          class={[
            'flex items-center gap-1.5 text-xs transition-colors',
            props.post.isLikedByCurrentUser
              ? 'text-red-400'
              : 'text-muted-foreground hover:text-red-400'
          ].join(' ')}
        >
          <span>{props.post.isLikedByCurrentUser ? '❤️' : '🤍'}</span>
          <span>{props.post.likeCount}</span>
        </button>
        <button
          onClick={() => setShowComment(!showComment())}
          class="flex items-center gap-1.5 text-xs text-muted-foreground hover:text-primary transition-colors"
        >
          <span>💬</span>
          <span>{props.post.commentCount}</span>
        </button>
        <span class="text-xs text-muted-foreground ml-auto">👁 {props.post.viewCount}</span>
      </div>

      <Show when={showComment()}>
        <div class="mt-3 flex gap-2">
          <input
            value={comment()}
            onInput={(e) => setComment(e.currentTarget.value)}
            placeholder="Write a comment…"
            class="flex-1 bg-background border border-surface-3 rounded-lg px-3 py-1.5 text-xs focus:outline-none focus:border-primary/60 text-foreground placeholder:text-muted-foreground"
            onKeyDown={(e) => e.key === 'Enter' && handleComment()}
          />
          <button
            onClick={handleComment}
            disabled={commenting() || !comment().trim()}
            class="px-3 py-1.5 bg-primary text-primary-foreground text-xs rounded-lg hover:bg-primary/90 disabled:opacity-50 transition-colors"
          >
            {commenting() ? '…' : 'Send'}
          </button>
        </div>
      </Show>
    </div>
  );
};

export const PostFeed: Component<PostFeedProps> = (props) => {
  return (
    <div class="space-y-4">
      <Show when={props.posts.length === 0}>
        <div class="text-center py-12 text-muted-foreground text-sm">
          <p class="text-3xl mb-3">📰</p>
          <p>No posts yet.</p>
        </div>
      </Show>
      <For each={props.posts}>
        {(post) => <PostCard post={post} onRefresh={props.onRefresh} />}
      </For>
    </div>
  );
};

