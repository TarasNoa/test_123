import { createSignal, Show, type Component } from 'solid-js';
import type { UserDto, UserStatsDto, UserPortfolioItemDto } from '../../lib/api-client';
import { apiClient } from '../../lib/api-client';

interface UserProfileProps {
  user: UserDto | null;
  stats: UserStatsDto | null;
  portfolio: UserPortfolioItemDto[];
  onAvatarUpload?: (file: File) => Promise<void>;
  onCoverUpload?: (file: File) => Promise<void>;
}

export const UserProfile: Component<UserProfileProps> = (props) => {
  const [editMode, setEditMode] = createSignal(false);
  const [newPost, setNewPost] = createSignal('');
  const [posting, setPosting] = createSignal(false);

  const handleAvatarChange = async (e: Event) => {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file && props.onAvatarUpload) await props.onAvatarUpload(file);
  };

  const handleCoverChange = async (e: Event) => {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file && props.onCoverUpload) await props.onCoverUpload(file);
  };

  const handleCreatePost = async () => {
    if (!newPost().trim()) return;
    setPosting(true);
    try {
      await apiClient.createPost(newPost().trim());
      setNewPost('');
    } finally {
      setPosting(false);
    }
  };

  const u = () => props.user;
  const s = () => props.stats;

  return (
    <div class="min-h-screen bg-background text-foreground">
      {/* Cover */}
      <div class="relative h-52 bg-gradient-to-r from-primary/30 to-primary/10 rounded-b-2xl overflow-hidden">
        <Show when={u()?.coverUrl}>
          <img src={u()!.coverUrl!} class="w-full h-full object-cover" alt="cover" />
        </Show>
        <label class="absolute bottom-3 right-3 cursor-pointer bg-black/40 hover:bg-black/60 text-white text-xs px-3 py-1.5 rounded-lg transition-colors">
          Change Cover
          <input type="file" accept="image/*" class="hidden" onChange={handleCoverChange} />
        </label>
      </div>

      <div class="max-w-4xl mx-auto px-4">
        {/* Avatar + name */}
        <div class="flex items-end gap-4 -mt-14 mb-6">
          <div class="relative">
            <div class="w-28 h-28 rounded-full border-4 border-background bg-surface-2 overflow-hidden">
              <Show when={u()?.avatarUrl} fallback={
                <div class="w-full h-full flex items-center justify-center text-4xl bg-primary/20">
                  {u()?.displayName?.[0]?.toUpperCase() ?? '?'}
                </div>
              }>
                <img src={u()!.avatarUrl!} class="w-full h-full object-cover" alt="avatar" />
              </Show>
            </div>
            <label class="absolute bottom-0 right-0 cursor-pointer w-8 h-8 bg-primary rounded-full flex items-center justify-center text-primary-foreground text-sm hover:bg-primary/80 transition-colors">
              ✎
              <input type="file" accept="image/*" class="hidden" onChange={handleAvatarChange} />
            </label>
          </div>

          <div class="pb-2 flex-1">
            <h1 class="text-2xl font-bold">{u()?.displayName ?? '—'}</h1>
            <p class="text-sm text-muted-foreground">{u()?.email}</p>
            <Show when={u()?.role}>
              <span class="inline-block mt-1 text-xs px-2 py-0.5 bg-primary/10 text-primary rounded-full capitalize">
                {u()!.role}
              </span>
            </Show>
          </div>
        </div>

        {/* Bio */}
        <Show when={u()?.bio}>
          <p class="text-sm text-muted-foreground mb-6 bg-surface p-4 rounded-xl border border-surface-3">
            {u()!.bio}
          </p>
        </Show>

        {/* Stats row */}
        <Show when={s()}>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 mb-8">
            <div class="bg-surface rounded-xl p-4 border border-surface-3 text-center">
              <p class="text-2xl font-bold text-primary">{s()!.totalProjects}</p>
              <p class="text-xs text-muted-foreground mt-1">Projects</p>
            </div>
            <div class="bg-surface rounded-xl p-4 border border-surface-3 text-center">
              <p class="text-2xl font-bold text-success">{s()!.completedTasks}</p>
              <p class="text-xs text-muted-foreground mt-1">Completed Tasks</p>
            </div>
            <div class="bg-surface rounded-xl p-4 border border-surface-3 text-center">
              <p class="text-2xl font-bold text-warning">{s()!.averageRating.toFixed(1)} ★</p>
              <p class="text-xs text-muted-foreground mt-1">Rating</p>
            </div>
            <div class="bg-surface rounded-xl p-4 border border-surface-3 text-center">
              <p class="text-2xl font-bold text-info">{s()!.portfolioItemsCount}</p>
              <p class="text-xs text-muted-foreground mt-1">Portfolio</p>
            </div>
          </div>
        </Show>

        {/* Info grid */}
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-8">
          <Show when={u()?.skills?.length}>
            <div class="bg-surface rounded-xl p-4 border border-surface-3">
              <h3 class="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Skills</h3>
              <div class="flex flex-wrap gap-2">
                {u()!.skills!.map((s) => (
                  <span class="text-xs px-2.5 py-1 bg-primary/10 text-primary rounded-full">{s}</span>
                ))}
              </div>
            </div>
          </Show>

          <div class="bg-surface rounded-xl p-4 border border-surface-3">
            <h3 class="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Info</h3>
            <div class="space-y-2 text-sm">
              <Show when={u()?.country || u()?.city}>
                <div class="flex gap-2">
                  <span class="text-muted-foreground">📍</span>
                  <span>{[u()?.city, u()?.country].filter(Boolean).join(', ')}</span>
                </div>
              </Show>
              <Show when={u()?.experience}>
                <div class="flex gap-2">
                  <span class="text-muted-foreground">💼</span>
                  <span>{u()!.experience}</span>
                </div>
              </Show>
              <Show when={u()?.hourlyRate}>
                <div class="flex gap-2">
                  <span class="text-muted-foreground">💰</span>
                  <span>${u()!.hourlyRate}/hr</span>
                </div>
              </Show>
              <Show when={u()?.website}>
                <div class="flex gap-2">
                  <span class="text-muted-foreground">🌐</span>
                  <a href={u()!.website!} target="_blank" class="text-primary hover:underline truncate">{u()!.website}</a>
                </div>
              </Show>
              <Show when={u()?.linkedInUrl}>
                <div class="flex gap-2">
                  <span class="text-muted-foreground">🔗</span>
                  <a href={u()!.linkedInUrl!} target="_blank" class="text-primary hover:underline">LinkedIn</a>
                </div>
              </Show>
            </div>
          </div>
        </div>

        {/* Portfolio */}
        <Show when={props.portfolio.length > 0}>
          <div class="mb-8">
            <h2 class="text-sm font-semibold uppercase tracking-wider text-muted-foreground mb-4">Portfolio</h2>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {props.portfolio.map((item) => (
                <div class="bg-surface rounded-xl p-4 border border-surface-3 hover:border-primary/40 transition-colors">
                  <div class="flex items-start justify-between mb-2">
                    <h3 class="text-sm font-semibold">{item.title}</h3>
                    <span class={[
                      'text-[10px] px-1.5 py-0.5 rounded-full uppercase',
                      item.status === 'Published' ? 'bg-success/10 text-success' : 'bg-warning/10 text-warning'
                    ].join(' ')}>{item.status}</span>
                  </div>
                  <p class="text-xs text-muted-foreground line-clamp-2 mb-3">{item.description}</p>
                  <div class="flex flex-wrap gap-1.5 mb-3">
                    {item.skillsUsed.map((sk) => (
                      <span class="text-[10px] px-1.5 py-0.5 bg-surface-2 text-muted-foreground rounded">{sk}</span>
                    ))}
                  </div>
                  <div class="flex items-center gap-4 text-[11px] text-muted-foreground">
                    <span>❤️ {item.likeCount}</span>
                    <span>👁 {item.viewCount}</span>
                    {item.liveUrl && <a href={item.liveUrl} target="_blank" class="text-primary hover:underline">Live ↗</a>}
                    {item.githubUrl && <a href={item.githubUrl} target="_blank" class="text-primary hover:underline">GitHub ↗</a>}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </Show>

        {/* Create post */}
        <div class="bg-surface rounded-xl p-4 border border-surface-3 mb-8">
          <h3 class="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Share something</h3>
          <textarea
            value={newPost()}
            onInput={(e) => setNewPost(e.currentTarget.value)}
            placeholder="What's on your mind?"
            rows={3}
            class="w-full bg-background border border-surface-3 rounded-lg p-3 text-sm resize-none focus:outline-none focus:border-primary/60 text-foreground placeholder:text-muted-foreground"
          />
          <div class="flex justify-end mt-2">
            <button
              onClick={handleCreatePost}
              disabled={posting() || !newPost().trim()}
              class="px-4 py-2 bg-primary text-primary-foreground text-sm rounded-lg hover:bg-primary/90 disabled:opacity-50 transition-colors"
            >
              {posting() ? 'Posting…' : 'Post'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

