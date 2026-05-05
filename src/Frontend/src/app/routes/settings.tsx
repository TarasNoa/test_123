import { Component } from "solid-js";

const Settings: Component = () => {
  return (
    <div class="p-8">
      <h1 class="text-2xl font-bold mb-6">Settings</h1>
      
      <div class="space-y-6">
        <div class="p-4 border rounded-lg">
          <h2 class="font-semibold mb-2">gRPC Connection</h2>
          <p class="text-sm text-muted-foreground mb-4">
            Rust Sandbox Endpoint
          </p>
          <input
            type="text"
            value="http://localhost:50051"
            class="w-full p-2 border rounded"
            placeholder="http://localhost:50051"
          />
        </div>

        <div class="p-4 border rounded-lg">
          <h2 class="font-semibold mb-2">AI Configuration</h2>
          <p class="text-sm text-muted-foreground mb-4">
            TanStack AI SDK Settings
          </p>
          <div class="space-y-2">
            <div>
              <label class="text-sm">Provider</label>
              <select class="w-full p-2 border rounded mt-1">
                <option>OpenAI</option>
                <option>Anthropic</option>
                <option>Local LLM</option>
              </select>
            </div>
            <div>
              <label class="text-sm">Model</label>
              <input
                type="text"
                value="gpt-4"
                class="w-full p-2 border rounded mt-1"
              />
            </div>
          </div>
        </div>

        <div class="p-4 border rounded-lg">
          <h2 class="font-semibold mb-2">Theme</h2>
          <div class="flex gap-2">
            <button class="px-4 py-2 border rounded hover:bg-muted">
              Light
            </button>
            <button class="px-4 py-2 border rounded hover:bg-muted">
              Dark
            </button>
            <button class="px-4 py-2 border rounded hover:bg-muted">
              System
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Settings;
