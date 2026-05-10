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

export type ContextType = "workspace" | "project" | "agent" | "user" | "task";

export interface WorkspaceContext {
  workspaceId: string;
  name?: string;
  description?: string;
  lastActiveTaskId?: string;
  metadata?: Record<string, unknown>;
}

export interface ProjectContext {
  projectId: string;
  title?: string;
  summary?: string;
  currentPhase?: string;
  teamMembers?: string[];
  metadata?: Record<string, unknown>;
}

export interface AgentContext {
  agentId: string;
  agentName?: string;
  role?: string;
  currentGoal?: string;
  status?: "idle" | "running" | "paused" | "error";
  metadata?: Record<string, unknown>;
}

export interface UserContext {
  userId: string;
  displayName?: string;
  email?: string;
  preferences?: Record<string, unknown>;
  recentActions?: string[];
  metadata?: Record<string, unknown>;
}

export interface TaskContext {
  taskId: string;
  title?: string;
  description?: string;
  status?: "open" | "in_progress" | "completed" | "blocked";
  requiredSkills?: string[];
  priority?: "low" | "medium" | "high";
  metadata?: Record<string, unknown>;
}

export interface ContextMemoryTypes {
  workspace: WorkspaceContext;
  project: ProjectContext;
  agent: AgentContext;
  user: UserContext;
  task: TaskContext;
}

export interface ContextMemory<T = Record<string, unknown>> {
  id: string;
  type: ContextType;
  data: T;
  metadata: {
    createdAt: Date;
    updatedAt: Date;
    lastAccessed: Date;
    accessCount: number;
  };
}

export type ContextMemoryData<T extends ContextType> = ContextMemoryTypes[T];

class ContextMemoryStore {
  private memories = new Map<string, ContextMemory<ContextMemoryData<ContextType>>>();
  private maxMemories = 1000;

  set<T extends ContextType>(
    contextType: T,
    contextId: string,
    data: Partial<ContextMemoryData<T>>
  ): void {
    const id = `${contextType}:${contextId}`;
    const existing = this.memories.get(id) as ContextMemory<ContextMemoryData<T>> | undefined;

    if (existing) {
      existing.data = { ...existing.data, ...data };
      existing.metadata.updatedAt = new Date();
      existing.metadata.lastAccessed = new Date();
      existing.metadata.accessCount++;
    } else {
      this.memories.set(id, {
        id,
        type: contextType,
        data: data as ContextMemoryData<T>,
        metadata: {
          createdAt: new Date(),
          updatedAt: new Date(),
          lastAccessed: new Date(),
          accessCount: 1,
        },
      });

      if (this.memories.size > this.maxMemories) {
        this.evictLeastRecentlyUsed();
      }
    }
  }

  get<T extends ContextType>(contextType: T, contextId: string): ContextMemory<ContextMemoryData<T>> | undefined {
    const id = `${contextType}:${contextId}`;
    const memory = this.memories.get(id) as ContextMemory<ContextMemoryData<T>> | undefined;

    if (memory) {
      memory.metadata.lastAccessed = new Date();
      memory.metadata.accessCount++;
    }

    return memory;
  }

  getByType<T extends ContextType>(contextType: T): ContextMemory<ContextMemoryData<T>>[] {
    return Array.from(this.memories.values())
      .filter(memory => memory.type === contextType)
      .sort((a, b) => b.metadata.lastAccessed.getTime() - a.metadata.lastAccessed.getTime())
      .map(memory => memory as ContextMemory<ContextMemoryData<T>>);
  }

  delete(contextType: ContextMemory["type"], contextId: string): void {
    const id = `${contextType}:${contextId}`;
    this.memories.delete(id);
  }

  clearType(contextType: ContextMemory["type"]): void {
    const keysToRemove: string[] = [];
    for (const [id, memory] of this.memories.entries()) {
      if (memory.type === contextType) {
        keysToRemove.push(id);
      }
    }
    keysToRemove.forEach(id => this.memories.delete(id));
  }

  clearAll(): void {
    this.memories.clear();
  }

  private evictLeastRecentlyUsed(): void {
    const sorted = Array.from(this.memories.entries())
      .sort((a, b) => a[1].metadata.lastAccessed.getTime() - b[1].metadata.lastAccessed.getTime());

    const countToEvict = Math.max(1, Math.floor(sorted.length * 0.1));
    sorted.slice(0, countToEvict).forEach(([id]) => this.memories.delete(id));
  }

  getStats() {
    const memories = Array.from(this.memories.values());
    const byType: Record<string, number> = {};
    memories.forEach(memory => {
      byType[memory.type] = (byType[memory.type] || 0) + 1;
    });
    const timestamps = memories.map(m => m.metadata.createdAt.getTime());
    return {
      total: this.memories.size,
      byType,
      oldest: timestamps.length ? new Date(Math.min(...timestamps)) : null,
      newest: timestamps.length ? new Date(Math.max(...timestamps)) : null,
    };
  }
}

export const contextMemoryStore = new ContextMemoryStore();

export function setWorkspaceContext(contextId: string, data: Partial<WorkspaceContext>): void {
  contextMemoryStore.set("workspace", contextId, data);
}

export function getWorkspaceContext(contextId: string): ContextMemory<WorkspaceContext> | undefined {
  return contextMemoryStore.get("workspace", contextId);
}

export function setProjectContext(projectId: string, data: Partial<ProjectContext>): void {
  contextMemoryStore.set("project", projectId, data);
}

export function getProjectContext(projectId: string): ContextMemory<ProjectContext> | undefined {
  return contextMemoryStore.get("project", projectId);
}

export function setAgentContext(agentId: string, data: Partial<AgentContext>): void {
  contextMemoryStore.set("agent", agentId, data);
}

export function getAgentContext(agentId: string): ContextMemory<AgentContext> | undefined {
  return contextMemoryStore.get("agent", agentId);
}

export function setUserContext(userId: string, data: Partial<UserContext>): void {
  contextMemoryStore.set("user", userId, data);
}

export function getUserContext(userId: string): ContextMemory<UserContext> | undefined {
  return contextMemoryStore.get("user", userId);
}

export function setTaskContext(taskId: string, data: Partial<TaskContext>): void {
  contextMemoryStore.set("task", taskId, data);
}

export function getTaskContext(taskId: string): ContextMemory<TaskContext> | undefined {
  return contextMemoryStore.get("task", taskId);
}
