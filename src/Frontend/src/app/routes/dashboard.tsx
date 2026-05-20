import { createSignal, onMount, type Component, For, Show } from "solid-js";
import { useNavigate, useLocation } from "@solidjs/router";
import { apiClient } from "../../lib/api-client";
import type { UserDto, UserStatsDto, UserPortfolioItemDto, PostDto, SocialUserDto, UserSkillsSummaryDto, RecommendedTaskDto } from "../../lib/api-client";
import { config } from "../../lib/config";

type Tab = "posts" | "portfolio" | "stats";

/* ─── Inline SVG icons ─── */
const HomeIcon = (p: { class?: string }) => (
  <svg class={p.class} fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" d="M3 9l9-7 9 7v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
    <polyline points="9 22 9 12 15 12 15 22" />
  </svg>
);
const CodeIcon = (p: { class?: string }) => (
  <svg class={p.class} fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
    <polyline points="16 18 22 12 16 6" />
    <polyline points="8 6 2 12 8 18" />
  </svg>
);
const ShopIcon = (p: { class?: string }) => (
  <svg class={p.class} fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" d="M6 2 3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z" />
    <line x1="3" y1="6" x2="21" y2="6" />
    <path stroke-linecap="round" stroke-linejoin="round" d="M16 10a4 4 0 0 1-8 0" />
  </svg>
);
const ChatIcon = (p: { class?: string }) => (
  <svg class={p.class} fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
  </svg>
);
const EditIcon = (p: { class?: string }) => (
  <svg class={p.class} fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" d="M17 3a2.828 2.828 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5L17 3z" />
  </svg>
);
const ChartIcon = (p: { class?: string }) => (
  <svg class={p.class} fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
    <line x1="18" y1="20" x2="18" y2="10" />
    <line x1="12" y1="20" x2="12" y2="4" />
    <line x1="6" y1="20" x2="6" y2="14" />
  </svg>
);
const SocialIcon = (p: { class?: string }) => (
  <svg class={p.class} fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
    <circle cx="18" cy="5" r="3" />
    <circle cx="6" cy="12" r="3" />
    <circle cx="18" cy="19" r="3" />
    <line x1="8.59" y1="13.51" x2="15.42" y2="17.49" />
    <line x1="15.41" y1="6.51" x2="8.59" y2="10.49" />
  </svg>
);
const SettingsIcon = (p: { class?: string }) => (
  <svg class={p.class} fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
    <circle cx="12" cy="12" r="3" />
    <path stroke-linecap="round" stroke-linejoin="round" d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06A1.65 1.65 0 0 0 15 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 9 15.37a1.65 1.65 0 0 0-1.82-.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.6a1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 15 4.6" />
  </svg>
);
const LogoutIcon = (p: { class?: string }) => (
  <svg class={p.class} fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
  </svg>
);

const navItems = [
  { href: "/dashboard", icon: HomeIcon, label: "Dashboard" },
  { href: "/ide", icon: CodeIcon, label: "IDE" },
  { href: "/marketplace", icon: ShopIcon, label: "Marketplace" },
  { href: "/chat", icon: ChatIcon, label: "Chat" },
  { href: "/collaboration", icon: EditIcon, label: "Collaboration" },
  { href: "/analytics", icon: ChartIcon, label: "Analytics" },
  { href: "/social", icon: SocialIcon, label: "Social" },
];

const Dashboard: Component = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [tab, setTab] = createSignal<Tab>("posts");
  const [user, setUser] = createSignal<UserDto | null>(null);
  const [stats, setStats] = createSignal<UserStatsDto | null>(null);
  const [portfolio, setPortfolio] = createSignal<UserPortfolioItemDto[]>([]);
  const [posts, setPosts] = createSignal<PostDto[]>([]);
  const [newPost, setNewPost] = createSignal("");
  const [postTags, setPostTags] = createSignal("");
  const [posting, setPosting] = createSignal(false);
  const [loading, setLoading] = createSignal(true);
  const [apiError, setApiError] = createSignal("");
  const [commentingPostId, setCommentingPostId] = createSignal<string | null>(null);
  const [commentText, setCommentText] = createSignal("");

  const [taskStats, setTaskStats] = createSignal<{ total: number; active: number; completed: number } | null>(null);
  const [recommended, setRecommended] = createSignal<SocialUserDto[]>([]);
  const [recommendedTasks, setRecommendedTasks] = createSignal<RecommendedTaskDto[]>([]);
  // Note: trending endpoint doesn't exist, using feed instead
  const [feedPosts, setFeedPosts] = createSignal<PostDto[]>([]);
  const [skills, setSkills] = createSignal<UserSkillsSummaryDto | null>(null);

  /* Edit profile modal */
  const [editModalOpen, setEditModalOpen] = createSignal(false);
  const [editName, setEditName] = createSignal("");
  const [editBio, setEditBio] = createSignal("");
  const [editLocation, setEditLocation] = createSignal("");
  const [editSaving, setEditSaving] = createSignal(false);

  let coverInputRef!: HTMLInputElement;

  onMount(async () => {
    const token = localStorage.getItem("accessToken");
    if (!token) { navigate("/auth"); return; }
    setApiError("");

    const [userRes, taskStatsRes] = await Promise.allSettled([
      apiClient.getMe(),
      apiClient.getTaskStats(),
    ]);

    if (userRes.status === "fulfilled") {
      setUser(userRes.value);
      if (userRes.value.displayName) {
        localStorage.setItem("displayName", userRes.value.displayName);
      }
    } else if ((userRes.reason as Error)?.message?.includes("401")) {
      navigate("/auth");
      return;
    } else {
      setApiError("Failed to load profile");
      setLoading(false);
      return;
    }

    if (taskStatsRes.status === "fulfilled") {
      setTaskStats(taskStatsRes.value);
    }

    setLoading(false);

    /* Lazy load */
    const [postsRes, portfolioRes, statsRes, recRes, feedRes, skillsRes, recTasksRes] = await Promise.allSettled([
      apiClient.getMyPosts(),
      apiClient.getMyPortfolio(),
      apiClient.getMyStats(),
      apiClient.getRecommendedConnections(),
      apiClient.getFeed(1, 5),
      apiClient.getMySkills(),
      apiClient.getRecommendedTasks(5),
    ]);

    if (postsRes.status === "fulfilled" && Array.isArray(postsRes.value)) setPosts(postsRes.value);
    if (portfolioRes.status === "fulfilled") setPortfolio(portfolioRes.value);
    if (statsRes.status === "fulfilled") setStats(statsRes.value);
    if (recRes.status === "fulfilled" && Array.isArray(recRes.value)) setRecommended(recRes.value.slice(0, 3));
    if (feedRes.status === "fulfilled" && Array.isArray(feedRes.value)) setFeedPosts(feedRes.value.slice(0, 5));
    if (skillsRes.status === "fulfilled") setSkills(skillsRes.value);
    if (recTasksRes.status === "fulfilled" && Array.isArray(recTasksRes.value)) setRecommendedTasks(recTasksRes.value);
  });

  const handleLogout = async () => {
    const refreshToken = localStorage.getItem("refreshToken");
    try { if (refreshToken) await apiClient.logout(refreshToken); } catch {}
    localStorage.clear();
    navigate("/auth");
  };

  const handleCreatePost = async () => {
    if (!newPost().trim()) return;
    setPosting(true); setApiError("");
    try {
      const tags = postTags().split(",").map(t => t.trim()).filter(Boolean);
      await apiClient.createPost(
        newPost().trim(),
        tags.length ? tags : undefined,
      );
      // Refresh posts list after creating
      const updatedPosts = await apiClient.getMyPosts();
      setPosts(updatedPosts);
      setNewPost(""); setPostTags("");
    } catch (e: any) {
      setApiError(e.message || "Failed to create post");
    } finally {
      setPosting(false);
    }
  };

  const handleLike = async (postId: string) => {
    try {
      await apiClient.likePost(postId);
      setPosts(prev => prev.map(p =>
        p.id === postId
          ? { ...p, isLikedByCurrentUser: !p.isLikedByCurrentUser, likeCount: p.isLikedByCurrentUser ? p.likeCount - 1 : p.likeCount + 1 }
          : p
      ));
    } catch {}
  };

  const handleAddComment = async (postId: string) => {
    if (!commentText().trim()) return;
    try {
      await apiClient.addComment(postId, commentText().trim());
      setCommentText("");
      setCommentingPostId(null);
      const updated = await apiClient.getMyPosts();
      setPosts(updated);
    } catch (e: any) {
      setApiError(e.message || "Failed to add comment");
    }
  };

  const handleFollow = async (userId: string) => {
    try {
      await fetch(`${config.apiBaseUrl}/api/v1/social/follow/${userId}`, {
        method: "POST",
        headers: { Authorization: `Bearer ${localStorage.getItem("accessToken")}` },
      });
      setRecommended(prev => prev.map(u =>
        u.id === userId ? { ...u, isFollowing: true } : u
      ));
    } catch {}
  };

  const handleSaveProfile = async () => {
    setEditSaving(true);
    try {
      await apiClient.updateProfile(editName(), editBio() || undefined, editLocation() || undefined);
      setUser(u => u ? { ...u, displayName: editName(), bio: editBio() || null, location: editLocation() || null } : u);
      localStorage.setItem("displayName", editName());
      setEditModalOpen(false);
    } catch {
      setApiError("Failed to save profile");
    } finally {
      setEditSaving(false);
    }
  };

  const avatarLetter = () =>
    user()?.displayName?.[0]?.toUpperCase() ?? "?";

  const joinedDate = () => {
    const d = user()?.createdAt;
    if (!d) return "";
    return new Date(d).toLocaleDateString("en-US", { month: "long", year: "numeric" });
  };

  return (
    <div class="flex h-screen bg-[#05050a] text-foreground overflow-hidden">
      {/* ─── Left Sidebar ─── */}
      <aside class="w-16 md:w-60 border-r border-white/5 flex flex-col fixed h-full z-10 bg-[#0F131A]">
        {/* Logo */}
        <div class="p-4 flex items-center gap-3">
          <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-[#35E0D0] to-[#2bc4b6] flex items-center justify-center text-black font-bold text-sm shrink-0">
            L4
          </div>
          <span class="hidden md:block font-bold text-sm text-foreground">Libr4</span>
        </div>

        {/* Nav items */}
        <nav class="flex-1 px-2 md:px-3 space-y-0.5">
          <For each={navItems}>{item => {
            const isActive = location.pathname === item.href;
            const Icon = item.icon;
            return (
              <a href={item.href} class={[
                "flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors",
                isActive
                  ? "bg-primary/10 text-primary font-medium"
                  : "text-muted-foreground hover:text-foreground hover:bg-white/5"
              ].join(" ")}>
                <Icon class="w-5 h-5 shrink-0" />
                <span class="hidden md:inline truncate">{item.label}</span>
              </a>
            );
          }}</For>
        </nav>

        {/* Bottom: settings + logout */}
        <div class="p-2 md:p-3 space-y-0.5 border-t border-white/5">
          <a href="/settings" class="flex items-center gap-3 px-3 py-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/5 text-sm transition-colors">
            <SettingsIcon class="w-5 h-5 shrink-0" />
            <span class="hidden md:inline">Settings</span>
          </a>
          <button onClick={handleLogout} class="flex items-center gap-3 px-3 py-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/5 text-sm w-full text-left transition-colors">
            <LogoutIcon class="w-5 h-5 shrink-0" />
            <span class="hidden md:inline">Logout</span>
          </button>
        </div>
      </aside>

      {/* ─── Main Content ─── */}
      <main class="flex-1 ml-16 md:ml-60 min-w-0 overflow-y-auto">
        {/* Banner */}
        <div class="relative z-0 h-32 group cursor-pointer overflow-hidden" onClick={() => coverInputRef?.click()}>
          <Show
            when={user()?.coverUrl}
            fallback={
              <div class="h-full bg-gradient-to-r from-[#0a1628] via-[#1a1a2e] to-[#0f131a]" />
            }
          >
            <img src={user()!.coverUrl!} class="w-full h-full object-cover" alt="cover" />
          </Show>
          <div class="absolute inset-0 bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
            <span class="text-white text-xs font-medium">Change cover</span>
          </div>
        </div>
        <input ref={el => { coverInputRef = el; }} type="file" accept="image/*" class="hidden"
          onChange={async e => {
            const file = e.currentTarget.files?.[0];
            if (!file) return;
            setApiError("");
            try {
              const res = await apiClient.uploadCover(file);
              setUser(u => u ? { ...u, coverUrl: res.coverUrl } : u);
            } catch (err: any) {
              setApiError(err?.message || "Cover upload failed");
            }
          }} />

        {/* Profile Header */}
        <div class="relative z-10 px-6 -mt-12 mb-6">
          <div class="flex flex-col sm:flex-row items-start sm:items-end gap-4">
            <Show when={user()?.avatarUrl} fallback={
              <div class="w-20 h-20 sm:w-24 sm:h-24 rounded-2xl bg-gradient-to-br from-[#35E0D0] to-[#2bc4b6] flex items-center justify-center text-black text-2xl sm:text-3xl font-bold border-4 border-[#05050a] shrink-0">
                {avatarLetter()}
              </div>
            }>
              <img src={user()!.avatarUrl!} class="w-20 h-20 sm:w-24 sm:h-24 rounded-2xl object-cover border-4 border-[#05050a] shrink-0" alt="avatar" />
            </Show>
            <div class="flex-1 pb-0 sm:pb-2 min-w-0">
              <div class="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2 sm:gap-4">
                <div class="min-w-0">
                  <h1 class="text-lg sm:text-xl font-bold truncate">{user()?.displayName ?? "—"}</h1>
                  <p class="text-sm text-muted-foreground">{user()?.email ? `@${user()!.email.split("@")[0]}` : ""}</p>
                  <Show when={user()?.location}>
                    <p class="text-xs text-muted-foreground mt-0.5">📍 {user()!.location}</p>
                  </Show>
                  <Show when={user()?.bio}>
                    <p class="text-xs text-muted-foreground mt-0.5 line-clamp-2">{user()!.bio}</p>
                  </Show>
                  <Show when={joinedDate()}>
                    <p class="text-xs text-muted-foreground mt-0.5">Joined {joinedDate()}</p>
                  </Show>
                </div>
                <button
                  onClick={() => { setEditModalOpen(true); setEditName(user()?.displayName || ""); setEditBio(user()?.bio || ""); setEditLocation(user()?.location || ""); }}
                  class="px-4 py-1.5 text-sm bg-surface-2 border border-white/10 rounded-lg hover:bg-surface-3 transition-colors shrink-0"
                >
                  Edit profile
                </button>
              </div>
            </div>
          </div>

          {/* Stats row */}
          <Show when={loading()}>
            <div class="flex flex-wrap items-center gap-x-6 gap-y-2 mt-4">
              <div class="h-5 w-16 bg-white/5 rounded animate-pulse" />
              <div class="h-5 w-16 bg-white/5 rounded animate-pulse" />
              <div class="h-5 w-16 bg-white/5 rounded animate-pulse" />
            </div>
          </Show>
          <Show when={!loading()}>
            <div class="flex flex-wrap items-center gap-x-6 gap-y-2 mt-4 text-sm">
              <span>
                <strong class="text-foreground">{stats()?.totalProjects ?? taskStats()?.total ?? 0}</strong>{" "}
                <span class="text-muted-foreground">projects</span>
              </span>
              <span>
                <strong class="text-foreground">{taskStats()?.active ?? 0}</strong>{" "}
                <span class="text-muted-foreground">active</span>
              </span>
              <span>
                <strong class="text-foreground">{stats()?.averageRating?.toFixed(1) ?? "—"}</strong>{" "}
                <span class="text-muted-foreground">rating</span>
              </span>
            </div>
          </Show>
        </div>

        {/* API Error */}
        <Show when={apiError()}>
          <div class="mx-4 sm:mx-6 my-4 p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-sm flex items-center justify-between gap-3 flex-wrap">
            <span>{apiError()}</span>
            <div class="flex items-center gap-3">
              <button onClick={() => window.location.reload()} class="underline">Retry</button>
              <button onClick={() => setApiError("")} class="text-lg leading-none hover:text-red-300">×</button>
            </div>
          </div>
        </Show>

        {/* Tabs */}
        <div class="px-6 border-b border-white/5 overflow-x-auto">
          <div class="flex gap-6 min-w-0">
            {(["posts", "portfolio", "stats"] as Tab[]).map((t) => (
              <button
                onClick={() => setTab(t)}
                class={[
                  "pb-3 text-sm font-medium capitalize transition-colors whitespace-nowrap",
                  tab() === t
                    ? "text-secondary border-b-2 border-secondary"
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
              <input
                type="text"
                value={postTags()}
                onInput={e => setPostTags(e.currentTarget.value)}
                placeholder="Tags: rust, ai, webdev"
                class="w-full bg-transparent text-xs text-muted-foreground mt-2 focus:outline-none focus:text-foreground"
              />
              <div class="flex items-center justify-between mt-2">
                <span class={`text-xs ${newPost().length > 450 ? "text-yellow-400" : "text-muted-foreground"}`}>
                  {newPost().length}/500
                </span>
                <button
                  onClick={handleCreatePost}
                  disabled={posting() || !newPost().trim() || newPost().length > 500}
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
                    <p class="text-sm text-foreground whitespace-pre-wrap mb-3">{post.content}</p>
                    <Show when={post.tags && post.tags.length > 0}>
                      <div class="flex flex-wrap gap-1.5 mb-3">
                        <For each={post.tags}>
                          {(tag) => <span class="text-xs text-secondary">#{tag}</span>}
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
                      <button
                        onClick={() => setCommentingPostId(commentingPostId() === post.id ? null : post.id)}
                        class="hover:text-secondary transition-colors"
                      >
                        💬 {post.commentCount}
                      </button>
                      <span class="ml-auto">👁 {post.viewCount}</span>
                    </div>
                    <Show when={(post as any).comments?.length > 0}>
                      <div class="mt-3 space-y-2">
                        <For each={(post as any).comments}>
                          {(c: any) => (
                            <div class="flex gap-2 text-xs">
                              <span class="w-6 h-6 rounded-full bg-white/10 flex items-center justify-center text-[10px] shrink-0">👤</span>
                              <div class="bg-white/5 rounded-lg px-3 py-1.5 flex-1">
                                <p class="text-foreground">{c.text}</p>
                              </div>
                            </div>
                          )}
                        </For>
                      </div>
                    </Show>
                    <Show when={commentingPostId() === post.id}>
                      <div class="mt-3 flex gap-2">
                        <input
                          value={commentText()}
                          onInput={(e) => setCommentText(e.currentTarget.value)}
                          placeholder="Write a comment…"
                          class="flex-1 bg-white/5 border border-white/10 rounded-lg px-3 py-1.5 text-xs focus:outline-none focus:border-[#35E0D0]/60 text-foreground placeholder:text-muted-foreground"
                          onKeyDown={(e) => e.key === 'Enter' && handleAddComment(post.id)}
                        />
                        <button
                          onClick={() => handleAddComment(post.id)}
                          disabled={!commentText().trim()}
                          class="px-3 py-1.5 bg-[#35E0D0] text-black text-xs rounded-lg hover:bg-[#2bc4b6] disabled:opacity-50 transition-colors"
                        >
                          Send
                        </button>
                      </div>
                    </Show>
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
                  <div class="bg-white/5 rounded-xl p-4 border border-white/5 hover:border-secondary/30 transition-colors">
                    <div class="flex items-start justify-between mb-2">
                      <h3 class="text-sm font-semibold">{item.title}</h3>
                      <span class={[
                        "text-[10px] px-1.5 py-0.5 rounded-full uppercase",
                        item.status === "Published" ? "bg-green-500/10 text-green-400" : "bg-yellow-500/10 text-yellow-400",
                      ].join(" ")}>{item.status}</span>
                    </div>
                    <p class="text-xs text-muted-foreground line-clamp-2 mb-3">{item.description}</p>
                    <div class="flex flex-wrap gap-1 mb-3">
                      <For each={item.skillsUsed?.slice(0, 4) || []}>
                        {(s) => <span class="text-[10px] px-1.5 py-0.5 bg-white/5 text-muted-foreground rounded">{s}</span>}
                      </For>
                    </div>
                    <div class="flex items-center gap-3 text-[11px] text-muted-foreground">
                      <span>❤️ {item.likeCount}</span>
                      <span>👁 {item.viewCount}</span>
                      <Show when={item.liveUrl}>
                        <a href={item.liveUrl!} target="_blank" class="text-secondary hover:underline ml-auto">Live ↗</a>
                      </Show>
                    </div>
                  </div>
                )}
              </For>
            </div>
          </Show>

          {/* Stats */}
          <Show when={tab() === "stats"}>
            <div class="space-y-6">
              {/* AI-Assessed Skills Section */}
              <Show when={skills()} fallback={
                <div class="text-center py-8">
                  <p class="text-sm text-muted-foreground">Skills assessment pending...</p>
                  <p class="text-xs text-muted-foreground mt-2">Complete CV verification to see your AI-assessed skills</p>
                </div>
              }>
                <div>
                  <div class="flex items-center gap-3 mb-4">
                    <h3 class="text-lg font-semibold">AI-Assessed Skills</h3>
                    <span class="text-xs bg-secondary/10 text-secondary px-2 py-1 rounded-full">
                      {skills()!.overallLevel}
                    </span>
                  </div>
                  
                  {/* Primary Expertise */}
                  <div class="mb-4 p-3 bg-secondary/5 rounded-lg border border-secondary/20">
                    <p class="text-xs text-muted-foreground mb-1">Primary Expertise</p>
                    <p class="font-medium text-secondary">{skills()!.primaryExpertise}</p>
                  </div>

                  {/* Skills List — compact multi-column */}
                  <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                    <For each={skills()!.skills?.slice(0, 12) || []}>
                      {(skill) => (
                        <div class="group relative bg-white/5 rounded-lg p-2.5 border border-white/5 hover:border-secondary/20 transition-colors">
                          <div class="flex items-center justify-between mb-1.5">
                            <div class="flex items-center gap-1.5">
                              <span class="text-xs font-medium">{skill.name}</span>
                              {/* Tooltip trigger */}
                              <div class="relative">
                                <svg
                                  class="w-3.5 h-3.5 text-muted-foreground hover:text-secondary cursor-help transition-colors"
                                  fill="none"
                                  stroke="currentColor"
                                  viewBox="0 0 24 24"
                                >
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                                </svg>
                                {/* Tooltip */}
                                <div class="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-56 p-2.5 bg-surface-2 border border-white/10 rounded-lg shadow-xl opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-50">
                                  <p class="text-xs font-medium mb-1">Why this score?</p>
                                  <p class="text-xs text-muted-foreground mb-1.5">
                                    {skill.assessmentReason || `Based on ${skill.experienceYears} years of experience in ${skill.contexts.join(', ')}`}
                                  </p>
                                  <div class="flex items-center gap-2 text-[10px]">
                                    <span class="text-secondary">{skill.level}</span>
                                    <span class="text-muted-foreground">•</span>
                                    <span class="text-muted-foreground">Source: {skill.source}</span>
                                  </div>
                                </div>
                              </div>
                            </div>
                            <span class="text-xs font-bold">{(skill.score / 10).toFixed(1)}</span>
                          </div>
                          {/* Compact progress bar */}
                          <div class="h-1.5 bg-white/10 rounded-full overflow-hidden">
                            <div
                              class="h-full bg-gradient-to-r from-primary to-secondary rounded-full transition-all duration-500"
                              style={`width: ${skill.score}%`}
                            />
                          </div>
                        </div>
                      )}
                    </For>
                  </div>

                  {/* Secondary Expertise */}
                  <Show when={skills()!.secondaryExpertise?.length > 0}>
                    <div class="mt-4 pt-4 border-t border-white/10">
                      <p class="text-xs text-muted-foreground mb-2">Secondary Expertise</p>
                      <div class="flex flex-wrap gap-2">
                        <For each={skills()!.secondaryExpertise}>
                          {(exp) => (
                            <span class="text-xs bg-white/5 px-2 py-1 rounded border border-white/10">
                              {exp}
                            </span>
                          )}
                        </For>
                      </div>
                    </div>
                  </Show>

                  {/* Recommended Professions */}
                  <Show when={skills()!.recommendations?.length > 0}>
                    <div class="mt-4 pt-4 border-t border-white/10">
                      <p class="text-xs text-muted-foreground mb-3">Recommended Professions</p>
                      <div class="flex flex-col gap-2">
                        <For each={skills()!.recommendations}>
                          {(prof, i) => (
                            <div class="flex items-center gap-3 p-2.5 rounded-lg bg-white/5 border border-white/5 hover:border-secondary/20 transition-colors">
                              <div class="w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold shrink-0"
                                style={`background: hsl(${210 + i() * 40}, 70%, 25%); color: hsl(${210 + i() * 40}, 80%, 75%)`}>
                                {i() + 1}
                              </div>
                              <span class="text-sm font-medium">{prof}</span>
                            </div>
                          )}
                        </For>
                      </div>
                    </div>
                  </Show>
                </div>
              </Show>

              {/* Traditional Stats */}
              <Show when={stats()}>
                <div class="pt-6 border-t border-white/10">
                  <h3 class="text-sm font-semibold mb-4 text-muted-foreground">Activity Stats</h3>
                  <div class="grid grid-cols-2 md:grid-cols-3 gap-4">
                    {([
                      { label: "Total Projects", value: stats()!.totalProjects },
                      { label: "Completed Projects", value: stats()!.completedProjects },
                      { label: "In Progress", value: stats()!.inProgressProjects },
                      { label: "Total Tasks", value: stats()!.totalTasks },
                      { label: "Completed Tasks", value: stats()!.completedTasks },
                      { label: "Portfolio Items", value: stats()!.portfolioItemsCount },
                    ] as { label: string; value: string | number }[]).map((s) => (
                      <div class="bg-white/5 rounded-xl p-4 border border-white/5">
                        <p class="text-xs text-muted-foreground mb-1">{s.label}</p>
                        <p class="text-xl font-bold text-secondary">{s.value}</p>
                      </div>
                    ))}
                  </div>
                </div>
              </Show>
            </div>
          </Show>
        </div>
      </main>

      {/* ─── Right Sidebar ─── */}
      <aside class="w-72 border-l border-white/5 hidden lg:flex flex-col p-4 space-y-6 shrink-0 overflow-y-auto">
        {/* Latest Feed */}
        <Show when={feedPosts().length > 0}>
          <div>
            <h3 class="text-sm font-semibold mb-3">Latest</h3>
            <div class="space-y-3">
              <For each={feedPosts()}>
                {(p) => (
                  <div class="text-sm">
                    <p class="text-foreground line-clamp-2">{p.content}</p>
                    <p class="text-xs text-muted-foreground mt-1">{p.likeCount} likes</p>
                  </div>
                )}
              </For>
            </div>
          </div>
        </Show>

        {/* Recommended Tasks */}
        <Show when={recommendedTasks().length > 0}>
          <div>
            <h3 class="text-sm font-semibold mb-3">Recommended for you</h3>
            <div class="space-y-3">
              <For each={recommendedTasks()}>
                {(task) => (
                  <div class="bg-white/5 rounded-xl p-3 border border-white/5 hover:border-secondary/30 transition-colors cursor-pointer"
                       onClick={() => navigate(`/marketplace/task/${task.taskId}`)}>
                    <div class="flex items-center justify-between mb-1">
                      <span class="text-xs font-medium text-secondary">
                        {(task.totalScore * 100).toFixed(0)}% match
                      </span>
                    </div>
                    <Show when={task.matchingSkills.length > 0}>
                      <div class="flex flex-wrap gap-1 mt-1">
                        <For each={task.matchingSkills.slice(0, 3)}>
                          {(skill) => (
                            <span class="text-[10px] px-1.5 py-0.5 bg-secondary/10 text-secondary rounded">
                              {skill}
                            </span>
                          )}
                        </For>
                      </div>
                    </Show>
                    <p class="text-xs text-muted-foreground mt-1 line-clamp-1">{task.explanation}</p>
                  </div>
                )}
              </For>
            </div>
          </div>
        </Show>

        {/* Who to follow */}
        <Show when={recommended().length > 0}>
          <div>
            <h3 class="text-sm font-semibold mb-3">Who to follow</h3>
            <div class="space-y-3">
              <For each={recommended()}>
                {(u) => (
                  <div class="flex items-center justify-between">
                    <div class="flex items-center gap-2">
                      <div class="w-8 h-8 rounded-full bg-white/10 flex items-center justify-center text-xs font-medium">
                        {u.displayName?.[0] ?? "?"}
                      </div>
                      <div>
                        <div class="text-sm font-medium">{u.displayName}</div>
                        <div class="text-xs text-muted-foreground">{u.handle}</div>
                      </div>
                    </div>
                    <button
                      onClick={() => handleFollow(u.id)}
                      disabled={u.isFollowing}
                      class={[
                        "px-3 py-1 text-xs rounded-full transition-colors",
                        u.isFollowing
                          ? "border border-white/10 text-muted-foreground"
                          : "border border-secondary text-secondary hover:bg-secondary/10"
                      ].join(" ")}
                    >
                      {u.isFollowing ? "Following" : "Follow"}
                    </button>
                  </div>
                )}
              </For>
            </div>
          </div>
        </Show>
      </aside>

      {/* ─── Edit Profile Modal ─── */}
      <Show when={editModalOpen()}>
        <div class="fixed inset-0 z-50 flex items-start sm:items-center justify-center bg-black/50 backdrop-blur-sm py-4 overflow-y-auto"
             onClick={e => { if (e.target === e.currentTarget) setEditModalOpen(false); }}>
          <div class="bg-[#0F131A] border border-white/10 rounded-2xl p-6 w-full max-w-md mx-4 max-h-[90vh] overflow-y-auto">
            <h2 class="text-base font-semibold mb-4">Edit Profile</h2>
            <div class="space-y-3">
              {/* Avatar upload */}
              <div class="flex items-center gap-4">
                <div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-primary to-secondary flex items-center justify-center text-black text-2xl font-bold">
                  {avatarLetter()}
                </div>
                <label class="px-3 py-1.5 text-xs border border-white/10 rounded-lg cursor-pointer hover:bg-white/5 transition-colors">
                  Upload photo
                  <input type="file" accept="image/*" class="hidden"
                    onChange={async e => {
                      const file = e.currentTarget.files?.[0];
                      if (!file) return;
                      try {
                        const res = await apiClient.uploadAvatar(file);
                        setUser(u => u ? { ...u, avatarUrl: res.avatarUrl } : u);
                      } catch {}
                    }} />
                </label>
              </div>

              <input type="text" value={editName()} onInput={e => setEditName(e.currentTarget.value)}
                placeholder="Display name"
                class="w-full px-3 py-2.5 bg-white/5 border border-white/10 rounded-xl text-sm text-foreground focus:outline-none focus:border-primary/40" />
              <input type="text" value={editLocation()} onInput={e => setEditLocation(e.currentTarget.value)}
                placeholder="Location (e.g. New York, USA)"
                class="w-full px-3 py-2.5 bg-white/5 border border-white/10 rounded-xl text-sm text-foreground focus:outline-none focus:border-primary/40" />
              <textarea value={editBio()} onInput={e => setEditBio(e.currentTarget.value)}
                placeholder="Bio"
                rows={3}
                class="w-full px-3 py-2.5 bg-white/5 border border-white/10 rounded-xl text-sm text-foreground focus:outline-none focus:border-primary/40 resize-none" />
            </div>
            <div class="flex gap-3 mt-5">
              <button onClick={() => setEditModalOpen(false)}
                class="flex-1 py-2.5 rounded-xl bg-white/5 text-sm hover:bg-white/10 transition-colors">
                Cancel
              </button>
              <button onClick={handleSaveProfile} disabled={editSaving()}
                class="flex-1 py-2.5 bg-gradient-to-r from-[#35E0D0] to-[#2bc4b6] text-black font-bold rounded-xl text-sm hover:opacity-90 disabled:opacity-50 transition-all">
                {editSaving() ? "Saving…" : "Save"}
              </button>
            </div>
          </div>
        </div>
      </Show>
    </div>
  );
};

export default Dashboard;
