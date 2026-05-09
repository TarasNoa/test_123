/**
 * Unified Workspace Event System
 * 
 * Central event types for all workspace activities.
 * Everything streams to UI through this unified system.
 */

export type WorkspaceEvent =
  | AgentStarted
  | AgentThinking
  | AgentCompleted
  | AgentError
  | GenerationStarted
  | GenerationProgress
  | GenerationCompleted
  | GenerationFailed
  | TaskAssigned
  | TaskStarted
  | TaskCompleted
  | TaskFailed
  | FileModified
  | FileCreated
  | FileDeleted
  | BuildStarted
  | BuildProgress
  | BuildCompleted
  | BuildFailed
  | DeploymentStarted
  | DeploymentProgress
  | DeploymentCompleted
  | DeploymentFailed
  | TestStarted
  | TestProgress
  | TestCompleted
  | TestFailed
  | AnalysisStarted
  | AnalysisCompleted
  | SuggestionGenerated
  | ExecutionGraphUpdated
  | WorkspaceSnapshotCreated
  | CollaborationEvent;

export interface AgentStarted {
  type: "AgentStarted";
  agentId: string;
  agentName: string;
  taskId: string;
  timestamp: Date;
}

export interface AgentThinking {
  type: "AgentThinking";
  agentId: string;
  thought: string;
  timestamp: Date;
}

export interface AgentCompleted {
  type: "AgentCompleted";
  agentId: string;
  taskId: string;
  result: string;
  duration: number;
  timestamp: Date;
}

export interface AgentError {
  type: "AgentError";
  agentId: string;
  taskId: string;
  error: string;
  timestamp: Date;
}

export interface GenerationStarted {
  type: "GenerationStarted";
  agentId: string;
  generationType: "code" | "test" | "docs" | "architecture";
  context: string;
  timestamp: Date;
}

export interface GenerationProgress {
  type: "GenerationProgress";
  agentId: string;
  progress: number;
  currentStep: string;
  timestamp: Date;
}

export interface GenerationCompleted {
  type: "GenerationCompleted";
  agentId: string;
  result: string;
  filesCreated: string[];
  duration: number;
  timestamp: Date;
}

export interface GenerationFailed {
  type: "GenerationFailed";
  agentId: string;
  error: string;
  timestamp: Date;
}

export interface TaskAssigned {
  type: "TaskAssigned";
  taskId: string;
  agentId: string;
  description: string;
  priority: "low" | "medium" | "high";
  timestamp: Date;
}

export interface TaskStarted {
  type: "TaskStarted";
  taskId: string;
  agentId: string;
  timestamp: Date;
}

export interface TaskCompleted {
  type: "TaskCompleted";
  taskId: string;
  agentId: string;
  result: string;
  duration: number;
  timestamp: Date;
}

export interface TaskFailed {
  type: "TaskFailed";
  taskId: string;
  agentId: string;
  error: string;
  timestamp: Date;
}

export interface FileModified {
  type: "FileModified";
  filePath: string;
  changes: string;
  userId?: string;
  timestamp: Date;
}

export interface FileCreated {
  type: "FileCreated";
  filePath: string;
  content: string;
  userId?: string;
  timestamp: Date;
}

export interface FileDeleted {
  type: "FileDeleted";
  filePath: string;
  userId?: string;
  timestamp: Date;
}

export interface BuildStarted {
  type: "BuildStarted";
  projectId: string;
  buildType: "dev" | "prod" | "test";
  timestamp: Date;
}

export interface BuildProgress {
  type: "BuildProgress";
  projectId: string;
  step: string;
  progress: number;
  timestamp: Date;
}

export interface BuildCompleted {
  type: "BuildCompleted";
  projectId: string;
  duration: number;
  output: string;
  timestamp: Date;
}

export interface BuildFailed {
  type: "BuildFailed";
  projectId: string;
  error: string;
  timestamp: Date;
}

export interface DeploymentStarted {
  type: "DeploymentStarted";
  projectId: string;
  environment: "staging" | "production";
  timestamp: Date;
}

export interface DeploymentProgress {
  type: "DeploymentProgress";
  projectId: string;
  step: string;
  progress: number;
  timestamp: Date;
}

export interface DeploymentCompleted {
  type: "DeploymentCompleted";
  projectId: string;
  environment: string;
  url: string;
  duration: number;
  timestamp: Date;
}

export interface DeploymentFailed {
  type: "DeploymentFailed";
  projectId: string;
  error: string;
  timestamp: Date;
}

export interface TestStarted {
  type: "TestStarted";
  testSuite: string;
  timestamp: Date;
}

export interface TestProgress {
  type: "TestProgress";
  testSuite: string;
  testsRun: number;
  testsPassed: number;
  testsFailed: number;
  timestamp: Date;
}

export interface TestCompleted {
  type: "TestCompleted";
  testSuite: string;
  total: number;
  passed: number;
  failed: number;
  duration: number;
  timestamp: Date;
}

export interface TestFailed {
  type: "TestFailed";
  testSuite: string;
  testName: string;
  error: string;
  timestamp: Date;
}

export interface AnalysisStarted {
  type: "AnalysisStarted";
  analysisType: "code" | "architecture" | "security" | "performance";
  target: string;
  timestamp: Date;
}

export interface AnalysisCompleted {
  type: "AnalysisCompleted";
  analysisType: string;
  findings: string[];
  timestamp: Date;
}

export interface SuggestionGenerated {
  type: "SuggestionGenerated";
  agentId: string;
  suggestion: string;
  context: string;
  confidence: number;
  timestamp: Date;
}

export interface ExecutionGraphUpdated {
  type: "ExecutionGraphUpdated";
  taskId: string;
  nodes: Array<{
    id: string;
    label: string;
    status: "pending" | "running" | "completed" | "error";
  }>;
  edges: Array<{ from: string; to: string }>;
  timestamp: Date;
}

export interface WorkspaceSnapshotCreated {
  type: "WorkspaceSnapshotCreated";
  snapshotId: string;
  description: string;
  timestamp: Date;
}

export interface CollaborationEvent {
  type: "CollaborationEvent";
  eventType: "user_joined" | "user_left" | "cursor_moved" | "selection_changed";
  userId: string;
  userName: string;
  data?: any;
  timestamp: Date;
}

/**
 * Event Stream
 * 
 * Central event bus for streaming events to UI
 */
export class EventStream {
  private listeners: Map<string, Set<(event: WorkspaceEvent) => void>> = new Map();
  private eventHistory: WorkspaceEvent[] = [];
  private maxHistorySize = 1000;

  subscribe(eventType: string, callback: (event: WorkspaceEvent) => void): () => void {
    if (!this.listeners.has(eventType)) {
      this.listeners.set(eventType, new Set());
    }
    this.listeners.get(eventType)!.add(callback);

    // Return unsubscribe function
    return () => {
      this.listeners.get(eventType)?.delete(callback);
    };
  }

  emit(event: WorkspaceEvent): void {
    // Add to history
    this.eventHistory.push(event);
    if (this.eventHistory.length > this.maxHistorySize) {
      this.eventHistory.shift();
    }

    // Notify listeners
    const listeners = this.listeners.get(event.type);
    if (listeners) {
      listeners.forEach(callback => callback(event));
    }

    // Also notify wildcard listeners
    const wildcardListeners = this.listeners.get("*");
    if (wildcardListeners) {
      wildcardListeners.forEach(callback => callback(event));
    }
  }

  getHistory(limit?: number): WorkspaceEvent[] {
    return limit ? this.eventHistory.slice(-limit) : this.eventHistory;
  }

  clearHistory(): void {
    this.eventHistory = [];
  }
}

// Global event stream instance
export const globalEventStream = new EventStream();
