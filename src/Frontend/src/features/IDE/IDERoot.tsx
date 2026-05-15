import { onMount, onCleanup } from 'solid-js';
import * as SignalR from '@microsoft/signalr';
import { store, setStore, addMessage, updateAgentProgress, updateFileInEditor, markFileAgentEditing, addTimelineEvent, updateTimelineEvent, addOutputLog } from './IDEStore';
import { config } from '../../lib/config';

export function useIDERoot() {
  let conn: SignalR.HubConnection;

  onMount(() => {
    const token = localStorage.getItem('accessToken') || '';

    conn = new SignalR.HubConnectionBuilder()
      .withUrl(`${config.wsBaseUrl}/hubs/agents`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect([0, 1000, 5000, 10000])
      .configureLogging(SignalR.LogLevel.Warning)
      .build();

    conn.on('AgentSpawned', (e: any) => {
      addMessage({ type: 'agent_spawned', id: crypto.randomUUID(), timestamp: new Date(), ...e });
      if (e.agentId) {
        setStore('activeAgents', e.agentId, {
          id: e.agentId,
          type: e.agentType,
          status: 'running',
          task: e.task,
          progress: 0,
          startedAt: new Date(),
          parentAgentId: e.parentAgentId,
        });
        addTimelineEvent({ agentId: e.agentId, agentType: e.agentType, task: e.task, start: new Date(), status: 'running' });
      }
    });

    conn.on('AgentThinking', (e: any) => {
      addMessage({ type: 'agent_thinking', id: crypto.randomUUID(), timestamp: new Date(), ...e });
    });

    conn.on('AgentFileEdit', (e: any) => {
      addMessage({ type: 'agent_file_edit', id: crypto.randomUUID(), timestamp: new Date(), status: 'pending', ...e });
      if (e.path && e.agentId) markFileAgentEditing(e.path, e.agentId);
    });

    conn.on('AgentQuestion', (e: any) => {
      addMessage({ type: 'agent_question', id: crypto.randomUUID(), timestamp: new Date(), ...e });
    });

    conn.on('AgentCompleted', (e: any) => {
      addMessage({ type: 'agent_completed', id: crypto.randomUUID(), timestamp: new Date(), ...e });
      if (e.agentId) {
        setStore('activeAgents', e.agentId, 'status', 'completed');
        setStore('activeAgents', e.agentId, 'progress', 100);
        updateTimelineEvent(e.agentId, { status: 'completed', end: new Date() });
      }
    });

    conn.on('AgentFailed', (e: any) => {
      addMessage({ type: 'agent_failed', id: crypto.randomUUID(), timestamp: new Date(), ...e });
      if (e.agentId) {
        setStore('activeAgents', e.agentId, 'status', 'failed');
        updateTimelineEvent(e.agentId, { status: 'failed', end: new Date() });
      }
    });

    conn.on('AgentConflict', (e: any) => {
      addMessage({ type: 'agent_conflict', id: crypto.randomUUID(), timestamp: new Date(), ...e });
    });

    conn.on('ParallelGroup', (e: any) => {
      addMessage({ type: 'parallel_group', id: crypto.randomUUID(), timestamp: new Date(), ...e });
    });

    conn.on('ArchitectPlan', (e: any) => {
      addMessage({ type: 'architect_plan', id: crypto.randomUUID(), timestamp: new Date(), ...e });
    });

    conn.on('ObserverInsight', (e: any) => {
      addMessage({ type: 'observer_insight', id: crypto.randomUUID(), timestamp: new Date(), ...e });
    });

    conn.on('ShadowBuildStarted', (e: any) => {
      addMessage({ type: 'shadow_build', id: crypto.randomUUID(), timestamp: new Date(), buildStatus: 'running', ...e });
      addOutputLog('build', 'Shadow build started...');
    });

    conn.on('ShadowBuildSuccess', (e: any) => {
      addMessage({ type: 'shadow_build', id: crypto.randomUUID(), timestamp: new Date(), buildStatus: 'success', ...e });
      addOutputLog('build', `Shadow build passed · ${e.duration || 0}ms`);
    });

    conn.on('ShadowBuildFailed', (e: any) => {
      addMessage({ type: 'shadow_build', id: crypto.randomUUID(), timestamp: new Date(), buildStatus: 'failed', ...e });
      addOutputLog('error', `Shadow build failed · ${e.errors?.length || 0} errors`);
    });

    conn.on('FileModified', (path: string, content: string) => {
      updateFileInEditor(path, content);
    });

    conn.on('AgentProgressUpdate', ({ agentId, progress, currentFile }: any) => {
      updateAgentProgress(agentId, progress, currentFile);
    });

    conn.on('AITextChunk', (chunk: string) => {
      if (!store.isAIStreaming) {
        setStore('isAIStreaming', true);
        const id = crypto.randomUUID();
        setStore('streamingMessageId', id);
        addMessage({ type: 'ai', id, text: chunk, timestamp: new Date(), isStreaming: true });
      } else {
        setStore('messages', (m) => {
          const last = m[m.length - 1];
          if (last && last.type === 'ai' && last.isStreaming) {
            return [...m.slice(0, -1), { ...last, text: last.text + chunk }];
          }
          return m;
        });
      }
    });

    conn.on('AITextComplete', () => {
      setStore('isAIStreaming', false);
      setStore('streamingMessageId', null);
      setStore('messages', (m) => {
        const last = m[m.length - 1];
        if (last && last.type === 'ai') {
          return [...m.slice(0, -1), { ...last, isStreaming: false }];
        }
        return m;
      });
    });

    conn.start()
      .then(() => {
        setStore('isConnected', true);
        return conn.invoke('JoinSession', store.sessionId || 'default');
      })
      .catch(() => setStore('isConnected', false));

    onCleanup(() => {
      conn?.stop();
    });
  });

  onMount(async () => {
    const sidebarWidth = localStorage.getItem('libr4_sidebar_width');
    if (sidebarWidth) setStore('sidebarWidth', parseInt(sidebarWidth, 10));
    const bottomHeight = localStorage.getItem('libr4_bottom_height');
    if (bottomHeight) setStore('bottomPanelHeight', parseInt(bottomHeight, 10));
    const aiWidth = localStorage.getItem('libr4_ai_width');
    if (aiWidth) setStore('aiPanelWidth', parseInt(aiWidth, 10));
    const savedAutonomy = localStorage.getItem('libr4_autonomy') as 'supervised' | 'semi-auto' | 'full-auto' | null;
    if (savedAutonomy) setStore('autonomyLevel', savedAutonomy);

    const saved = localStorage.getItem('libr4_ide_session');
    if (saved) {
      setStore('sessionId', saved);
    } else {
      const newId = crypto.randomUUID();
      setStore('sessionId', newId);
      localStorage.setItem('libr4_ide_session', newId);
    }

    setStore('fileTree', [
      { id: 'src', path: 'src', name: 'src', type: 'folder', isOpen: true, children: [
        { id: 'src/app', path: 'src/app', name: 'app', type: 'folder', isOpen: false, children: [
          { id: 'src/app/app.tsx', path: 'src/app/app.tsx', name: 'app.tsx', type: 'file', language: 'typescript' },
          { id: 'src/app/app.css', path: 'src/app/app.css', name: 'app.css', type: 'file', language: 'css' },
        ]},
        { id: 'src/lib', path: 'src/lib', name: 'lib', type: 'folder', isOpen: false, children: [
          { id: 'src/lib/api-client.ts', path: 'src/lib/api-client.ts', name: 'api-client.ts', type: 'file', language: 'typescript' },
          { id: 'src/lib/config.ts', path: 'src/lib/config.ts', name: 'config.ts', type: 'file', language: 'typescript' },
        ]},
        { id: 'src/features', path: 'src/features', name: 'features', type: 'folder', isOpen: true, children: [
          { id: 'src/features/IDE', path: 'src/features/IDE', name: 'IDE', type: 'folder', isOpen: false, children: [] },
        ]},
      ]},
      { id: 'package.json', path: 'package.json', name: 'package.json', type: 'file', language: 'json' },
      { id: 'tsconfig.json', path: 'tsconfig.json', name: 'tsconfig.json', type: 'file', language: 'json' },
    ]);
  });
}
