/**
 * AI Context Memory Service
 * 
 * Manages AI context memory for different domains:
 * - Workspace Context
 * - Project Context
 * - Agent Context
 * - User Context
 * - Task Context
 * 
 * Service layer - business logic separated from components.
 */

export interface ContextMemory<T = Record<string, unknown>> {
  id: string;
  type: "workspace" | "project" | "agent" | "user" | "task";
  data: T;
  metadata: {
    createdAt: Date;
    updatedAt: Date;
    lastAccessed: Date;
    accessCount: number;
  };
}

export interface WorkspaceContext {
  currentView: string;
  activeProjects: string[];
  recentFiles: string[];
  activeAgents: string[];
  buildStatus: "success" | "failed" | "pending" | "building";
  deploymentStatus: "deployed" | "failed" | "pending" | "deploying";
}

export interface ProjectContext {
  projectId: string;
  projectName: string;
  projectType: string;
  techStack: string[];
  lastBuild: Date;
  lastDeployment: Date;
  openFiles: string[];
  activeTasks: string[];
  teamMembers: string[];
}

export interface AgentContext {
  agentId: string;
  agentName: string;
  agentType: string;
  capabilities: string[];
  currentTask?: string;
  taskHistory: Array<{
    taskId: string;
    completedAt: Date;
    result: string;
  }>;
  performance: {
    totalTasks: number;
    successRate: number;
    avgDuration: number;
  };
}

export interface UserContext {
  userId: string;
  userName: string;
  preferences: {
    theme: "dark" | "light";
    editor: string;
    shortcuts: Record<string, string>;
  };
  recentActions: Array<{
    action: string;
    timestamp: Date;
    context: string;
  }>;
  skills: string[];
  expertise: string[];
}

export interface TaskContext {
  taskId: string;
  taskName: string;
  taskType: string;
  status: "pending" | "in_progress" | "completed" | "failed";
  assignedAgent?: string;
  dependencies: string[];
  relatedFiles: string[];
  relatedTasks: string[];
  history: Array<{
    action: string;
    timestamp: Date;
    actor: string;
  }>;
}

/**
 * Context Memory Store
 */
class ContextMemoryStore {
  private memories: Map<string, ContextMemory> = new Map();
  private maxMemories = 1000;

  /**
   * Create or update context memory
   */
  set(contextType: ContextMemory["type"], contextId: string, data: Record<string, unknown>): void {
    const id = `${contextType}:${contextId}`;
    const existing = this.memories.get(id);

    if (existing) {
      existing.data = { ...existing.data, ...data };
      existing.metadata.updatedAt = new Date();
      existing.metadata.lastAccessed = new Date();
      existing.metadata.accessCount++;
    } else {
      this.memories.set(id, {
        id,
        type: contextType,
        data,
        metadata: {
          createdAt: new Date(),
          updatedAt: new Date(),
          lastAccessed: new Date(),
          accessCount: 1,
        },
      });

      // Enforce max memories limit
      if (this.memories.size > this.maxMemories) {
        this.evictLeastRecentlyUsed();
      }
    }
  }

  /**
   * Get context memory
   */
  get(contextType: ContextMemory["type"], contextId: string): ContextMemory | undefined {
    const id = `${contextType}:${contextId}`;
    const memory = this.memories.get(id);

    if (memory) {
      memory.metadata.lastAccessed = new Date();
      memory.metadata.accessCount++;
    }

    return memory;
  }

  /**
   * Get all memories of a specific type
   */
  getByType(contextType: ContextMemory["type"]): ContextMemory[] {
    return Array.from(this.memories.values())
      .filter(memory => memory.type === contextType)
      .sort((a, b) => b.metadata.lastAccessed.getTime() - a.metadata.lastAccessed.getTime());
  }

  /**
   * Delete context memory
   */
  delete(contextType: ContextMemory["type"], contextId: string): void {
    const id = `${contextType}:${contextId}`;
    this.memories.delete(id);
  }

  /**
   * Clear all memories of a specific type
   */
  clearType(contextType: ContextMemory["type"]): void {
    for (const [id, memory] of this.memories.entries()) {
      if (memory.type === contextType) {
        this.memories.delete(id);
      }
    }
  }

  /**
   * Clear all memories
   */
  clearAll(): void {
    this.memories.clear();
  }

  /**
   * Evict least recently used memories
   */
  private evictLeastRecentlyUsed(): void {
    const sorted = Array.from(this.memories.entries())
      .sort((a, b) => a[1].metadata.lastAccessed.getTime() - b[1].metadata.lastAccessed.getTime());

    const toEvict = sorted.slice(0, Math.floor(sorted.length * 0.1));
    toEvict.forEach(([id]) => this.memories.delete(id));
  }

  /**
   * Get memory statistics
   */
  getStats(): {
    total: number;
    byType: Record<string, number>;
    oldest: Date | null;
    newest: Date | null;
  } {
    const memories = Array.from(this.memories.values());
    const byType: Record<string, number> = {};

    memories.forEach(memory => {
      byType[memory.type] = (byType[memory.type] || 0) + 1;
    });

    const timestamps = memories.map(m => m.metadata.createdAt);
    const oldest = timestamps.length > 0 ? new Date(Math.min(...timestamps.map(t => t.getTime()))) : null;
    const newest = timestamps.length > 0 ? new Date(Math.max(...timestamps.map(t => t.getTime()))) : null;

    return {
      total: this.memories.size,
      byType,
      oldest,
      newest,
    };
  }
}

// Global context memory store instance
export const contextMemoryStore = new ContextMemoryStore();

/**
 * Context Memory Helpers
 */

export function setWorkspaceContext(contextId: string, data: Partial<WorkspaceContext>): void {
  contextMemoryStore.set("workspace", contextId, data);
}

export function getWorkspaceContext(contextId: string): ContextMemory<WorkspaceContext> | undefined {
  const memory = contextMemoryStore.get("workspace", contextId);
  if (!memory) return undefined;
  return memory as unknown as ContextMemory<WorkspaceContext>;
}

export function setProjectContext(projectId: string, data: Partial<ProjectContext>): void {
  contextMemoryStore.set("project", projectId, data);
}

export function getProjectContext(projectId: string): ContextMemory<ProjectContext> | undefined {
  const memory = contextMemoryStore.get("project", projectId);
  if (!memory) return undefined;
  return memory as unknown as ContextMemory<ProjectContext>;
}

export function setAgentContext(agentId: string, data: Partial<AgentContext>): void {
  contextMemoryStore.set("agent", agentId, data);
}

export function getAgentContext(agentId: string): ContextMemory<AgentContext> | undefined {
  const memory = contextMemoryStore.get("agent", agentId);
  if (!memory) return undefined;
  return memory as unknown as ContextMemory<AgentContext>;
}

export function setUserContext(userId: string, data: Partial<UserContext>): void {
  contextMemoryStore.set("user", userId, data);
}

export function getUserContext(userId: string): ContextMemory<UserContext> | undefined {
  const memory = contextMemoryStore.get("user", userId);
  if (!memory) return undefined;
  return memory as unknown as ContextMemory<UserContext>;
}

export function setTaskContext(taskId: string, data: Partial<TaskContext>): void {
  contextMemoryStore.set("task", taskId, data);
}

export function getTaskContext(taskId: string): ContextMemory<TaskContext> | undefined {
  const memory = contextMemoryStore.get("task", taskId);
  if (!memory) return undefined;
  return memory as unknown as ContextMemory<TaskContext>;
}
