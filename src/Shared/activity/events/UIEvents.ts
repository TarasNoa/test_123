/**
 * UI Events
 * 
 * User interface events for component interactions and UI state changes.
 * Separated from business logic to maintain clean event architecture.
 */

export type UIEvent =
  | ButtonClicked
  | ModalOpened
  | ModalClosed
  | TabSwitched
  | SidebarToggled
  | PanelResized
  | CommandPaletteOpened
  | CommandPaletteClosed
  | CommandExecuted;

export interface ButtonClicked {
  type: "ButtonClicked";
  timestamp: Date;
  buttonId: string;
  context?: string;
}

export interface ModalOpened {
  type: "ModalOpened";
  timestamp: Date;
  modalId: string;
  context?: string;
}

export interface ModalClosed {
  type: "ModalClosed";
  timestamp: Date;
  modalId: string;
  action?: "confirm" | "cancel" | "close";
}

export interface TabSwitched {
  type: "TabSwitched";
  timestamp: Date;
  tabId: string;
  previousTabId?: string;
}

export interface SidebarToggled {
  type: "SidebarToggled";
  timestamp: Date;
  sidebarId: string;
  isOpen: boolean;
}

export interface PanelResized {
  type: "PanelResized";
  timestamp: Date;
  panelId: string;
  newSize: number;
  direction: "horizontal" | "vertical";
}

export interface CommandPaletteOpened {
  type: "CommandPaletteOpened";
  timestamp: Date;
  trigger: "shortcut" | "button" | "api";
}

export interface CommandPaletteClosed {
  type: "CommandPaletteClosed";
  timestamp: Date;
  reason: "command_executed" | "escape" | "outside_click";
}

export interface CommandExecuted {
  type: "CommandExecuted";
  timestamp: Date;
  commandId: string;
  category: string;
  context?: Record<string, unknown>;
}
