import { For, Show, type Component } from 'solid-js';
import type { FilmstripItem } from './reviewUtils';

export const EvidenceFilmstrip: Component<{
  items: FilmstripItem[];
  activeStep?: number | null;
}> = (props) => (
  <div class="border-t border-surface-3 bg-surface-2/30 p-2">
    <div class="text-[10px] text-muted-foreground mb-1">Evidence filmstrip</div>
    <Show when={props.items.length > 0} fallback={
      <p class="text-[10px] text-muted-foreground">No screenshots or video for this file.</p>
    }>
      <div class="flex gap-2 overflow-x-auto pb-1">
        <For each={props.items}>{(item) => {
          const isVideo = item.fileName.endsWith('.webm') || item.kind.toLowerCase().includes('video');
          const active = props.activeStep != null && item.stepNumber === props.activeStep;
          return (
            <a
              href={item.url}
              target="_blank"
              rel="noreferrer"
              class={[
                'shrink-0 w-28 rounded border overflow-hidden hover:border-secondary/50',
                active ? 'border-secondary ring-1 ring-secondary/40' : 'border-surface-3',
              ].join(' ')}
              title={`${item.source} · ${item.fileName}${item.stepNumber != null ? ` · step ${item.stepNumber}` : ''}`}
            >
              <Show when={isVideo} fallback={
                <img
                  src={item.thumbnailUrl ?? item.url}
                  alt={item.fileName}
                  class="w-full h-16 object-cover bg-black"
                />
              }>
                <div class="w-full h-16 bg-black flex items-center justify-center text-[10px] text-muted-foreground">
                  ▶ video
                </div>
              </Show>
              <div class="px-1 py-0.5 text-[9px] truncate text-muted-foreground">
                {item.stepNumber != null ? `step ${item.stepNumber}` : item.source}
              </div>
            </a>
          );
        }}</For>
      </div>
    </Show>
  </div>
);

export default EvidenceFilmstrip;
