import { Component, createSignal, onMount, onCleanup } from "solid-js";
import { colors } from "../ui/tokens";

interface ResizablePanelProps {
  direction: "horizontal" | "vertical";
  defaultSize: number;
  minSize?: number;
  maxSize?: number;
  children: any;
  onResize?: (size: number) => void;
}

/**
 * Resizable Panel Component
 * 
 * Panel with resize functionality for:
 * - Horizontal or vertical resizing
 * - Min/max size constraints
 * - Resize callback
 * - Smooth drag interaction
 */
export const ResizablePanel: Component<ResizablePanelProps> = (props) => {
  const [size, setSize] = createSignal(props.defaultSize);
  const [isResizing, setIsResizing] = createSignal(false);

  const minSize = props.minSize || 100;
  const maxSize = props.maxSize || 800;

  const handleMouseDown = (e: MouseEvent) => {
    e.preventDefault();
    setIsResizing(true);
    
    const handleMouseMove = (e: MouseEvent) => {
      let newSize: number;
      
      if (props.direction === "horizontal") {
        newSize = e.clientX;
      } else {
        newSize = e.clientY;
      }

      newSize = Math.max(minSize, Math.min(maxSize, newSize));
      setSize(newSize);
      props.onResize?.(newSize);
    };

    const handleMouseUp = () => {
      setIsResizing(false);
      window.removeEventListener("mousemove", handleMouseMove);
      window.removeEventListener("mouseup", handleMouseUp);
    };

    window.addEventListener("mousemove", handleMouseMove);
    window.addEventListener("mouseup", handleMouseUp);
  };

  const panelStyle = () => {
    if (props.direction === "horizontal") {
      return {
        width: `${size()}px`,
        height: "100%",
      };
    } else {
      return {
        width: "100%",
        height: `${size()}px`,
      };
    }
  };

  const resizeHandleStyle = () => {
    const baseStyle = {
      "background-color": "transparent",
      cursor: props.direction === "horizontal" ? "col-resize" : "row-resize",
      transition: "background-color 0.15s",
    };

    if (isResizing()) {
      return {
        ...baseStyle,
        "background-color": colors.turquoise,
      };
    }

    return baseStyle;
  };

  return (
    <div
      class="flex relative"
      style={{
        ...panelStyle(),
        "min-width": `${minSize}px`,
        "max-width": `${maxSize}px`,
        "min-height": `${minSize}px`,
        "max-height": `${maxSize}px`,
      }}
    >
      {props.children}
      <div
        class="absolute"
        style={{
          ...(props.direction === "horizontal"
            ? { right: 0, top: 0, bottom: 0, width: "4px" }
            : { bottom: 0, left: 0, right: 0, height: "4px" }
          ),
          ...resizeHandleStyle(),
        }}
        onMouseDown={handleMouseDown}
      />
    </div>
  );
};

interface SplitViewProps {
  direction: "horizontal" | "vertical";
  firstPanel: {
    defaultSize: number;
    minSize?: number;
    maxSize?: number;
    children: any;
  };
  secondPanel: {
    defaultSize: number;
    minSize?: number;
    maxSize?: number;
    children: any;
  };
}

/**
 * Split View Component
 * 
 * Two-panel split view with:
 * - Horizontal or vertical layout
 * - Resizable panels
 * - Independent size constraints
 * - Resize callbacks
 */
export const SplitView: Component<SplitViewProps> = (props) => {
  return (
    <div
      class="flex"
      style={{
        flexDirection: props.direction === "horizontal" ? "row" : "column",
        width: "100%",
        height: "100%",
      }}
    >
      <ResizablePanel
        direction={props.direction}
        defaultSize={props.firstPanel.defaultSize}
        minSize={props.firstPanel.minSize}
        maxSize={props.firstPanel.maxSize}
      >
        {props.firstPanel.children}
      </ResizablePanel>
      <ResizablePanel
        direction={props.direction}
        defaultSize={props.secondPanel.defaultSize}
        minSize={props.secondPanel.minSize}
        maxSize={props.secondPanel.maxSize}
      >
        {props.secondPanel.children}
      </ResizablePanel>
    </div>
  );
};
