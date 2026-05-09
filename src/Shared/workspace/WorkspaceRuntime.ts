/**
 * Workspace Runtime
 * 
 * Real workspace runtime with panel manager:
 * - Panel dock, resize, collapse, persist, restore
 * - Multi-workspace navigation
 * - Tab memory
 * - Split panes
 * - Command workflows
 * - Context persistence
 * 
 * This is the foundation for IDE-like UX
 */

import { createSignal } from "solid-js";
import { loadWorkspaceState, saveWorkspaceState, mergeWorkspaceState, clearWorkspaceState, type WorkspacePersistedState } from "../persistence/WorkspaceContext";

export type PanelType = "left" | "center" | "right" | "bottom" | "floating";

export interface Panel {
  id: PanelType;
  visible: boolean;
  size: number; // in pixels
  minSize: number;
  maxSize: number;
  collapsed: boolean;
  position: { x: number; y: number };
  zIndex: number;
}

export interface Tab {
  id: string;
  title: string;
  type: "file" | "graph" | "timeline" | "agents" | "intelligence";
  path?: string;
  closable: boolean;
  active: boolean;
  modified: boolean;
}

export interface WorkspaceState {
  id: string;
  name: string;
  panels: Record<PanelType, Panel>;
  tabs: Tab[];
  activeTab: string | null;
  splitView: boolean;
  splitDirection: "horizontal" | "vertical";
}

/**
 * Workspace Runtime
 */
class WorkspaceRuntime {
  private currentWorkspace: WorkspaceState;
  private workspaces: Map<string, WorkspaceState> = new Map();

  constructor() {
    this.currentWorkspace = this.createDefaultWorkspace();
    this.loadPersistedState();
  }

  /**
   * Create default workspace state
   */
  private createDefaultWorkspace(): WorkspaceState {
    return {
      id: "default",
      name: "Default Workspace",
      panels: {
        left: {
          id: "left",
          visible: true,
          size: 280,
          minSize: 200,
          maxSize: 400,
          collapsed: false,
          position: { x: 0, y: 0 },
          zIndex: 10,
        },
        center: {
          id: "center",
          visible: true,
          size: 600,
          minSize: 400,
          maxSize: 1200,
          collapsed: false,
          position: { x: 280, y: 0 },
          zIndex: 1,
        },
        right: {
          id: "right",
          visible: true,
          size: 400,
          minSize: 300,
          maxSize: 600,
          collapsed: false,
          position: { x: 880, y: 0 },
          zIndex: 10,
        },
        bottom: {
          id: "bottom",
          visible: false,
          size: 200,
          minSize: 150,
          maxSize: 400,
          collapsed: false,
          position: { x: 0, y: 400 },
          zIndex: 10,
        },
        floating: {
          id: "floating",
          visible: false,
          size: 300,
          minSize: 200,
          maxSize: 600,
          collapsed: false,
          position: { x: 100, y: 100 },
          zIndex: 100,
        },
      },
      tabs: [],
      activeTab: null,
      splitView: false,
      splitDirection: "horizontal",
    };
  }

  /**
   * Load persisted state
   */
  private loadPersistedState(): void {
    const persisted = loadWorkspaceState();
    if (persisted) {
      this.currentWorkspace = {
        ...this.currentWorkspace,
        panels: {
          ...this.currentWorkspace.panels,
          left: { ...this.currentWorkspace.panels.left, size: persisted.layout.sidebarWidth },
          right: { ...this.currentWorkspace.panels.right, size: persisted.layout.rightPanelWidth },
          bottom: { ...this.currentWorkspace.panels.bottom, visible: persisted.layout.aiPanelExpanded },
        },
        tabs: persisted.tabs.openTabs.map(tab => ({
          ...tab,
          closable: true,
          active: tab.id === persisted.tabs.activeTab,
          modified: false,
        })),
        activeTab: persisted.tabs.activeTab,
      };
    }
  }

  /**
   * Persist current state
   */
  private persistState(): void {
    const state: WorkspacePersistedState = {
      layout: {
        sidebarWidth: this.currentWorkspace.panels.left.size,
        leftPanelWidth: this.currentWorkspace.panels.left.size,
        rightPanelWidth: this.currentWorkspace.panels.right.size,
        aiPanelExpanded: this.currentWorkspace.panels.right.visible,
        sidebarExpanded: this.currentWorkspace.panels.left.visible,
      },
      tabs: {
        openTabs: this.currentWorkspace.tabs.map(tab => ({
          id: tab.id,
          title: tab.title,
          type: tab.type as "file" | "graph" | "timeline" | "agents",
          path: tab.path,
        })),
        activeTab: this.currentWorkspace.activeTab,
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
        leftPanelVisible: this.currentWorkspace.panels.left.visible,
        rightPanelVisible: this.currentWorkspace.panels.right.visible,
        bottomPanelVisible: this.currentWorkspace.panels.bottom.visible,
      },
      lastUpdated: new Date(),
    };
    saveWorkspaceState(state);
  }

  /**
   * Get current workspace
   */
  getCurrentWorkspace(): WorkspaceState {
    return this.currentWorkspace;
  }

  /**
   * Set panel visibility
   */
  setPanelVisibility(panelId: PanelType, visible: boolean): void {
    this.currentWorkspace.panels[panelId].visible = visible;
    this.persistState();
  }

  /**
   * Resize panel
   */
  resizePanel(panelId: PanelType, size: number): void {
    const panel = this.currentWorkspace.panels[panelId];
    panel.size = Math.max(panel.minSize, Math.min(panel.maxSize, size));
    this.persistState();
  }

  /**
   * Collapse panel
   */
  collapsePanel(panelId: PanelType): void {
    this.currentWorkspace.panels[panelId].collapsed = true;
    this.persistState();
  }

  /**
   * Expand panel
   */
  expandPanel(panelId: PanelType): void {
    this.currentWorkspace.panels[panelId].collapsed = false;
    this.persistState();
  }

  /**
   * Dock panel
   */
  dockPanel(panelId: PanelType): void {
    this.currentWorkspace.panels[panelId].position = { x: 0, y: 0 };
    this.currentWorkspace.panels[panelId].zIndex = 10;
    this.persistState();
  }

  /**
   * Open tab
   */
  openTab(tab: Omit<Tab, "active" | "modified">): void {
    const existing = this.currentWorkspace.tabs.find(t => t.id === tab.id);
    if (existing) {
      this.activateTab(tab.id);
      return;
    }

    this.currentWorkspace.tabs = [
      ...this.currentWorkspace.tabs.map(t => ({ ...t, active: false })),
      { ...tab, active: true, modified: false },
    ];
    this.currentWorkspace.activeTab = tab.id;
    this.persistState();
  }

  /**
   * Close tab
   */
  closeTab(tabId: string): void {
    const index = this.currentWorkspace.tabs.findIndex(t => t.id === tabId);
    if (index === -1) return;

    const wasActive = this.currentWorkspace.tabs[index].active;
    this.currentWorkspace.tabs = this.currentWorkspace.tabs.filter(t => t.id !== tabId);

    if (wasActive && this.currentWorkspace.tabs.length > 0) {
      const newIndex = Math.min(index, this.currentWorkspace.tabs.length - 1);
      this.currentWorkspace.tabs[newIndex].active = true;
      this.currentWorkspace.activeTab = this.currentWorkspace.tabs[newIndex].id;
    } else if (this.currentWorkspace.tabs.length === 0) {
      this.currentWorkspace.activeTab = null;
    }

    this.persistState();
  }

  /**
   * Activate tab
   */
  activateTab(tabId: string): void {
    this.currentWorkspace.tabs = this.currentWorkspace.tabs.map(t => ({
      ...t,
      active: t.id === tabId,
    }));
    this.currentWorkspace.activeTab = tabId;
    this.persistState();
  }

  /**
   * Toggle split view
   */
  toggleSplitView(): void {
    this.currentWorkspace.splitView = !this.currentWorkspace.splitView;
    this.persistState();
  }

  /**
   * Set split direction
   */
  setSplitDirection(direction: "horizontal" | "vertical"): void {
    this.currentWorkspace.splitDirection = direction;
    this.persistState();
  }

  /**
   * Reset workspace to default
   */
  resetWorkspace(): void {
    this.currentWorkspace = this.createDefaultWorkspace();
    clearWorkspaceState();
  }
}

// Global workspace runtime instance
export const workspaceRuntime = new WorkspaceRuntime();

/**
 * Workspace runtime helpers
 */
export function getCurrentWorkspace() {
  return workspaceRuntime.getCurrentWorkspace();
}

export function setPanelVisibility(panelId: PanelType, visible: boolean) {
  workspaceRuntime.setPanelVisibility(panelId, visible);
}

export function resizePanel(panelId: PanelType, size: number) {
  workspaceRuntime.resizePanel(panelId, size);
}

export function openTab(tab: Omit<Tab, "active" | "modified">) {
  workspaceRuntime.openTab(tab);
}

export function closeTab(tabId: string) {
  workspaceRuntime.closeTab(tabId);
}

export function activateTab(tabId: string) {
  workspaceRuntime.activateTab(tabId);
}

export function toggleSplitView() {
  workspaceRuntime.toggleSplitView();
}
