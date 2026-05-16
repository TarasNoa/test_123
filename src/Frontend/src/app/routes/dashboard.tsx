import { createSignal, onMount, type Component, For, Show } from "solid-js";
import { useNavigate } from "@solidjs/router";
import { apiClient } from "../../lib/api-client";
import type { UserDto, UserStatsDto, UserPortfolioItemDto, PostDto } from "../../lib/api-client";

type Tab = "posts" | "portfolio" | "stats";

const Dashboard: Component = () => {
  const navigate = useNavigate();
  const [tab, setTab] = createSignal<Tab>("posts");
  const [user, setUser] = createSignal<UserDto | null>(null);
  const [stats, setStats] = createSignal<UserStatsDto | null>(null);
  const [portfolio, setPortfolio] = createSignal<UserPortfolioItemDto[]>([]);
  const [posts, setPosts] = createSignal<PostDto[]>([]);
  const [newPost, setNewPost] = createSignal("");
  const [posting, setPosting] = createSignal(false);

  const trending = [
    { tag: "#web3", posts: 102 },
    { tag: "#rust", posts: 90 },
    { tag: "#ai", posts: 170 },
    { tag: "#freelance", posts: 30 },
    { tag: "#startup", posts: 40 },
  ];

  const whoToFollow = [
    { name: "Alex Dev", handle: "@alexdev" },
    { name: "Sarah Design", handle: "@sarahdsgn" },
    { name: "Mike AI", handle: "@mikeai" },
  ];

  onMount(async () => {
    const [u, s, p, f] = await Promise.allSettled([
      apiClient.getMe(),
      apiClient.getMyStats(),
      apiClient.getMyPortfolio(),
      apiClient.getMyPosts(),
    ]);
    if (u.status === "fulfilled") setUser(u.value);
    if (s.status === "fulfilled") setStats(s.value);
    if (p.status === "fulfilled") setPortfolio(p.value);
    if (f.status === "fulfilled") setPosts(f.value);
  });

  const handleCreatePost = async () => {
    if (!newPost().trim()) return;
    setPosting(true);
    try {
      const created = await apiClient.createPost(newPost().trim());
      setPosts((prev) => [created, ...prev]);
      setNewPost("");
    } finally {
      setPosting(false);
    }
  };

  const handleLike = async (postId: string) => {
    await apiClient.likePost(postId);
    const updated = await apiClient.getMyPosts();
    setPosts(updated);
  };

  const avatarLetter = () =>
    user()?.displayName?.[0]?.toUpperCase() ?? "?";

  const joinedDate = () => {
    const d = user()?.createdAt;
    if (!d) return "";
    return new Date(d).toLocaleDateString("en-US", { month: "long", year: "numeric" });
  };

  return (
    <div class="flex min-h-screen bg-[#05050a] text-foreground">
      {/* Left Sidebar */}
      <aside class="w-16 md:w-60 border-r border-white/5 flex flex-col fixed h-full z-10">
        <div class="p-4 md:p-6">
          <div class="w-8 h-8 rounded-lg bg-[#35E0D0] flex items-center justify-center text-black font-bold text-sm">
            L4
          </div>
        </div>
        <nav class="flex-1 px-2 md:px-4 space-y-1">
          <a
            href="/dashboard"
            class="flex items-center gap-3 px-3 py-2 rounded-lg bg-white/5 text-[#35E0D0] text-sm font-medium"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M2.25 12l8.954-8.955c.44-.439 1.152-.439 1.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25" />
            </svg>
            <span class="hidden md:inline">Dashboard</span>
          </a>
          <a
            href="/ide"
            class="flex items-center gap-3 px-3 py-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/5 text-sm transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6.75 7.5l3 2.25-3 2.25m4.5 0h3m-9 8.25h13.5A2.25 2.25 0 0021 18V6a2.25 2.25 0 00-2.25-2.25H5.25A2.25 2.25 0 003 6v12a2.25 2.25 0 002.25 2.25z" />
            </svg>
            <span class="hidden md:inline">IDE</span>
          </a>
        </nav>
        <div class="p-2 md:p-4">
          <button
            onClick={() => {
              localStorage.removeItem("accessToken");
              localStorage.removeItem("refreshToken");
              navigate("/auth");
            }}
            class="flex items-center gap-3 px-3 py-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/5 text-sm w-full text-left transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
            </svg>
            <span class="hidden md:inline">Logout</span>
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main class="flex-1 ml-16 md:ml-60 min-w-0">
        {/* Banner */}
        <div class="h-32 bg-gradient-to-r from-[#0a1628] via-[#1a1a2e] to-[#0f131a]" />

        {/* Profile Header */}
        <div class="px-6 -mt-12 mb-6">
          <div class="flex items-end gap-4">
            <Show when={user()?.avatarUrl} fallback={
              <div class="w-24 h-24 rounded-2xl bg-gradient-to-br from-[#35E0D0] to-[#2bc4b6] flex items-center justify-center text-black text-3xl font-bold border-4 border-[#05050a] shrink-0">
                {avatarLetter()}
              </div>
            }>
              <img
                src={user()!.avatarUrl!}
                class="w-24 h-24 rounded-2xl object-cover border-4 border-[#05050a] shrink-0"
                alt="avatar"
              />
            </Show>
            <div class="flex-1 pb-2 min-w-0">
              <div class="flex items-center justify-between gap-4">
                <div class="min-w-0">
                  <h1 class="text-xl font-bold truncate">{user()?.displayName ?? "—"}</h1>
                  <p class="text-sm text-muted-foreground">{user()?.email ? `@${user()!.email.split("@")[0]}` : ""}</p>
                  <Show when={joinedDate()}>
                    <p class="text-xs text-muted-foreground mt-1">Joined {joinedDate()}</p>
                  </Show>
                </div>
                <button class="px-4 py-1.5 text-sm border border-white/10 rounded-lg hover:bg-white/5 transition-colors shrink-0">
                  Edit profile
                </button>
              </div>
            </div>
          </div>

          {/* Stats row */}
          <div class="flex items-center gap-6 mt-4 text-sm">
            <span>
              <strong class="text-foreground">{stats()?.totalProjects ?? 0}</strong>{" "}
              <span class="text-muted-foreground">projects</span>
            </span>
            <span>
              <strong class="text-foreground">{stats()?.completedTasks ?? 0}</strong>{" "}
              <span class="text-muted-foreground">tasks</span>
            </span>
            <span>
              <strong class="text-foreground">{stats()?.averageRating?.toFixed(1) ?? "0.0"}</strong>{" "}
              <span class="text-muted-foreground">rating</span>
            </span>
          </div>
        </div>

        {/* Tabs */}
        <div class="px-6 border-b border-white/5">
          <div class="flex gap-6">
            {(["posts", "portfolio", "stats"] as Tab[]).map((t) => (
              <button
                onClick={() => setTab(t)}
                class={[
                  "pb-3 text-sm font-medium capitalize transition-colors",
                  tab() === t
                    ? "text-[#35E0D0] border-b-2 border-[#35E0D0]"
                    : "text-muted-foreground hover:text-foreground",
                ].join(" ")}
              >
                {t.charAt(0).toUpperCase() + t.slice(1)}
              </button>
            ))}
          </div>
        </div>

        {/* Tab content */}
        <div class="px-6 py-6">
          {/* Posts */}
          <Show when={tab() === "posts"}>
            {/* New post */}
            <div class="mb-6 bg-white/5 rounded-xl p-4 border border-white/5">
              <textarea
                value={newPost()}
                onInput={(e) => setNewPost(e.currentTarget.value)}
                placeholder="Share an update…"
                rows={3}
                class="w-full bg-transparent text-sm text-foreground placeholder:text-muted-foreground resize-none focus:outline-none"
              />
              <div class="flex justify-end mt-2">
                <button
                  onClick={handleCreatePost}
                  disabled={posting() || !newPost().trim()}
                  class="px-4 py-1.5 bg-[#35E0D0] text-black text-sm font-medium rounded-lg hover:bg-[#2bc4b6] disabled:opacity-50 transition-colors"
                >
                  {posting() ? "Posting…" : "Post"}
                </button>
              </div>
            </div>

            <Show when={posts().length === 0}>
              <p class="text-sm text-muted-foreground text-center py-8">No posts yet. Start a project to see activity here.</p>
            </Show>
            <div class="space-y-4">
              <For each={posts()}>
                {(post) => (
                  <div class="bg-white/5 rounded-xl p-4 border border-white/5">
                    <Show when={post.title}>
                      <p class="text-sm font-semibold mb-1">{post.title}</p>
                    </Show>
                    <p class="text-sm text-foreground whitespace-pre-wrap mb-3">{post.content}</p>
                    <Show when={post.tags.length > 0}>
                      <div class="flex flex-wrap gap-1.5 mb-3">
                        <For each={post.tags}>
                          {(tag) => <span class="text-xs text-[#35E0D0]">#{tag}</span>}
                        </For>
                      </div>
                    </Show>
                    <div class="flex items-center gap-4 text-xs text-muted-foreground">
                      <button
                        onClick={() => handleLike(post.id)}
                        class={post.isLikedByCurrentUser ? "text-red-400" : "hover:text-red-400 transition-colors"}
                      >
                        {post.isLikedByCurrentUser ? "❤️" : "🤍"} {post.likeCount}
                      </button>
                      <span>💬 {post.commentCount}</span>
                      <span class="ml-auto">👁 {post.viewCount}</span>
                    </div>
                  </div>
                )}
              </For>
            </div>
          </Show>

          {/* Portfolio */}
          <Show when={tab() === "portfolio"}>
            <Show when={portfolio().length === 0}>
              <p class="text-sm text-muted-foreground text-center py-8">No portfolio items yet.</p>
            </Show>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <For each={portfolio()}>
                {(item) => (
                  <div class="bg-white/5 rounded-xl p-4 border border-white/5 hover:border-[#35E0D0]/30 transition-colors">
                    <div class="flex items-start justify-between mb-2">
                      <h3 class="text-sm font-semibold">{item.title}</h3>
                      <span class={[
                        "text-[10px] px-1.5 py-0.5 rounded-full uppercase",
                        item.status === "Published" ? "bg-green-500/10 text-green-400" : "bg-yellow-500/10 text-yellow-400",
                      ].join(" ")}>{item.status}</span>
                    </div>
                    <p class="text-xs text-muted-foreground line-clamp-2 mb-3">{item.description}</p>
                    <div class="flex flex-wrap gap-1 mb-3">
                      <For each={item.skillsUsed.slice(0, 4)}>
                        {(s) => <span class="text-[10px] px-1.5 py-0.5 bg-white/5 text-muted-foreground rounded">{s}</span>}
                      </For>
                    </div>
                    <div class="flex items-center gap-3 text-[11px] text-muted-foreground">
                      <span>❤️ {item.likeCount}</span>
                      <span>👁 {item.viewCount}</span>
                      <Show when={item.liveUrl}>
                        <a href={item.liveUrl!} target="_blank" class="text-[#35E0D0] hover:underline ml-auto">Live ↗</a>
                      </Show>
                    </div>
                  </div>
                )}
              </For>
            </div>
          </Show>

          {/* Stats */}
          <Show when={tab() === "stats"}>
            <Show when={stats()} fallback={<p class="text-sm text-muted-foreground text-center py-8">Loading stats…</p>}>
              <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
                {([
                  { label: "Total Projects", value: stats()!.totalProjects },
                  { label: "Completed Projects", value: stats()!.completedProjects },
                  { label: "In Progress", value: stats()!.inProgressProjects },
                  { label: "Total Tasks", value: stats()!.totalTasks },
                  { label: "Completed Tasks", value: stats()!.completedTasks },
                  { label: "Portfolio Items", value: stats()!.portfolioItemsCount },
                  { label: "Reviews", value: stats()!.reviewsCount },
                  { label: "Total Earnings", value: `$${stats()!.totalEarnings.toFixed(0)}` },
                  { label: "Total Spent", value: `$${stats()!.totalSpent.toFixed(0)}` },
                ] as { label: string; value: string | number }[]).map((s) => (
                  <div class="bg-white/5 rounded-xl p-4 border border-white/5">
                    <p class="text-xs text-muted-foreground mb-1">{s.label}</p>
                    <p class="text-xl font-bold text-[#35E0D0]">{s.value}</p>
                  </div>
                ))}
              </div>
            </Show>
          </Show>
        </div>
      </main>

      {/* Right Sidebar */}
      <aside class="w-72 border-l border-white/5 hidden xl:flex flex-col p-4 space-y-6 shrink-0">
        {/* Trending */}
        <div>
          <h3 class="text-sm font-semibold mb-3">Trending</h3>
          <div class="space-y-2">
            <For each={trending}>
              {(t) => (
                <div class="flex items-center justify-between text-sm">
                  <span class="text-[#35E0D0]">{t.tag}</span>
                  <span class="text-xs text-muted-foreground">{t.posts} posts</span>
                </div>
              )}
            </For>
          </div>
        </div>

        {/* Who to follow */}
        <div>
          <h3 class="text-sm font-semibold mb-3">Who to follow</h3>
          <div class="space-y-3">
            <For each={whoToFollow}>
              {(u) => (
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <div class="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center text-xs font-medium">
                      {u.name[0]}
                    </div>
                    <div>
                      <div class="text-sm font-medium">{u.name}</div>
                      <div class="text-xs text-muted-foreground">{u.handle}</div>
                    </div>
                  </div>
                  <button class="px-3 py-1 text-xs border border-[#35E0D0] text-[#35E0D0] rounded-full hover:bg-[#35E0D0]/10 transition-colors">
                    Follow
                  </button>
                </div>
              )}
            </For>
          </div>
        </div>
      </aside>
    </div>
  );
};

export default Dashboard;
