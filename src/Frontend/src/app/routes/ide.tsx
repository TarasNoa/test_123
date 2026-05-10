import { createSignal, onMount, onCleanup } from 'solid-js';
import * as monaco from 'monaco-editor';
import { AgentPanel } from '../../widgets/AgentPanel';
import { AIActivityFeed } from '../../widgets/AIActivityFeed';
import { ExecutionGraph } from '../../widgets/ExecutionGraph';
import { IDETabs } from '../../widgets/IDETabs';
import { MultiAgentVisualization } from '../../widgets/MultiAgentVisualization';
import { WorkspaceSidebar } from '../../widgets/WorkspaceSidebar';
import { WorkspaceTimeline } from '../../widgets/WorkspaceTimeline';
import { apiClient } from '../../lib/api-client';

export default function IDE() {
  const [activeTab, setActiveTab] = createSignal('code');
  const [agents, setAgents] = createSignal([]);
  const [activities, setActivities] = createSignal([]);
  const [editor, setEditor] = createSignal<monaco.editor.IStandaloneCodeEditor | null>(null);
  const [generatedCode, setGeneratedCode] = createSignal('');
  const [explanation, setExplanation] = createSignal('');

  let editorContainer: HTMLDivElement | undefined;

  onMount(async () => {
    // Initialize Monaco Editor
    if (editorContainer) {
      const monacoEditor = monaco.editor.create(editorContainer, {
        value: '// Welcome to Libr4 IDE\nconsole.log("Hello, World!");',
        language: 'typescript',
        theme: 'vs-dark',
        automaticLayout: true,
      });
      setEditor(monacoEditor);
    }

    // Load agents
    try {
      const response = await fetch('/api/ai/agents');
      const data = await response.json();
      setAgents(data.agents || []);
    } catch (error) {
      console.error('Failed to load agents:', error);
    }

    // Initialize activities
    setActivities([
      { id: 'activity-1', type: 'code_generation', message: 'Generated React component', timestamp: new Date() },
      { id: 'activity-2', type: 'task_recommendation', message: 'Recommended 3 tasks', timestamp: new Date() },
    ]);
  });

  onCleanup(() => {
    editor()?.dispose();
  });

  const handleGenerateCode = async () => {
    const prompt = editor()?.getValue() || '';
    if (!prompt.trim()) return;

    try {
      const result = await apiClient.generateCode({ prompt });
      setGeneratedCode(result.generatedCode);
      // Insert generated code into editor
      editor()?.setValue(result.generatedCode);
    } catch (error) {
      console.error('Failed to generate code:', error);
    }
  };

  const handleExplainCode = async () => {
    const code = editor()?.getValue() || '';
    if (!code.trim()) return;

    try {
      const result = await apiClient.explainCode({ code });
      setExplanation(result.explanation);
    } catch (error) {
      console.error('Failed to explain code:', error);
    }
  };

  return (
    <div class="ide-page">
      <WorkspaceSidebar />
      <div class="ide-main">
        <IDETabs activeTab={activeTab()} onTabChange={setActiveTab} />
        <div class="ide-toolbar">
          <button onClick={handleGenerateCode}>Generate Code</button>
          <button onClick={handleExplainCode}>Explain Code</button>
        </div>
        <div class="ide-content">
          {activeTab() === 'code' && (
            <div class="code-editor-container">
              <div ref={editorContainer} class="monaco-editor" />
              {explanation() && (
                <div class="code-explanation">
                  <h3>Code Explanation</h3>
                  <p>{explanation()}</p>
                </div>
              )}
            </div>
          )}
          {activeTab() === 'agents' && <AgentPanel agents={agents()} />}
          {activeTab() === 'execution' && <ExecutionGraph />}
          {activeTab() === 'multi-agent' && <MultiAgentVisualization />}
        </div>
        <AIActivityFeed activities={activities()} />
        <WorkspaceTimeline />
      </div>
    </div>
  );
}
