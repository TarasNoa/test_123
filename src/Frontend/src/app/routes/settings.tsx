import { Component, createSignal } from "solid-js";
import { config } from "../../lib/config";

const Settings: Component = () => {
  const [grpcUrl, setGrpcUrl] = createSignal(config.grpcBaseUrl);
  const [theme, setTheme] = createSignal<"light" | "dark" | "system">("system");

  const applyTheme = (t: "light" | "dark" | "system") => {
    setTheme(t);
    const root = document.documentElement;
    if (t === "dark") root.classList.add("dark");
    else if (t === "light") root.classList.remove("dark");
    else {
      // system — проверяем prefers-color-scheme
      if (window.matchMedia("(prefers-color-scheme: dark)").matches) {
        root.classList.add("dark");
      } else {
        root.classList.remove("dark");
      }
    }
  };

  return (
    <div class="min-h-screen bg-background text-foreground p-6">
      <div class="max-w-2xl mx-auto space-y-6">
        <h1 class="text-2xl font-bold mb-6">Settings</h1>

        <div class="p-6 bg-surface border border-surface-3 rounded-2xl space-y-4">
          <div>
            <h2 class="font-semibold text-foreground">gRPC Connection</h2>
            <p class="text-sm text-muted-foreground">Rust Sandbox Endpoint</p>
          </div>
          <input
            type="text"
            value={grpcUrl()}
            onInput={(e) => setGrpcUrl(e.currentTarget.value)}
            class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
            placeholder="http://localhost:50051"
          />
          <p class="text-xs text-muted-foreground">
            Changes apply on next code execution
          </p>
        </div>

        <div class="p-6 bg-surface border border-surface-3 rounded-2xl space-y-4">
          <div>
            <h2 class="font-semibold text-foreground">AI Configuration</h2>
            <p class="text-sm text-muted-foreground">AI SDK Settings</p>
          </div>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-muted-foreground mb-1">Provider</label>
              <select class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all">
                <option>Local LLM (Ollama)</option>
                <option>OpenAI</option>
                <option>Anthropic</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-muted-foreground mb-1">Model</label>
              <input
                type="text"
                value="qwen3:8b"
                class="w-full px-4 py-2.5 bg-surface-2 border border-surface-3 rounded-lg text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
              />
            </div>
          </div>
        </div>

        <div class="p-6 bg-surface border border-surface-3 rounded-2xl space-y-4">
          <h2 class="font-semibold text-foreground">Theme</h2>
          <div class="flex gap-2">
            {(["light", "dark", "system"] as const).map((t) => (
              <button
                class={
                  theme() === t
                    ? 'px-4 py-2 bg-primary text-primary-foreground font-medium rounded-lg transition-colors'
                    : 'px-4 py-2 bg-surface-2 border border-surface-3 text-muted-foreground rounded-lg hover:text-foreground hover:bg-surface-3 transition-colors'
                }
                onClick={() => applyTheme(t)}
              >
                {t.charAt(0).toUpperCase() + t.slice(1)}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default Settings;
