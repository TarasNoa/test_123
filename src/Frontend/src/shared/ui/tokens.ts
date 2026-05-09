/**
 * Design System Tokens
 * 
 * Centralized design tokens for consistent UI across the application.
 * These values should be used throughout the application to maintain visual consistency.
 */

export const colors = {
  // Background colors
  bg: "#07090D",
  surface: "#0F131A",
  surface2: "#141922",
  surface3: "#1D2430",

  // Text colors
  text: "#F5F7FA",
  textMuted: "#98A2B3",
  textSecondary: "#6B7280",

  // Accent colors
  turquoise: "#35E0D0",
  turquoiseDark: "#2BC4B5",
  turquoiseLight: "rgba(53, 224, 208, 0.12)",

  // Secondary accent
  purple: "#9B7CFF",
  purpleDark: "#8B6CEF",
  purpleLight: "rgba(155, 124, 255, 0.12)",

  // Status colors
  success: "#22C55E",
  warning: "#F59E0B",
  error: "#EF4444",
  info: "#3B82F6",

  // Border colors
  border: "#1D2430",
  borderLight: "#2D3748",
  borderAccent: "#35E0D0",

  // Interactive states
  hover: "rgba(53, 224, 208, 0.08)",
  active: "rgba(53, 224, 208, 0.15)",
  focus: "rgba(53, 224, 208, 0.25)",
} as const;

export const spacing = {
  xs: "4px",
  sm: "8px",
  md: "12px",
  lg: "16px",
  xl: "24px",
  "2xl": "32px",
  "3xl": "48px",
  "4xl": "64px",
} as const;

export const radius = {
  sm: "8px",
  md: "12px",
  lg: "16px",
  xl: "20px",
  full: "9999px",
} as const;

export const shadows = {
  sm: "0 1px 2px rgba(0, 0, 0, 0.3)",
  md: "0 4px 6px rgba(0, 0, 0, 0.4)",
  lg: "0 10px 15px rgba(0, 0, 0, 0.5)",
  xl: "0 20px 25px rgba(0, 0, 0, 0.6)",
  glow: "0 0 20px rgba(53, 224, 208, 0.3)",
  glowPurple: "0 0 20px rgba(155, 124, 255, 0.3)",
} as const;

export const typography = {
  fontFamily: {
    sans: "Inter, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    mono: "'JetBrains Mono', 'Fira Code', monospace",
  },
  fontSize: {
    xs: "12px",
    sm: "14px",
    base: "16px",
    lg: "18px",
    xl: "20px",
    "2xl": "24px",
    "3xl": "30px",
    "4xl": "36px",
  },
  fontWeight: {
    normal: "400",
    medium: "500",
    semibold: "600",
    bold: "700",
  },
  lineHeight: {
    tight: "1.25",
    normal: "1.5",
    relaxed: "1.75",
  },
} as const;

export const transitions = {
  fast: "0.15s ease",
  normal: "0.2s ease",
  slow: "0.3s ease",
} as const;

export const zIndex = {
  base: 1,
  dropdown: 10,
  sticky: 20,
  fixed: 30,
  modal: 40,
  popover: 50,
  tooltip: 60,
} as const;

export const breakpoints = {
  sm: "640px",
  md: "768px",
  lg: "1024px",
  xl: "1280px",
  "2xl": "1536px",
} as const;

export const layout = {
  // Sidebar
  sidebarCollapsed: "72px",
  sidebarExpanded: "240px",
  sidebarTransition: "0.2s ease",

  // Panel
  panelMin: "280px",
  panelMax: "400px",
  panelTransition: "0.2s ease",

  // Header
  headerHeight: "64px",

  // Content
  contentMax: "1400px",
} as const;
