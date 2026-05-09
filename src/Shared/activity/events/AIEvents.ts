/**
 * AI Events
 * 
 * AI agent and generation events for AI orchestration and intelligence.
 * Separated from business logic to maintain clean event architecture.
 */

export type AIEvent =
  | AgentStarted
  | AgentThinking
  | AgentCompleted
  | AgentError
  | AgentPaused
  | AgentResumed
  | GenerationStarted
  | GenerationProgress
  | GenerationCompleted
  | GenerationFailed
  | SuggestionGenerated
  | AnalysisStarted
  | AnalysisCompleted
  | ContextMemoryUpdated;

export interface AgentStarted {
  type: "AgentStarted";
  timestamp: Date;
  agentId: string;
  agentName: string;
  taskId: string;
  taskType: string;
}

export interface AgentThinking {
  type: "AgentThinking";
  timestamp: Date;
  agentId: string;
  thought: string;
  confidence?: number;
}

export interface AgentCompleted {
  type: "AgentCompleted";
  timestamp: Date;
  agentId: string;
  taskId: string;
  result: string;
  duration: number;
  success: boolean;
}

export interface AgentError {
  type: "AgentError";
  timestamp: Date;
  agentId: string;
  taskId: string;
  error: string;
  errorType: "timeout" | "api_error" | "validation" | "unknown";
}

export interface AgentPaused {
  type: "AgentPaused";
  timestamp: Date;
  agentId: string;
  reason?: string;
}

export interface AgentResumed {
  type: "AgentResumed";
  timestamp: Date;
  agentId: string;
}

export interface GenerationStarted {
  type: "GenerationStarted";
  timestamp: Date;
  agentId: string;
  generationType: "code" | "test" | "docs" | "architecture" | "refactor";
  context: string;
  targetFile?: string;
}

export interface GenerationProgress {
  type: "GenerationProgress";
  timestamp: Date;
  agentId: string;
  progress: number;
  currentStep: string;
  estimatedRemaining?: number;
}

export interface GenerationCompleted {
  type: "GenerationCompleted";
  timestamp: Date;
  agentId: string;
  result: string;
  filesCreated: string[];
  filesModified: string[];
  duration: number;
}

export interface GenerationFailed {
  type: "GenerationFailed";
  timestamp: Date;
  agentId: string;
  error: string;
  partialResult?: string;
}

export interface SuggestionGenerated {
  type: "SuggestionGenerated";
  timestamp: Date;
  agentId: string;
  suggestion: string;
  context: string;
  confidence: number;
  category: "refactor" | "optimize" | "security" | "performance";
}

export interface AnalysisStarted {
  type: "AnalysisStarted";
  timestamp: Date;
  analysisType: "code" | "architecture" | "security" | "performance" | "complexity";
  target: string;
  scope: "file" | "directory" | "project";
}

export interface AnalysisCompleted {
  type: "AnalysisCompleted";
  timestamp: Date;
  analysisType: string;
  target: string;
  findings: Array<{
    severity: "low" | "medium" | "high" | "critical";
    message: string;
    location?: string;
    suggestion?: string;
  }>;
  duration: number;
}

export interface ContextMemoryUpdated {
  type: "ContextMemoryUpdated";
  timestamp: Date;
  contextType: "workspace" | "project" | "agent" | "user" | "task";
  contextId: string;
  updateType: "created" | "updated" | "deleted";
  data: Record<string, unknown>;
}
