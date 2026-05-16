import { createSignal, onMount, type Component } from 'solid-js';
import { useNavigate } from '@solidjs/router';
import { apiClient } from '../../lib/api-client';
import type { UserDto, UserStatsDto, UserPortfolioItemDto, PostDto } from '../../lib/api-client';
import { UserProfile } from '../../widgets/social/UserProfile';
import { PostFeed } from '../../widgets/social/PostFeed';
import { UserConnections } from '../../widgets/social/UserConnections';
import { ActivityFeed, type ActivityItem } from '../../widgets/social/ActivityFeed';

type Tab = 'profile' | 'feed' | 'connections' | 'activity';

const Social: Component = () => {
  const navigate = useNavigate();
  const [tab, setTab] = createSignal<Tab>('profile');
  const [loading, setLoading] = createSignal(true);

  const [user, setUser] = createSignal<UserDto | null>(null);
  const [stats, setStats] = createSignal<UserStatsDto | null>(null);
  const [portfolio, setPortfolio] = createSignal<UserPortfolioItemDto[]>([]);
  const [posts, setPosts] = createSignal<PostDto[]>([]);
  const [myPosts, setMyPosts] = createSignal<PostDto[]>([]);
  const [activities, setActivities] = createSignal<ActivityItem[]>([]);

  onMount(async () => {
    await loadAll();
  });

  const loadAll = async () => {
    setLoading(true);
    try {
      const [u, s, p, f, mp] = await Promise.allSettled([
        apiClient.getMe(),
        apiClient.getMyStats(),
        apiClient.getMyPortfolio(),
        apiClient.getFeed(1, 20),
        apiClient.getMyPosts(),
      ]);
      if (u.status === 'fulfilled') setUser(u.value);
      if (s.status === 'fulfilled') setStats(s.value);
      if (p.status === 'fulfilled') setPortfolio(p.value);
      if (f.status === 'fulfilled') setPosts(f.value);
      if (mp.status === 'fulfilled') {
        setMyPosts(mp.value);
        setActivities(
          mp.value.map((post) => ({
            id: post.id,
            type: 'post_created',
            actorDisplayName: u.status === 'fulfilled' ? u.value.displayName : undefined,
            targetTitle: post.title ?? undefined,
            targetId: post.id,
            createdAt: post.createdAt,
          }))
        );
      }
    } finally {
      setLoading(false);
    }
  };

  const handleAvatarUpload = async (file: File) => {
    const res = await apiClient.uploadAvatar(file);
    setUser((prev) => prev ? { ...prev, avatarUrl: res.avatarUrl } : prev);
  };

  const handleCoverUpload = async (file: File) => {
    const res = await apiClient.uploadCover(file);
    setUser((prev) => prev ? { ...prev, coverUrl: res.coverUrl } : prev);
  };

  const tabs: { id: Tab; label: string; icon: string }[] = [
    { id: 'profile', label: 'Profile', icon: '👤' },
    { id: 'feed', label: 'Feed', icon: '📰' },
    { id: 'connections', label: 'Connections', icon: '👥' },
    { id: 'activity', label: 'Activity', icon: '⏰' },
  ];

  return (
    <div class="min-h-screen bg-background text-foreground">
      {/* Top nav */}
      <div class="sticky top-0 z-10 bg-background/95 backdrop-blur border-b border-surface-3">
        <div class="max-w-4xl mx-auto px-4 flex items-center justify-between h-14">
          <div class="flex items-center gap-1">
            {tabs.map((t) => (
              <button
                onClick={() => setTab(t.id)}
                class={[
                  'flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-sm transition-colors',
                  tab() === t.id
                    ? 'bg-primary/10 text-primary font-medium'
                    : 'text-muted-foreground hover:text-foreground hover:bg-surface-2'
                ].join(' ')}
              >
                <span>{t.icon}</span>
                <span class="hidden sm:inline">{t.label}</span>
              </button>
            ))}
          </div>

          <button
            onClick={() => navigate('/ide')}
            class="px-3 py-1.5 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:bg-primary/90 transition-colors"
          >
            Open IDE
          </button>
        </div>
      </div>

      {/* Content */}
      <div class="max-w-4xl mx-auto px-4 py-6">
        {loading() ? (
          <div class="flex items-center justify-center py-24">
            <div class="animate-pulse text-primary font-medium">Loading…</div>
          </div>
        ) : (
          <>
            {tab() === 'profile' && (
              <UserProfile
                user={user()}
                stats={stats()}
                portfolio={portfolio()}
                onAvatarUpload={handleAvatarUpload}
                onCoverUpload={handleCoverUpload}
              />
            )}

            {tab() === 'feed' && (
              <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <div class="lg:col-span-2">
                  <PostFeed posts={posts()} onRefresh={() => apiClient.getFeed(1, 20).then(setPosts)} />
                </div>
                <aside class="space-y-4">
                  <div class="bg-surface rounded-xl border border-surface-3 p-4">
                    <h3 class="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                      My Posts
                    </h3>
                    <p class="text-2xl font-bold text-primary">{myPosts().length}</p>
                    <p class="text-xs text-muted-foreground mt-1">published</p>
                  </div>
                  {stats() && (
                    <div class="bg-surface rounded-xl border border-surface-3 p-4">
                      <h3 class="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">
                        Quick Stats
                      </h3>
                      <div class="space-y-2 text-sm">
                        <div class="flex justify-between">
                          <span class="text-muted-foreground">Rating</span>
                          <span class="text-warning font-medium">{stats()!.averageRating.toFixed(1)} ★</span>
                        </div>
                        <div class="flex justify-between">
                          <span class="text-muted-foreground">Projects</span>
                          <span class="font-medium">{stats()!.totalProjects}</span>
                        </div>
                        <div class="flex justify-between">
                          <span class="text-muted-foreground">Completed</span>
                          <span class="text-success font-medium">{stats()!.completedTasks}</span>
                        </div>
                      </div>
                    </div>
                  )}
                </aside>
              </div>
            )}

            {tab() === 'connections' && (
              <UserConnections
                followers={[]}
                following={[]}
              />
            )}

            {tab() === 'activity' && (
              <ActivityFeed activities={activities()} />
            )}
          </>
        )}
      </div>
    </div>
  );
};

export default Social;
