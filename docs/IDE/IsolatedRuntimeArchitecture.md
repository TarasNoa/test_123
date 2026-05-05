# Isolated Runtime + Shadow Workspace Pool + Bidirectional Sync

This document describes the **execution half** of the Autonomous App
Generation orchestrator: how generated code is safely executed, how several
shadow workspaces coexist in a single VM, and how edits stay in sync between
the IDE and the runtime.

## Goals

1. **True isolation** — generated application code never runs directly on the
   host. Docker / VM mediates every command.
2. **Any tech stack for generated apps** — while the Libr4 codebase itself
   stays on C# (infrastructure), F# (algorithms) and Rust (media), the apps
   that the orchestrator produces can be in Python, Node, Go, Rust, Java,
   C#, … — whatever fits the user request.
3. **Multiple workspaces per VM** — one long-living isolated runtime can host
   many workspaces (as the user asked: "несколько workspace хранятся в одной
   виртуальной машине").
4. **Bidirectional sync** — files edited in the IDE instantly reflect inside
   the runtime; files edited by agents inside the runtime instantly reflect
   in the IDE.

## Architectural layers

```
┌───────────────────────────────────────────────────────────────────────┐
│                          AppGenerationOrchestrator                    │
│       (plan ▸ generate ▸ run ▸ analyze ▸ fix ▸ iterate)               │
└──────────────────────────────┬────────────────────────────────────────┘
                               │   uses
                               ▼
┌───────────────────────────────────────────────────────────────────────┐
│                   IShadowExecutionService                             │
│                (IsolatedShadowExecutionService)                       │
└──────────┬───────────────────────┬────────────────────────┬───────────┘
           │ prepare / update      │ watch host dir         │ exec
           ▼                       ▼                        ▼
 ┌───────────────────┐  ┌───────────────────────────┐  ┌──────────────────┐
 │  IWorkspacePool   │  │ IWorkspaceSyncService     │  │  IRuntimeSession │
 │  (VmWorkspacePool)│  │ (FileSystemWorkspaceSync) │  │  (DockerSession) │
 └────────┬──────────┘  └───────────┬───────────────┘  └────────┬─────────┘
          │ backed by               │ FileSystemWatcher         │ docker exec
          ▼                         ▼                           ▼
 ┌───────────────────────────────────────────────────────────────────────┐
 │                     IIsolatedRuntime  (provider)                      │
 │   default: DockerIsolatedRuntime                                      │
 │   stubs:   WslIsolatedRuntime / HyperVIsolatedRuntime                 │
 │   fallback: ProcessIsolatedRuntime (developer machines only)          │
 └───────────────────────────────────────────────────────────────────────┘
```

## Key concepts

### `IIsolatedRuntime` / `IRuntimeSession`

A *runtime* is a factory for *sessions*. A *session* is one live isolated
environment (a container, a WSL distro, a VM) that has mounted a host
directory under a stable guest path.

```csharp
var session = await runtime.StartSessionAsync(
    image: "python:3.12-slim",
    hostMountPath: @"C:\Temp\libr4-shadow-pool\python_3_12_slim-ab12cd34");

var result = await session.ExecAsync("pytest -q", workingSubDirectory: "banking-api");
```

Commands are executed in the guest via `docker exec` (or `wsl --`, `ssh`, …).
Because the host directory is bind-mounted, any file the agent writes inside
the guest is instantly visible on the host.

### `IWorkspacePool` / `WorkspaceHandle`

A workspace is a subfolder under the runtime's mount root. The pool
guarantees that workspaces requesting the *same* runtime image share the
*same* session — so a single long-living VM can host several parallel
workspaces (matching the user's "несколько workspace в одной VM" model).

```
C:\Temp\libr4-shadow-pool\python_3_12_slim-ab12cd34\     ← session host root
  ├── 7f3a…/   workspace A (iteration 1 of userA)
  ├── 9c11…/   workspace B (iteration 3 of userB)
  └── e102…/   workspace C

inside guest container:
/workspace/7f3a…  /workspace/9c11…  /workspace/e102…
```

When the last workspace in a bucket is released, the session (and the host
root) is torn down.

### `IWorkspaceSyncService`

The bind-mount gives us file-level sync for free — but the IDE also needs to
*know* when a file changed inside the runtime (e.g. the fixer agent edited
`src/app.py`). `FileSystemWorkspaceSyncService` wraps a
`FileSystemWatcher` over each workspace's host directory and raises
`WorkspaceFileChange` events. An IDE client (or another service) subscribes
and refreshes its editors / sends the IDE a "buffer changed" signal.

Flow in both directions:

```
IDE edits       host file write  ──► guest sees it immediately (bind mount)
Agent edits     guest file write ──► host sees it immediately (bind mount)
                                 ──► FileSystemWatcher fires OnFileChanged
                                 ──► IDE client refreshes editor
```

### Tech-stack freedom

The orchestrator's project (Libr4) stays on its standard stack — **C#, F#,
Rust, no Python**. But the *generated* applications can be in whatever stack
the planner picks: `python:3.12-slim`, `node:22-alpine`,
`mcr.microsoft.com/dotnet/sdk:8.0`, `rust:1.80`, `golang:1.23-alpine`,
`eclipse-temurin:21-jdk`, …

The `GenerationPlan` now carries three extra fields that make this possible:

- `RuntimeImage` — container image / VM profile.
- `BuildCommands` — ordered list of shell commands that build the code.
- `TestCommands` — shell commands whose exit code 0 means tests passed.

`IsolatedShadowExecutionService` runs exactly these commands inside the
session. If the planner omits them, sensible defaults are used (`dotnet
restore/build/test`, `pytest`, `npm test`, `cargo test`, `go test`, …).

### Image migration between iterations

If a fix from the LLM changes the language (rare but possible), the pool
detects that the requested `RuntimeImage` differs from the current session's
image, acquires a new workspace in a session that matches, copies the files
across and releases the old workspace — all transparent to the orchestrator.

## Provider matrix

| Provider                      | Isolation      | Sync mechanism              | Status    |
| ----------------------------- | -------------- | --------------------------- | --------- |
| `DockerIsolatedRuntime`       | container      | bind-mount                  | **default** |
| `WslIsolatedRuntime`          | WSL2 distro    | `/mnt/…` bind               | stub      |
| `HyperVIsolatedRuntime`       | full VM        | virtiofs / SMB              | stub      |
| `ProcessIsolatedRuntime`      | **none**       | direct file system          | fallback  |

The orchestrator never talks to a provider directly — it only asks
`IIsolatedRuntime` / `IWorkspacePool`. Swapping the default is a one-liner
in `AutonomousAppGenerationDependencyInjection`.

## Security notes

- The default image has **no network isolation**. For untrusted LLM output,
  add `--network=none` in `DockerIsolatedRuntime` (trivial change).
- `ProcessIsolatedRuntime` is NOT a security boundary; it exists only so
  developers without Docker installed can smoke-test the feature.
- All commands are executed under `sh -c` inside the guest — the shell is
  fully the guest's, host shell is only used to spawn `docker`.

## Extending with a real VM runtime

To plug in Hyper-V:

1. Implement `IIsolatedRuntime.StartSessionAsync`:
   - create / reuse a pooled VM from a golden image;
   - mount the host directory via virtiofs or SMB at `/mnt/libr4`;
   - return a `HyperVSession` that executes commands via SSH.
2. Keep the same `GuestMountPath` semantics so `FileSystemWorkspaceSyncService`
   continues to work unchanged.
3. Swap the registration:
   ```csharp
   services.AddSingleton<IIsolatedRuntime, HyperVIsolatedRuntime>();
   ```
