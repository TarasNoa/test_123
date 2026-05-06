import { Component, createSignal } from "solid-js";
import { AgentEventList } from "../../components/AgentEventList";

const IDE: Component = () => {
  const [code, setCode] = createSignal("// Welcome to Libr4 IDE\n// Write your code here\n\nconsole.log('Hello, Golden Stack!');");
  const [output, setOutput] = createSignal("");
  const [isExecuting, setIsExecuting] = createSignal(false);
  const [isAgentBusy, setIsAgentBusy] = createSignal(false);

  const executeCode = async () => {
    setIsExecuting(true);
    setIsAgentBusy(true);
    setOutput("Executing in Rust sandbox...");

    // Placeholder for gRPC call to Rust sandbox
    try {
      // const result = await grpcClient.executeCode({ code: code() });
      // setOutput(result.stdout);
      setTimeout(() => {
        setOutput("Hello, Golden Stack!\n\n[Demo: gRPC call to Rust sandbox]");
        setIsExecuting(false);
        setIsAgentBusy(false);
      }, 1000);
    } catch (error) {
      setOutput(`Error: ${error}`);
      setIsExecuting(false);
      setIsAgentBusy(false);
    }
  };

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
            disabled={isExecuting() || isAgentBusy()}
          >
            {isExecuting() ? "Running..." : isAgentBusy() ? "Agent Busy" : "Run"}
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
          <h2 class="text-sm font-semibold mb-2">Output</h2>
          <pre class="flex-1 w-full p-4 font-mono text-sm border rounded bg-muted overflow-auto mb-4">
            {output() || "No output yet"}
          </pre>
          
          <AgentEventList />
        </div>
      </div>
    </div>
  );
};

export default IDE;
