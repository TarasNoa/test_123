import { Component } from "solid-js";
import { colors, radius, spacing, transitions } from "./tokens";

interface ButtonProps {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "sm" | "md" | "lg";
  disabled?: boolean;
  onClick?: () => void;
  children: any;
}

/**
 * Button Component
 * 
 * Primary button with variants:
 * - primary: turquoise background, dark text
 * - secondary: surface2 background, text
 * - ghost: transparent background, text
 * - danger: error background, white text
 */
export const Button: Component<ButtonProps> = (props) => {
  const getVariantStyles = () => {
    switch (props.variant) {
      case "primary":
        return {
          "background-color": colors.turquoise,
          color: colors.bg,
          border: "none",
        };
      case "secondary":
        return {
          "background-color": colors.surface2,
          color: colors.text,
          border: `1px solid ${colors.border}`,
        };
      case "ghost":
        return {
          "background-color": "transparent",
          color: colors.text,
          border: "none",
        };
      case "danger":
        return {
          "background-color": colors.error,
          color: "#FFFFFF",
          border: "none",
        };
      default:
        return {
          "background-color": colors.turquoise,
          color: colors.bg,
          border: "none",
        };
    }
  };

  const getSizeStyles = () => {
    switch (props.size) {
      case "sm":
        return {
          padding: `${spacing.sm} ${spacing.lg}`,
          "font-size": "14px",
        };
      case "lg":
        return {
          padding: `${spacing.lg} ${spacing.xl}`,
          "font-size": "16px",
        };
      default:
        return {
          padding: `${spacing.md} ${spacing.xl}`,
          "font-size": "14px",
        };
    }
  };

  return (
    <button
      onClick={props.onClick}
      disabled={props.disabled}
      class="font-medium rounded-lg transition-all cursor-pointer hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed"
      style={{
        ...getVariantStyles(),
        ...getSizeStyles(),
        "border-radius": radius.md,
        transition: transitions.normal,
        opacity: props.disabled ? 0.5 : 1,
      }}
    >
      {props.children}
    </button>
  );
};
