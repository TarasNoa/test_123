import { type Component } from "solid-js";
import { useNavigate } from "@solidjs/router";

/* ─── Dashboard ─── */
const Dashboard: Component = () => {
  const navigate = useNavigate();

  const trending = [
    { tag: '#web3', posts: 102 },
    { tag: '#rust', posts: 90 },
    { tag: '#ai', posts: 170 },
    { tag: '#freelance', posts: 30 },
    { tag: '#startup', posts: 40 },
  ];

  const whoToFollow = [
    { name: 'Alex Dev', handle: '@alexdev' },
    { name: 'Sarah Design', handle: '@sarahdsgn' },
    { name: 'Mike AI', handle: '@mikeai' },
  ];

  return (
    <div class="flex min-h-screen bg-[#05050a] text-foreground">
      {/* Left Sidebar */}
      <aside class="w-16 md:w-60 border-r border-white/5 flex flex-col fixed h-full">
        <div class="p-4 md:p-6">
          <div class="w-8 h-8 rounded-lg bg-[#35E0D0] flex items-center justify-center text-black font-bold text-sm">
            L4
          </div>
        </div>
        <nav class="flex-1 px-2 md:px-4 space-y-1">
          <a href="/dashboard" class="flex items-center gap-3 px-3 py-2 rounded-lg bg-white/5 text-[#35E0D0] text-sm font-medium">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M2.25 12l8.954-8.955c.44-.439 1.152-.439 1.591 0L21.75 12M4.5 9.75v10.125c0 .621.504 1.125 1.125 1.125H9.75v-4.875c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21h4.125c.621 0 1.125-.504 1.125-1.125V9.75M8.25 21h8.25" />
            </svg>
            <span class="hidden md:inline">Dashboard</span>
          </a>
        </nav>
        <div class="p-2 md:p-4">
          <button class="flex items-center gap-3 px-3 py-2 rounded-lg text-muted-foreground hover:text-foreground hover:bg-white/5 text-sm w-full text-left">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9" />
            </svg>
            <span class="hidden md:inline">Logout</span>
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main class="flex-1 ml-16 md:ml-60">
        {/* Banner */}
        <div class="h-32 bg-gradient-to-r from-[#0a1628] via-[#1a1a2e] to-[#0f131a] relative" />

        {/* Profile Header */}
        <div class="px-6 -mt-12 mb-6">
          <div class="flex items-end gap-4">
            <div class="w-24 h-24 rounded-2xl bg-gradient-to-br from-[#35E0D0] to-[#2bc4b6] flex items-center justify-center text-black text-3xl font-bold border-4 border-[#05050a]">
              T
            </div>
            <div class="flex-1 pb-2">
              <div class="flex items-center justify-between">
                <div>
                  <h1 class="text-xl font-bold">tarar</h1>
                  <p class="text-sm text-muted-foreground">@dkdkulul</p>
                  <p class="text-xs text-muted-foreground mt-1">Joined May 2021</p>
                </div>
                <button class="px-4 py-1.5 text-sm border border-white/10 rounded-lg hover:bg-white/5 transition-colors">
                  Edit profile
                </button>
              </div>
            </div>
          </div>

          {/* Stats */}
          <div class="flex items-center gap-6 mt-4 text-sm">
            <span><strong class="text-foreground">0</strong> <span class="text-muted-foreground">projects</span></span>
            <span><strong class="text-foreground">0</strong> <span class="text-muted-foreground">tasks</span></span>
            <span><strong class="text-foreground">0.0</strong> <span class="text-muted-foreground">rating</span></span>
          </div>
        </div>

        {/* Tabs */}
        <div class="px-6 border-b border-white/5">
          <div class="flex gap-6">
            <button class="pb-3 text-sm font-medium text-[#35E0D0] border-b-2 border-[#35E0D0]">Posts</button>
            <button class="pb-3 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors">Portfolio</button>
            <button class="pb-3 text-sm font-medium text-muted-foreground hover:text-foreground transition-colors">Stats</button>
          </div>
        </div>

        {/* Posts content */}
        <div class="px-6 py-8">
          <p class="text-sm text-muted-foreground text-center">No posts yet. Start a project to see activity here.</p>
        </div>
      </main>

      {/* Right Sidebar */}
      <aside class="w-72 border-l border-white/5 hidden xl:block p-4 space-y-6">
        {/* Trending */}
        <div>
          <h3 class="text-sm font-semibold mb-3">Trending</h3>
          <div class="space-y-2">
            {trending.map((t) => (
              <div class="flex items-center justify-between text-sm">
                <span class="text-[#35E0D0]">{t.tag}</span>
                <span class="text-xs text-muted-foreground">{t.posts} posts</span>
              </div>
            ))}
          </div>
        </div>

        {/* Who to follow */}
        <div>
          <h3 class="text-sm font-semibold mb-3">Who to follow</h3>
          <div class="space-y-3">
            {whoToFollow.map((user) => (
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-2">
                  <div class="w-8 h-8 rounded-full bg-surface-2 flex items-center justify-center text-xs font-medium">
                    {user.name[0]}
                  </div>
                  <div>
                    <div class="text-sm font-medium">{user.name}</div>
                    <div class="text-xs text-muted-foreground">{user.handle}</div>
                  </div>
                </div>
                <button class="px-3 py-1 text-xs border border-[#35E0D0] text-[#35E0D0] rounded-full hover:bg-[#35E0D0]/10 transition-colors">
                  Follow
                </button>
              </div>
            ))}
          </div>
        </div>
      </aside>
    </div>
  );
};

export default Dashboard;
