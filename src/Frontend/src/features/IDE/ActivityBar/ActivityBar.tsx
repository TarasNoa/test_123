import { Show, type Component } from 'solid-js';
import { store, setStore } from '../IDEStore';

const IconFiles: Component = () => (
  <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
    <path stroke-linecap="round" stroke-linejoin="round" d="M2.25 12.75V12A2.25 2.25 0 014.5 9.75h15A2.25 2.25 0 0121.75 12v.75m-8.69-6A2.25 2.25 0 0111.256 4.5h6.994A2.25 2.25 0 0120.25 7.5v.75" />
  </svg>
);

const IconSearch: Component = () => (
  <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
    <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
  </svg>
);

const IconGit: Component = () => (
  <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
    <path stroke-linecap="round" stroke-linejoin="round" d="M7.5 21L3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5" />
  </svg>
);

const IconSettings: Component = () => (
  <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
    <path stroke-linecap="round" stroke-linejoin="round" d="M10.343 3.94c.09-.546.56-.94 1.11-.94h1.093c.55 0 1.02.394 1.11.94l.149.894c.07.424.384.764.78.93.398.164.855.142 1.205-.108l.737-.527a1.125 1.125 0 011.45.12l.773.774c.39.389.44 1.002.12 1.45l-.527.737c-.25.35-.272.806-.107 1.204.165.397.505.71.93.78l.893.15c.546.09.94.56.94 1.11v1.092c0 .55-.394 1.02-.94 1.11l-.893.149c-.425.07-.765.383-.93.78-.165.398-.143.854.107 1.204l.527.738c.32.447.27 1.06-.12 1.45l-.774.773a1.125 1.125 0 01-1.449.12l-.738-.527c-.35-.25-.806-.272-1.204-.107-.397.165-.71.505-.781.929l-.149.894c-.09.542-.56.94-1.11.94h-1.093c-.55 0-1.019-.398-1.11-.94l-.148-.894c-.071-.424-.384-.764-.781-.93-.398-.164-.854-.142-1.204.108l-.738.527c-.447.32-1.06.269-1.45-.12l-.773-.774a1.125 1.125 0 01-.12-1.45l.527-.737c.25-.35.273-.806.108-1.204-.165-.397-.505-.71-.93-.78l-.894-.15c-.542-.09-.94-.56-.94-1.11v-1.093c0-.55.398-1.019.94-1.11l.894-.149c.424-.07.765-.383.93-.78.165-.398.143-.854-.107-1.204l-.527-.738a1.125 1.125 0 01.12-1.45l.773-.773a1.125 1.125 0 011.45-.12l.737.527c.35.25.807.272 1.204.107.397-.165.71-.505.78-.929l.15-.894z" />
    <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
  </svg>
);

const ActivityButton: Component<{ active: boolean; icon: any; onClick: () => void }> = (props) => (
  <button
    onClick={props.onClick}
    class={[
      'relative w-full flex items-center justify-center py-3 rounded-lg transition-all',
      props.active ? 'text-secondary bg-secondary/10' : 'text-muted-foreground hover:text-foreground',
    ].join(' ')}
  >
    {props.icon}
    <Show when={props.active}>
      <div class="absolute left-0 top-1/2 -translate-y-1/2 w-[2px] h-6 bg-secondary rounded-r" />
    </Show>
  </button>
);

export const ActivityBar: Component = () => {
  return (
    <aside class="w-12 shrink-0 flex flex-col items-center py-2 bg-surface border-r border-surface-3">
      <div class="flex flex-col gap-1 w-full px-1.5">
        <ActivityButton
          active={store.activeActivityTab === 'files'}
          icon={<IconFiles />}
          onClick={() => setStore('activeActivityTab', 'files')}
        />
        <ActivityButton
          active={store.activeActivityTab === 'search'}
          icon={<IconSearch />}
          onClick={() => setStore('activeActivityTab', 'search')}
        />
        <ActivityButton
          active={store.activeActivityTab === 'git'}
          icon={<IconGit />}
          onClick={() => setStore('activeActivityTab', 'git')}
        />
      </div>

      <div class="mt-auto w-full px-1.5">
        <ActivityButton active={false} icon={<IconSettings />} onClick={() => {}} />
      </div>
    </aside>
  );
};
