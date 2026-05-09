import { Component } from "solid-js";
import { colors, radius, spacing } from "./tokens";

interface InputProps {
  type?: "text" | "email" | "password" | "number";
  placeholder?: string;
  value?: string;
  onInput?: (value: string) => void;
  disabled?: boolean;
  error?: boolean;
}

/**
 * Input Component
 * 
 * Text input with:
 * - Dark background #0F131A
 * - Border 1px solid #1D2430
 * - Focus border color #35E0D0
 * - Error state with red border
 */
export const Input: Component<InputProps> = (props) => {
  return (
    <input
      type={props.type || "text"}
      placeholder={props.placeholder}
      value={props.value}
      onInput={(e) => props.onInput?.(e.currentTarget.value)}
      disabled={props.disabled}
      class="w-full px-4 py-2 rounded-lg outline-none transition-all"
      style={{
        "background-color": colors.surface2,
        border: props.error ? `1px solid ${colors.error}` : `1px solid ${colors.border}`,
        color: colors.text,
        "border-radius": radius.md,
        "font-size": "14px",
      }}
      classList={{
        "opacity-50 cursor-not-allowed": props.disabled,
      }}
    />
  );
};
