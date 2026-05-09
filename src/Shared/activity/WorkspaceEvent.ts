/**
 * Unified Workspace Event System
 * 
 * Central event types for all workspace activities.
 * Events are separated by domain to avoid God Object anti-pattern.
 * 
 * Domains:
 * - System Events (application lifecycle)
 * - UI Events (component interactions)
 * - AI Events (agent orchestration)
 * - Workspace Events (files, builds, deployments)
 * - Collaboration Events (real-time collaboration)
 */

// Import event domains
import type { SystemEvent } from "./events/SystemEvents";
import type { UIEvent } from "./events/UIEvents";
import type { AIEvent } from "./events/AIEvents";
import type { WorkspaceEvent as WorkspaceDomainEvent } from "./events/WorkspaceEvents";
import type { CollaborationEvent } from "./events/CollaborationEvents";

// Re-export all event types as unified WorkspaceEvent type
export type WorkspaceEvent = SystemEvent | UIEvent | AIEvent | WorkspaceDomainEvent | CollaborationEvent;

// Re-export event types for convenience
export type { SystemEvent, UIEvent, AIEvent, CollaborationEvent };
export type { WorkspaceEvent as WorkspaceDomainEvent };

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
