# Canonical User Flows

These are the core user flows that the entire UI should be optimized around.

## Flow 1: Create Project

**Steps:**
1. User creates project
2. AI analyzes scope and requirements
3. Execution graph is generated automatically
4. Tasks are distributed to agents
5. IDE opens with project context

**UI Focus:**
- Project creation form (minimal friction)
- AI analysis progress (transparent but calm)
- Execution graph visualization (primary focus)
- Task assignment view (secondary)
- IDE integration (seamless transition)

**Key UX Questions:**
- What's happening? AI is analyzing project scope
- What's important? Execution graph and task breakdown
- What to do next? Review tasks and start execution

## Flow 2: Freelancer Joins

**Steps:**
1. Freelancer accepts task
2. AI matches freelancer skills to task requirements
3. Context is loaded (project files, dependencies, previous work)
4. Suggested tasks are shown based on skills
5. Workspace opens with relevant context

**UI Focus:**
- Task acceptance (one-click)
- AI skill matching (transparent)
- Context loading (fast)
- Task suggestions (relevant only)
- Workspace setup (instant)

**Key UX Questions:**
- What's happening? AI is matching skills to tasks
- What's important? Relevant tasks and project context
- What to do next? Start working on suggested task

## Flow 3: Build Fails

**Steps:**
1. Build fails in CI/CD
2. AI analyzes error logs and context
3. Fix is proposed with explanation
4. Agent is assigned to implement fix
5. Build is retried automatically

**UI Focus:**
- Build failure notification (clear, actionable)
- AI analysis (reasoning shown only for trust)
- Fix proposal (primary focus)
- Agent assignment (secondary)
- Build retry (automatic)

**Key UX Questions:**
- What's happening? Build failed, AI analyzing
- What's important? Fix proposal and reasoning
- What to do next? Approve fix or investigate manually

## Flow 4: Deployment

**Steps:**
1. Deployment is triggered
2. AI predicts deployment risks
3. Risk mitigation is suggested
4. Deployment proceeds with monitoring
5. Post-deployment validation runs

**UI Focus:**
- Deployment trigger (one-click)
- Risk prediction (ambient)
- Mitigation suggestions (if high risk)
- Deployment progress (primary)
- Validation results (on completion)

**Key UX Questions:**
- What's happening? Deployment in progress
- What's important? Progress and any risks
- What to do next? Monitor or investigate issues

## Flow 5: Agent Handoff

**Steps:**
1. Agent completes task
2. AI determines next logical step
3. Handoff to next agent with context
4. Progress updates shown
5. Execution graph updates

**UI Focus:**
- Task completion (clear)
- Handoff decision (transparent)
- Agent assignment (ambient)
- Progress updates (minimal)
- Graph update (automatic)

**Key UX Questions:**
- What's happening? Task completed, handoff in progress
- What's important? Next step and current agent
- What to do next? Monitor or intervene

## UI Optimization Principles

**For all flows:**
1. **Layer 1 (Current Task):** Always dominant - what user is actively doing
2. **Layer 2 (Relevant AI):** Only contextual intelligence that helps decision making
3. **Layer 3 (Ambient):** Minimal - progress, status, activity
4. **Layer 4 (Historical):** Hidden by default - only show on request

**Anti-patterns to avoid:**
- Showing all AI activity at once
- Endless feeds of events
- Screaming dashboards
- Glowing chaos
- Non-actionable intelligence

**Good patterns:**
- Quiet, focused, calm
- Only actionable intelligence
- Contextual AI assistance
- Minimal visual noise
- Clear next steps
