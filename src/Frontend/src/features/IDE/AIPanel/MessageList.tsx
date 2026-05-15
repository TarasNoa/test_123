import { createEffect, Show, Switch, Match, For, type Component } from 'solid-js';
import { store } from '../IDEStore';
import type { ChatMessage } from '../IDEStore';
import { UserMessage } from './messages/UserMessage';
import { AITextMessage } from './messages/AITextMessage';
import { AgentSpawnedCard } from './messages/AgentSpawnedCard';
import { AgentThinkingLine } from './messages/AgentThinkingLine';
import { AgentFileEditCard } from './messages/AgentFileEditCard';
import { AgentQuestionCard } from './messages/AgentQuestionCard';
import { AgentCompletedCard } from './messages/AgentCompletedCard';
import { AgentFailedCard } from './messages/AgentFailedCard';
import { ShadowBuildLine } from './messages/ShadowBuildLine';
import { ParallelGroupCard } from './messages/ParallelGroupCard';
import { AgentConflictCard } from './messages/AgentConflictCard';
import { ArchitectPlanCard } from './messages/ArchitectPlanCard';
import { ObserverInsightCard } from './messages/ObserverInsightCard';

export const MessageList: Component = () => {
  let scrollRef: HTMLDivElement;

  createEffect(() => {
    if (scrollRef && store.messages.length > 0) {
      scrollRef.scrollTop = scrollRef.scrollHeight;
    }
  });

  return (
    <div ref={(el) => { scrollRef = el; }} class="flex-1 overflow-y-auto p-3 space-y-3">
      <For each={store.messages}>{(msg) => (
        <Switch>
          <Match when={msg.type === 'user'}>
            <UserMessage msg={msg} />
          </Match>
          <Match when={msg.type === 'ai'}>
            <AITextMessage msg={msg} />
          </Match>
          <Match when={msg.type === 'agent_spawned'}>
            <AgentSpawnedCard msg={msg} />
          </Match>
          <Match when={msg.type === 'agent_thinking'}>
            <AgentThinkingLine msg={msg} />
          </Match>
          <Match when={msg.type === 'agent_file_edit'}>
            <AgentFileEditCard msg={msg} />
          </Match>
          <Match when={msg.type === 'agent_question'}>
            <AgentQuestionCard msg={msg} />
          </Match>
          <Match when={msg.type === 'agent_completed'}>
            <AgentCompletedCard msg={msg} />
          </Match>
          <Match when={msg.type === 'agent_failed'}>
            <AgentFailedCard msg={msg} />
          </Match>
          <Match when={msg.type === 'shadow_build'}>
            <ShadowBuildLine msg={msg} />
          </Match>
          <Match when={msg.type === 'parallel_group'}>
            <ParallelGroupCard msg={msg} />
          </Match>
          <Match when={msg.type === 'agent_conflict'}>
            <AgentConflictCard msg={msg} />
          </Match>
          <Match when={msg.type === 'architect_plan'}>
            <ArchitectPlanCard msg={msg} />
          </Match>
          <Match when={msg.type === 'observer_insight'}>
            <ObserverInsightCard msg={msg} />
          </Match>
        </Switch>
      )}</For>
      <Show when={store.messages.length === 0}>
        <div class="flex flex-col items-center justify-center h-full text-muted-foreground/50 text-xs text-center space-y-2">
          <svg class="w-8 h-8 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z" />
          </svg>
          <p>Describe a task, ask a question,</p>
          <p>or @mention a file to get started</p>
        </div>
      </Show>
    </div>
  );
};
