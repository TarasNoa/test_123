import { createStore } from 'solid-js/store';

export type AgentType =
  | 'CodeGenAgent'
  | 'TestAgent'
  | 'ReviewAgent'
  | 'FixAgent'
  | 'PlannerAgent'
  | 'ShadowBuildAgent'
  | 'SearchAgent'
  | 'ObserverAgent'
  | 'DocAgent'
  | 'SecurityAgent';

export interface FileNode {
  id: string;
  path: string;
  name: string;
  type: 'file' | 'folder';
  language?: string;
  children?: FileNode[];
  isOpen?: boolean;
  isDirty?: boolean;
  isAgentEditing?: boolean;
  agentId?: string;
}

export interface Tab {
  id: string;
  path: string;
  name: string;
  language: string;
  content: string;
  isDirty: boolean;
  isAgentEditing: boolean;
  originalContent?: string;
  proposedContent?: string;
}

export interface AgentState {
  id: string;
  type: AgentType;
  status: 'running' | 'completed' | 'failed' | 'waiting';
  task: string;
  currentFile?: string;
  progress: number;
  parentAgentId?: string;
  startedAt: Date;
}

export interface BuildError {
  file: string;
  line: number;
  column: number;
  message: string;
  severity: 'error' | 'warning';
}

export interface ArchitectPlanStep {
  number: number;
  description: string;
  agentType: AgentType;
  estimatedFiles: string[];
}

export interface ChatMessage {
  type:
    | 'user'
    | 'ai'
    | 'agent_spawned'
    | 'agent_thinking'
    | 'agent_file_edit'
    | 'agent_question'
    | 'agent_completed'
    | 'agent_failed'
    | 'shadow_build'
    | 'parallel_group'
    | 'agent_conflict'
    | 'architect_plan'
    | 'observer_insight';
  id: string;
  timestamp: Date;
  text?: string;
  attachedFiles?: string[];
  selectedCode?: { code: string; language: string; lines: string };
  isStreaming?: boolean;
  agentId?: string;
  agentType?: AgentType;
  task?: string;
  targetFiles?: string[];
  parentAgentId?: string;
  message?: string;
  isMemoryRetrieval?: boolean;
  path?: string;
  linesAdded?: number;
  linesRemoved?: number;
  preview?: string;
  status?: 'pending' | 'accepted' | 'rejected';
  question?: string;
  options?: string[];
  summary?: string;
  filesModified?: string[];
  whatWasDone?: string;
  whatWasNOTDone?: string;
  nextStep?: string;
  duration?: number;
  error?: string;
  canAutoFix?: boolean;
  canRetry?: boolean;
  buildStatus?: 'running' | 'success' | 'failed';
  buildDuration?: number;
  errors?: BuildError[];
  testsRun?: number;
  testsPassed?: number;
  agents?: Array<{
    agentId: string;
    agentType: AgentType;
    task: string;
    progress: number;
    currentFile?: string;
    status: 'running' | 'completed' | 'failed';
  }>;
  conflictFile?: string;
  conflictAgents?: string[];
  planId?: string;
  title?: string;
  steps?: ArchitectPlanStep[];
  pattern?: string;
  suggestion?: string;
  frequency?: number;
}

export interface IDEState {
  sessionId: string;
  isConnected: boolean;

  activeActivityTab: 'files' | 'search' | 'git';
  sidebarOpen: boolean;
  sidebarWidth: number;

  fileTree: FileNode[];
  openTabs: Tab[];
  activeTabId: string;

  cursorPosition: { line: number; column: number };

  bottomPanelOpen: boolean;
  bottomPanelHeight: number;
  bottomPanelTab: 'terminal' | 'output' | 'problems' | 'timeline' | 'ai-log' | 'subagents' | 'flow';
  activeGenerationRunId: string | null;
  subagents: Array<{
    id: string;
    runId: string;
    name: string;
    task: string;
    status: string;
    createdAtUtc: string;
    updatedAtUtc: string;
    outputPreview?: string | null;
    error?: string | null;
  }>;
  delegations: Array<{
    id: string;
    runId: string;
    task: string;
    status: string;
    createdAtUtc: string;
    updatedAtUtc: string;
    outputPreview?: string | null;
    error?: string | null;
  }>;
  backgroundFleet: {
    runningCount: number;
    queuedCount: number;
  } | null;
  flowProgress: {
    runId: string;
    flowName: string;
    currentNodeId?: string | null;
    status: string;
    nodes: Array<{ nodeId: string; status: string; attempts?: number; lastError?: string | null }>;
    updatedAtUtc: string;
  } | null;
  problems: BuildError[];
  outputLog: Array<{ level: string; text: string; timestamp: Date }>;
  timelineEvents: Array<{
    agentId: string;
    agentType: AgentType;
    task: string;
    start: Date;
    end?: Date;
    status: 'running' | 'completed' | 'failed';
  }>;
  aiLog: ChatMessage[];

  aiPanelOpen: boolean;
  aiPanelWidth: number;
  messages: ChatMessage[];
  isAIStreaming: boolean;
  streamingMessageId: string | null;
  autonomyLevel: 'supervised' | 'semi-auto' | 'full-auto';
  selectedModel: string;
  inputText: string;
  contextFiles: string[];
  contextSelectedCode: string | null;

  activeAgents: Record<string, AgentState>;
  agentHistory: AgentState[];

  lastBuildStatus: 'idle' | 'running' | 'success' | 'failed';
  lastBuildTime?: number;
  lastBuildErrors: BuildError[];

  diffTabId: string | null;
}

export const [store, setStore] = createStore<IDEState>({
  sessionId: '',
  isConnected: false,

  activeActivityTab: 'files',
  sidebarOpen: true,
  sidebarWidth: 240,

  fileTree: [],
  openTabs: [],
  activeTabId: '',

  cursorPosition: { line: 1, column: 1 },

  bottomPanelOpen: true,
  bottomPanelHeight: 200,
  bottomPanelTab: 'terminal',
  activeGenerationRunId: null,
  subagents: [],
  delegations: [],
  backgroundFleet: null,
  flowProgress: null,
  problems: [],
  outputLog: [],
  timelineEvents: [],
  aiLog: [],

  aiPanelOpen: true,
  aiPanelWidth: 360,
  messages: [],
  isAIStreaming: false,
  streamingMessageId: null,
  autonomyLevel: 'semi-auto',
  selectedModel: 'docker-model-runner',
  inputText: '',
  contextFiles: [],
  contextSelectedCode: null,

  activeAgents: {},
  agentHistory: [],

  lastBuildStatus: 'idle',
  lastBuildErrors: [],

  diffTabId: null,
});

export function addMessage(msg: ChatMessage) {
  setStore('messages', (m) => [...m, msg]);
  if (msg.type === 'shadow_build') {
    const status = msg.buildStatus ?? 'idle';
    setStore('lastBuildStatus', status);
    if (msg.buildDuration) setStore('lastBuildTime', msg.buildDuration);
    if (msg.errors) setStore('lastBuildErrors', msg.errors);
    if (status === 'failed' || status === 'success') {
      setStore('problems', msg.errors ?? []);
      addOutputLog(status === 'failed' ? 'error' : 'info', `Shadow build ${status}${msg.errors?.length ? ` — ${msg.errors.length} errors` : ''}`);
    }
  }
  if (msg.type === 'agent_file_edit' || msg.type === 'agent_completed' || msg.type === 'agent_failed') {
    setStore('aiLog', (l) => [...l, msg]);
  }
}

export function addOutputLog(level: string, text: string) {
  setStore('outputLog', (l) => [...l, { level, text, timestamp: new Date() }]);
}

export function updateAgentProgress(agentId: string, progress: number, currentFile?: string) {
  setStore('activeAgents', agentId, 'progress', progress);
  if (currentFile) {
    setStore('activeAgents', agentId, 'currentFile', currentFile);
  }
}

export function updateFileInEditor(path: string, content: string) {
  setStore('openTabs', (tabs) =>
    tabs.map((t) => (t.path === path ? { ...t, content, isDirty: true } : t))
  );
  setStore('fileTree', (tree) => markAgentEditing(tree, path, false));
}

export function markFileAgentEditing(path: string, agentId: string) {
  setStore('fileTree', (tree) => markAgentEditing(tree, path, true, agentId));
}

function markAgentEditing(
  nodes: FileNode[],
  path: string,
  editing: boolean,
  agentId?: string
): FileNode[] {
  return nodes.map((n) => {
    if (n.path === path) return { ...n, isAgentEditing: editing, agentId: editing ? agentId : undefined };
    if (n.children) return { ...n, children: markAgentEditing(n.children, path, editing, agentId) };
    return n;
  });
}

export function markFileDirty(path: string, dirty: boolean) {
  setStore('fileTree', (tree) => markDirty(tree, path, dirty));
}

function markDirty(nodes: FileNode[], path: string, dirty: boolean): FileNode[] {
  return nodes.map((n) => {
    if (n.path === path) return { ...n, isDirty: dirty };
    if (n.children) return { ...n, children: markDirty(n.children, path, dirty) };
    return n;
  });
}

export function addTab(tab: Tab) {
  setStore('openTabs', (tabs) => {
    if (tabs.some((t) => t.id === tab.id)) return tabs;
    return [...tabs, tab];
  });
  setStore('activeTabId', tab.id);
}

export function closeTab(tabId: string) {
  setStore('openTabs', (tabs) => {
    const remaining = tabs.filter((t) => t.id !== tabId);
    setStore('activeTabId', (id) => {
      if (id !== tabId) return id;
      return remaining.length > 0 ? remaining[remaining.length - 1].id : '';
    });
    return remaining;
  });
}

export function setTabDirty(tabId: string, dirty: boolean) {
  setStore('openTabs', (tabs) => tabs.map((t) => (t.id === tabId ? { ...t, isDirty: dirty } : t)));
}

export function addTimelineEvent(event: IDEState['timelineEvents'][number]) {
  setStore('timelineEvents', (e) => [...e, event]);
}

export function updateTimelineEvent(agentId: string, patch: Partial<IDEState['timelineEvents'][number]>) {
  setStore('timelineEvents', (events) =>
    events.map((e) => (e.agentId === agentId ? { ...e, ...patch } : e))
  );
}
