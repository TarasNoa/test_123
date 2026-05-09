/**
 * Workspace Intelligence Layer
 * 
 * Context-aware AI suggestions for:
 * - Build failures
 * - Freelancer idle
 * - Deadline risks
 * 
 * Service layer - business logic separated from components.
 */

import type { AIEvent } from "../../Shared/activity/events/AIEvents";
import type { WorkspaceEvent } from "../../Shared/activity/events/WorkspaceEvents";
import { globalEventStream } from "../../Shared/activity/WorkspaceEvent";

export interface IntelligenceSuggestion {
  id: string;
  type: "build_failure" | "freelancer_idle" | "deadline_risk" | "performance_issue" | "security_issue";
  severity: "low" | "medium" | "high" | "critical";
  title: string;
  description: string;
  suggestedActions: Array<{
    id: string;
    label: string;
    action: () => Promise<void> | void;
  }>;
  context: Record<string, unknown>;
  timestamp: Date;
}

export interface IntelligenceContext {
  buildStatus: "success" | "failed" | "pending" | "building";
  lastBuildError?: string;
  activeFreelancers: Array<{
    id: string;
    name: string;
    status: "idle" | "busy";
    skills: string[];
  }>;
  projectDeadlines: Array<{
    projectId: string;
    deadline: Date;
    completion: number;
    status: "on_track" | "at_risk" | "overdue";
  }>;
}

/**
 * Intelligence Engine
 */
class WorkspaceIntelligenceEngine {
  private suggestions: IntelligenceSuggestion[] = [];
  private context: IntelligenceContext = {
    buildStatus: "pending",
    activeFreelancers: [],
    projectDeadlines: [],
  };

  constructor() {
    this.subscribeToEvents();
  }

  /**
   * Subscribe to workspace events
   */
  private subscribeToEvents(): void {
    // Listen for build events
    globalEventStream.subscribe("BuildFailed", (event: WorkspaceEvent) => {
      if (event.type === "BuildFailed") {
        this.handleBuildFailure(event);
      }
    });

    // Listen for agent events
    globalEventStream.subscribe("AgentCompleted", (event: AIEvent) => {
      if (event.type === "AgentCompleted") {
        this.handleAgentCompletion(event);
      }
    });
  }

  /**
   * Handle build failure
   */
  private handleBuildFailure(event: WorkspaceEvent & { type: "BuildFailed" }): void {
    this.context.buildStatus = "failed";
    this.context.lastBuildError = event.error;

    const suggestion: IntelligenceSuggestion = {
      id: `build-failure-${Date.now()}`,
      type: "build_failure",
      severity: "high",
      title: "Build Failed",
      description: `Build failed in step: ${event.step}. Error: ${event.error}`,
      suggestedActions: [
        {
          id: "analyze-error",
          label: "Analyze error",
          action: async () => {
            console.log("Analyzing build error:", event.error);
            // AI error analysis logic
          },
        },
        {
          id: "suggest-fix",
          label: "Suggest fix",
          action: async () => {
            console.log("Suggesting fix for:", event.error);
            // AI fix suggestion logic
          },
        },
        {
          id: "assign-agent",
          label: "Assign agent to fix",
          action: async () => {
            console.log("Assigning agent to fix build");
            // Agent assignment logic
          },
        },
      ],
      context: {
        projectId: event.projectId,
        error: event.error,
        step: event.step,
        exitCode: event.exitCode,
      },
      timestamp: new Date(),
    };

    this.addSuggestion(suggestion);
  }

  /**
   * Handle agent completion
   */
  private handleAgentCompletion(event: AIEvent & { type: "AgentCompleted" }): void {
    if (!event.success) {
      this.handleAgentError(event);
    }
  }

  /**
   * Handle agent error
   */
  private handleAgentError(event: AIEvent & { type: "AgentError" | "AgentCompleted" }): void {
    const suggestion: IntelligenceSuggestion = {
      id: `agent-error-${Date.now()}`,
      type: "performance_issue",
      severity: "medium",
      title: "Agent Error",
      description: `Agent ${event.agentId} encountered an error`,
      suggestedActions: [
        {
          id: "retry-task",
          label: "Retry task",
          action: async () => {
            console.log("Retrying task for agent:", event.agentId);
          },
        },
        {
          id: "reassign-agent",
          label: "Reassign to different agent",
          action: async () => {
            console.log("Reassigning task from agent:", event.agentId);
          },
        },
      ],
      context: {
        agentId: event.agentId,
        taskId: event.taskId,
      },
      timestamp: new Date(),
    };

    this.addSuggestion(suggestion);
  }

  /**
   * Check for idle freelancers
   */
  checkIdleFreelancers(): void {
    const idleFreelancers = this.context.activeFreelancers.filter(f => f.status === "idle");

    idleFreelancers.forEach(freelancer => {
      const suggestion: IntelligenceSuggestion = {
        id: `freelancer-idle-${freelancer.id}-${Date.now()}`,
        type: "freelancer_idle",
        severity: "low",
        title: `${freelancer.name} is idle`,
        description: "Freelancer has no active tasks",
        suggestedActions: [
          {
            id: "find-tasks",
            label: "Find suitable tasks",
            action: async () => {
              console.log("Finding tasks for freelancer:", freelancer.name);
              // AI task matching logic
            },
          },
          {
            id: "assign-project",
            label: "Assign to project",
            action: async () => {
              console.log("Assigning freelancer to project:", freelancer.name);
              // Project assignment logic
            },
          },
        ],
        context: {
          freelancerId: freelancer.id,
          freelancerName: freelancer.name,
          skills: freelancer.skills,
        },
        timestamp: new Date(),
      };

      this.addSuggestion(suggestion);
    });
  }

  /**
   * Check for deadline risks
   */
  checkDeadlineRisks(): void {
    const atRiskProjects = this.context.projectDeadlines.filter(
      p => p.status === "at_risk" || p.status === "overdue"
    );

    atRiskProjects.forEach(project => {
      const suggestion: IntelligenceSuggestion = {
        id: `deadline-risk-${project.projectId}-${Date.now()}`,
        type: "deadline_risk",
        severity: project.status === "overdue" ? "critical" : "high",
        title: `Project deadline ${project.status}`,
        description: `Project is ${project.status} (${project.completion}% complete)`,
        suggestedActions: [
          {
            id: "redistribute-tasks",
            label: "Redistribute tasks",
            action: async () => {
              console.log("Redistributing tasks for project:", project.projectId);
              // AI task redistribution logic
            },
          },
          {
            id: "add-resources",
            label: "Add more resources",
            action: async () => {
              console.log("Adding resources to project:", project.projectId);
              // Resource allocation logic
            },
          },
          {
            id: "adjust-scope",
            label: "Adjust project scope",
            action: async () => {
              console.log("Adjusting scope for project:", project.projectId);
              // Scope adjustment logic
            },
          },
        ],
        context: {
          projectId: project.projectId,
          deadline: project.deadline,
          completion: project.completion,
          status: project.status,
        },
        timestamp: new Date(),
      };

      this.addSuggestion(suggestion);
    });
  }

  /**
   * Add suggestion
   */
  private addSuggestion(suggestion: IntelligenceSuggestion): void {
    this.suggestions.push(suggestion);
    
    // Limit to 50 suggestions
    if (this.suggestions.length > 50) {
      this.suggestions.shift();
    }
  }

  /**
   * Get active suggestions
   */
  getActiveSuggestions(): IntelligenceSuggestion[] {
    return this.suggestions.filter(s => {
      // Remove suggestions older than 1 hour
      const oneHourAgo = new Date(Date.now() - 60 * 60 * 1000);
      return s.timestamp > oneHourAgo;
    });
  }

  /**
   * Get suggestions by type
   */
  getSuggestionsByType(type: IntelligenceSuggestion["type"]): IntelligenceSuggestion[] {
    return this.getActiveSuggestions().filter(s => s.type === type);
  }

  /**
   * Get suggestions by severity
   */
  getSuggestionsBySeverity(severity: IntelligenceSuggestion["severity"]): IntelligenceSuggestion[] {
    return this.getActiveSuggestions().filter(s => s.severity === severity);
  }

  /**
   * Dismiss suggestion
   */
  dismissSuggestion(suggestionId: string): void {
    this.suggestions = this.suggestions.filter(s => s.id !== suggestionId);
  }

  /**
   * Update context
   */
  updateContext(context: Partial<IntelligenceContext>): void {
    this.context = { ...this.context, ...context };

    // Trigger checks
    if (context.activeFreelancers) {
      this.checkIdleFreelancers();
    }
    if (context.projectDeadlines) {
      this.checkDeadlineRisks();
    }
  }

  /**
   * Clear all suggestions
   */
  clearSuggestions(): void {
    this.suggestions = [];
  }
}

// Global intelligence engine instance
export const workspaceIntelligence = new WorkspaceIntelligenceEngine();

/**
 * Intelligence helpers
 */
export function updateBuildStatus(status: IntelligenceContext["buildStatus"], error?: string): void {
  workspaceIntelligence.updateContext({ buildStatus: status, lastBuildError: error });
}

export function updateFreelancers(freelancers: IntelligenceContext["activeFreelancers"]): void {
  workspaceIntelligence.updateContext({ activeFreelancers: freelancers });
}

export function updateProjectDeadlines(deadlines: IntelligenceContext["projectDeadlines"]): void {
  workspaceIntelligence.updateContext({ projectDeadlines: deadlines });
}

export function getIntelligenceSuggestions(): IntelligenceSuggestion[] {
  return workspaceIntelligence.getActiveSuggestions();
}

export function dismissIntelligenceSuggestion(suggestionId: string): void {
  workspaceIntelligence.dismissSuggestion(suggestionId);
}
