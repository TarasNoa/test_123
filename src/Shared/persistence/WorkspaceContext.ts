/**
 * Workspace Context Persistence
 * 
 * Persists and restores workspace state:
 * - Layout state (panel sizes, sidebar state)
 * - Opened tabs
 * - Selected nodes (graph, timeline, agents)
 * - AI context (active section, suggestions)
 * - Panel state (expanded/collapsed)
 * 
 * Instant restoration on workspace reload
 */

export interface WorkspaceLayoutState {
  sidebarWidth: number;
  leftPanelWidth: number;
  rightPanelWidth: number;
  aiPanelExpanded: boolean;
  sidebarExpanded: boolean;
}

export interface WorkspaceTabsState {
  openTabs: Array<{
    id: string;
    title: string;
    type: "file" | "graph" | "timeline" | "agents";
    path?: string;
  }>;
  activeTab: string | null;
}

export interface WorkspaceSelectionState {
  graphSelectedNode: string | null;
  timelineSelectedEvent: string | null;
  agentsSelectedAgent: string | null;
}

export interface WorkspaceAIContextState {
  activeSection: string;
  dismissedSuggestions: string[];
}

export interface WorkspacePanelState {
  leftPanelVisible: boolean;
  rightPanelVisible: boolean;
  bottomPanelVisible: boolean;
}

export interface WorkspacePersistedState {
  layout: WorkspaceLayoutState;
  tabs: WorkspaceTabsState;
  selection: WorkspaceSelectionState;
  aiContext: WorkspaceAIContextState;
  panels: WorkspacePanelState;
  lastUpdated: Date;
}

const STORAGE_KEY = "workspace_state";

/**
 * Save workspace state to localStorage
 */
export function saveWorkspaceState(state: WorkspacePersistedState): void {
  try {
    const serialized = JSON.stringify({
      ...state,
      lastUpdated: new Date().toISOString(),
    });
    localStorage.setItem(STORAGE_KEY, serialized);
  } catch (error) {
    console.error("Failed to save workspace state:", error);
  }
}

/**
 * Load workspace state from localStorage
 */
export function loadWorkspaceState(): WorkspacePersistedState | null {
  try {
    const serialized = localStorage.getItem(STORAGE_KEY);
    if (!serialized) return null;

    const state = JSON.parse(serialized);
    
    // Convert ISO strings back to Dates
    if (state.lastUpdated) {
      state.lastUpdated = new Date(state.lastUpdated);
    }

    return state;
  } catch (error) {
    console.error("Failed to load workspace state:", error);
    return null;
  }
}

/**
 * Clear workspace state from localStorage
 */
export function clearWorkspaceState(): void {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch (error) {
    console.error("Failed to clear workspace state:", error);
  }
}

/**
 * Get default workspace state
 */
export function getDefaultWorkspaceState(): WorkspacePersistedState {
  return {
    layout: {
      sidebarWidth: 280,
      leftPanelWidth: 300,
      rightPanelWidth: 400,
      aiPanelExpanded: true,
      sidebarExpanded: true,
    },
    tabs: {
      openTabs: [],
      activeTab: null,
    },
    selection: {
      graphSelectedNode: null,
      timelineSelectedEvent: null,
      agentsSelectedAgent: null,
    },
    aiContext: {
      activeSection: "recommendations",
      dismissedSuggestions: [],
    },
    panels: {
      leftPanelVisible: true,
      rightPanelVisible: true,
      bottomPanelVisible: false,
    },
    lastUpdated: new Date(),
  };
}

/**
 * Update specific part of workspace state
 */
export function updateWorkspaceState<K extends keyof WorkspacePersistedState>(
  key: K,
  value: WorkspacePersistedState[K]
): void {
  const currentState = loadWorkspaceState() || getDefaultWorkspaceState();
  const updatedState = {
    ...currentState,
    [key]: value,
    lastUpdated: new Date(),
  };
  saveWorkspaceState(updatedState);
}

/**
 * Merge workspace state with partial updates
 */
export function mergeWorkspaceState(updates: Partial<WorkspacePersistedState>): void {
  const currentState = loadWorkspaceState() || getDefaultWorkspaceState();
  const updatedState = {
    ...currentState,
    ...updates,
    lastUpdated: new Date(),
  };
  saveWorkspaceState(updatedState);
}
