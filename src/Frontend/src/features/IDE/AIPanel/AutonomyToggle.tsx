import { type Component } from 'solid-js';
import { store, setStore } from '../IDEStore';

type Level = 'supervised' | 'semi-auto' | 'full-auto';

const levels: { key: Level; label: string }[] = [
  { key: 'supervised', label: 'Supervised' },
  { key: 'semi-auto', label: 'Semi-auto' },
  { key: 'full-auto', label: 'Full auto' },
];

export const AutonomyToggle: Component = () => {
  return (
    <div class="shrink-0 flex items-center gap-1 px-3 py-1.5 border-b border-surface-3">
      {levels.map((level) => (
        <button
          onClick={() => {
            setStore('autonomyLevel', level.key);
            localStorage.setItem('libr4_autonomy', level.key);
          }}
          class={[
            'flex-1 text-[10px] font-medium py-1 rounded transition-all',
            store.autonomyLevel === level.key
              ? 'bg-primary/10 text-primary'
              : 'text-muted-foreground hover:text-foreground hover:bg-surface-2/50',
          ].join(' ')}
        >
          {level.label}
        </button>
      ))}
    </div>
  );
};
