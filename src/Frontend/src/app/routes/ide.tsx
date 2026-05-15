import { Show, type Component } from "solid-js";
import { useNavigate } from "@solidjs/router";
import { store, setStore } from "../../features/IDE/IDEStore";
import { useIDERoot } from "../../features/IDE/IDERoot";
import { ActivityBar } from "../../features/IDE/ActivityBar/ActivityBar";
import { Sidebar } from "../../features/IDE/Sidebar/Sidebar";
import { EditorArea } from "../../features/IDE/Editor/EditorArea";
import { BottomPanel } from "../../features/IDE/BottomPanel/BottomPanel";
import { AIPanel } from "../../features/IDE/AIPanel/AIPanel";
import { StatusBar } from "../../features/IDE/StatusBar";
import { ResizeDivider } from "../../features/IDE/ResizeDivider";

export default function IDEPage() {
  const navigate = useNavigate();
  useIDERoot();

  const token = localStorage.getItem("accessToken");
  if (!token) {
    navigate("/auth");
    return null;
  }

  return (
    <div class="h-screen w-screen flex flex-col bg-background text-foreground overflow-hidden text-sm">
      {/* Main row */}
      <div class="flex-1 flex min-h-0">
        <ActivityBar />
        <Sidebar />
        <Show when={store.sidebarOpen}>
          <ResizeDivider
            direction="vertical"
            onResize={(delta) => {
              const next = Math.max(160, Math.min(400, store.sidebarWidth + delta));
              setStore("sidebarWidth", next);
              localStorage.setItem("libr4_sidebar_width", String(next));
            }}
          />
        </Show>

        {/* Center: Editor + BottomPanel */}
        <div class="flex-1 flex flex-col min-w-0">
          <EditorArea />

          <Show when={store.bottomPanelOpen}>
            <ResizeDivider
              direction="horizontal"
              onResize={(delta) => {
                const next = Math.max(100, Math.min(500, store.bottomPanelHeight - delta));
                setStore("bottomPanelHeight", next);
                localStorage.setItem("libr4_bottom_height", String(next));
              }}
            />
            <BottomPanel />
          </Show>
        </div>

        <Show when={store.aiPanelOpen}>
          <ResizeDivider
            direction="vertical"
            onResize={(delta) => {
              const next = Math.max(280, Math.min(500, store.aiPanelWidth - delta));
              setStore("aiPanelWidth", next);
              localStorage.setItem("libr4_ai_width", String(next));
            }}
          />
        </Show>
        <AIPanel />
      </div>

      <StatusBar />
    </div>
  );
}
