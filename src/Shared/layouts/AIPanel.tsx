import { Component, createSignal } from "solid-js";
import { colors, layout } from "../ui/tokens";

interface AIMessage {
  id: string;
  type: "thought" | "task" | "analysis" | "suggestion" | "execution";
  content: string;
  timestamp: Date;
}

interface AIPanelProps {
  messages?: AIMessage[];
  onMessageSend?: (message: string) => void;
}

/**
 * AI Panel
 * 
 * Right-side AI orchestration panel with:
 * - Width: 280px min / 400px max
 * - Dark background #0F131A
 * - Border-left: 1px solid #1D2430
 * - Thoughts, tasks, analysis, suggestions, execution graph
 */
export const AIPanel: Component<AIPanelProps> = (props) => {
  const [expanded, setExpanded] = createSignal(true);
  const [inputValue, setInputValue] = createSignal("");

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
          <span class="text-sm font-semibold" style={{ color: colors.text }}>AI Agent</span>
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
          {/* Messages List */}
          <div class="flex-1 overflow-auto p-4 space-y-3">
            {props.messages?.map((msg) => (
              <div
                class="p-3 rounded-lg"
                style={{
                  "background-color": colors.surface2,
                  border: `1px solid ${colors.border}`,
                }}
              >
                <div class="flex items-center gap-2 mb-2">
                  <span
                    class="text-xs font-medium px-2 py-1 rounded"
                    style={{
                      "background-color": colors.turquoiseLight,
                      color: colors.turquoise,
                    }}
                  >
                    {msg.type}
                  </span>
                  <span class="text-xs" style={{ color: colors.textMuted }}>
                    {msg.timestamp.toLocaleTimeString()}
                  </span>
                </div>
                <p class="text-sm" style={{ color: colors.text }}>
                  {msg.content}
                </p>
              </div>
            ))}
          </div>

          {/* Input Area */}
          <div class="p-4 border-t" style={{ "border-color": colors.border }}>
            <div class="flex gap-2">
              <input
                type="text"
                value={inputValue()}
                onInput={(e) => setInputValue(e.currentTarget.value)}
                placeholder="Ask AI..."
                class="flex-1 px-3 py-2 rounded-lg text-sm"
                style={{
                  "background-color": colors.surface2,
                  border: `1px solid ${colors.border}`,
                  color: colors.text,
                }}
              />
              <button
                onClick={() => {
                  if (inputValue()) {
                    props.onMessageSend?.(inputValue());
                    setInputValue("");
                  }
                }}
                class="px-4 py-2 rounded-lg text-sm font-medium"
                style={{
                  "background-color": colors.turquoise,
                  color: colors.bg,
                }}
              >
                Send
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
};
