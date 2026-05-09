import { Component, createSignal } from "solid-js";
import { colors, layout, spacing } from "../ui/tokens";
import { getIntelligenceSuggestions, type IntelligenceSuggestion } from "../../Services/intelligence/WorkspaceIntelligence";

interface AIPanelProps {
  activeReasoning?: string;
  contextMemory?: Array<{
    type: string;
    data: string;
  }>;
}

/**
 * AI Intelligence Panel
 * 
 * Right-side AI intelligence layer (NOT a chat) with sections:
 * - Active reasoning
 * - Recommendations
 * - Risks
 * - Opportunities
 * - Suggested actions
 * - Running agents
 * - Context memory
 * 
 * Width: 280px min / 400px max
 * Dark background #0F131A
 * Border-left: 1px solid #1D2430
 */
export const AIPanel: Component<AIPanelProps> = (props) => {
  const [expanded, setExpanded] = createSignal(true);
  const [activeSection, setActiveSection] = createSignal("recommendations");

  const suggestions = () => getIntelligenceSuggestions();

  const sections = [
    { id: "reasoning", label: "Reasoning", icon: "💭" },
    { id: "recommendations", label: "Recommendations", icon: "💡" },
    { id: "risks", label: "Risks", icon: "⚠️" },
    { id: "opportunities", label: "Opportunities", icon: "🎯" },
    { id: "actions", label: "Actions", icon: "⚡" },
    { id: "agents", label: "Agents", icon: "🤖" },
    { id: "context", label: "Context", icon: "🧠" },
  ];

  return (
    <div
      class="flex flex-col h-full"
      style={{
        width: expanded() ? layout.panelMax : layout.panelMin,
        "background-color": colors.surface,
        "border-left": `1px solid ${colors.border}`,
        transition: layout.panelTransition,
      }}
    >
      {/* Header */}
      <div class="px-4 py-3 border-b flex items-center justify-between" style={{ "border-color": colors.border }}>
        <div class="flex items-center gap-2">
          <div class="w-2 h-2 rounded-full" style={{ "background-color": colors.turquoise }} />
          <span class="text-sm font-semibold" style={{ color: colors.text }}>AI Intelligence</span>
        </div>
        <button
          onClick={() => setExpanded(!expanded())}
          class="text-sm"
          style={{ color: colors.textMuted }}
        >
          {expanded() ? "−" : "+"}
        </button>
      </div>

      {expanded() && (
        <>
          {/* Section Tabs */}
          <div class="flex items-center gap-1 px-2 py-2 overflow-x-auto" style={{ "border-bottom": `1px solid ${colors.border}` }}>
            {sections.map((section) => (
              <button
                onClick={() => setActiveSection(section.id)}
                class="flex items-center gap-1 px-3 py-1.5 rounded-lg text-xs whitespace-nowrap"
                style={{
                  "background-color": activeSection() === section.id ? colors.surface2 : "transparent",
                  color: activeSection() === section.id ? colors.text : colors.textMuted,
                }}
              >
                <span>{section.icon}</span>
                <span>{section.label}</span>
              </button>
            ))}
          </div>

          {/* Section Content */}
          <div class="flex-1 overflow-auto p-4">
            {activeSection() === "reasoning" && (
              <div class="space-y-3">
                {props.activeReasoning ? (
                  <div
                    class="p-3 rounded-lg"
                    style={{
                      "background-color": colors.surface2,
                      border: `1px solid ${colors.border}`,
                    }}
                  >
                    <p class="text-sm" style={{ color: colors.text }}>
                      {props.activeReasoning}
                    </p>
                  </div>
                ) : (
                  <p class="text-sm" style={{ color: colors.textMuted }}>
                    No active reasoning
                  </p>
                )}
              </div>
            )}

            {activeSection() === "recommendations" && (
              <div class="space-y-3">
                {suggestions().filter(s => s.type === "build_failure" || s.type === "performance_issue").length > 0 ? (
                  suggestions()
                    .filter(s => s.type === "build_failure" || s.type === "performance_issue")
                    .map((suggestion) => (
                      <div
                        class="p-3 rounded-lg"
                        style={{
                          "background-color": colors.surface2,
                          border: `1px solid ${suggestion.severity === "critical" ? colors.error : colors.border}`,
                        }}
                      >
                        <div class="flex items-center gap-2 mb-2">
                          <span class="text-sm font-semibold" style={{ color: colors.text }}>
                            {suggestion.title}
                          </span>
                          <span
                            class="text-xs px-2 py-0.5 rounded"
                            style={{
                              "background-color": suggestion.severity === "critical" ? "rgba(239, 68, 68, 0.12)" : "rgba(53, 224, 208, 0.12)",
                              color: suggestion.severity === "critical" ? colors.error : colors.turquoise,
                            }}
                          >
                            {suggestion.severity}
                          </span>
                        </div>
                        <p class="text-xs mb-2" style={{ color: colors.textMuted }}>
                          {suggestion.description}
                        </p>
                        <div class="flex flex-wrap gap-2">
                          {suggestion.suggestedActions.map((action) => (
                            <button
                              onClick={() => action.action()}
                              class="text-xs px-2 py-1 rounded"
                              style={{
                                "background-color": colors.turquoise,
                                color: colors.bg,
                              }}
                            >
                              {action.label}
                            </button>
                          ))}
                        </div>
                      </div>
                    ))
                ) : (
                  <p class="text-sm" style={{ color: colors.textMuted }}>
                    No recommendations
                  </p>
                )}
              </div>
            )}

            {activeSection() === "risks" && (
              <div class="space-y-3">
                {suggestions().filter(s => s.type === "deadline_risk" || s.type === "security_issue").length > 0 ? (
                  suggestions()
                    .filter(s => s.type === "deadline_risk" || s.type === "security_issue")
                    .map((suggestion) => (
                      <div
                        class="p-3 rounded-lg"
                        style={{
                          "background-color": colors.surface2,
                          border: `1px solid ${suggestion.severity === "critical" ? colors.error : colors.warning}`,
                        }}
                      >
                        <div class="flex items-center gap-2 mb-2">
                          <span class="text-sm font-semibold" style={{ color: colors.text }}>
                            {suggestion.title}
                          </span>
                          <span
                            class="text-xs px-2 py-0.5 rounded"
                            style={{
                              "background-color": suggestion.severity === "critical" ? "rgba(239, 68, 68, 0.12)" : "rgba(251, 191, 36, 0.12)",
                              color: suggestion.severity === "critical" ? colors.error : colors.warning,
                            }}
                          >
                            {suggestion.severity}
                          </span>
                        </div>
                        <p class="text-xs mb-2" style={{ color: colors.textMuted }}>
                          {suggestion.description}
                        </p>
                        <div class="flex flex-wrap gap-2">
                          {suggestion.suggestedActions.map((action) => (
                            <button
                              onClick={() => action.action()}
                              class="text-xs px-2 py-1 rounded"
                              style={{
                                "background-color": colors.warning,
                                color: colors.bg,
                              }}
                            >
                              {action.label}
                            </button>
                          ))}
                        </div>
                      </div>
                    ))
                ) : (
                  <p class="text-sm" style={{ color: colors.textMuted }}>
                    No risks detected
                  </p>
                )}
              </div>
            )}

            {activeSection() === "opportunities" && (
              <div class="space-y-3">
                {suggestions().filter(s => s.type === "freelancer_idle").length > 0 ? (
                  suggestions()
                    .filter(s => s.type === "freelancer_idle")
                    .map((suggestion) => (
                      <div
                        class="p-3 rounded-lg"
                        style={{
                          "background-color": colors.surface2,
                          border: `1px solid ${colors.success}`,
                        }}
                      >
                        <div class="flex items-center gap-2 mb-2">
                          <span class="text-sm font-semibold" style={{ color: colors.text }}>
                            {suggestion.title}
                          </span>
                        </div>
                        <p class="text-xs mb-2" style={{ color: colors.textMuted }}>
                          {suggestion.description}
                        </p>
                        <div class="flex flex-wrap gap-2">
                          {suggestion.suggestedActions.map((action) => (
                            <button
                              onClick={() => action.action()}
                              class="text-xs px-2 py-1 rounded"
                              style={{
                                "background-color": colors.success,
                                color: colors.bg,
                              }}
                            >
                              {action.label}
                            </button>
                          ))}
                        </div>
                      </div>
                    ))
                ) : (
                  <p class="text-sm" style={{ color: colors.textMuted }}>
                    No opportunities detected
                  </p>
                )}
              </div>
            )}

            {activeSection() === "actions" && (
              <div class="space-y-3">
                {suggestions().length > 0 ? (
                  suggestions().map((suggestion) => (
                    <div
                      class="p-3 rounded-lg"
                      style={{
                        "background-color": colors.surface2,
                        border: `1px solid ${colors.border}`,
                      }}
                    >
                      <p class="text-xs mb-2" style={{ color: colors.text }}>
                        {suggestion.description}
                      </p>
                      <div class="flex flex-wrap gap-2">
                        {suggestion.suggestedActions.map((action) => (
                          <button
                            onClick={() => action.action()}
                            class="text-xs px-2 py-1 rounded"
                            style={{
                              "background-color": colors.turquoise,
                              color: colors.bg,
                            }}
                          >
                            {action.label}
                          </button>
                        ))}
                      </div>
                    </div>
                  ))
                ) : (
                  <p class="text-sm" style={{ color: colors.textMuted }}>
                    No suggested actions
                  </p>
                )}
              </div>
            )}

            {activeSection() === "agents" && (
              <div class="space-y-3">
                <p class="text-sm" style={{ color: colors.textMuted }}>
                  Running agents
                </p>
              </div>
            )}

            {activeSection() === "context" && (
              <div class="space-y-3">
                {props.contextMemory && props.contextMemory.length > 0 ? (
                  props.contextMemory.map((item) => (
                    <div
                      class="p-3 rounded-lg"
                      style={{
                        "background-color": colors.surface2,
                        border: `1px solid ${colors.border}`,
                      }}
                    >
                      <div class="flex items-center gap-2 mb-2">
                        <span class="text-xs font-semibold" style={{ color: colors.textMuted }}>
                          {item.type}
                        </span>
                      </div>
                      <p class="text-xs" style={{ color: colors.text }}>
                        {item.data}
                      </p>
                    </div>
                  ))
                ) : (
                  <p class="text-sm" style={{ color: colors.textMuted }}>
                    No context memory
                  </p>
                )}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};
