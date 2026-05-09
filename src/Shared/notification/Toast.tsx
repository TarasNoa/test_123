import { Component, createSignal, onMount, onCleanup } from "solid-js";
import { colors, radius, spacing, zIndex } from "../ui/tokens";

interface ToastProps {
  id: string;
  type: "success" | "error" | "warning" | "info";
  message: string;
  onClose: () => void;
  duration?: number;
}

/**
 * Toast Component
 * 
 * Individual toast notification with:
 * - Type-based styling (success, error, warning, info)
 * - Auto-dismiss after duration
 * - Smooth enter/exit animations
 * - Close button
 */
export const Toast: Component<ToastProps> = (props) => {
  const [isVisible, setIsVisible] = createSignal(false);
  const [isExiting, setIsExiting] = createSignal(false);

  const getTypeColor = () => {
    switch (props.type) {
      case "success":
        return colors.success;
      case "error":
        return colors.error;
      case "warning":
        return colors.warning;
      case "info":
        return colors.info;
      default:
        return colors.info;
    }
  };

  const getTypeIcon = () => {
    switch (props.type) {
      case "success":
        return "✓";
      case "error":
        return "✕";
      case "warning":
        return "⚠";
      case "info":
        return "ℹ";
      default:
        return "ℹ";
    }
  };

  const handleClose = () => {
    setIsExiting(true);
    setTimeout(() => {
      props.onClose();
    }, 200);
  };

  onMount(() => {
    // Enter animation
    requestAnimationFrame(() => {
      setIsVisible(true);
    });

    // Auto-dismiss
    if (props.duration !== 0) {
      const duration = props.duration || 5000;
      setTimeout(handleClose, duration);
    }
  });

  return (
    <div
      class="flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg transition-all duration-200"
      style={{
        "background-color": "#0F131A",
        border: `1px solid ${colors.border}`,
        "border-radius": radius.md,
        "border-left": `4px solid ${getTypeColor()}`,
        opacity: isVisible() && !isExiting() ? 1 : 0,
        transform: isVisible() && !isExiting() ? "translateX(0)" : "translateX(100%)",
      }}
    >
      <div
        class="w-6 h-6 rounded-full flex items-center justify-center text-sm"
        style={{
          "background-color": "rgba(53, 224, 208, 0.12)",
          color: getTypeColor(),
        }}
      >
        {getTypeIcon()}
      </div>
      <span class="text-sm flex-1" style={{ color: colors.text }}>
        {props.message}
      </span>
      <button
        onClick={handleClose}
        class="text-xs hover:opacity-70 transition-opacity"
        style={{ color: colors.textMuted }}
      >
        ✕
      </button>
    </div>
  );
};

interface ToastContainerProps {
  toasts: Array<{
    id: string;
    type: "success" | "error" | "warning" | "info";
    message: string;
    timestamp: Date;
  }>;
  onRemove: (id: string) => void;
}

/**
 * Toast Container
 * 
 * Container for toast notifications with:
 * - Fixed position (top-right)
 * - Stacking order
 * - Max width constraints
 * - Smooth animations
 */
export const ToastContainer: Component<ToastContainerProps> = (props) => {
  return (
    <div
      class="fixed top-4 right-4 flex flex-col gap-2 max-w-sm w-full pointer-events-none"
      style={{ "z-index": zIndex.modal }}
    >
      {props.toasts.map((toast) => (
        <div class="pointer-events-auto">
          <Toast
            id={toast.id}
            type={toast.type}
            message={toast.message}
            onClose={() => props.onRemove(toast.id)}
          />
        </div>
      ))}
    </div>
  );
};
