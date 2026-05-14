import { Component } from "solid-js";

const Home: Component = () => {
  const modules = [
    { name: 'IDE', path: '/ide', desc: 'Code editor with AI assistance', color: 'bg-primary' },
    { name: 'Marketplace', path: '/marketplace', desc: 'AI-powered freelance exchange', color: 'bg-secondary' },
    { name: 'Social', path: '/social', desc: 'Developer network & feed', color: 'bg-accent' },
    { name: 'Chat', path: '/chat', desc: 'Real-time messaging', color: 'bg-info' },
    { name: 'Platform', path: '/platform', desc: 'Team workspace & channels', color: 'bg-success' },
    { name: 'Collaboration', path: '/collaboration', desc: 'Shared docs & whiteboards', color: 'bg-warning' },
    { name: 'Analytics', path: '/analytics', desc: 'Metrics & dashboards', color: 'bg-error' },
    { name: 'Settings', path: '/settings', desc: 'Configuration', color: 'bg-muted' },
  ];

  return (
    <div class="flex flex-col items-center justify-center min-h-screen bg-background text-foreground p-8">
      <h1 class="text-5xl font-extrabold mb-2 tracking-tight">Libr4</h1>
      <p class="text-lg text-muted-foreground mb-10">
        AI-powered developer platform
      </p>

      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 w-full max-w-5xl">
        {modules.map((m) => (
          <a
            href={m.path}
            class="group p-5 bg-surface border border-surface-3 rounded-2xl hover:border-primary/40 hover:bg-surface-2 transition-all"
          >
            <div class={`w-10 h-10 ${m.color} rounded-lg mb-3 opacity-80 group-hover:opacity-100 transition-opacity`} />
            <h3 class="font-semibold text-foreground mb-1">{m.name}</h3>
            <p class="text-sm text-muted-foreground">{m.desc}</p>
          </a>
        ))}
      </div>

      <div class="mt-10 flex gap-4">
        <a
          href="/auth"
          class="px-6 py-2.5 bg-primary text-primary-foreground font-medium rounded-lg hover:bg-primary/90 transition-colors"
        >
          Login / Register
        </a>
      </div>
    </div>
  );
};

export default Home;
