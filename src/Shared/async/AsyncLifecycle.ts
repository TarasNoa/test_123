/**
 * Unified Async Lifecycle
 * 
 * Normalizes async behavior across the application:
 * - idle
 * - loading
 * - streaming
 * - success
 * - partial
 * - error
 * - retrying
 * - cancelled
 * 
 * Prevents loading chaos, race conditions, stale context, flickering
 */

export type AsyncState =
  | "idle"
  | "loading"
  | "streaming"
  | "success"
  | "partial"
  | "error"
  | "retrying"
  | "cancelled";

export interface AsyncStateContext<T = unknown> {
  state: AsyncState;
  data: T | null;
  error: Error | null;
  timestamp: number;
  retryCount: number;
}

export class AsyncLifecycle<T = unknown> {
  private state: AsyncState = "idle";
  private data: T | null = null;
  private error: Error | null = null;
  private retryCount = 0;
  private listeners: Set<(context: AsyncStateContext<T>) => void> = new Set();

  /**
   * Get current state context
   */
  getContext(): AsyncStateContext<T> {
    return {
      state: this.state,
      data: this.data,
      error: this.error,
      timestamp: Date.now(),
      retryCount: this.retryCount,
    };
  }

  /**
   * Get current state
   */
  getState(): AsyncState {
    return this.state;
  }

  /**
   * Get current data
   */
  getData(): T | null {
    return this.data;
  }

  /**
   * Get current error
   */
  getError(): Error | null {
    return this.error;
  }

  /**
   * Check if is idle
   */
  isIdle(): boolean {
    return this.state === "idle";
  }

  /**
   * Check if is loading
   */
  isLoading(): boolean {
    return this.state === "loading";
  }

  /**
   * Check if is streaming
   */
  isStreaming(): boolean {
    return this.state === "streaming";
  }

  /**
   * Check if is success
   */
  isSuccess(): boolean {
    return this.state === "success";
  }

  /**
   * Check if is partial
   */
  isPartial(): boolean {
    return this.state === "partial";
  }

  /**
   * Check if is error
   */
  isError(): boolean {
    return this.state === "error";
  }

  /**
   * Check if is retrying
   */
  isRetrying(): boolean {
    return this.state === "retrying";
  }

  /**
   * Check if is cancelled
   */
  isCancelled(): boolean {
    return this.state === "cancelled";
  }

  /**
   * Check if is pending (loading, streaming, retrying)
   */
  isPending(): boolean {
    return this.state === "loading" || this.state === "streaming" || this.state === "retrying";
  }

  /**
   * Set state to idle
   */
  setIdle(): void {
    this.state = "idle";
    this.data = null;
    this.error = null;
    this.retryCount = 0;
    this.notify();
  }

  /**
   * Set state to loading
   */
  setLoading(): void {
    this.state = "loading";
    this.notify();
  }

  /**
   * Set state to streaming with partial data
   */
  setStreaming(data: T): void {
    this.state = "streaming";
    this.data = data;
    this.notify();
  }

  /**
   * Set state to success with data
   */
  setSuccess(data: T): void {
    this.state = "success";
    this.data = data;
    this.error = null;
    this.retryCount = 0;
    this.notify();
  }

  /**
   * Set state to partial with data
   */
  setPartial(data: T): void {
    this.state = "partial";
    this.data = data;
    this.notify();
  }

  /**
   * Set state to error
   */
  setError(error: Error): void {
    this.state = "error";
    this.error = error;
    this.notify();
  }

  /**
   * Set state to retrying
   */
  setRetrying(error: Error): void {
    this.state = "retrying";
    this.error = error;
    this.retryCount++;
    this.notify();
  }

  /**
   * Set state to cancelled
   */
  setCancelled(): void {
    this.state = "cancelled";
    this.notify();
  }

  /**
   * Subscribe to state changes
   */
  subscribe(listener: (context: AsyncStateContext<T>) => void): () => void {
    this.listeners.add(listener);
    listener(this.getContext());

    return () => {
      this.listeners.delete(listener);
    };
  }

  /**
   * Notify all listeners
   */
  private notify(): void {
    const context = this.getContext();
    this.listeners.forEach(listener => listener(context));
  }

  /**
   * Reset to idle
   */
  reset(): void {
    this.setIdle();
  }
}

/**
 * Async lifecycle helpers
 */
export function createAsyncLifecycle<T = unknown>(): AsyncLifecycle<T> {
  return new AsyncLifecycle<T>();
}

/**
 * Async state utilities
 */
export function isPending(state: AsyncState): boolean {
  return state === "loading" || state === "streaming" || state === "retrying";
}

export function isTerminal(state: AsyncState): boolean {
  return state === "success" || state === "error" || state === "cancelled";
}

export function isTransient(state: AsyncState): boolean {
  return state === "loading" || state === "streaming";
}

/**
 * Async state transition validation
 */
export function canTransition(from: AsyncState, to: AsyncState): boolean {
  const transitions: Record<AsyncState, AsyncState[]> = {
    idle: ["loading", "cancelled"],
    loading: ["success", "error", "streaming", "cancelled"],
    streaming: ["success", "error", "partial", "cancelled"],
    success: ["idle", "loading", "cancelled"],
    partial: ["loading", "streaming", "success", "error", "cancelled"],
    error: ["idle", "loading", "retrying", "cancelled"],
    retrying: ["loading", "success", "error", "cancelled"],
    cancelled: ["idle", "loading"],
  };

  return transitions[from]?.includes(to) ?? false;
}

/**
 * Async state transition with validation
 */
export function transitionState(
  lifecycle: AsyncLifecycle,
  to: AsyncState,
  data?: unknown,
  error?: Error
): boolean {
  const from = lifecycle.getState();

  if (!canTransition(from, to)) {
    console.warn(`Invalid async state transition: ${from} -> ${to}`);
    return false;
  }

  switch (to) {
    case "idle":
      lifecycle.setIdle();
      break;
    case "loading":
      lifecycle.setLoading();
      break;
    case "streaming":
      if (data !== undefined) lifecycle.setStreaming(data);
      break;
    case "success":
      if (data !== undefined) lifecycle.setSuccess(data);
      break;
    case "partial":
      if (data !== undefined) lifecycle.setPartial(data);
      break;
    case "error":
      if (error) lifecycle.setError(error);
      break;
    case "retrying":
      if (error) lifecycle.setRetrying(error);
      break;
    case "cancelled":
      lifecycle.setCancelled();
      break;
  }

  return true;
}
