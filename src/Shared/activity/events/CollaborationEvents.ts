/**
 * Collaboration Events
 * 
 * Real-time collaboration events for multi-user workspace interactions.
 * Separated from business logic to maintain clean event architecture.
 */

export type CollaborationEvent =
  | UserJoined
  | UserLeft
  | CursorMoved
  | SelectionChanged
  | PresenceUpdated
  | CommentAdded
  | CommentResolved
  | ConflictDetected
  | ConflictResolved;

export interface UserJoined {
  type: "UserJoined";
  timestamp: Date;
  userId: string;
  userName: string;
  workspaceId: string;
}

export interface UserLeft {
  type: "UserLeft";
  timestamp: Date;
  userId: string;
  userName: string;
  workspaceId: string;
}

export interface CursorMoved {
  type: "CursorMoved";
  timestamp: Date;
  userId: string;
  userName: string;
  filePath: string;
  position: { line: number; column: number };
}

export interface SelectionChanged {
  type: "SelectionChanged";
  timestamp: Date;
  userId: string;
  userName: string;
  filePath: string;
  selection: { start: { line: number; column: number }; end: { line: number; column: number } };
}

export interface PresenceUpdated {
  type: "PresenceUpdated";
  timestamp: Date;
  userId: string;
  status: "online" | "away" | "busy" | "offline";
  activity?: string;
}

export interface CommentAdded {
  type: "CommentAdded";
  timestamp: Date;
  userId: string;
  userName: string;
  targetId: string;
  targetType: "file" | "task" | "code";
  content: string;
}

export interface CommentResolved {
  type: "CommentResolved";
  timestamp: Date;
  userId: string;
  userName: string;
  commentId: string;
}

export interface ConflictDetected {
  type: "ConflictDetected";
  timestamp: Date;
  filePath: string;
  conflictType: "edit" | "delete" | "rename";
  users: Array<{ userId: string; userName: string }>;
}

export interface ConflictResolved {
  type: "ConflictResolved";
  timestamp: Date;
  filePath: string;
  resolution: "merge" | "overwrite" | "keep_both";
  resolvedBy: string;
}
