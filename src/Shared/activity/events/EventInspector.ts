/**
 * Simple Dev Event Inspector
 * 
 * Event observability for debugging:
 * - Recent events
 * - Source
 * - Target
 * - Duration
 * - Subscribers
 * 
 * Helps debugging: who called what, why UI updated, where race condition
 */

export interface EventRecord {
  id: string;
  eventType: string;
  source: string;
  timestamp: number;
  duration?: number;
  subscribers: string[];
}

export class EventInspector {
  private records: EventRecord[] = [];
  private maxRecords = 100;
  private enabled = false;

  /**
   * Enable/disable inspector
   */
  setEnabled(enabled: boolean): void {
    this.enabled = enabled;
  }

  /**
   * Check if enabled
   */
  isEnabled(): boolean {
    return this.enabled;
  }

  /**
   * Record event
   */
  recordEvent(eventType: string, source: string, subscribers: string[]): void {
    if (!this.enabled) return;

    const record: EventRecord = {
      id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      eventType,
      source,
      timestamp: Date.now(),
      subscribers,
    };

    this.records.unshift(record);

    // Keep only maxRecords
    if (this.records.length > this.maxRecords) {
      this.records = this.records.slice(0, this.maxRecords);
    }
  }

  /**
   * Record event with duration
   */
  recordEventWithDuration(
    eventType: string,
    source: string,
    duration: number,
    subscribers: string[]
  ): void {
    if (!this.enabled) return;

    const record: EventRecord = {
      id: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      eventType,
      source,
      timestamp: Date.now(),
      duration,
      subscribers,
    };

    this.records.unshift(record);

    // Keep only maxRecords
    if (this.records.length > this.maxRecords) {
      this.records = this.records.slice(0, this.maxRecords);
    }
  }

  /**
   * Get recent events
   */
  getRecentEvents(limit = 20): EventRecord[] {
    return this.records.slice(0, limit);
  }

  /**
   * Get events by type
   */
  getEventsByType(eventType: string): EventRecord[] {
    return this.records.filter(r => r.eventType === eventType);
  }

  /**
   * Get events by source
   */
  getEventsBySource(source: string): EventRecord[] {
    return this.records.filter(r => r.source === source);
  }

  /**
   * Get events in time range
   */
  getEventsInTimeRange(startTime: number, endTime: number): EventRecord[] {
    return this.records.filter(r => r.timestamp >= startTime && r.timestamp <= endTime);
  }

  /**
   * Get event statistics
   */
  getStatistics(): {
    totalEvents: number;
    eventTypes: Record<string, number>;
    sources: Record<string, number>;
    avgDuration: number;
  } {
    const eventTypes: Record<string, number> = {};
    const sources: Record<string, number> = {};
    let totalDuration = 0;
    let durationCount = 0;

    this.records.forEach(record => {
      eventTypes[record.eventType] = (eventTypes[record.eventType] || 0) + 1;
      sources[record.source] = (sources[record.source] || 0) + 1;

      if (record.duration !== undefined) {
        totalDuration += record.duration;
        durationCount++;
      }
    });

    return {
      totalEvents: this.records.length,
      eventTypes,
      sources,
      avgDuration: durationCount > 0 ? totalDuration / durationCount : 0,
    };
  }

  /**
   * Clear records
   */
  clearRecords(): void {
    this.records = [];
  }

  /**
   * Export records as JSON
   */
  exportRecords(): string {
    return JSON.stringify(this.records, null, 2);
  }
}

// Global event inspector instance
export const eventInspector = new EventInspector();

/**
 * Event inspector helpers
 */
export function enableEventInspector(): void {
  eventInspector.setEnabled(true);
}

export function disableEventInspector(): void {
  eventInspector.setEnabled(false);
}

export function isEventInspectorEnabled(): boolean {
  return eventInspector.isEnabled();
}

export function recordEvent(eventType: string, source: string, subscribers: string[]): void {
  eventInspector.recordEvent(eventType, source, subscribers);
}

export function recordEventWithDuration(
  eventType: string,
  source: string,
  duration: number,
  subscribers: string[]
): void {
  eventInspector.recordEventWithDuration(eventType, source, duration, subscribers);
}

export function getRecentEvents(limit = 20): EventRecord[] {
  return eventInspector.getRecentEvents(limit);
}

export function getEventStatistics() {
  return eventInspector.getStatistics();
}

export function clearEventRecords(): void {
  eventInspector.clearRecords();
}

export function exportEventRecords(): string {
  return eventInspector.exportRecords();
}
