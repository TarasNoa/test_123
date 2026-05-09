/**
 * Event Ownership Map
 * 
 * Controls event explosion by defining strict ownership:
 * - AI events: ONLY AI domain owns them
 * - Workspace events: ONLY workspace domain owns them
 * - Graph events: ONLY graph domain owns them
 * - Collaboration events: ONLY collaboration domain owns them
 * - System events: ONLY system domain owns them
 * 
 * Prevents cascading chaos from agents, AI, graph, workspace, collaboration
 */

export type EventDomain = "system" | "ui" | "ai" | "workspace" | "collaboration";

export interface EventOwnershipRule {
  domain: EventDomain;
  owns: string[]; // Event types this domain owns
  canEmit: string[]; // Event types this domain can emit
  canSubscribe: string[]; // Event types this domain can subscribe to
}

export const EVENT_OWNERSHIP_MAP: Record<EventDomain, EventOwnershipRule> = {
  system: {
    domain: "system",
    owns: ["SystemStarted", "SystemReady", "SystemError", "SystemShutdown", "SystemConfigChanged"],
    canEmit: ["SystemStarted", "SystemReady", "SystemError", "SystemShutdown", "SystemConfigChanged"],
    canSubscribe: [], // System doesn't subscribe to events
  },
  ui: {
    domain: "ui",
    owns: ["ButtonClicked", "ModalOpened", "ModalClosed", "TabSwitched", "SidebarToggled", "PanelResized", "CommandExecuted", "FocusChanged"],
    canEmit: ["ButtonClicked", "ModalOpened", "ModalClosed", "TabSwitched", "SidebarToggled", "PanelResized", "CommandExecuted", "FocusChanged"],
    canSubscribe: ["SystemStarted", "SystemReady", "SystemError", "AgentStarted", "AgentCompleted", "TaskAssigned", "BuildStarted", "BuildCompleted", "BuildFailed"],
  },
  ai: {
    domain: "ai",
    owns: ["AgentStarted", "AgentThinking", "AgentCompleted", "AgentFailed", "GenerationProgress", "AnalysisCompleted", "ContextMemoryUpdated", "ReasoningStep", "SuggestionGenerated"],
    canEmit: ["AgentStarted", "AgentThinking", "AgentCompleted", "AgentFailed", "GenerationProgress", "AnalysisCompleted", "ContextMemoryUpdated", "ReasoningStep", "SuggestionGenerated"],
    canSubscribe: ["SystemStarted", "SystemReady", "TaskAssigned", "FileModified", "BuildFailed", "DeploymentCompleted"],
  },
  workspace: {
    domain: "workspace",
    owns: ["FileModified", "FileCreated", "FileDeleted", "TaskAssigned", "TaskCompleted", "TaskFailed", "BuildStarted", "BuildCompleted", "BuildFailed", "DeploymentStarted", "DeploymentCompleted", "DeploymentFailed", "TestStarted", "TestCompleted", "TestFailed"],
    canEmit: ["FileModified", "FileCreated", "FileDeleted", "TaskAssigned", "TaskCompleted", "TaskFailed", "BuildStarted", "BuildCompleted", "BuildFailed", "DeploymentStarted", "DeploymentCompleted", "DeploymentFailed", "TestStarted", "TestCompleted", "TestFailed"],
    canSubscribe: ["SystemStarted", "SystemReady", "AgentCompleted", "SuggestionGenerated", "UserJoined", "CursorMoved"],
  },
  collaboration: {
    domain: "collaboration",
    owns: ["UserJoined", "UserLeft", "CursorMoved", "SelectionChanged", "CommentAdded", "CommentResolved", "ConflictDetected", "ConflictResolved"],
    canEmit: ["UserJoined", "UserLeft", "CursorMoved", "SelectionChanged", "CommentAdded", "CommentResolved", "ConflictDetected", "ConflictResolved"],
    canSubscribe: ["SystemStarted", "SystemReady", "FileModified", "TaskAssigned"],
  },
};

/**
 * Validate event ownership
 */
export function canEmitEvent(domain: EventDomain, eventType: string): boolean {
  const rule = EVENT_OWNERSHIP_MAP[domain];
  return rule.canEmit.includes(eventType);
}

/**
 * Validate event subscription
 */
export function canSubscribeToEvent(domain: EventDomain, eventType: string): boolean {
  const rule = EVENT_OWNERSHIP_MAP[domain];
  return rule.canSubscribe.includes(eventType);
}

/**
 * Get event owner
 */
export function getEventOwner(eventType: string): EventDomain | null {
  for (const [domain, rule] of Object.entries(EVENT_OWNERSHIP_MAP)) {
    if (rule.owns.includes(eventType)) {
      return domain as EventDomain;
    }
  }
  return null;
}

/**
 * Validate event transition (can event A trigger event B?)
 */
export function canEventTrigger(sourceEvent: string, targetEvent: string): boolean {
  const sourceOwner = getEventOwner(sourceEvent);
  const targetOwner = getEventOwner(targetEvent);

  if (!sourceOwner || !targetOwner) {
    return false;
  }

  // Same domain can trigger its own events
  if (sourceOwner === targetOwner) {
    return true;
  }

  // Specific cross-domain triggers (whitelist approach)
  const allowedTransitions: Record<string, string[]> = {
    // AI can trigger workspace events
    "ai": ["workspace"],
    // Workspace can trigger AI events
    "workspace": ["ai"],
    // System can trigger anything
    "system": ["ui", "ai", "workspace", "collaboration"],
  };

  return allowedTransitions[sourceOwner]?.includes(targetOwner) ?? false;
}

/**
 * Event ownership violation checker
 */
export class EventOwnershipChecker {
  private violations: string[] = [];

  /**
   * Check if domain can emit event
   */
  checkEmit(domain: EventDomain, eventType: string): boolean {
    if (!canEmitEvent(domain, eventType)) {
      this.violations.push(`Domain ${domain} cannot emit event ${eventType}`);
      return false;
    }
    return true;
  }

  /**
   * Check if domain can subscribe to event
   */
  checkSubscribe(domain: EventDomain, eventType: string): boolean {
    if (!canSubscribeToEvent(domain, eventType)) {
      this.violations.push(`Domain ${domain} cannot subscribe to event ${eventType}`);
      return false;
    }
    return true;
  }

  /**
   * Check if event can trigger another event
   */
  checkTrigger(sourceEvent: string, targetEvent: string): boolean {
    if (!canEventTrigger(sourceEvent, targetEvent)) {
      this.violations.push(`Event ${sourceEvent} cannot trigger event ${targetEvent}`);
      return false;
    }
    return true;
  }

  /**
   * Get all violations
   */
  getViolations(): string[] {
    return this.violations;
  }

  /**
   * Clear violations
   */
  clearViolations(): void {
    this.violations = [];
  }

  /**
   * Check if has violations
   */
  hasViolations(): boolean {
    return this.violations.length > 0;
  }
}

/**
 * Event ownership helpers
 */
export const eventOwnershipChecker = new EventOwnershipChecker();

export function validateEventEmit(domain: EventDomain, eventType: string): boolean {
  return eventOwnershipChecker.checkEmit(domain, eventType);
}

export function validateEventSubscribe(domain: EventDomain, eventType: string): boolean {
  return eventOwnershipChecker.checkSubscribe(domain, eventType);
}

export function validateEventTrigger(sourceEvent: string, targetEvent: string): boolean {
  return eventOwnershipChecker.checkTrigger(sourceEvent, targetEvent);
}

export function getEventOwnershipViolations(): string[] {
  return eventOwnershipChecker.getViolations();
}

export function clearEventOwnershipViolations(): void {
  eventOwnershipChecker.clearViolations();
}
