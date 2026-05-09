import { Component, createSignal, onMount, onCleanup } from "solid-js";
import { colors, radius, spacing } from "../shared/ui/tokens";
import { globalEventStream } from "../shared/activity/WorkspaceEvent";

interface TimelineEvent {
  id: string;
  timestamp: Date;
  type: "ai_action" | "agent_task" | "build" | "deployment" | "collaboration";
  icon: string;
  title: string;
  description: string;
  actor?: string;
  status?: "success" | "error" | "pending";
}

interface WorkspaceTimelineProps {
  projectId: string;
}

/**
 * Workspace Timeline
 * 
 * Cinematic execution timeline with:
 * - Live updates via event stream
 * - Animated event entries
 * - Type-based styling
 * - Actor attribution
 * - Status indicators
 * - Smooth transitions
 * 
 * This feels like a runtime, not just an activity feed
 */
export const WorkspaceTimeline: Component<WorkspaceTimelineProps> = (props) => {
  const [events, setEvents] = createSignal<TimelineEvent[]>([]);
  const [highlightedEvent, setHighlightedEvent] = createSignal<string | null>(null);

  const getEventTypeColor = (type: TimelineEvent["type"]): string => {
    switch (type) {
      case "ai_action":
        return colors.turquoise;
      case "agent_task":
        return colors.info;
      case "build":
        return colors.warning;
      case "deployment":
        return colors.success;
      case "collaboration":
        return colors.purple;
      default:
        return colors.textMuted;
    }
  };

  const formatTime = (date: Date): string => {
    const hours = date.getHours().toString().padStart(2, "0");
    const minutes = date.getMinutes().toString().padStart(2, "0");
    return `${hours}:${minutes}`;
  };

  const formatRelativeTime = (date: Date): string => {
    const now = new Date();
    const diff = now.getTime() - date.getTime();
    const seconds = Math.floor(diff / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (seconds < 60) return "just now";
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    return formatTime(date);
  };

  const addEvent = (event: TimelineEvent): void => {
    setEvents(prev => [event, ...prev].slice(0, 50)); // Keep last 50 events
    
    // Highlight new event briefly
    setHighlightedEvent(event.id);
    setTimeout(() => setHighlightedEvent(null), 2000);
  };

  onMount(() => {
    // Add some initial events for demo
    const now = new Date();
    const initialEvents: TimelineEvent[] = [
      {
        id: "1",
        timestamp: new Date(now.getTime() - 60000),
        type: "ai_action",
        icon: "🤖",
        title: "AI created architecture",
        description: "Generated project architecture with 3 phases",
        actor: "AI System",
        status: "success",
      },
      {
        id: "2",
        timestamp: new Date(now.getTime() - 120000),
        type: "agent_task",
        icon: "⚡",
        title: "Backend tasks generated",
        description: "12 tasks created for API development",
        actor: "Architecture Agent",
        status: "success",
      },
      {
        id: "3",
        timestamp: new Date(now.getTime() - 300000),
        type: "collaboration",
        icon: "👤",
        title: "Maria accepted API task",
        description: "Maria assigned to authentication endpoint",
        actor: "Maria",
        status: "success",
      },
      {
        id: "4",
        timestamp: new Date(now.getTime() - 480000),
        type: "build",
        icon: "🔨",
        title: "Build failed",
        description: "TypeScript compilation error in auth.ts",
        actor: "CI/CD",
        status: "error",
      },
      {
        id: "5",
        timestamp: new Date(now.getTime() - 540000),
        type: "ai_action",
        icon: "🤖",
        title: "AI proposed fix",
        description: "Suggested import path correction",
        actor: "AI System",
        status: "success",
      },
      {
        id: "6",
        timestamp: new Date(now.getTime() - 600000),
        type: "build",
        icon: "✅",
        title: "Build recovered",
        description: "Build successful after fix applied",
        actor: "CI/CD",
        status: "success",
      },
    ];

    setEvents(initialEvents);

    // Subscribe to workspace events
    const unsubscribe = globalEventStream.subscribe("*", (event) => {
      const timestamp = event.timestamp || new Date();
      
      // Convert workspace events to timeline events
      if (event.type === "AgentStarted") {
        addEvent({
          id: `agent-${timestamp.getTime()}`,
          timestamp,
          type: "agent_task",
          icon: "🤖",
          title: `${event.agentName} started`,
          description: `Working on task: ${event.taskId}`,
          actor: event.agentName,
          status: "pending",
        });
      } else if (event.type === "AgentCompleted") {
        addEvent({
          id: `agent-${timestamp.getTime()}`,
          timestamp,
          type: "agent_task",
          icon: "✅",
          title: `${event.agentName} completed`,
          description: event.success ? "Task completed successfully" : "Task failed",
          actor: event.agentName,
          status: event.success ? "success" : "error",
        });
      } else if (event.type === "BuildFailed") {
        addEvent({
          id: `build-${timestamp.getTime()}`,
          timestamp,
          type: "build",
          icon: "❌",
          title: "Build failed",
          description: `Error in ${event.step}: ${event.error}`,
          actor: "CI/CD",
          status: "error",
        });
      } else if (event.type === "BuildCompleted") {
        addEvent({
          id: `build-${timestamp.getTime()}`,
          timestamp,
          type: "build",
          icon: "✅",
          title: "Build completed",
          description: "Build successful",
          actor: "CI/CD",
          status: "success",
        });
      } else if (event.type === "DeploymentStarted") {
        addEvent({
          id: `deploy-${timestamp.getTime()}`,
          timestamp,
          type: "deployment",
          icon: "🚀",
          title: "Deployment started",
          description: `Deploying to ${event.environment}`,
          actor: "CI/CD",
          status: "pending",
        });
      } else if (event.type === "DeploymentCompleted") {
        addEvent({
          id: `deploy-${timestamp.getTime()}`,
          timestamp,
          type: "deployment",
          icon: "✅",
          title: "Deployment completed",
          description: `Deployed to ${event.environment}`,
          actor: "CI/CD",
          status: "success",
        });
      }
    });

    onCleanup(unsubscribe);
  });

  return (
    <div
      class="h-full overflow-auto"
      style={{
        "background-color": colors.surface,
        "border-radius": radius.lg,
        border: `1px solid ${colors.border}`,
      }}
    >
      <div class="p-4">
        <h3 class="text-sm font-semibold mb-4" style={{ color: colors.text }}>
          Execution Timeline
        </h3>

        <div class="space-y-3">
          {events().map((event, index) => (
            <div
              class="flex gap-3 p-3 rounded-lg transition-all"
              style={{
                "background-color": highlightedEvent() === event.id ? colors.surface2 : "transparent",
                border: highlightedEvent() === event.id ? `1px solid ${colors.turquoise}` : "1px solid transparent",
                opacity: highlightedEvent() === event.id ? 1 : 0.9,
                transform: highlightedEvent() === event.id ? "scale(1.02)" : "scale(1)",
              }}
            >
              {/* Icon */}
              <div
                class="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center text-lg"
                style={{
                  "background-color": `rgba(${parseInt(getEventTypeColor(event.type).slice(1, 3), 16)}, ${parseInt(getEventTypeColor(event.type).slice(3, 5), 16)}, ${parseInt(getEventTypeColor(event.type).slice(5, 7), 16)}, 0.12)`,
                  border: `1px solid ${getEventTypeColor(event.type)}`,
                }}
              >
                {event.icon}
              </div>

              {/* Content */}
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-sm font-medium" style={{ color: colors.text }}>
                    {event.title}
                  </span>
                  {event.status === "error" && (
                    <span class="text-xs px-2 py-0.5 rounded" style={{ "background-color": "rgba(239, 68, 68, 0.12)", color: colors.error }}>
                      Failed
                    </span>
                  )}
                  {event.status === "success" && (
                    <span class="text-xs px-2 py-0.5 rounded" style={{ "background-color": "rgba(34, 197, 94, 0.12)", color: colors.success }}>
                      Success
                    </span>
                  )}
                </div>
                <p class="text-xs mb-1" style={{ color: colors.textMuted }}>
                  {event.description}
                </p>
                <div class="flex items-center gap-2">
                  <span class="text-xs" style={{ color: colors.textMuted }}>
                    {formatTime(event.timestamp)}
                  </span>
                  <span class="text-xs" style={{ color: colors.textMuted }}>
                    •
                  </span>
                  <span class="text-xs" style={{ color: colors.textMuted }}>
                    {formatRelativeTime(event.timestamp)}
                  </span>
                  {event.actor && (
                    <>
                      <span class="text-xs" style={{ color: colors.textMuted }}>
                        •
                      </span>
                      <span class="text-xs" style={{ color: colors.text }}>
                        {event.actor}
                      </span>
                    </>
                  )}
                </div>
              </div>
            </div>
          ))}

          {events().length === 0 && (
            <div class="text-center py-8">
              <p class="text-sm" style={{ color: colors.textMuted }}>
                No events yet
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
