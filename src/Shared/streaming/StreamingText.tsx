import { Component, createSignal, onMount, onCleanup } from "solid-js";
import { colors } from "../ui/tokens";

interface StreamingTextProps {
  text: string;
  speed?: number;
  onComplete?: () => void;
}

/**
 * Streaming Text Component
 * 
 * Text that streams character by character with:
 * - Configurable typing speed
 * - Smooth animation
 * - Completion callback
 * - Cursor indicator
 */
export const StreamingText: Component<StreamingTextProps> = (props) => {
  const [displayedText, setDisplayedText] = createSignal("");
  const [isComplete, setIsComplete] = createSignal(false);
  const speed = props.speed || 30;

  let intervalId: number | undefined;

  onMount(() => {
    let index = 0;
    intervalId = setInterval(() => {
      if (index < props.text.length) {
        setDisplayedText(props.text.slice(0, index + 1));
        index++;
      } else {
        setIsComplete(true);
        clearInterval(intervalId);
        props.onComplete?.();
      }
    }, speed);
  });

  onCleanup(() => {
    if (intervalId) {
      clearInterval(intervalId);
    }
  });

  return (
    <span style={{ color: colors.text }}>
      {displayedText()}
      {!isComplete() && (
        <span
          class="inline-block w-2 h-4 ml-1"
          style={{
            "background-color": colors.turquoise,
            animation: "blink 1s step-end infinite",
          }}
        />
      )}
      <style>{`
        @keyframes blink {
          0%, 100% { opacity: 1; }
          50% { opacity: 0; }
        }
      `}</style>
    </span>
  );
};
