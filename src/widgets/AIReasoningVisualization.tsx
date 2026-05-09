import { Component, createSignal, onMount, onCleanup } from "solid-js";
import { colors, radius, spacing } from "../shared/ui/tokens";
import { globalEventStream } from "../shared/activity/WorkspaceEvent";

interface ReasoningStep {
  id: string;
  step: number;
  description: string;
  status: "pending" | "in_progress" | "completed" | "error";
  duration?: number;
  result?: string;
  timestamp: Date;
}

interface AIReasoningVisualizationProps {
  agentId: string;
}

/**
 * AI Reasoning Visualization
 * 
 * Shows step-by-step AI thinking process:
 * - Transparent reasoning steps
 * - Progress tracking
 * - Status indicators
 * - Duration measurement
 * - Results display
 * 
 * This radically improves trust by making AI reasoning transparent
 */
export const AIReasoningVisualization: Component<AIReasoningVisualizationProps> = (props) => {
  const [steps, setSteps] = createSignal<ReasoningStep[]>([]);
  const [currentStep, setCurrentStep] = createSignal(0);

  const addStep = (description: string): void => {
    const newStep: ReasoningStep = {
      id: `step-${Date.now()}`,
      step: steps().length + 1,
      description,
      status: "in_progress",
      timestamp: new Date(),
    };
    setSteps(prev => [...prev, newStep]);
    setCurrentStep(steps().length);
  };

  const updateStep = (stepId: string, status: ReasoningStep["status"], result?: string): void => {
    setSteps(prev =>
      prev.map(step =>
        step.id === stepId
          ? {
              ...step,
              status,
              result,
              duration: Date.now() - step.timestamp.getTime(),
            }
          : step
      )
    );
  };

  const simulateReasoning = (): void => {
    // Simulate AI reasoning process
    const reasoningSteps = [
      "Analyzing dependencies...",
      "Matching freelancer skills...",
      "Estimating delivery risk...",
      "Building execution graph...",
      "Validating constraints...",
      "Generating recommendations...",
    ];

    let stepIndex = 0;

    const runStep = () => {
      if (stepIndex < reasoningSteps.length) {
        const stepId = addStep(reasoningSteps[stepIndex]);

        // Simulate step completion
        setTimeout(() => {
          updateStep(stepId, "completed", "Step completed successfully");
          stepIndex++;
          runStep();
        }, 800 + Math.random() * 1200);
      }
    };

    runStep();
  };

  onMount(() => {
    // Subscribe to agent thinking events
    const unsubscribe = globalEventStream.subscribe("AgentThinking", (event) => {
      if (event.type === "AgentThinking" && event.agentId === props.agentId) {
        addStep(event.thought);
      }
    });

    // Start simulation for demo
    simulateReasoning();

    onCleanup(unsubscribe);
  });

  const getStepIcon = (status: ReasoningStep["status"]): string => {
    switch (status) {
      case "pending":
        return "○";
      case "in_progress":
        return "◐";
      case "completed":
        return "✓";
      case "error":
        return "✕";
      default:
        return "○";
    }
  };

  const getStepColor = (status: ReasoningStep["status"]): string => {
    switch (status) {
      case "pending":
        return colors.textMuted;
      case "in_progress":
        return colors.turquoise;
      case "completed":
        return colors.success;
      case "error":
        return colors.error;
      default:
        return colors.textMuted;
    }
  };

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
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-sm font-semibold" style={{ color: colors.text }}>
            AI Reasoning Process
          </h3>
          <button
            onClick={simulateReasoning}
            class="text-xs px-3 py-1.5 rounded-lg"
            style={{
              "background-color": colors.surface2,
              border: `1px solid ${colors.border}`,
              color: colors.text,
            }}
          >
            Rerun
          </button>
        </div>

        <div class="space-y-2">
          {steps().map((step, index) => (
            <div
              class="flex gap-3 p-3 rounded-lg transition-all"
              style={{
                "background-color": step.status === "in_progress" ? colors.surface2 : "transparent",
                border: step.status === "in_progress" ? `1px solid ${colors.turquoise}` : "1px solid transparent",
              }}
            >
              {/* Step Number */}
              <div
                class="flex-shrink-0 w-6 h-6 rounded-full flex items-center justify-center text-xs"
                style={{
                  "background-color": `rgba(${parseInt(getStepColor(step.status).slice(1, 3), 16)}, ${parseInt(getStepColor(step.status).slice(3, 5), 16)}, ${parseInt(getStepColor(step.status).slice(5, 7), 16)}, 0.12)`,
                  color: getStepColor(step.status),
                  border: `1px solid ${getStepColor(step.status)}`,
                }}
              >
                {getStepIcon(step.status)}
              </div>

              {/* Content */}
              <div class="flex-1">
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-xs font-medium" style={{ color: colors.text }}>
                    {step.description}
                  </span>
                  {step.status === "in_progress" && (
                    <span
                      class="text-xs px-2 py-0.5 rounded"
                      style={{
                        "background-color": "rgba(53, 224, 208, 0.12)",
                        color: colors.turquoise,
                      }}
                    >
                      Running
                    </span>
                  )}
                  {step.status === "completed" && step.duration && (
                    <span class="text-xs" style={{ color: colors.textMuted }}>
                      {Math.round(step.duration / 100)} / 10s
                    </span>
                  )}
                </div>
                {step.result && (
                  <p class="text-xs" style={{ color: colors.textMuted }}>
                    {step.result}
                  </p>
                )}
              </div>
            </div>
          ))}

          {steps().length === 0 && (
            <div class="text-center py-8">
              <p class="text-sm" style={{ color: colors.textMuted }}>
                No reasoning steps yet
              </p>
            </div>
          )}
        </div>

        {/* Progress Bar */}
        {steps().length > 0 && (
          <div class="mt-4 pt-4" style={{ "border-top": `1px solid ${colors.border}` }}>
            <div class="flex items-center justify-between mb-2">
              <span class="text-xs" style={{ color: colors.textMuted }}>
                Progress
              </span>
              <span class="text-xs" style={{ color: colors.text }}>
                {steps().filter(s => s.status === "completed").length} / {steps().length}
              </span>
            </div>
            <div
              class="h-1.5 rounded-full"
              style={{
                "background-color": colors.surface2,
                overflow: "hidden",
              }}
            >
              <div
                class="h-full rounded-full transition-all duration-500"
                style={{
                  width: `${(steps().filter(s => s.status === "completed").length / steps().length) * 100}%`,
                  "background-color": colors.turquoise,
                }}
              />
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
