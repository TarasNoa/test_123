import { createSignal, onMount, For } from 'solid-js';
import { apiClient } from '../../lib/api-client';

export default function IDE() {
  const [activeTab, setActiveTab] = createSignal('code');
  const [agents, setAgents] = createSignal([]);
  const [code, setCode] = createSignal('// Welcome to Libr4 IDE\nconsole.log("Hello, World!");');
  const [explanation, setExplanation] = createSignal('');
  const [activities, setActivities] = createSignal([
    { id: 'activity-1', type: 'code_generation', message: 'Generated React component', timestamp: new Date() },
    { id: 'activity-2', type: 'task_recommendation', message: 'Recommended 3 tasks', timestamp: new Date() },
  ]);

  onMount(async () => {
    try {
      const response = await fetch('/api/v1/ai/agents');
      const data = await response.json();
      setAgents(data.agents || []);
    } catch (error) {
      console.error('Failed to load agents:', error);
    }
  });

  const handleGenerateCode = async () => {
    const prompt = code();
    if (!prompt.trim()) return;
    try {
      const result = await apiClient.generateCode({ prompt });
      setCode(result.generatedCode);
    } catch (error) {
      console.error('Failed to generate code:', error);
    }
  };

  const handleExplainCode = async () => {
    if (!code().trim()) return;
    try {
      const result = await apiClient.explainCode({ code: code() });
      setExplanation(result.explanation);
    } catch (error) {
      console.error('Failed to explain code:', error);
    }
  };

  const tabs = [
    { id: 'code', label: 'Code' },
    { id: 'agents', label: 'Agents' },
    { id: 'execution', label: 'Execution' },
  ];

  return (
    <div class="flex h-screen bg-background text-foreground">
      {/* Sidebar */}
      <aside class="w-64 bg-surface border-r border-surface-3 flex flex-col">
        <div class="p-4 border-b border-surface-3">
          <h1 class="text-lg font-bold text-primary">Libr4 IDE</h1>
        </div>
        <nav class="flex-1 p-2 space-y-1">
          {tabs.map((tab) => (
            <button
              onClick={() => setActiveTab(tab.id)}
              class={
                activeTab() === tab.id
                  ? 'w-full text-left px-3 py-2 rounded-md bg-surface-2 text-primary font-medium transition-colors'
                  : 'w-full text-left px-3 py-2 rounded-md text-muted-foreground hover:text-foreground hover:bg-surface-2/50 transition-colors'
              }
            >
              {tab.label}
            </button>
          ))}
        </nav>
        <div class="p-4 border-t border-surface-3">
          <div class="text-xs text-muted-foreground space-y-2">
            <p>AI Activity</p>
            <For each={activities()}>
              {(a) => (
                <div class="flex items-center gap-2">
                  <span class="w-1.5 h-1.5 rounded-full bg-primary" />
                  <span class="truncate">{a.message}</span>
                </div>
              )}
            </For>
          </div>
        </div>
      </aside>

      {/* Main */}
      <main class="flex-1 flex flex-col min-w-0">
        {/* Toolbar */}
        <div class="h-14 border-b border-surface-3 flex items-center px-4 gap-3">
          <button
            onClick={handleGenerateCode}
            class="px-4 py-2 bg-primary text-primary-foreground text-sm font-medium rounded-lg hover:bg-primary/90 active:scale-[0.98] transition-all"
          >
            Generate Code
          </button>
          <button
            onClick={handleExplainCode}
            class="px-4 py-2 bg-secondary text-secondary-foreground text-sm font-medium rounded-lg hover:bg-secondary/90 active:scale-[0.98] transition-all"
          >
            Explain Code
          </button>
        </div>

        {/* Content */}
        <div class="flex-1 p-4 overflow-auto">
          {activeTab() === 'code' && (
            <div class="space-y-4">
              <textarea
                value={code()}
                onInput={(e) => setCode(e.currentTarget.value)}
                class="w-full h-[60vh] p-4 bg-surface-2 border border-surface-3 rounded-lg text-foreground font-mono text-sm resize-none focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary transition-all"
                spellcheck={false}
              />
              {explanation() && (
                <div class="p-4 bg-surface-2 border border-surface-3 rounded-lg">
                  <h3 class="text-sm font-semibold text-primary mb-2">Explanation</h3>
                  <p class="text-sm text-muted-foreground">{explanation()}</p>
                </div>
              )}
            </div>
          )}

          {activeTab() === 'agents' && (
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {agents().length === 0 && (
                <p class="text-muted-foreground col-span-full">No agents loaded</p>
              )}
            </div>
          )}

          {activeTab() === 'execution' && (
            <div class="p-4 bg-surface-2 border border-surface-3 rounded-lg">
              <p class="text-muted-foreground">Execution graph will appear here</p>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}
