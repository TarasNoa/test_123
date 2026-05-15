import { createSignal, onMount, type Component, For, Show } from "solid-js";
import { useNavigate } from "@solidjs/router";
import { apiClient } from "../../lib/api-client";
import type { UserDto, UserStatsDto, UserProjectDto, PostDto } from "../../lib/api-client";

/* ─── Stat Card ─── */
const StatCard: Component<{
  label: string;
  value: string | number;
  color?: string;
}> = (props) => (
  <div class="bg-surface rounded-lg p-4 border border-surface-3">
    <p class="text-xs text-muted-foreground uppercase tracking-wider mb-1">{props.label}</p>
    <p class={["text-xl font-bold", props.color || "text-foreground"].join(" ")}>
      {props.value}
    </p>
  </div>
);

/* ─── Quick Link Button ─── */
const QuickLink: Component<{
  href: string;
  label: string;
  icon: string;
  description?: string;
}> = (props) => {
  const navigate = useNavigate();
  return (
    <button
      onClick={() => navigate(props.href)}
      class="flex items-center gap-3 p-4 bg-surface rounded-lg border border-surface-3 hover:border-primary/50 transition-colors text-left w-full group"
    >
      <span class="text-lg group-hover:scale-110 transition-transform">{props.icon}</span>
      <div>
        <p class="text-sm font-medium text-foreground">{props.label}</p>
        <Show when={props.description}>
          <p class="text-xs text-muted-foreground">{props.description}</p>
        </Show>
      </div>
    </button>
  );
};

/* ─── Project Card ─── */
const ProjectCard: Component<{ project: UserProjectDto }> = (props) => (
  <div class="bg-surface rounded-lg p-3 border border-surface-3 hover:border-primary/30 transition-colors">
    <div class="flex items-center justify-between mb-2">
      <p class="text-sm font-medium text-foreground truncate">{props.project.title}</p>
      <span
        class={[
          "text-[10px] px-1.5 py-0.5 rounded-full uppercase tracking-wide",
          props.project.status === "InProgress"
            ? "bg-info/10 text-info"
            : props.project.status === "Completed"
              ? "bg-success/10 text-success"
              : "bg-warning/10 text-warning",
        ].join(" ")}
      >
        {props.project.status}
      </span>
    </div>
    <p class="text-xs text-muted-foreground line-clamp-2 mb-2">{props.project.description}</p>
    <div class="flex items-center gap-3 text-[10px] text-muted-foreground">
      <span>Progress {props.project.progress}%</span>
      <span>Team {props.project.teamSize}</span>
      <span>{props.project.budget ? `$${props.project.budget}` : "—"}</span>
    </div>
  </div>
);

/* ─── Feed Post Card ─── */
const PostCard: Component<{ post: PostDto }> = (props) => (
  <div class="bg-surface rounded-lg p-3 border border-surface-3">
    <p class="text-sm text-foreground mb-1">{props.post.content}</p>
    <div class="flex items-center gap-3 text-[10px] text-muted-foreground">
      <span>❤️ {props.post.likeCount}</span>
      <span>💬 {props.post.commentCount}</span>
    </div>
  </div>
);

/* ─── Dashboard ─── */
const Dashboard: Component = () => {
  const navigate = useNavigate();
  const [user, setUser] = createSignal<UserDto | null>(null);
  const [stats, setStats] = createSignal<UserStatsDto | null>(null);
  const [projects, setProjects] = createSignal<UserProjectDto[]>([]);
  const [feed, setFeed] = createSignal<PostDto[]>([]);
  const [loading, setLoading] = createSignal(true);

  onMount(async () => {
    try {
      const [u, s, p, f] = await Promise.allSettled([
        apiClient.getMe(),
        apiClient.getMyStats(),
        apiClient.getMyProjects(),
        apiClient.getFeed(1, 3),
      ]);

      if (u.status === "fulfilled") setUser(u.value);
      if (s.status === "fulfilled") setStats(s.value);
      if (p.status === "fulfilled") setProjects(p.value);
      if (f.status === "fulfilled") setFeed(f.value);
    } catch {
      // silently fail — dashboard stays usable with empty states
    } finally {
      setLoading(false);
    }
  });

  return (
    <div class="min-h-screen bg-background text-foreground p-6">
      {/* Header */}
      <div class="max-w-6xl mx-auto mb-8">
        <div class="flex items-center justify-between mb-1">
          <h1 class="text-2xl font-bold">
            <Show when={user()} fallback="Dashboard">
              Hi, {user()!.displayName || "there"} 👋
            </Show>
          </h1>
          <button
            onClick={() => navigate("/ide")}
            class="px-4 py-2 bg-primary text-primary-foreground rounded-lg text-sm font-medium hover:bg-primary/90 transition-colors"
          >
            Open IDE
          </button>
        </div>
        <p class="text-sm text-muted-foreground">
          Here is what is happening with your projects today.
        </p>
      </div>

      <div class="max-w-6xl mx-auto grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left column — stats + projects */}
        <div class="lg:col-span-2 space-y-6">
          {/* Stats */}
          <Show when={!loading()} fallback={
            <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
              <For each={[1, 2, 3, 4]}>{() =>
                <div class="bg-surface rounded-lg p-4 border border-surface-3 animate-pulse h-20" />
              }</For>
            </div>
          }>
            <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
              <StatCard label="Active Projects" value={stats()?.inProgressProjects ?? 0} />
              <StatCard label="Completed" value={stats()?.completedProjects ?? 0} color="text-success" />
              <StatCard label="Total Tasks" value={stats()?.totalTasks ?? 0} />
              <StatCard
                label="Rating"
                value={stats()?.averageRating ? `${stats()!.averageRating.toFixed(1)} ★` : "—"}
                color="text-warning"
              />
            </div>
          </Show>

          {/* Projects */}
          <div>
            <div class="flex items-center justify-between mb-3">
              <h2 class="text-sm font-semibold uppercase tracking-wider text-muted-foreground">My Projects</h2>
              <button
                onClick={() => navigate("/marketplace")}
                class="text-xs text-primary hover:text-primary/80 transition-colors"
              >
                View all →
              </button>
            </div>
            <Show when={projects().length > 0} fallback={
              <div class="bg-surface rounded-lg p-6 border border-surface-3 text-center">
                <p class="text-sm text-muted-foreground">No projects yet.</p>
                <button
                  onClick={() => navigate("/marketplace")}
                  class="mt-3 text-xs text-primary hover:underline"
                >
                  Browse marketplace
                </button>
              </div>
            }>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                <For each={projects().slice(0, 4)}>{(p) => <ProjectCard project={p} />}</For>
              </div>
            </Show>
          </div>

          {/* Recent activity feed */}
          <div>
            <h2 class="text-sm font-semibold uppercase tracking-wider text-muted-foreground mb-3">Recent Activity</h2>
            <Show when={feed().length > 0} fallback={
              <div class="bg-surface rounded-lg p-6 border border-surface-3 text-center">
                <p class="text-sm text-muted-foreground">No recent activity.</p>
              </div>
            }>
              <div class="space-y-3">
                <For each={feed()}>{(post) => <PostCard post={post} />}</For>
              </div>
            </Show>
          </div>
        </div>

        {/* Right column — quick links + extra stats */}
        <div class="space-y-6">
          {/* Quick Actions */}
          <div>
            <h2 class="text-sm font-semibold uppercase tracking-wider text-muted-foreground mb-3">Quick Actions</h2>
            <div class="space-y-3">
              <QuickLink href="/ide" label="Open IDE" icon="💻" description="Continue coding with AI assistance" />
              <QuickLink href="/marketplace" label="Browse Tasks" icon="🔍" description="Find new projects to work on" />
              <QuickLink href="/social" label="Community" icon="💬" description="Connect with other developers" />
            </div>
          </div>

          {/* Earnings */}
          <Show when={stats()}>
            <div class="bg-surface rounded-lg p-4 border border-surface-3">
              <h2 class="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Finances</h2>
              <div class="space-y-3">
                <div class="flex items-center justify-between">
                  <span class="text-sm text-muted-foreground">Total Earnings</span>
                  <span class="text-sm font-medium text-success">${stats()!.totalEarnings.toFixed(0)}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span class="text-sm text-muted-foreground">Total Spent</span>
                  <span class="text-sm font-medium text-foreground">${stats()!.totalSpent.toFixed(0)}</span>
                </div>
                <div class="h-px bg-surface-3" />
                <div class="flex items-center justify-between">
                  <span class="text-sm font-medium text-foreground">Net</span>
                  <span class="text-sm font-bold text-primary">
                    ${(stats()!.totalEarnings - stats()!.totalSpent).toFixed(0)}
                  </span>
                </div>
              </div>
            </div>
          </Show>

          {/* Portfolio */}
          <Show when={stats()}>
            <div class="bg-surface rounded-lg p-4 border border-surface-3">
              <h2 class="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-3">Profile</h2>
              <div class="space-y-2 text-sm">
                <div class="flex items-center justify-between">
                  <span class="text-muted-foreground">Portfolio items</span>
                  <span class="font-medium">{stats()!.portfolioItemsCount}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span class="text-muted-foreground">Reviews</span>
                  <span class="font-medium">{stats()!.reviewsCount}</span>
                </div>
                <div class="flex items-center justify-between">
                  <span class="text-muted-foreground">Completed tasks</span>
                  <span class="font-medium">{stats()!.completedTasks}</span>
                </div>
              </div>
            </div>
          </Show>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
