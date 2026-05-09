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
    <div class="p-8">
      <h1 class="text-2xl font-bold mb-6">Settings</h1>

      <div class="space-y-6">
        <div class="p-4 border rounded-lg">
          <h2 class="font-semibold mb-2">gRPC Connection</h2>
          <p class="text-sm text-muted-foreground mb-4">Rust Sandbox Endpoint</p>
          <input
            type="text"
            value={grpcUrl()}
            onInput={(e) => setGrpcUrl(e.currentTarget.value)}
            class="w-full p-2 border rounded"
            placeholder="http://localhost:50051"
          />
          <p class="text-xs text-muted-foreground mt-1">
            Изменение применяется при следующем выполнении кода
          </p>
        </div>

        <div class="p-4 border rounded-lg">
          <h2 class="font-semibold mb-2">AI Configuration</h2>
          <p class="text-sm text-muted-foreground mb-4">TanStack AI SDK Settings</p>
          <div class="space-y-2">
            <div>
              <label class="text-sm">Provider</label>
              <select class="w-full p-2 border rounded mt-1">
                <option>Local LLM (Ollama)</option>
                <option>OpenAI</option>
                <option>Anthropic</option>
              </select>
            </div>
            <div>
              <label class="text-sm">Model</label>
              <input
                type="text"
                value="qwen3:8b"
                class="w-full p-2 border rounded mt-1"
              />
            </div>
          </div>
        </div>

        <div class="p-4 border rounded-lg">
          <h2 class="font-semibold mb-2">Theme</h2>
          <div class="flex gap-2">
            {(["light", "dark", "system"] as const).map((t) => (
              <button
                class="px-4 py-2 border rounded hover:bg-muted"
                classList={{ "bg-primary text-primary-foreground": theme() === t }}
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
