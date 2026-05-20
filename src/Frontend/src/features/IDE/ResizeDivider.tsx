import { createSignal, onMount, onCleanup, type Component } from 'solid-js';

interface ResizeDividerProps {
  direction: 'vertical' | 'horizontal';
  onResize: (delta: number) => void;
  minSize?: number;
  maxSize?: number;
  class?: string;
}

export const ResizeDivider: Component<ResizeDividerProps> = (props) => {
  const [isDragging, setIsDragging] = createSignal(false);
  let startPos = 0;

  const isVertical = () => props.direction === 'vertical';

  const onMouseDown = (e: MouseEvent) => {
    setIsDragging(true);
    startPos = isVertical() ? e.clientX : e.clientY;
    document.body.style.cursor = isVertical() ? 'col-resize' : 'row-resize';
    document.body.style.userSelect = 'none';
  };

  const onMouseMove = (e: MouseEvent) => {
    if (!isDragging()) return;
    const currentPos = isVertical() ? e.clientX : e.clientY;
    const delta = currentPos - startPos;
    startPos = currentPos;
    props.onResize(delta);
  };

  const onMouseUp = () => {
    if (!isDragging()) return;
    setIsDragging(false);
    document.body.style.cursor = '';
    document.body.style.userSelect = '';
  };

  onMount(() => {
    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);
  });

  onCleanup(() => {
    window.removeEventListener('mousemove', onMouseMove);
    window.removeEventListener('mouseup', onMouseUp);
  });

  return (
    <div
      class={[
        'shrink-0 hover:bg-secondary/30 transition-colors z-50',
        isVertical() ? 'w-[3px] cursor-col-resize' : 'h-[3px] cursor-row-resize',
        isDragging() ? 'bg-secondary/40' : 'bg-transparent',
        props.class || '',
      ].join(' ')}
      onMouseDown={onMouseDown}
    />
  );
};
