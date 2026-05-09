/**
 * System Events
 * 
 * Low-level system events for application lifecycle and infrastructure.
 * Separated from business logic to maintain clean event architecture.
 */

export type SystemEvent =
  | SystemStarted
  | SystemReady
  | SystemError
  | SystemShutdown
  | SystemConfigChanged;

export interface SystemStarted {
  type: "SystemStarted";
  timestamp: Date;
  version: string;
}

export interface SystemReady {
  type: "SystemReady";
  timestamp: Date;
  readyTime: number;
}

export interface SystemError {
  type: "SystemError";
  timestamp: Date;
  error: string;
  code?: string;
  severity: "low" | "medium" | "high" | "critical";
}

export interface SystemShutdown {
  type: "SystemShutdown";
  timestamp: Date;
  reason?: string;
}

export interface SystemConfigChanged {
  type: "SystemConfigChanged";
  timestamp: Date;
  configKey: string;
  oldValue: unknown;
  newValue: unknown;
}
