import { Component, createSignal, onMount } from "solid-js";
import { fetchAgentEvents } from "../../lib/api-client";

const IDE: Component = () => {
  const [code, setCode] = createSignal("// Welcome to Libr4 IDE\n// Write your code here\n\nconsole.log('Hello, Golden Stack!');");
  const [output, setOutput] = createSignal("");
  const [isExecuting, setIsExecuting] = createSignal(false);
  const [events, setEvents] = createSignal<any[]>([]);
  const [isLoadingEvents, setIsLoadingEvents] = createSignal(false);

  const executeCode = async () => {
    setIsExecuting(true);
    setOutput("Executing in Rust sandbox...");

    // Placeholder for gRPC call to Rust sandbox
    try {
      // const result = await grpcClient.executeCode({ code: code() });
      // setOutput(result.stdout);
      setTimeout(() => {
        setOutput("Hello, Golden Stack!\n\n[Demo: gRPC call to Rust sandbox]");
        setIsExecuting(false);
      }, 1000);
    } catch (error) {
      setOutput(`Error: ${error}`);
      setIsExecuting(false);
    }
  };

  const loadEvents = async () => {
    setIsLoadingEvents(true);
    try {
      const data = await fetchAgentEvents();
      setEvents(data);
    } catch (error) {
      console.error("Failed to load events:", error);
    } finally {
      setIsLoadingEvents(false);
    }
  };

  onMount(() => {
    loadEvents();
  });

  return (
    <div class="flex flex-col h-screen">
      <header class="border-b p-4 flex items-center justify-between">
        <h1 class="text-xl font-bold">Libr4 IDE</h1>
        <div class="flex gap-2">
          <button
            class="px-4 py-2 bg-secondary rounded hover:opacity-90"
            onClick={() => setCode("")}
          >
            Clear
          </button>
          <button
            class="px-4 py-2 bg-primary text-primary-foreground rounded hover:opacity-90 disabled:opacity-50"
            onClick={executeCode}
            disabled={isExecuting()}
          >
            {isExecuting() ? "Running..." : "Run"}
          </button>
        </div>
      </header>

      <div class="flex flex-1">
        <div class="flex-1 p-4">
          <textarea
            class="w-full h-full p-4 font-mono text-sm border rounded resize-none bg-background"
            value={code()}
            onInput={(e) => setCode(e.currentTarget.value)}
            placeholder="Write your code here..."
          />
        </div>

        <div class="flex-1 p-4 border-l flex flex-col">
          <div class="flex items-center justify-between mb-2">
            <h2 class="text-sm font-semibold">Output</h2>
            <button
              class="text-xs px-2 py-1 bg-muted rounded hover:opacity-90"
              onClick={loadEvents}
              disabled={isLoadingEvents()}
            >
              {isLoadingEvents() ? "Loading..." : "Refresh Events"}
            </button>
          </div>
          <pre class="flex-1 w-full p-4 font-mono text-sm border rounded bg-muted overflow-auto">
            {output() || "No output yet"}
          </pre>
          
          <div class="mt-4">
            <h3 class="text-sm font-semibold mb-2">Agent Events ({events().length})</h3>
            <div class="h-32 overflow-auto border rounded bg-background p-2">
              {events().length === 0 ? (
                <p class="text-sm text-muted-foreground">No events yet</p>
              ) : (
                events().map((event) => (
                  <div key={event.id} class="text-xs p-1 border-b">
                    <span class="font-semibold">{event.type}</span>
                    <span class="text-muted-foreground"> - {event.timestamp}</span>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default IDE;
