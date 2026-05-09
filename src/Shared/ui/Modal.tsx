import { Component, createSignal } from "solid-js";
import { colors, radius, spacing, zIndex } from "./tokens";

interface ModalProps {
  isOpen?: boolean;
  onClose?: () => void;
  title?: string;
  children?: any;
}

/**
 * Modal Component
 * 
 * Overlay modal with:
 * - Dark overlay background
 * - Centered content
 * - Close on backdrop click
 * - z-index: 40
 */
export const Modal: Component<ModalProps> = (props) => {
  const [isOpen, setIsOpen] = createSignal(props.isOpen || false);

  const handleClose = () => {
    setIsOpen(false);
    props.onClose?.();
  };

  if (!isOpen()) return null;

  return (
    <div
      class="fixed inset-0 flex items-center justify-center"
      style={{
        "background-color": "rgba(0, 0, 0, 0.7)",
        "z-index": zIndex.modal,
      }}
      onClick={handleClose}
    >
      <div
        class="rounded-lg"
        style={{
          "background-color": colors.surface,
          border: `1px solid ${colors.border}`,
          "border-radius": radius.lg,
          "max-width": "500px",
          "width": "90%",
          padding: spacing.xl,
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {props.title && (
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-lg font-semibold" style={{ color: colors.text }}>
              {props.title}
            </h2>
            <button
              onClick={handleClose}
              class="text-sm"
              style={{ color: colors.textMuted }}
            >
              ✕
            </button>
          </div>
        )}
        {props.children}
      </div>
    </div>
  );
};
