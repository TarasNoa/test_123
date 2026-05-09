import { createSignal, createEffect } from "solid-js";
import { globalEventStream, WorkspaceEvent } from "../activity/WorkspaceEvent";

/**
 * Global Workspace Store
 * 
 * Central state management for workspace to avoid local useState chaos.
 * All state is reactive and streams events to the event system.
 */

// Workspace state
export const [workspaceState, setWorkspaceState] = createSignal({
  currentView: "dashboard" as "dashboard" | "ide" | "marketplace" | "project" | "team" | "settings",
  sidebarExpanded: true,
  aiPanelExpanded: true,
  commandPaletteOpen: false,
  isFullscreen: false,
});

// Project state
export const [projectState, setProjectState] = createSignal({
  currentProjectId: null as string | null,
  projects: [] as Array<{
    id: string;
    name: string;
    description: string;
    status: "active" | "archived" | "draft";
    lastModified: Date;
  }>,
  activeFile: null as string | null,
  openFiles: [] as string[],
});

// Agent state
export const [agentState, setAgentState] = createSignal({
  activeAgents: [] as Array<{
    id: string;
    name: string;
    status: "idle" | "active" | "busy" | "error";
    currentTask?: string;
    progress?: number;
  }>,
  availableAgents: [] as Array<{
    id: string;
    name: string;
    capabilities: string[];
  }>,
});

// Editor state
export const [editorState, setEditorState] = createSignal({
  activeTab: null as string | null,
  tabs: [] as Array<{
    id: string;
    label: string;
    icon?: string;
    modified: boolean;
    closable: boolean;
  }>,
  cursorPosition: { line: 0, column: 0 },
  selection: null as { start: number; end: number } | null,
  unsavedChanges: false,
});

// Marketplace state
export const [marketplaceState, setMarketplaceState] = createSignal({
  opportunities: [] as Array<{
    id: string;
    title: string;
    description: string;
    type: "ai" | "automation" | "integration";
    reward: string;
    difficulty: "easy" | "medium" | "hard";
  }>,
  filters: {
    type: "all" as "all" | "ai" | "automation" | "integration",
    difficulty: "all" as "all" | "easy" | "medium" | "hard",
  },
});

// Activity state
export const [activityState, setActivityState] = createSignal({
  events: [] as WorkspaceEvent[],
  maxHistorySize: 1000,
});

// Toast state
export const [toastState, setToastState] = createSignal({
  toasts: [] as Array<{
    id: string;
    type: "success" | "error" | "warning" | "info";
    message: string;
    timestamp: Date;
  }>,
});

/**
 * Workspace Actions
 */
export const workspaceActions = {
  setView: (view: "dashboard" | "ide" | "marketplace" | "project" | "team" | "settings") => {
    setWorkspaceState((prev) => ({ ...prev, currentView: view }));
    globalEventStream.emit({
      type: "CollaborationEvent",
      eventType: "cursor_moved",
      userId: "system",
      userName: "System",
      data: { view },
      timestamp: new Date(),
    });
  },

  toggleSidebar: () => {
    setWorkspaceState((prev) => ({ ...prev, sidebarExpanded: !prev.sidebarExpanded }));
  },

  toggleAIPanel: () => {
    setWorkspaceState((prev) => ({ ...prev, aiPanelExpanded: !prev.aiPanelExpanded }));
  },

  toggleCommandPalette: () => {
    setWorkspaceState((prev) => ({ ...prev, commandPaletteOpen: !prev.commandPaletteOpen }));
  },

  toggleFullscreen: () => {
    setWorkspaceState((prev) => ({ ...prev, isFullscreen: !prev.isFullscreen }));
  },
};

/**
 * Project Actions
 */
export const projectActions = {
  setCurrentProject: (projectId: string | null) => {
    setProjectState(prev => ({ ...prev, currentProjectId: projectId }));
  },

  addProject: (project: {
    id: string;
    name: string;
    description: string;
    status: "active" | "archived" | "draft";
    lastModified: Date;
  }) => {
    setProjectState(prev => ({
      ...prev,
      projects: [...prev.projects, project],
    }));
    globalEventStream.emit({
      type: "FileCreated",
      filePath: project.id,
      content: "",
      timestamp: new Date(),
    });
  },

  openFile: (filePath: string) => {
    setProjectState(prev => {
      if (!prev.openFiles.includes(filePath)) {
        return { ...prev, openFiles: [...prev.openFiles, filePath], activeFile: filePath };
      }
      return { ...prev, activeFile: filePath };
    });
    globalEventStream.emit({
      type: "FileModified",
      filePath,
      changes: "opened",
      timestamp: new Date(),
    });
  },

  closeFile: (filePath: string) => {
    setProjectState(prev => ({
      ...prev,
      openFiles: prev.openFiles.filter(f => f !== filePath),
      activeFile: prev.activeFile === filePath ? prev.openFiles[prev.openFiles.length - 1] || null : prev.activeFile,
    }));
  },
};

/**
 * Agent Actions
 */
export const agentActions = {
  addAgent: (agent: {
    id: string;
    name: string;
    status: "idle" | "active" | "busy" | "error";
    currentTask?: string;
    progress?: number;
  }) => {
    setAgentState(prev => ({
      ...prev,
      activeAgents: [...prev.activeAgents, agent],
    }));
    globalEventStream.emit({
      type: "AgentStarted",
      agentId: agent.id,
      agentName: agent.name,
      taskId: agent.currentTask || "",
      timestamp: new Date(),
    });
  },

  updateAgentStatus: (agentId: string, status: "idle" | "active" | "busy" | "error") => {
    setAgentState(prev => ({
      ...prev,
      activeAgents: prev.activeAgents.map(a =>
        a.id === agentId ? { ...a, status } : a
      ),
    }));
  },

  updateAgentProgress: (agentId: string, progress: number) => {
    setAgentState(prev => ({
      ...prev,
      activeAgents: prev.activeAgents.map(a =>
        a.id === agentId ? { ...a, progress } : a
      ),
    }));
  },

  removeAgent: (agentId: string) => {
    setAgentState(prev => ({
      ...prev,
      activeAgents: prev.activeAgents.filter(a => a.id !== agentId),
    }));
  },
};

/**
 * Editor Actions
 */
export const editorActions = {
  openTab: (tab: {
    id: string;
    label: string;
    icon?: string;
    modified: boolean;
    closable: boolean;
  }) => {
    setEditorState(prev => {
      const existingTab = prev.tabs.find(t => t.id === tab.id);
      if (existingTab) {
        return { ...prev, activeTab: tab.id };
      }
      return {
        ...prev,
        tabs: [...prev.tabs, tab],
        activeTab: tab.id,
      };
    });
  },

  closeTab: (tabId: string) => {
    setEditorState(prev => {
      const newTabs = prev.tabs.filter(t => t.id !== tabId);
      return {
        ...prev,
        tabs: newTabs,
        activeTab: prev.activeTab === tabId ? (newTabs[newTabs.length - 1]?.id || null) : prev.activeTab,
      };
    });
  },

  setActiveTab: (tabId: string) => {
    setEditorState(prev => ({ ...prev, activeTab: tabId }));
  },

  updateCursorPosition: (line: number, column: number) => {
    setEditorState(prev => ({ ...prev, cursorPosition: { line, column } }));
  },

  markUnsaved: () => {
    setEditorState(prev => ({ ...prev, unsavedChanges: true }));
  },

  markSaved: () => {
    setEditorState(prev => ({ ...prev, unsavedChanges: false }));
  },
};

/**
 * Toast Actions
 */
export const toastActions = {
  addToast: (toast: {
    type: "success" | "error" | "warning" | "info";
    message: string;
  }) => {
    const id = Date.now().toString();
    setToastState(prev => ({
      ...prev,
      toasts: [
        ...prev.toasts,
        { id, ...toast, timestamp: new Date() },
      ],
    }));

    // Auto-remove after 5 seconds
    setTimeout(() => {
      toastActions.removeToast(id);
    }, 5000);
  },

  removeToast: (id: string) => {
    setToastState(prev => ({
      ...prev,
      toasts: prev.toasts.filter(t => t.id !== id),
    }));
  },

  clearToasts: () => {
    setToastState(prev => ({ ...prev, toasts: [] }));
  },
};

// Subscribe to event stream and update activity state
createEffect(() => {
  const unsubscribe = globalEventStream.subscribe("*", (event) => {
    setActivityState(prev => {
      const newEvents = [...prev.events, event];
      if (newEvents.length > prev.maxHistorySize) {
        newEvents.shift();
      }
      return { ...prev, events: newEvents };
    });
  });

  return unsubscribe;
});
