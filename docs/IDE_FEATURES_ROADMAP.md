# IDE Features Roadmap

Frontend IDE features for Libr4 Autonomous App Generation.

## Overview
This document tracks IDE features requested for the chat with AI agent and terminal integration in the autonomous app generation workflow.

---

## Feature 1: Auto-Translation in Chat
**Status:** ✅ **COMPLETED**

### Description
Auto-translation of chat messages based on user's account language or browser language.

### Implementation
- **File:** `frontend/src/hooks/useTranslation.ts`
  - Hook for determining target language: `account languages → browser language → 'en'`
  - Translation cache (Map) to avoid duplicate API calls
  - `translateContent()` - single string translation
  - `translateMessages()` - batch translation
  - `isTranslating` - loading indicator

- **File:** `frontend/src/lib/translation-api.ts` (already existed)
  - `translateBatch()` - API call for batch translation
  - `detectPreferredLanguage()` - language detection logic
  - `normalizeLanguageCode()` - code normalization
  - `getLanguageLabel()` - localized language names

- **File:** `frontend/src/lib/auth.tsx`
  - Updated `AuthUser` interface to include `languages?: Array<{ code: string; proficiency: string }>`

- **File:** `frontend/src/components/ide/agent-chat.tsx`
  - Integrated `useTranslation` hook
  - Language indicator badge in header (Globe icon + language label)
  - Translation spinner when in progress
  - Auto-translation on language change
  - Placeholder localization (Russian/English)
  - All system messages auto-translated

### Backend Requirements
- `/ai/translate/batch` endpoint for translation
- User profile with preferred languages

---

## Feature 2: Code Blocks in Chat
**Status:** ✅ **COMPLETED**

### Description
Display code snippets in chat with syntax highlighting, copy button, and language indicator.

### Implementation
- **File:** `frontend/src/components/ui/code-block.tsx`
  - `CodeBlock` component with:
    - Header with filename/language
    - Copy-to-clipboard button with check icon feedback
    - Line numbers (optional)
    - ANSI-to-HTML formatting (simplified)
  - `InlineCode` component for inline code
  - `parseCodeBlocks()` - parses markdown-style code blocks (```language code```)
  - `MessageContent` - renders message with code blocks

- **File:** `frontend/src/components/ide/agent-chat.tsx`
  - Added `codeBlocks` field to `ChatMessage`
  - Code blocks indicator (Terminal icon + file count)
  - Integrated `MessageContent` for rendering

### Backend Requirements
- None (client-side parsing)

---

## Feature 3: Agent "Thinking" Display
**Status:** ✅ **COMPLETED**

### Description
Show AI agent's reasoning process (thoughts) like Cursor/Windsurf - make the agent communicate with the user instead of just generating randomly.

### Implementation
- **File:** `frontend/src/components/ide/agent-chat.tsx`
  - Added `thinking` field to `ChatMessage`
  - Added `'thinking'` to message types
  - Amber-styled thinking block with:
    - Lightbulb icon
    - "Думаю..." / "Thinking..." header
    - Italic text for thoughts
    - Border separator from main content
  - Status indicators for different message types

### Backend Requirements
- Agent LLM to output reasoning process
- Message type field set to `'thinking'`

---

## Feature 4: Terminal Panel with Tabs
**Status:** ✅ **COMPLETED**

### Description
Terminal panel under IDE where users can execute commands inside shadow workspace. Support multiple tabs for parallel processes (e.g., backend in tab 1, frontend in tab 2).

### Implementation
- **File:** `frontend/src/lib/terminal-api.ts`
  - `TerminalSession` interface (id, shell, cwd, env vars, status, history)
  - `CommandEntry` interface (command, output, exit code, duration)
  - `terminalApi` HTTP client:
    - `createSession()` - create new terminal session
    - `listSessions()` - list all sessions
    - `getSession()` - get session details
    - `executeCommand()` - execute command
    - `getHistory()` - get command history
    - `terminateSession()` - close session
    - `resize()` - resize terminal
  - `TerminalWebSocket` class for real-time output
  - `ansiToHtml()` - ANSI color formatting
  - `formatCommandOutput()` - format output with timing

- **File:** `frontend/src/components/ide/Terminal.tsx`
  - `TerminalPanel` component:
    - Multiple tabs support
    - Tab creation (Plus button)
    - Tab close (X button)
    - Session management with WebSocket
    - Command input with shell prompt
    - Output display with ANSI formatting
    - Clear, Copy, Maximize/Minimize, Hide buttons
    - Running indicator (amber pulse)
  - `TerminalOutputCard` - for displaying terminal output in chat
  - Fallback to mock execution if no backend

- **File:** `frontend/src/app/(ide)/ide/[projectId]/page.tsx`
  - Replaced `OutputPanel` with `TerminalPanel`
  - Integrated with `workspaceId`
  - `onCommandOutput` callback for logging

### Backend Requirements
- `/api/ide/terminal/sessions` - session management
- `/api/ide/terminal/execute` - command execution
- `/api/ide/terminal/sessions/{id}/history` - command history
- `/api/ide/terminal/sessions/{id}/terminate` - terminate session
- `/api/ide/terminal/sessions/{id}/resize` - resize terminal
- `/ws/terminal/{id}` - WebSocket for real-time output
- Shadow workspace command execution

---

## Feature 5: Build/Test/Security Events in Chat & Terminal
**Status:** ✅ **COMPLETED**

### Description
When agent tries to build, test, or security scan the application, show it in chat AND in terminal at the bottom.

### Implementation
- **File:** `frontend/src/components/ide/agent-chat.tsx`
  - Added message types:
    - `'build-start'` - build started
    - `'build-complete'` - build finished
    - `'test-start'` - testing started
    - `'test-complete'` - testing finished
    - `'security-scan'` - security scanning
    - `'terminal-output'` - terminal command output
  - Added `terminalOutput` field to `ChatMessage`:
    - `command` - executed command
    - `output` - command output
    - `exitCode` - exit status
    - `durationMs` - execution time
  - Color-coded event indicators:
    - Blue (build/test start) with Hammer/Terminal icon
    - Green (complete) with CheckCircle2 icon
    - Purple (security) with Shield icon
    - All with animate-pulse when in progress
  - `TerminalOutputCard` integration in messages

- **File:** `frontend/src/components/ide/Terminal.tsx`
  - `TerminalOutputCard` component:
    - Command header
    - Exit code badge (OK/destructive)
    - Duration display
    - Expandable output
    - Copy button

### Backend Requirements
- Agent to emit build/test/security events
- Command execution hooks
- Event streaming to frontend

---

## Feature 6: Agent Orchestration Display
**Status:** ✅ **COMPLETED**

### Description
Show which agent the AI called, which sub-agents that agent uses, and for what purpose. Display agent hierarchy in chat.

### Implementation
- **File:** `frontend/src/components/ide/agent-chat.tsx`
  - Added `AgentInfo` interface:
    - `id`, `name`, `role`, `description`
    - `status` (idle/working/completed/failed)
    - `subAgents` - nested agent hierarchy
    - `purpose` - why agent was called
    - `input` - what was passed to agent
    - `output` - what agent returned
  - Added `agentOrchestration` field to `ChatMessage`:
    - `rootAgent` - top-level agent
    - `triggeredBy` - who triggered (LLM/user/system)
    - `timestamp`
  - Added `'agent-call'` message type
  - `AgentOrchestrationCard` component:
    - Indigo-styled orchestration card
    - Workflow icon in header
    - Triggered by badge
    - Expandable agent tree with chevrons
    - Status icons (pulse for working, check for complete, X for failed)
    - Cpu icon for each agent
    - Role badge
    - Purpose display
    - Input display with ArrowRight icon
    - Sub-agents section with Users icon
    - Nested depth with border lines

### Backend Requirements
- Agent orchestration tracking
- Agent hierarchy data
- Event emission for agent calls

---

## Summary

| Feature | Status | Files Changed |
|---------|--------|---------------|
| Auto-Translation | ✅ Complete | `useTranslation.ts`, `auth.tsx`, `agent-chat.tsx` |
| Code Blocks | ✅ Complete | `code-block.tsx`, `agent-chat.tsx` |
| Agent Thinking | ✅ Complete | `agent-chat.tsx` |
| Terminal Panel | ✅ Complete | `terminal-api.ts`, `Terminal.tsx`, `ide/[projectId]/page.tsx` |
| Build/Test Events | ✅ Complete | `agent-chat.tsx`, `Terminal.tsx` |
| Agent Orchestration | ✅ Complete | `agent-chat.tsx` |

---

## Pending Backend Work

### Required Endpoints
1. **Translation API**
   - `POST /ai/translate/batch` - batch translation
   - User profile with `languages` field

2. **Terminal API**
   - `POST /api/ide/terminal/sessions` - create session
   - `GET /api/ide/terminal/sessions` - list sessions
   - `GET /api/ide/terminal/sessions/{id}` - get session
   - `POST /api/ide/terminal/execute` - execute command
   - `GET /api/ide/terminal/sessions/{id}/history` - get history
   - `POST /api/ide/terminal/sessions/{id}/terminate` - terminate
   - `POST /api/ide/terminal/sessions/{id}/resize` - resize
   - `WS /ws/terminal/{id}` - WebSocket for output

3. **Agent Events**
   - Agent LLM to output reasoning (`thinking` field)
   - Build/test/security event emission
   - Agent orchestration tracking and hierarchy data

### Shadow Workspace Integration
- Command execution in isolated environment
- File system access
- Process management for parallel terminals

---

## Next Steps

1. Implement backend endpoints for terminal
2. Add agent reasoning output to LLM prompts
3. Integrate build/test/security event hooks
4. Add agent orchestration tracking to autonomous generation
5. Connect WebSocket for real-time terminal output
