import { Component, createSignal } from "solid-js";
import { colors, radius, spacing, zIndex } from "../ui/tokens";
import { getActionsForContext, executeAction, type AIAction, type ContextType } from "../../services/ai/ContextualActions";

interface ContextualActionsMenuProps {
  contextType: ContextType;
  context: unknown;
  isOpen: boolean;
  onClose: () => void;
  position?: { x: number; y: number };
}

/**
 * Contextual Actions Menu Component
 * 
 * Dumb renderer for contextual AI actions.
 * Business logic in services/ai/ContextualActions.ts
 * 
 * Renders AI actions embedded into every surface:
 * - Tasks: Analyze, Split, Estimate, Find freelancers, Generate roadmap
 * - Code: Refactor, Explain, Generate tests, Optimize, Document, Find bugs
 * - Projects: Build execution graph, Estimate completion, Generate hiring plan, Analyze architecture
 */
export const ContextualActionsMenu: Component<ContextualActionsMenuProps> = (props) => {
  const [isExecuting, setIsExecuting] = createSignal<string | null>(null);

  const actions = () => getActionsForContext(props.contextType);

  const handleActionClick = async (action: AIAction) => {
    if (action.requiresContext && !props.context) {
      console.warn("Action requires context but none provided");
      return;
    }

    setIsExecuting(action.id);
    try {
      await executeAction(action.id, props.context);
      props.onClose();
    } catch (error) {
      console.error("Action execution failed:", error);
    } finally {
      setIsExecuting(null);
    }
  };

  const getCategoryIcon = () => {
    return {
      analyze: "🔍",
      generate: "✨",
      refactor: "🔧",
      optimize: "⚡",
      explain: "💡",
      test: "🧪",
      document: "📝",
    };
  };

  const groupedActions = () => {
    const groups: Record<string, AIAction[]> = {};
    actions().forEach(action => {
      if (!groups[action.category]) {
        groups[action.category] = [];
      }
      groups[action.category].push(action);
    });
    return groups;
  };

  if (!props.isOpen) return null;

  return (
    <div
      class="fixed z-50 w-64 rounded-lg shadow-2xl"
      style={{
        "background-color": "#0F131A",
        border: `1px solid ${colors.border}`,
        "border-radius": radius.lg,
        "z-index": zIndex.dropdown,
        ...(props.position ? { left: `${props.position.x}px`, top: `${props.position.y}px` } : {}),
      }}
    >
      <div class="p-2">
        {Object.entries(groupedActions()).map(([category, categoryActions]) => (
          <div class="mb-2 last:mb-0">
            <div class="flex items-center gap-2 px-2 py-1 mb-1">
              <span class="text-sm">{getCategoryIcon()[category as keyof typeof getCategoryIcon()]}</span>
              <span class="text-xs font-semibold uppercase" style={{ color: colors.textMuted }}>
                {category}
              </span>
            </div>
            <div>
              {categoryActions.map(action => (
                <button
                  onClick={() => handleActionClick(action)}
                  disabled={isExecuting() === action.id}
                  class="w-full flex items-center gap-3 px-2 py-2 rounded-lg transition-all text-left"
                  style={{
                    "background-color": isExecuting() === action.id ? colors.surface2 : "transparent",
                    border: isExecuting() === action.id ? `1px solid ${colors.focus}` : "1px solid transparent",
                    opacity: isExecuting() === action.id ? 0.7 : 1,
                  }}
                >
                  <span class="text-sm">{action.icon}</span>
                  <div class="flex-1">
                    <div class="text-sm" style={{ color: colors.text }}>
                      {action.label}
                    </div>
                    {action.description && (
                      <div class="text-xs" style={{ color: colors.textMuted }}>
                        {action.description}
                      </div>
                    )}
                  </div>
                  {action.shortcut && (
                    <span
                      class="text-xs px-2 py-1 rounded font-mono"
                      style={{
                        "background-color": colors.surface2,
                        color: colors.textMuted,
                      }}
                    >
                      {action.shortcut}
                    </span>
                  )}
                </button>
              ))}
            </div>
          </div>
        ))}
        
        {actions().length === 0 && (
          <div class="text-center py-4">
            <p class="text-sm" style={{ color: colors.textMuted }}>
              No actions available
            </p>
          </div>
        )}
      </div>
    </div>
  );
};
