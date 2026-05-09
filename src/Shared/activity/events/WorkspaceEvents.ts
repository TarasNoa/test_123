/**
 * Workspace Events
 * 
 * Workspace and project events for file operations, builds, deployments, and project lifecycle.
 * Separated from business logic to maintain clean event architecture.
 */

export type WorkspaceEvent =
  | FileModified
  | FileCreated
  | FileDeleted
  | FileRenamed
  | TaskAssigned
  | TaskStarted
  | TaskCompleted
  | TaskFailed
  | BuildStarted
  | BuildProgress
  | BuildCompleted
  | BuildFailed
  | DeploymentStarted
  | DeploymentProgress
  | DeploymentCompleted
  | DeploymentFailed
  | TestStarted
  | TestProgress
  | TestCompleted
  | TestFailed
  | WorkspaceSnapshotCreated
  | WorkspaceSnapshotRestored;

export interface FileModified {
  type: "FileModified";
  timestamp: Date;
  filePath: string;
  changes: string;
  userId?: string;
  lineCount?: number;
}

export interface FileCreated {
  type: "FileCreated";
  timestamp: Date;
  filePath: string;
  content: string;
  userId?: string;
  fileType: string;
}

export interface FileDeleted {
  type: "FileDeleted";
  timestamp: Date;
  filePath: string;
  userId?: string;
}

export interface FileRenamed {
  type: "FileRenamed";
  timestamp: Date;
  oldPath: string;
  newPath: string;
  userId?: string;
}

export interface TaskAssigned {
  type: "TaskAssigned";
  timestamp: Date;
  taskId: string;
  agentId: string;
  description: string;
  priority: "low" | "medium" | "high" | "critical";
  estimatedDuration?: number;
}

export interface TaskStarted {
  type: "TaskStarted";
  timestamp: Date;
  taskId: string;
  agentId: string;
}

export interface TaskCompleted {
  type: "TaskCompleted";
  timestamp: Date;
  taskId: string;
  agentId: string;
  result: string;
  duration: number;
}

export interface TaskFailed {
  type: "TaskFailed";
  timestamp: Date;
  taskId: string;
  agentId: string;
  error: string;
  retryCount: number;
}

export interface BuildStarted {
  type: "BuildStarted";
  timestamp: Date;
  projectId: string;
  buildType: "dev" | "prod" | "test";
  branch: string;
}

export interface BuildProgress {
  type: "BuildProgress";
  timestamp: Date;
  projectId: string;
  step: string;
  progress: number;
  output?: string;
}

export interface BuildCompleted {
  type: "BuildCompleted";
  timestamp: Date;
  projectId: string;
  duration: number;
  output: string;
  artifacts: string[];
}

export interface BuildFailed {
  type: "BuildFailed";
  timestamp: Date;
  projectId: string;
  error: string;
  step: string;
  exitCode: number;
}

export interface DeploymentStarted {
  type: "DeploymentStarted";
  timestamp: Date;
  projectId: string;
  environment: "staging" | "production";
  version: string;
}

export interface DeploymentProgress {
  type: "DeploymentProgress";
  timestamp: Date;
  projectId: string;
  step: string;
  progress: number;
}

export interface DeploymentCompleted {
  type: "DeploymentCompleted";
  timestamp: Date;
  projectId: string;
  environment: string;
  url: string;
  duration: number;
}

export interface DeploymentFailed {
  type: "DeploymentFailed";
  timestamp: Date;
  projectId: string;
  error: string;
  rollback?: boolean;
}

export interface TestStarted {
  type: "TestStarted";
  timestamp: Date;
  testSuite: string;
  testCount: number;
}

export interface TestProgress {
  type: "TestProgress";
  timestamp: Date;
  testSuite: string;
  testsRun: number;
  testsPassed: number;
  testsFailed: number;
  currentTest?: string;
}

export interface TestCompleted {
  type: "TestCompleted";
  timestamp: Date;
  testSuite: string;
  total: number;
  passed: number;
  failed: number;
  skipped: number;
  duration: number;
}

export interface TestFailed {
  type: "TestFailed";
  timestamp: Date;
  testSuite: string;
  testName: string;
  error: string;
  stackTrace?: string;
}

export interface WorkspaceSnapshotCreated {
  type: "WorkspaceSnapshotCreated";
  timestamp: Date;
  snapshotId: string;
  description: string;
  state: Record<string, unknown>;
}

export interface WorkspaceSnapshotRestored {
  type: "WorkspaceSnapshotRestored";
  timestamp: Date;
  snapshotId: string;
  success: boolean;
}
