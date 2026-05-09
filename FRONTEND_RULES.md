# Frontend Architecture Rules

**CRITICAL**: These rules MUST be followed by all AI agents and developers. Read before any code changes.

---

## 📁 Folder Structure

```
src/
├── entities/          # Business entities (Project, Agent, Task, User)
├── features/          # Feature modules (ai-generation, collaboration, marketplace)
├── widgets/           # Reusable UI widgets (ProjectCard, AgentPanel, ExecutionGraph)
├── pages/             # Page components (Dashboard, IDE, Marketplace, ProjectWorkspace)
├── shared/            # Shared utilities and infrastructure
│   ├── activity/      # Event system (WorkspaceEvent types)
│   ├── interaction/   # Interaction layer (CommandPalette, Hotkeys)
│   ├── layout/        # Layout components (ResizablePanel, WorkspaceLayout)
│   ├── notification/  # Notifications (Toast)
│   ├── streaming/     # Streaming UI (StreamingText, ThinkingIndicator)
│   ├── store/         # Global state (workspaceStore, projectStore, agentStore)
│   ├── ui/            # Shared UI components (Button, Card, Input, Tabs)
│   └── layouts/       # Layout wrappers (Sidebar, Topbar, AIPanel)
└── Frontend/          # SolidJS app entry point
```

**Rules**:
- NEVER create files outside this structure
- Features are self-contained modules (entities + widgets + logic)
- AI orchestration lives in `services/` (NOT in components)
- Components are dumb renderers only

---

## 🎯 Naming Conventions

### Files
- **Components**: PascalCase (`ProjectCard.tsx`, `AgentPanel.tsx`)
- **Utilities**: camelCase (`workspaceStore.ts`, `eventStream.ts`)
- **Types**: PascalCase (`WorkspaceEvent.ts`, `AgentConfig.ts`)
- **Constants**: SCREAMING_SNAKE_CASE (`DESIGN_TOKENS.ts`)

### Components
- **Export**: Named export only (`export const ComponentName`)
- **Props Interface**: `ComponentNameProps`
- **File name matches component name**

### Functions/Variables
- **Functions**: camelCase (`handleClick`, `formatDate`)
- **Constants**: SCREAMING_SNAKE_CASE (`MAX_RETRY_COUNT`)
- **Types**: PascalCase (`WorkspaceEvent`, `AgentStatus`)

---

## 📦 Import Rules

**STRICT ORDER**:
1. React/SolidJS imports
2. Third-party libraries
3. Internal shared imports (`../shared/...`)
4. Feature/entity imports
5. Relative imports
6. Types only imports

**Example**:
```typescript
import { Component } from "solid-js";
import { createSignal } from "solid-js/store";
import { colors } from "../shared/ui/tokens";
import { workspaceStore } from "../shared/store/workspaceStore";
import { ProjectCard } from "../widgets/ProjectCard";
import type { WorkspaceEvent } from "../shared/activity/WorkspaceEvent";
```

**Rules**:
- NEVER use absolute imports without alias configuration
- Group imports by type with blank lines
- Sort imports alphabetically within groups
- Use `type` keyword for type-only imports

---

## 🏗️ Component Architecture

### Component Structure
```typescript
// 1. Imports
import { Component } from "solid-js";
import { colors } from "../shared/ui/tokens";

// 2. Props Interface
interface ComponentNameProps {
  prop1: string;
  prop2?: number;
  onAction?: () => void;
}

// 3. Component Definition
export const ComponentName: Component<ComponentNameProps> = (props) => {
  // 4. Local state (minimal)
  const [localState, setLocalState] = createSignal(false);

  // 5. Effects
  onMount(() => {
    // Mount logic
  });

  // 6. Helper functions
  const handleClick = () => {
    // Handler logic
  };

  // 7. Render
  return (
    <div style={{ color: colors.text }}>
      {/* JSX */}
    </div>
  );
};
```

**Rules**:
- Components are dumb renderers (NO business logic)
- Business logic in `services/` or `features/`
- Use global stores instead of local state when possible
- Keep components under 300 lines

---

## 🎨 Styling Rules

### Design Tokens ONLY
```typescript
// ✅ CORRECT
<div style={{ color: colors.text, padding: spacing.md }} />

// ❌ WRONG
<div style={{ color: "#FFFFFF", padding: "16px" }} />
```

**Rules**:
- NEVER use inline hex values
- ALWAYS use design tokens from `../shared/ui/tokens.ts`
- Use Tailwind classes for layout and spacing
- Use inline styles ONLY for design tokens

### Tailwind Usage
```typescript
// ✅ CORRECT
<div class="flex items-center gap-4 p-4" style={{ color: colors.text }} />

// ❌ WRONG
<div class="text-white bg-blue-500 p-4" />
```

**Rules**:
- Tailwind for layout (flex, grid, gap, p, m)
- Design tokens for colors, borders, shadows
- NEVER use Tailwind for colors (except utility classes like `bg-transparent`)

---

## 📏 Spacing Rules

Use design tokens from `spacing`:
- `spacing.xs` (4px)
- `spacing.sm` (8px)
- `spacing.md` (16px)
- `spacing.lg` (24px)
- `spacing.xl` (32px)
- `spacing.2xl` (48px)
- `spacing.3xl` (64px)

**Rules**:
- NEVER use arbitrary pixel values
- Use spacing tokens consistently
- Follow 8px grid system

---

## 🎭 Event System Rules

### Event Types
Separate event domains:
```typescript
// System Events
SystemEvent = SystemStarted | SystemError | SystemShutdown

// UI Events
UIEvent = ButtonClicked | ModalOpened | TabSwitched

// AI Events
AIEvent = AgentStarted | AgentThinking | AgentCompleted | GenerationProgress

// Workspace Events
WorkspaceEvent = FileModified | TaskAssigned | BuildFailed | DeploymentStarted

// Collaboration Events
CollaborationEvent = UserJoined | CursorMoved | SelectionChanged
```

**Rules**:
- NEVER mix event domains
- Use specific event types (not generic "Event")
- Subscribe to specific event types (not wildcard "*")
- Event history limited to 1000 items max

### Event Streaming
```typescript
// ✅ CORRECT
globalEventStream.emit({
  type: "AgentStarted",
  agentId: agent.id,
  agentName: agent.name,
  taskId: task.id,
  timestamp: new Date(),
});

// ❌ WRONG
globalEventStream.emit({
  type: "Event",
  data: { agent, task },
});
```

**Rules**:
- Events are immutable
- Include timestamp on all events
- Use typed event interfaces
- EventStream is NOT a God Object

---

## 🎬 Animation Rules

### "Silent Intelligence" Principle
```typescript
// ✅ CORRECT - Subtle
style={{ transition: "all 0.15s ease" }}

// ❌ WRONG - Over-animated
style={{ animation: "bounce 1s infinite" }}
```

**Rules**:
- Transitions: 0.15s - 0.2s (fast)
- NO infinite animations (except thinking indicator)
- NO bounce/slide effects
- Subtle fade-in/out only
- Cursor/Linear-like motion (calm, not noisy)

### Animation Types
- **Enter**: Fade-in (0.15s)
- **Exit**: Fade-out (0.15s)
- **Hover**: Background color change (0.15s)
- **Streaming**: Character-by-character (20-30ms per char)
- **Thinking**: Pulsing dots (1.5s ease-in-out)

---

## 🤖 AI Integration Rules

### Component vs Service Separation
```typescript
// ✅ CORRECT - Component is dumb renderer
export const AgentPanel: Component<AgentPanelProps> = (props) => {
  const agent = agentState().activeAgents.find(a => a.id === props.agentId);
  return <div>{agent?.status}</div>;
};

// ❌ WRONG - Component has AI logic
export const AgentPanel: Component<AgentPanelProps> = (props) => {
  const [agent, setAgent] = createSignal(null);
  const startAgent = async () => {
    const result = await fetch('/api/agents/start');
    // AI logic in component - WRONG
  };
};
```

**Rules**:
- Components are renderers ONLY
- AI logic in `services/ai/`
- Components consume from stores
- NO API calls in components

### AI Panel Structure
```
AI Panel Sections:
- Active reasoning (current AI thought)
- Recommendations (context-aware suggestions)
- Risks (detected issues)
- Opportunities (improvements)
- Suggested actions (quick actions)
- Running agents (active agents status)
- Context memory (workspace/project context)
```

**Rules**:
- AI panel is NOT a chat
- Intelligence layer, not conversation layer
- Context-aware, not query-based
- Silent intelligence, not noisy chat

---

## 📊 Store Rules

### Global Stores
```typescript
// ✅ CORRECT - Separate stores by domain
workspaceStore, projectStore, agentStore, editorStore, marketplaceStore, activityStore

// ❌ WRONG - Monolithic store
appStore
```

**Rules**:
- One store per domain
- Stores are reactive (SolidJS signals)
- Actions defined separately from state
- NO nested state updates (use immutable patterns)

### Store Actions
```typescript
// ✅ CORRECT
export const agentActions = {
  addAgent: (agent: Agent) => { /* ... */ },
  updateAgentStatus: (id: string, status: AgentStatus) => { /* ... */ },
};

// ❌ WRONG
setAgentState(prev => ({
  ...prev,
  activeAgents: [...prev.activeAgents, agent]
}));
```

**Rules**:
- Use action functions, NOT direct setters
- Actions emit events to EventStream
- Actions are pure (no side effects in actions)
- Side effects in effects or services

---

## 🔒 Architecture Freeze Rules

**CRITICAL**: Architecture is frozen. Do NOT change structure without explicit approval.

### Frozen Conventions
- Folder structure (entities/features/widgets/pages/shared)
- Naming conventions (PascalCase components, camelCase functions)
- Import order (React → libs → shared → features → relative)
- Styling approach (design tokens + Tailwind)
- Event system (separate domains, typed events)
- Store pattern (domain-specific stores + actions)
- AI integration (services/ for logic, components for rendering)

### When to Request Change
- New domain requires new folder
- Pattern change affects >5 files
- Breaking change to existing convention
- Performance issue requires architecture change

---

## 🎯 UX Density Rules

### Density Principles
- **Compact**: Information-dense, not airy
- **Fast**: Instant interactions, no loading states
- **Keyboard-driven**: Primary input is keyboard
- **Professional**: Tool-like, not app-like

### Spacing
- Default spacing: `spacing.sm` (8px) or `spacing.md` (16px)
- Card padding: `spacing.lg` (24px)
- Section gaps: `spacing.xl` (32px)
- NO giant cards or excessive whitespace

### Typography
- Base font: 14px (text-sm) or 16px (text-base)
- Headings: 18px-24px max
- NO giant hero text
- Compact, readable, professional

### Interactions
- Hover: 0.15s transition
- Click: Instant feedback
- Keyboard: Primary navigation
- NO slow animations or delays

---

## 🚫 Anti-Patterns

### NEVER Do These
1. **Inline hex colors** - Use design tokens
2. **Local state chaos** - Use global stores
3. **Components with AI logic** - Use services/
4. **Monolithic stores** - Domain-specific stores
5. **Over-animated UI** - Silent intelligence
6. **AI panel as chat** - Intelligence layer
7. **Arbitrary spacing** - Design tokens
8. **God Object EventStream** - Separate event domains
9. **Breaking conventions** - Architecture is frozen
10. **Mobile-like density** - Professional tool density

---

## ✅ Checklist Before Commit

- [ ] File follows folder structure
- [ ] Naming conventions followed
- [ ] Imports in correct order
- [ ] Design tokens used (no hex values)
- [ ] Component under 300 lines
- [ ] No business logic in components
- [ ] Events typed and domain-separated
- [ ] Stores use actions (not direct setters)
- [ ] Animations subtle (0.15s transitions)
- [ ] UX density professional (compact, fast)
- [ ] TypeScript errors resolved
- [ ] No console.log or debug code

---

**Last Updated**: May 9, 2026
**Version**: 1.0
**Status**: FROZEN - Do NOT change without explicit approval
