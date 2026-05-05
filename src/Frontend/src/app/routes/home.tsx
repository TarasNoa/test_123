import { Component } from "solid-js";

const Home: Component = () => {
  return (
    <div class="flex flex-col items-center justify-center min-h-screen p-8">
      <h1 class="text-4xl font-bold mb-4">Libr4 IDE</h1>
      <p class="text-lg text-muted-foreground mb-8">
        Golden Stack Frontend (2026)
      </p>
      <div class="grid gap-4 text-sm">
        <div class="p-4 border rounded-lg">
          <h3 class="font-semibold mb-2">Framework: SolidStart (SolidJS)</h3>
          <p class="text-muted-foreground">Zero overhead, instant reactivity</p>
        </div>
        <div class="p-4 border rounded-lg">
          <h3 class="font-semibold mb-2">Runtime: Bun</h3>
          <p class="text-muted-foreground">Fastest JS runtime, written in Zig</p>
        </div>
        <div class="p-4 border rounded-lg">
          <h3 class="font-semibold mb-2">AI: TanStack AI SDK</h3>
          <p class="text-muted-foreground">Vendor-neutral, extreme type safety</p>
        </div>
        <div class="p-4 border rounded-lg">
          <h3 class="font-semibold mb-2">Communication: gRPC-web</h3>
          <p class="text-muted-foreground">Direct Protobuf from C# to TypeScript</p>
        </div>
      </div>
      <a
        href="/ide"
        class="mt-8 px-6 py-3 bg-primary text-primary-foreground rounded-lg hover:opacity-90 transition-opacity"
      >
        Open IDE
      </a>
    </div>
  );
};

export default Home;
